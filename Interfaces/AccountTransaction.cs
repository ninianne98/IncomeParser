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
			var colName = new string[] { "Ticker", "Security", "Symbol", "Symbol/CUSIP #", "Symbol/ CUSIP", "Symbol(CUSIP)" };

			foreach (var cn in colName) {
				if (rh.Exists(cn)) {
					var cellvalue = rh.ReadCell(cn);

					if (string.IsNullOrEmpty(cellvalue) == false
							&& cn.ToLowerInvariant() == "symbol(cusip)") {
						var symb = cellvalue.Split('(');
						if (symb.Length > 0) {
							cellvalue = symb[0];
						}
					}

					return (cellvalue ?? "N/A").ToUpperInvariant();
				}
			}

			return "N/A";
		}

		protected DateTime? GetTradeDate(RowHelper rh) {
			var colName = new string[] { "Trade Date", "Post Date", "Run Date", "Date" };

			foreach (var cn in colName) {
				if (rh.Exists(cn)) {
					return rh.ReadCell(cn).StringToDate();
				}
			}

			return null;
		}

		protected DateTime? GetSettleDate(RowHelper rh) {
			var colName = new string[] { "Settlement Date", "Post Date", "Date" };

			foreach (var cn in colName) {
				if (rh.Exists(cn)) {
					return rh.ReadCell(cn).StringToDate();
				}
			}

			return null;
		}
	}
}