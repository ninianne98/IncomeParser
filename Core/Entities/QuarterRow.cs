using Carrotware.IncomeParser.Core;
using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Entities {

	public class QuarterRow {

		public QuarterRow() {
			this.Quarter = 0;
			this.Year = ParseHelper.MIN_YEAR;
		}

		public QuarterRow(int quarter, int year) {
			this.Quarter = quarter;
			this.Year = year;

			SetDates();
		}

		public QuarterRow(int quarter, TaxYearData taxData) {
			var quarterInfo = taxData.Quarters.Where(x => x.Quarter == quarter).FirstOrDefault();

			if (quarterInfo != null) {
				this.Quarter = quarterInfo.Quarter;
				this.Year = quarterInfo.Year;

				this.QuarterStartDate = quarterInfo.QuarterStartDate;
				this.QuarterEndDate = quarterInfo.QuarterEndDate;
			} else {
				// fallback
				this.Quarter = quarter;
				this.Year = taxData.Year;

				SetDates();
			}
		}

		private void SetDates() {
			var monthInt = CoreConfig.GetMonthsForQuarter(this.Quarter);
			var startMonth = monthInt.Min();
			var endMonth = monthInt.Max();

			this.QuarterStartDate = ParseHelper.GetStartDateByNumber(this.Year, startMonth);
			this.QuarterEndDate = ParseHelper.GetEndDateByNumber(this.Year, endMonth);
		}

		public int Quarter { get; set; } = 1;
		public int Year { get; set; } = DateTime.Now.Year;

		public DateTime QuarterStartDate { get; set; } = DateTime.MinValue;
		public DateTime QuarterEndDate { get; set; } = DateTime.MinValue;

		public List<QuarterlyTotalRow> QuarterlyTotalRows { get; set; } = new List<QuarterlyTotalRow>();

		public List<TransactionDetail> IncomeDetails { get; set; } = new List<TransactionDetail>();

		public List<TransactionDetail> SaleDetails { get; set; } = new List<TransactionDetail>();

		public List<WashMatch> WashMatches { get; set; } = new List<WashMatch>();
	}
}