using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class WashDetail {

		public WashDetail() { }

		public WashDetail(IFileCoreData data, TransactionRow row) {
			this.BrokerIdentity = data.BrokerIdentity;
			this.FileExtractType = data.FileExtractType;
			this.AccountIdentity = data.AccountIdentity;

			this.SecuritySymbol = row.SecuritySymbol;
			this.TransactionDate = row.TransactionDate;
			this.Quantity = row.Quantity;
			this.UnitPrice = row.UnitPrice;
			this.Fees = row.Fees;
			this.TransactionAmount = row.TransactionAmount;
		}

		public BrokerIdentity BrokerIdentity { get; set; }
		public FileExtractType FileExtractType { get; set; }
		public string AccountIdentity { get; set; } = string.Empty;

		public string SecuritySymbol { get; set; } = string.Empty;
		public DateTime TransactionDate { get; set; } = DateTime.MinValue;
		public decimal Quantity { get; set; } = 0;
		public decimal UnitPrice { get; set; } = 0;
		public decimal Fees { get; set; } = 0;
		public decimal TransactionAmount { get; set; } = 0;
	}
}