using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

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
			this.BrokerIdentity = MerrillBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.TransactionRows = new List<TransactionRow>();

			var rh = new RowHelper();

			bool activityViewExport = false;
			int rowHeader = 0;
			int rowData = 1;

			string fileTextSnippet = ParseHelper.ReadFirst2KB(this.FileInfo.FullName) ?? string.Empty;
			fileTextSnippet = fileTextSnippet.ToLowerInvariant();

			//two different ways exist to get the activity info - this is a way to detect
			if (fileTextSnippet.Contains("exported on:")
					&& fileTextSnippet.Contains("selected account(s):")) {
				activityViewExport = true;
				rowHeader = 2;
				rowData = 5;
			}

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(rowHeader);

				for (int r = rowData; r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 8) {
							if (r <= (rowData + 3)) {
								if (activityViewExport) {
									this.AccountIdentity = rh.ReadEmptyCell("Account");
								} else {
									this.AccountIdentity = rh.ReadEmptyCell("Account Registration")
											+ " " + rh.ReadEmptyCell("Account #");
								}
							}

							var desc1 = string.Empty;
							var description = string.Empty;
							if (activityViewExport) {
								description = rh.ReadCell("Description");
							} else {
								desc1 = rh.ReadEmptyCell("Description 1");
								description = desc1 + "  :  " + rh.ReadEmptyCell("Type")
										+ "  :  " + rh.ReadEmptyCell("Description 2");
							}

							var row = new TransactionRow(this.Rows[r]);
							row.SecuritySymbol = GetTicker(rh);
							row.ActionText = description ?? string.Empty;

							row.TransactionDate = GetTradeDate(rh) ?? DateTime.Now;
							row.SettlementDate = GetSettleDate(rh) ?? DateTime.Now;

							if (row.ActionText.ToLowerInvariant().StartsWith("dividend")
									|| desc1.ToLowerInvariant().StartsWith("dividend")) {
								row.TransactionType = TransactionType.Dividend;
							} else {
								if (row.ActionText.ToLowerInvariant().StartsWith("journal")
									|| row.ActionText.ToLowerInvariant().StartsWith("redemption")
									|| row.ActionText.ToLowerInvariant().StartsWith("reinvestment")
									|| row.ActionText.ToLowerInvariant().StartsWith("direct deposit")
									|| row.ActionText.ToLowerInvariant().StartsWith("deposit")
									|| row.ActionText.ToLowerInvariant().StartsWith("funds received")) {
									row.TransactionType = TransactionType.Journal;
								}
								if (row.ActionText.ToLowerInvariant().Contains("interest")
									|| desc1.ToLowerInvariant().StartsWith("interest")) {
									row.TransactionType = TransactionType.Interest;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("sale")
									|| desc1.ToLowerInvariant().StartsWith("sale")) {
									row.TransactionType = TransactionType.Sell;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("purchase")
									|| row.ActionText.ToLowerInvariant().StartsWith("subscription")
									|| desc1.ToLowerInvariant().StartsWith("purchase")
									|| desc1.ToLowerInvariant().StartsWith("subscription")) {
									row.TransactionType = TransactionType.Buy;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("long term capital gain")
									|| desc1.ToLowerInvariant().StartsWith("long term capital gain")) {
									row.TransactionType = TransactionType.DistributionLT;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("short term capital gain")
									|| desc1.ToLowerInvariant().StartsWith("short term capital gain")) {
									row.TransactionType = TransactionType.DistributionST;
								}
							}

							row.Fees = 0;
							row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;

							if (activityViewExport) {
								row.UnitPrice = rh.ReadCell("Price").StringToDecimal() ?? 0;
								row.TransactionAmount = rh.ReadCell("Amount").StringToDecimal() ?? 0;
							} else {
								row.UnitPrice = rh.ReadCell("Price ($)").StringToDecimal() ?? 0;
								row.TransactionAmount = rh.ReadCell("Amount ($)").StringToDecimal() ?? 0;
							}

							row.SetRowText(fields);

							this.TransactionRows.Add(row);
						}
					}
				}
			}
		}
	}
}