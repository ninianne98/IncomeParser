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
	}
}