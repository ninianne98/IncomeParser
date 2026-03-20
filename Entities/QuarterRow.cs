namespace Carrotware.IncomeParser.Entities {

	public class QuarterRow {

		public QuarterRow() {
			this.Quarter = 0;
			this.QuarterlyTotalRows = new List<QuarterlyTotalRow>();
		}

		public QuarterRow(int quarter, int year) : this() {
			this.Quarter = quarter;
			this.Year = year;

			var startMonth = ((quarter - 1) * 3 + 1);
			var endMonth = quarter * 3;
			int endMonthEndDate = DateTime.DaysInMonth(year, endMonth);

			this.QuarterStartDate = new DateTime(year, startMonth, 1);
			this.QuarterEndDate = new DateTime(year, endMonth, endMonthEndDate);

			this.QuarterlyTotalRows = new List<QuarterlyTotalRow>();
		}

		public QuarterRow(int quarter, int year, DateTime startD, DateTime endD) : this(quarter, year) {
			this.QuarterStartDate = startD;
			this.QuarterEndDate = endD;

			this.QuarterlyTotalRows = new List<QuarterlyTotalRow>();
			this.WashMatches = new List<WashMatch>();
		}

		public int Quarter { get; set; } = 1;
		public int Year { get; set; } = DateTime.Now.Year;

		public DateTime QuarterStartDate { get; set; } = DateTime.MinValue;
		public DateTime QuarterEndDate { get; set; } = DateTime.MinValue;

		public List<QuarterlyTotalRow> QuarterlyTotalRows { get; set; } = new List<QuarterlyTotalRow>();

		public List<WashMatch> WashMatches { get; set; } = new List<WashMatch>();
	}
}