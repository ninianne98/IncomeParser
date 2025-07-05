using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class ChaseGainLoss : AccountGainLoss {

		public ChaseGainLoss() : base() {
			this.SetFileType();
		}

		public ChaseGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = BrokerIdentity.JPMorganChase;
		}

		public override void ParseFile() {
			int r = 0;
			this.GainLossRows = new List<GainLossRow>();

			bool firstLine = true;  // because first line is a header row
			var rh = new RowHelper();

			using (var parser = new TextFieldParser(this.FileInfo.FullName)) {
				parser.HasFieldsEnclosedInQuotes = true;
				parser.TextFieldType = FieldType.Delimited;
				parser.SetDelimiters(",");

				while (!parser.EndOfData) {
					var fields = parser.ReadFields();
					rh.LoadRow(fields);

					if (fields != null) {
						if (firstLine) {
							rh = new RowHelper(fields);
							firstLine = false;
						} else {
							if (fields.Length > 10) {
								if (r < 2) {
									this.AccountIdentity = rh.ReadCell("Account Name")
										+ " " + rh.ReadCell("Account Number");
								}
								bool isDistribution = false;

								var detail = rh.ReadCell("Description") ?? string.Empty;
								var aqDate = rh.ReadCell("Acquired Date");
								var sDate = rh.ReadCell("Sale Date");

								var row = new GainLossRow(this.Rows[r]);
								row.SecuritySymbol = rh.ReadCell("Ticker") ?? "N/A";

								row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
								row.UnitCost = rh.ReadCell("Unit Cost Basis").StringToDecimal() ?? 0;
								row.UnitProceeds = rh.ReadCell("Unit Sale Price").StringToDecimal() ?? 0;

								if (string.IsNullOrEmpty(aqDate) && string.IsNullOrEmpty(sDate) && row.Quantity == 0) {
									// transaction log will reflect the distributions, this scan is to capture sales
									// if no dates or quantities, it's a distribution
									isDistribution = true;
								} else {
									row.DateOpened = aqDate.StringToDate() ?? DateTime.Now;
									row.DateClosed = sDate.StringToDate() ?? DateTime.Now;
								}

								if (isDistribution == false) {
									var ltcg = rh.ReadCell("Long Term Realized Gain Loss USD").StringToDecimal() ?? 0;
									var stcg = rh.ReadCell("Short Term Realized Gain Loss USD").StringToDecimal() ?? 0;

									row.GainLossType = (ltcg != 0) || (stcg == 0) ? GainLossType.Long : GainLossType.Short;

									row.Proceeds = rh.ReadCell("Market Cost/Proceeds USD").StringToDecimal() ?? 0;
									row.CostBasis = rh.ReadCell("Cost Basis USD").StringToDecimal() ?? 0;

									if (row.Proceeds == 0) {
										row.Proceeds = row.GainLossType == GainLossType.Long ? ltcg : stcg;
									}

									this.GainLossRows.Add(row);
								}
							}
						}
					}

					r++;
				}
			}
		}
	}
}