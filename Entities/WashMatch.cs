namespace Carrotware.IncomeParser.Entities {

	public class WashMatch {

		public WashMatch() { }

		public WashMatch(string ticker, GainLossRow row, List<WashDetail> details) {
			this.Ticker = ticker;
			this.AlternateTickers = new List<string>();
			this.GainLossRow = row;
			this.WashDetails = details;
		}

		public WashMatch(string ticker, List<string> alts, GainLossRow row, List<WashDetail> details) {
			this.Ticker = ticker;
			this.AlternateTickers = alts;
			this.GainLossRow = row;
			this.WashDetails = details;
		}

		public List<string> AlternateTickers { get; set; } = new List<string>();
		public GainLossRow GainLossRow { get; set; } = new GainLossRow();
		public string Ticker { get; set; } = string.Empty;
		public List<WashDetail> WashDetails { get; set; } = new List<WashDetail>();

		public decimal ProportionLoss { get; set; } = 0;
		public decimal LotCount { get; set; } = 0;
		public decimal TotalQuantityLost { get; set; } = 0;
	}
}