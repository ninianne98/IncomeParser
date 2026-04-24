using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class SchwabGainLoss : AccountGainLoss {

		public SchwabGainLoss() : base() {
			this.SetFileType();
		}

		public SchwabGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = SchwabBrokerSummary.BROKER_SUMMARY_IDENTITY;
		}

		public override void ParseFile() {
			this.GainLossRows = new List<GainLossRow>();
			int rowHeader = -1;

			if (this.Rows.Count >= 2) {
				var row0 = this.Rows[0].ToLowerInvariant();
				if (row0.Contains("realized gain/loss - lot details")) {
					rowHeader = 1;
				}

				if (row0.Contains("symbol")
					&& row0.Contains("closed date")
					&& row0.Contains("opened date")) {
					rowHeader = 0;
				}
			}

			var rh = new RowHelper();

			var fileToken = this.FileInfo.Name.Replace(" ", "_");
			this.AccountIdentity = fileToken;

			var pos = fileToken.ToLowerInvariant().IndexOf("_gainloss_");
			if (pos > 0) {
				this.AccountIdentity = fileToken.Substring(0, pos).Replace("_", " ");
			}

			using (var parser = rh.LoadFile(this.FileInfo.FullName)) {
				rh.ReadFile();
				rh.SetHeaderRow(rowHeader);

				for (int r = (rowHeader + 1); r <= rh.FileRows.Count; r++) {
					var fields = rh.LoadRow(r);

					if (fields != null) {
						if (fields.Length > 10) {
							var row = new GainLossRow(this.Rows[r]);
							row.SecuritySymbol = GetTicker(rh);
							row.SecurityDescription = rh.ReadEmptyCell("Name");
							row.DateOpened = rh.ReadCell("Opened Date").StringToDate() ?? DateTime.Now;
							row.DateClosed = rh.ReadCell("Closed Date").StringToDate() ?? DateTime.Now;

							row.GainLossType = rh.ReadEmptyCell("Term").ToLowerInvariant().Contains("short") ? GainLossType.Short : GainLossType.Long;

							row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
							row.UnitCost = rh.ReadCell("Cost Per Share").StringToDecimal() ?? 0;
							row.UnitProceeds = rh.ReadCell("Proceeds Per Share").StringToDecimal() ?? 0;

							row.Proceeds = rh.ReadCell("Proceeds").StringToDecimal() ?? 0;
							row.CostBasis = rh.ReadCell("Cost Basis (CB)").StringToDecimal() ?? 0;

							this.GainLossRows.Add(row);
						}
					}
				}
			}
		}
	}
}