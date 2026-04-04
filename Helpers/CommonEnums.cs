using System.ComponentModel;

namespace Carrotware.IncomeParser.Helpers {

	public enum FileExtractType {
		Unknown,
		GainLoss,
		TransactionLog,
	}

	//=============================

	public enum IncomeType {
		Unknown,

		[Description("Long Term CG")]
		LongTermCG,

		[Description("Short Term GG")]
		ShortTermGG,

		[Description("Dividend")]
		Dividend,

		[Description("Interest")]
		Interest,
	}

	//=============================

	public enum GainLossType {
		Unknown,
		Short,
		Long,
	}

	//=============================

	public enum TransactionType {
		Unknown,
		Journal,
		Buy,
		Sell,
		Dividend,
		Interest,
		DistributionLT,
		DistributionST,
	}
}