using Carrotware.IncomeParser.Core;
using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Entities {

	public class QuarterRow {

		public QuarterRow() {
			this.Quarter = 0;
			this.QuarterlyTotalRows = new List<QuarterlyTotalRow>();
		}

		public QuarterRow(int quarter, int year) : this() {
			this.Quarter = quarter;
			this.Year = year;

			var monthInt = CoreConfig.GetMonthsForQuarter(quarter);
			var startMonth = monthInt.Min();
			var endMonth = monthInt.Max();

			this.QuarterStartDate = ParseHelper.GetStartDateByNumber(year, startMonth);
			this.QuarterEndDate = ParseHelper.GetEndDateByNumber(year, endMonth);

			this.QuarterlyTotalRows = new List<QuarterlyTotalRow>();
			this.WashMatches = new List<WashMatch>();
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