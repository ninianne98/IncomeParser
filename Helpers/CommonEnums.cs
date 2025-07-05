namespace Carrotware.IncomeParser.Helpers {

	public enum BrokerIdentity {
		Unknown,
		Schwab,
		JPMorganChase,
		Fidelity,
		MerrillEdge,
	}

	//=============================

	public enum FileExtractType {
		Unknown,
		GainLoss,
		TransactionLog,
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
		Interest,
		Buy,
		Sell,
		DistributionLT,
		DistributionST,
		Dividend,
	}
}