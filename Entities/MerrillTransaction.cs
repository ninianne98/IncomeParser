using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class MerrillTransaction : AccountTransaction {

		public MerrillTransaction() : base() {
			this.SetFileType();
		}

		public MerrillTransaction(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.TransactionLog;
			this.BrokerIdentity = BrokerIdentity.MerrillEdge;
		}

		public override void ParseFile() {
			int r = 0;
			this.TransactionRows = new List<TransactionRow>();

			bool firstLine = true;  // because first line is a header row
			var rh = new RowHelper();

			using (var parser = new TextFieldParser(this.FileInfo.FullName)) {
				parser.HasFieldsEnclosedInQuotes = true;
				parser.TextFieldType = FieldType.Delimited;
				parser.SetDelimiters(",");

				while (!parser.EndOfData) {
					var fields = parser.ReadFields();
					rh.LoadRow(fields);
					if (r >= 2) {
						if (fields != null) {
							if (firstLine && r == 2) {
								rh = new RowHelper(fields);
								firstLine = false;
							} else {
								if (fields.Length > 8 && r >= 5) {
									if (r <= 7) {
										this.AccountIdentity = rh.ReadEmptyCell("Account");
									}
									var description = rh.ReadCell("Description");

									var row = new TransactionRow(this.Rows[r]);
									row.SecuritySymbol = GetTicker(rh);
									row.ActionText = description ?? string.Empty;

									row.TransactionDate = rh.ReadCell("Settlement Date").StringToDate() ?? DateTime.Now;

									if (row.ActionText.ToLowerInvariant().StartsWith("dividend")) {
										row.TransactionType = TransactionType.Dividend;
									} else {
										if (row.ActionText.ToLowerInvariant().StartsWith("journal")
											|| row.ActionText.ToLowerInvariant().StartsWith("reinvestment")
											|| row.ActionText.ToLowerInvariant().StartsWith("direct deposit")
											|| row.ActionText.ToLowerInvariant().StartsWith("deposit")
											|| row.ActionText.ToLowerInvariant().StartsWith("funds received")) {
											row.TransactionType = TransactionType.Journal;
										}
										if (row.ActionText.ToLowerInvariant().Contains("interest")) {
											row.TransactionType = TransactionType.Interest;
										}
										if (row.ActionText.ToLowerInvariant().StartsWith("sale")
											|| row.ActionText.ToLowerInvariant().StartsWith("redemption")) {
											row.TransactionType = TransactionType.Sell;
										}
										if (row.ActionText.ToLowerInvariant().StartsWith("purchase")
											|| row.ActionText.ToLowerInvariant().StartsWith("subscription")) {
											row.TransactionType = TransactionType.Buy;
										}
										if (row.ActionText.ToLowerInvariant().StartsWith("long term capital gain")) {
											row.TransactionType = TransactionType.DistributionLT;
										}
										if (row.ActionText.ToLowerInvariant().StartsWith("short term capital gain")) {
											row.TransactionType = TransactionType.DistributionST;
										}
									}

									row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
									row.UnitPrice = rh.ReadCell("Price").StringToDecimal() ?? 0;
									row.Fees = 0;
									row.TransactionAmount = rh.ReadCell("Amount").StringToDecimal() ?? 0;

									this.TransactionRows.Add(row);
								}
							}
						}
					}
					r++;
				}
			}
		}
	}
}