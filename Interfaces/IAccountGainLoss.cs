using Carrotware.IncomeParser.Entities;

namespace Carrotware.IncomeParser.Interfaces {

	public interface IAccountGainLoss {
		List<GainLossRow> GainLossRows { get; set; }
	}
}