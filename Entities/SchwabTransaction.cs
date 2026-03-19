using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class SchwabTransaction : AccountTransaction {

		public SchwabTransaction() : base() {
			this.SetFileType();
		}

		public SchwabTransaction(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.TransactionLog;
			this.BrokerIdentity = BrokerIdentity.Schwab;
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

					if (fields != null) {
						if (firstLine) {
							rh = new RowHelper(fields);
							firstLine = false;
						} else {
							if (fields.Length > 6) {
								if (r < 2) {
									var tpos = this.AccountIdentity.IndexOf("_Transactions_");
									if (tpos > 0) {
										this.AccountIdentity = this.AccountIdentity.Substring(0, tpos).Replace("_", " ");
									}
								}

								var row = new TransactionRow(this.Rows[r]);
								row.SecuritySymbol = GetTicker(rh);
								row.ActionText = rh.ReadCell("Action") ?? string.Empty;

								row.TransactionDate = rh.ReadCell("Date").StringToDate() ?? DateTime.Now;

								if (row.ActionText.ToLowerInvariant().Contains("dividend")
									|| row.ActionText.ToLowerInvariant().EndsWith(" div")
									|| row.ActionText.ToLowerInvariant().Contains("div adjustment")
									|| row.ActionText.ToLowerInvariant().Contains("qualified div")
									|| row.ActionText.ToLowerInvariant().Contains("qual div")
									|| row.ActionText.ToLowerInvariant().Contains("cash div")) {
									row.TransactionType = TransactionType.Dividend;
								} else {
									if (row.ActionText.ToLowerInvariant().Contains("journal")
										|| row.ActionText.ToLowerInvariant().Contains("deposit")
										|| row.ActionText.ToLowerInvariant().Contains("transfer")
										|| row.ActionText.ToLowerInvariant().Contains("redemption")
										|| row.ActionText.ToLowerInvariant().Contains("spin")
										|| row.ActionText.ToLowerInvariant().Contains("tax")
										|| row.ActionText.ToLowerInvariant().Contains("fee")) {
										row.TransactionType = TransactionType.Journal;
									}
									if (row.ActionText.ToLowerInvariant().Contains("interest")) {
										row.TransactionType = TransactionType.Interest;
									}
									if (row.ActionText.ToLowerInvariant().Contains("cash in lieu")) {
										row.TransactionType = TransactionType.Dividend;
									}
									if (row.ActionText.ToLowerInvariant().Equals("sell")) {
										row.TransactionType = TransactionType.Sell;
									}
									if (row.ActionText.ToLowerInvariant().Equals("buy")) {
										row.TransactionType = TransactionType.Buy;
									}
									if (row.ActionText.ToLowerInvariant().Contains("long term cap")) {
										row.TransactionType = TransactionType.DistributionLT;
									}
									if (row.ActionText.ToLowerInvariant().Contains("short term cap")) {
										row.TransactionType = TransactionType.DistributionST;
									}
								}

								row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
								row.UnitPrice = rh.ReadCell("Price").StringToDecimal() ?? 0;
								row.Fees = rh.ReadCell("Fees & Comm").StringToDecimal() ?? 0;
								row.TransactionAmount = rh.ReadCell("Amount").StringToDecimal() ?? 0;

								this.TransactionRows.Add(row);
							}
						}
					}

					r++;
				}
			}
		}
	}
}