using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Interfaces {

	public abstract class AccountGainLoss : FileCoreData, IAccountGainLoss {

		public AccountGainLoss() : base() {
			this.FileExtractType = FileExtractType.GainLoss;
			this.GainLossRows = new List<GainLossRow>();
		}

		public AccountGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.GainLossRows = new List<GainLossRow>();
		}

		public List<GainLossRow> GainLossRows { get; set; }
	}
}