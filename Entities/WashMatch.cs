using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class WashMatch {
		private List<GainLossRow> _gains = new List<GainLossRow>();
		public List<GainLossRow> _losses = new List<GainLossRow>();

		public WashMatch() { }

		public WashMatch(GainLossRow row, List<string> alts, IEnumerable<GainLossRow> gains) {
			this.GainLossRow = row;
			this.AlternateTickers = alts;
			_gains = gains.ToList();

			Calculate();
		}

		public void Calculate() {
			var glr = this.GainLossRow;

			var ticker = glr.SecuritySymbol.ToUpperInvariant();
			var washStart = glr.DateClosed.AddDays(-31).Date;
			var washEnd = glr.DateClosed.AddDays(31).Date;

			_losses = _gains.Where(x => x.GainLoss < 0 && x.Quantity != 0).ToList();
			var tickerLoss = _losses.Where(x => x.SecuritySymbol == ticker);

			var lotCount = tickerLoss.Count();
			var totalQuantityLost = tickerLoss.Sum(x => x.Quantity);

			var proportionLoss = glr.Quantity / totalQuantityLost;

			this.WashStart = washStart;
			this.WashEnd = washEnd;

			this.LotCount = lotCount;
			this.TotalQuantityLost = totalQuantityLost;
			this.ProportionLoss = proportionLoss;
		}

		public List<WashDetail> ProcessDocuments(List<IFileCoreData> documents) {
			var washes = new List<WashDetail>();
			var glr = this.GainLossRow;
			var proportionLoss = this.ProportionLoss;
			var alternates = this.AlternateTickers;
			var washStart = this.WashStart;
			var washEnd = this.WashEnd;
			var lotCount = this.LotCount;

			// only need to scan if there are losses to the main ticker, if there's no loss, can't have a wash
			if (lotCount > 0) {
				foreach (var d in documents.Where(x => x is IAccountTransaction)) {
					var doc = (IAccountTransaction)d;
					var washesFound = doc.TransactionRows.Where(x => alternates.Contains(x.SecuritySymbol.ToUpperInvariant())
										&& x.TransactionType == TransactionType.Buy
										&& x.TransactionDate >= washStart && x.TransactionDate <= washEnd)
									.Select(x => new WashDetail(d, x));

					washes = washes.Union(washesFound).OrderBy(x => x.TransactionDate).ToList();
				}
			}

			washes = washes.OrderBy(x => x.SecuritySymbol)
					.OrderByDescending(x => x.Quantity)
					.OrderByDescending(x => x.TransactionAmount)
					.OrderBy(x => x.TransactionDate).ToList();

			this.WashDetails = washes;

			var washShares = washes.Sum(x => x.Quantity);
			var fracAllowed = 1 - (washShares < glr.Quantity ? (washShares / glr.Quantity) : 1);
			var lossAllowed = fracAllowed * glr.GainLoss / proportionLoss;
			var adjProportionLost = fracAllowed / proportionLoss;
			var adjustment = -1 * (glr.GainLoss - lossAllowed);

			this.WashShares = washShares;
			this.FracAllowed = fracAllowed;
			this.LossAllowed = lossAllowed;
			this.AdjProportionLost = adjProportionLost;
			this.Adjustment = adjustment;

			return washes;
		}

		public List<string> AlternateTickers { get; set; } = new List<string>();
		public GainLossRow GainLossRow { get; set; } = new GainLossRow();

		public List<WashDetail> WashDetails { get; private set; } = new List<WashDetail>();

		public DateTime WashStart { get; private set; } = DateTime.Now.AddYears(-20).Date;
		public DateTime WashEnd { get; private set; } = DateTime.Now.AddYears(-15).Date;

		public decimal ProportionLoss { get; private set; } = 0;
		public decimal LotCount { get; private set; } = 0;
		public decimal TotalQuantityLost { get; private set; } = 0;
		public decimal WashShares { get; private set; } = 0;
		public decimal FracAllowed { get; private set; } = 0;
		public decimal LossAllowed { get; private set; } = 0;
		public decimal AdjProportionLost { get; private set; } = 0;
		public decimal Adjustment { get; private set; } = 0;
	}
}