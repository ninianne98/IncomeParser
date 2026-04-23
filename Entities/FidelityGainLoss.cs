using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class FidelityGainLoss : AccountGainLoss {

		public FidelityGainLoss() : base() {
			this.SetFileType();
		}

		public FidelityGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = FidelityBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.GainLossRows = new List<GainLossRow>();
			var rh = new RowHelper();

			var fileToken = this.FileInfo.Name.Replace(" ", "_");
			this.AccountIdentity = fileToken;

			var pos = fileToken.ToLowerInvariant().IndexOf("portfolio_closed_lots");
			if (pos > 0) {
				this.AccountIdentity = fileToken.Substring(0, pos).Replace("_", " ").Trim();
			}

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(0);

				for (int r = 1; r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 8) {
							var row = new GainLossRow(this.Rows[r]);
							row.SecuritySymbol = GetTicker(rh);
							row.SecurityDescription = rh.ReadEmptyCell("Security description");

							row.DateOpened = rh.ReadCell("Date acquired").StringToDate() ?? DateTime.Now;
							row.DateClosed = rh.ReadCell("Date sold").StringToDate() ?? DateTime.Now;

							row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;

							row.Proceeds = rh.ReadCell("Proceeds").StringToDecimal() ?? 0;
							row.CostBasis = rh.ReadCell("Cost basis").StringToDecimal() ?? 0;

							var ltcg = rh.ReadCell("Long-term gain/loss").StringToDecimal() ?? 0;
							var stcg = rh.ReadCell("Short-term gain/loss").StringToDecimal() ?? 0;

							row.GainLossType = ltcg != 0 ? GainLossType.Long : GainLossType.Short;
							if (ltcg == stcg) {
								row.GainLossType = (row.DateOpened.AddYears(1).Date <= row.DateClosed.Date)
											? GainLossType.Long : GainLossType.Short;
							}

							row.UnitCost = row.Quantity != 0 ? (row.CostBasis / row.Quantity) : 0;
							row.UnitProceeds = row.Quantity != 0 ? (row.Proceeds / row.Quantity) : 0;

							this.GainLossRows.Add(row);
						}
					}
				}
			}
		}
	}
}