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