using Carrotware.IncomeParser.Entities;

namespace Carrotware.IncomeParser.Interfaces {

	public interface IAccountTransaction {
		List<TransactionRow> TransactionRows { get; set; }
	}
}