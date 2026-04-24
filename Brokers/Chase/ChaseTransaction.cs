using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

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
			this.BrokerIdentity = ChaseBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.TransactionRows = new List<TransactionRow>();

			var rh = new RowHelper();

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(0);

				for (int r = 1; r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 15) {
							if (r <= 2) {
								this.AccountIdentity = rh.ReadCell("Account Name")
									+ " " + rh.ReadCell("Account Number");
							}

							var detail = (rh.ReadCell("Description") ?? string.Empty).ToLowerInvariant();

							var row = new TransactionRow(this.Rows[r]);
							row.SecuritySymbol = GetTicker(rh);
							row.ActionText = rh.ReadCell("Type") ?? string.Empty;

							row.TransactionDate = GetTradeDate(rh) ?? DateTime.Now;
							row.SettlementDate = GetSettleDate(rh) ?? DateTime.Now;

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
									|| row.ActionText.ToLowerInvariant().Contains("red")
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

							row.SetRowText(fields);

							this.TransactionRows.Add(row);
						}
					}
				}
			}
		}
	}
}