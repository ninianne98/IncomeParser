using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class ChaseTransaction : AccountTransaction {

		public ChaseTransaction() : base() {
			this.SetFileType();
		}

		public ChaseTransaction(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.TransactionLog;
			this.BrokerIdentity = BrokerIdentity.JPMorganChase;
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
							if (fields.Length > 15) {
								if (r < 2) {
									this.AccountIdentity = rh.ReadCell("Account Name")
										+ " " + rh.ReadCell("Account Number");
								}

								var detail = (rh.ReadCell("Description") ?? string.Empty).ToLowerInvariant();

								var row = new TransactionRow(this.Rows[r]);
								row.SecuritySymbol = (rh.ReadCell("Ticker") ?? "N/A").Trim();
								row.ActionText = rh.ReadCell("Type") ?? string.Empty;

								row.TransactionDate = rh.ReadCell("Settlement Date").StringToDate() ?? DateTime.Now;

								if (row.ActionText.ToLowerInvariant().Contains("dividend")) {
									row.TransactionType = TransactionType.Dividend;

									if (detail.Contains("lt cap-gain div")
										|| detail.Contains("l/t cap gns")) {
										row.TransactionType = TransactionType.DistributionLT;
									}
									if (detail.Contains("st cap-gain div")
										|| detail.Contains("s/t cap gns")) {
										row.TransactionType = TransactionType.DistributionST;
									}
								} else {
									if (row.ActionText.ToLowerInvariant().Contains("journal")
										|| row.ActionText.ToLowerInvariant().Contains("jnl")
										|| row.ActionText.ToLowerInvariant().Contains("acp")
										|| row.ActionText.ToLowerInvariant().Contains("wdl")
										|| row.ActionText.ToLowerInvariant().Contains("dbs")
										|| row.ActionText.ToLowerInvariant().Contains("bnk")
										|| row.ActionText.ToLowerInvariant().Contains("reinvest")) {
										row.TransactionType = TransactionType.Journal;
									}
									if (row.ActionText.ToLowerInvariant().Contains("interest")) {
										row.TransactionType = TransactionType.Interest;
									}
									if (row.ActionText.ToLowerInvariant().Equals("sell")) {
										row.TransactionType = TransactionType.Sell;
									}
									if (row.ActionText.ToLowerInvariant().Equals("buy")) {
										row.TransactionType = TransactionType.Buy;
									}

									if (row.ActionText.ToLowerInvariant().Equals("cap")) {
										if ((detail.Contains("lt cap-gain")
											|| detail.Contains("l/t cap gns"))
											|| !string.IsNullOrEmpty(rh.ReadCell("G/L Long USDs"))) {
											row.TransactionType = TransactionType.DistributionLT;
										}
										if ((detail.Contains("st cap-gain")
											|| detail.Contains("s/t cap gns"))
											|| !string.IsNullOrEmpty(rh.ReadCell("G/L Short USD"))) {
											row.TransactionType = TransactionType.DistributionST;
										}
									}
								}

								row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
								row.UnitPrice = rh.ReadCell("Price USD").StringToDecimal() ?? 0;
								row.Fees = rh.ReadCell("Commissions USD").StringToDecimal() ?? 0;
								row.TransactionAmount = rh.ReadCell("Amount USD").StringToDecimal() ?? 0;

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