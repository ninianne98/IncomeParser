using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class FidelityTransaction : AccountTransaction {

		public FidelityTransaction() : base() {
			this.SetFileType();
		}

		public FidelityTransaction(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.TransactionLog;
			this.BrokerIdentity = FidelityBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.TransactionRows = new List<TransactionRow>();

			var rh = new RowHelper();

			var fileToken = this.FileInfo.Name.Replace(" ", "_");
			this.AccountIdentity = fileToken;

			var pos = fileToken.ToLowerInvariant().IndexOf("history_for_account");
			if (pos > 0) {
				this.AccountIdentity = fileToken.Substring(0, pos).Replace("_", " ").Trim();
			}

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(0);

				for (int r = 1; r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 6) {
							var row = new TransactionRow(this.Rows[r]);
							row.SecuritySymbol = GetTicker(rh);
							row.ActionText = rh.ReadCell("Action") ?? string.Empty;

							row.TransactionDate = GetTradeDate(rh) ?? DateTime.Now;
							row.SettlementDate = GetSettleDate(rh) ?? DateTime.Now;

							if (row.ActionText.ToLowerInvariant().StartsWith("dividend")
								|| row.ActionText.ToLowerInvariant().Contains("div adjustment")
								|| row.ActionText.ToLowerInvariant().Contains("qualified div")
								|| row.ActionText.ToLowerInvariant().Contains("qual div")
								|| row.ActionText.ToLowerInvariant().Contains("cash div")) {
								row.TransactionType = TransactionType.Dividend;
							} else {
								if (row.ActionText.ToLowerInvariant().Contains("journal")
									|| row.ActionText.ToLowerInvariant().Contains("transferred")
									|| row.ActionText.ToLowerInvariant().Contains("reinvestment")
									|| row.ActionText.ToLowerInvariant().Contains("deposit")
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
								if (row.ActionText.ToLowerInvariant().StartsWith("you sold")) {
									row.TransactionType = TransactionType.Sell;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("you bought")) {
									row.TransactionType = TransactionType.Buy;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("long-term cap")) {
									row.TransactionType = TransactionType.DistributionLT;
								}
								if (row.ActionText.ToLowerInvariant().StartsWith("short-term cap")) {
									row.TransactionType = TransactionType.DistributionST;
								}
							}

							row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
							row.UnitPrice = rh.ReadCell("Price ($)").StringToDecimal() ?? 0;

							row.Fees = (rh.ReadCell("Commission ($)").StringToDecimal() ?? 0)
										+ (rh.ReadCell("Fees ($)").StringToDecimal() ?? 0);

							row.TransactionAmount = rh.ReadCell("Amount ($)").StringToDecimal() ?? 0;

							row.SetRowText(fields);

							this.TransactionRows.Add(row);
						}
					}
				}
			}
		}
	}
}