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
	}
}