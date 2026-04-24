using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class TransactionDetail {

		public TransactionDetail() { }

		public TransactionDetail(IBrokerSummary data, TransactionRow row) {
			this.BrokerIdentity = data.BrokerIdentity;
			this.AccountIdentity = data.AccountIdentity;

			this.SecuritySymbol = row.SecuritySymbol;
			if (string.IsNullOrEmpty(this.SecuritySymbol)) {
				this.SecuritySymbol = row.TransactionType.ToString();
			}

			this.TransactionType = row.TransactionType;
			this.TransactionDate = row.TransactionDate;
			this.TransactionAmount = row.TransactionAmount;
		}

		public string BrokerIdentity { get; set; } = string.Empty;
		public string AccountIdentity { get; set; } = string.Empty;

		public string SecuritySymbol { get; set; } = string.Empty;

		public TransactionType TransactionType { get; set; } = TransactionType.Unknown;
		public DateTime TransactionDate { get; set; } = DateTime.MinValue;
		public decimal TransactionAmount { get; set; } = 0;
	}
}