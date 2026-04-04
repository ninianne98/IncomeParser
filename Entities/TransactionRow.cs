using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Entities {

	public class TransactionRow {

		public TransactionRow() {
		}

		public TransactionRow(string? rowText) {
			this.RowText = rowText;
		}

		public TransactionRow(string[] values) {
			SetRowText(values);
		}

		public TransactionRow(string? rowText, TransactionType tt) {
			this.RowText = rowText;
			this.TransactionType = tt;
		}

		public TransactionRow(string[] values, TransactionType tt) {
			SetRowText(values);
			this.TransactionType = tt;
		}

		public void SetRowText(string[] values) {
			// set from fields parsed from CSV reader
			this.RowText = string.Join("  :  ", values);
		}

		public string? RowText { get; set; } = string.Empty;

		public string SecuritySymbol { get; set; } = string.Empty;
		public string ActionText { get; set; } = string.Empty;
		public TransactionType TransactionType { get; set; } = TransactionType.Unknown;
		public DateTime TransactionDate { get; set; } = DateTime.MinValue;
		public DateTime SettlementDate { get; set; } = DateTime.MinValue;
		public decimal Quantity { get; set; } = 0;
		public decimal UnitPrice { get; set; } = 0;
		public decimal Fees { get; set; } = 0;
		public decimal TransactionAmount { get; set; } = 0;
	}
}