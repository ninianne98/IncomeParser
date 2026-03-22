using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class MerrillGainLoss : AccountGainLoss {

		public MerrillGainLoss() : base() {
			this.SetFileType();
		}

		public MerrillGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = MerrillBrokerSummary.BROKER_SUMMARY_IDENTITY;
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
									this.AccountIdentity = rh.ReadCell("Account Registration")
										+ " " + rh.ReadCell("Account #");
								}

								var row = new GainLossRow(this.Rows[r]);
								// Merrill uses gibberish symbols...
								// must patch later
								row.SecuritySymbol = GetTicker(rh);

								row.DateOpened = rh.ReadCell("Acquisition Date").StringToDate() ?? DateTime.Now;
								row.DateClosed = rh.ReadCell("Liquidation Date").StringToDate() ?? DateTime.Now;

								row.SecurityDescription = rh.ReadEmptyCell("Security Description");
								row.GainLossType = rh.ReadEmptyCell("Short/Long").ToLowerInvariant().Contains("short") ? GainLossType.Short : GainLossType.Long;

								row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
								row.UnitCost = rh.ReadCell("Acquisition Price ($)").StringToDecimal() ?? 0;
								row.UnitProceeds = rh.ReadCell("Liquidation Price ($)").StringToDecimal() ?? 0;

								row.Proceeds = rh.ReadCell("Liquidation Amount ($)").StringToDecimal() ?? 0;
								row.CostBasis = rh.ReadCell("Acquisition Cost ($)").StringToDecimal() ?? 0;

								this.GainLossRows.Add(row);
							}
						}
					}

					r++;
				}
			}
		}
	}
}