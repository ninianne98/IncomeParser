using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Entities {

	public class GainLossRow {

		public GainLossRow() {
		}

		public GainLossRow(string? rowText) {
			this.RowText = rowText;
		}

		public GainLossRow(string? rowText, GainLossType tt) {
			this.RowText = rowText;
			this.GainLossType = tt;
		}

		public string? RowText { get; set; } = string.Empty;

		public GainLossType GainLossType { get; set; } = GainLossType.Unknown;

		public string SecuritySymbol { get; set; } = string.Empty;
		public string SecurityDescription { get; set; } = string.Empty;
		public DateTime DateOpened { get; set; } = DateTime.MinValue;
		public DateTime DateClosed { get; set; } = DateTime.MinValue;

		public decimal Quantity { get; set; } = 0;
		public decimal UnitCost { get; set; } = 0;
		public decimal UnitProceeds { get; set; } = 0;

		public decimal Proceeds { get; set; } = 0;
		public decimal CostBasis { get; set; } = 0;

		public decimal GainLoss {
			get {
				return this.Proceeds - this.CostBasis;
			}
		}
	}
}