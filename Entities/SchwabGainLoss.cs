using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace Carrotware.IncomeParser.Entities {

	public class SchwabGainLoss : AccountGainLoss {

		public SchwabGainLoss() : base() {
			this.SetFileType();
		}

		public SchwabGainLoss(FileInfo file, List<string> rows) : base(file, rows) {
			this.SetFileType();
		}

		public override void SetFileType() {
			base.SetFileType();

			this.FileExtractType = FileExtractType.GainLoss;
			this.BrokerIdentity = SchwabBrokerSummary.BROKER_SUMMARY_IDENTITY;
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
					if (r > 0) {
						if (fields != null) {
							if (firstLine && r == 1) {
								rh = new RowHelper(fields);
								firstLine = false;
							} else {
								if (fields.Length > 10) {
									if (r < 3) {
										var glpos = this.AccountIdentity.IndexOf("_GainLoss_");
										if (glpos > 0) {
											this.AccountIdentity = this.AccountIdentity.Substring(0, glpos).Replace("_", " ");
										}
									}

									var row = new GainLossRow(this.Rows[r]);
									row.SecuritySymbol = GetTicker(rh);
									row.SecurityDescription = rh.ReadEmptyCell("Name");
									row.DateOpened = rh.ReadCell("Opened Date").StringToDate() ?? DateTime.Now;
									row.DateClosed = rh.ReadCell("Closed Date").StringToDate() ?? DateTime.Now;

									row.GainLossType = rh.ReadEmptyCell("Term").ToLowerInvariant().Contains("short") ? GainLossType.Short : GainLossType.Long;

									row.Quantity = rh.ReadCell("Quantity").StringToDecimal() ?? 0;
									row.UnitCost = rh.ReadCell("Cost Per Share").StringToDecimal() ?? 0;
									row.UnitProceeds = rh.ReadCell("Proceeds Per Share").StringToDecimal() ?? 0;

									row.Proceeds = rh.ReadCell("Proceeds").StringToDecimal() ?? 0;
									row.CostBasis = rh.ReadCell("Cost Basis (CB)").StringToDecimal() ?? 0;

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