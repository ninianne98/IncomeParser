using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class MerrillGainLoss : AccountGainLoss {

		public MerrillGainLoss() : base() {
			this.SetFileType();
		}

		public MerrillGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = MerrillBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.GainLossRows = new List<GainLossRow>();

			var rh = new RowHelper();

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(0);

				for (int r = 1; r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 10) {
							if (r <= 2) {
								this.AccountIdentity = rh.ReadCell("Account Registration")
									+ " " + rh.ReadCell("Account #");
							}

							var row = new GainLossRow(this.Rows[r]);
							// Merrill uses gibberish symbols...
							// must patch later, cross check with transaction logs
							row.SecuritySymbol = GetTicker(rh);

							row.DateOpened = rh.ReadCell("Acquisition Date").StringToDate() ?? DateTime.Now;
							row.DateClosed = rh.ReadCell("Liquidation Date").StringToDate() ?? DateTime.Now;

							row.SecurityDescription = rh.ReadEmptyCell("Security Description");
							row.GainLossType = rh.ReadEmptyCell("Short/Long").ToLowerInvariant().Contains("short") ? GainLossType.Short : GainLossType.Long;

							row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
							row.UnitCost = rh.ReadCell("Acquisition Price ($)").StringToDecimal() ?? 0;
							row.UnitProceeds = rh.ReadCell("Liquidation Price ($)").StringToDecimal() ?? 0;

							row.Proceeds = rh.ReadCell("Liquidation Amount ($)").StringToDecimal() ?? 0;
							row.CostBasis = rh.ReadCell("Acquisition Cost ($)").StringToDecimal() ?? 0;

							this.GainLossRows.Add(row);
						}
					}
				}
			}
		}
	}
}