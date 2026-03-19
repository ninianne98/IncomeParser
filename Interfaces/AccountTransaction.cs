using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Interfaces {

	public abstract class AccountTransaction : FileCoreData, IAccountTransaction {

		public AccountTransaction() : base() {
			this.FileExtractType = FileExtractType.TransactionLog;
			this.TransactionRows = new List<TransactionRow>();
		}

		public AccountTransaction(FileInfo file, List<string> rows) : base(file, rows) {
			this.TransactionRows = new List<TransactionRow>();
		}

		public List<TransactionRow> TransactionRows { get; set; }

		protected string GetTicker(RowHelper rh) {
			var colName = new string[4] { "Ticker", "Security", "Symbol", "Symbol/ CUSIP" };

			foreach (var c in colName) {
				if (rh.Exists(c)) {
					return (rh.ReadCell(c) ?? "N/A").ToUpperInvariant();
				}
			}

			return "N/A";
		}
	}
}