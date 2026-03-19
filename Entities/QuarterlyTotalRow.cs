using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Entities {

	public class QuarterlyTotalRow {

		public QuarterlyTotalRow() {
			this.IncomeType = IncomeType.Unknown;
			this.Income = 0;
			this.Adjustment = 0;
		}

		public QuarterlyTotalRow(IncomeType tt, int quarter) {
			this.IncomeType = tt;
			this.Quarter = quarter;
			this.Income = 0;
			this.Adjustment = 0;
		}

		public QuarterlyTotalRow(IncomeType tt, int quarter, decimal total) : this(tt, quarter) {
			this.Income = total;
		}

		public IncomeType IncomeType { get; set; } = IncomeType.Unknown;

		public int Quarter { get; set; } = 1;

		public decimal Income { get; set; } = 0;

		public decimal Adjustment { get; set; } = 0;

		public decimal TotalIncome {
			get {
				return this.Income + this.Adjustment;
			}
		}
	}
}