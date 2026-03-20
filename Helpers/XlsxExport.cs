using Carrotware.IncomeParser.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Drawing;
using Color = System.Drawing.Color;

namespace Carrotware.IncomeParser.Helpers {

	public class XlsxExport {

		public XlsxExport() { }

		public XlsxExport(IEnumerable<BrokerSummary> brokers) {
			this.BrokerSummaries = brokers;
		}

		public IEnumerable<BrokerSummary> BrokerSummaries { get; set; } = new List<BrokerSummary>();

		private const string Sheet_UnwashedSheet = "Unwashed";
		private const string Sheet_WashedSheet = "Washed";
		private const string Sheet_Washed = "Washes";

		private int _baseFont = 12;
		private Color _colorQ_Head = ColorTranslator.FromHtml("#AEC69D");
		private Color _colorY_Head = ColorTranslator.FromHtml("#8A9E7C");

		public void GenerateReport() {
			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;
			string fileName = Path.Join(settingFolder, ParserWorkerBee.OutputReportExcel);

			using (var ms = new MemoryStream()) {
				using (var sl = new SLDocument()) {
					sl.RenameWorksheet(SLDocument.DefaultFirstSheetName, Sheet_WashedSheet);
					sl.AddWorksheet(Sheet_UnwashedSheet);
					sl.AddWorksheet(Sheet_Washed);

					CreateQuarterlyData(sl, Sheet_WashedSheet);
					CreateQuarterlyData(sl, Sheet_UnwashedSheet);
					CreateWashData(sl);

					sl.SelectWorksheet(Sheet_WashedSheet);
					sl.SaveAs(ms);
				}

				ms.Position = 0;
				using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
					ms.CopyTo(fs);
				}
			}
		}

		protected SLDocument CreateWashData(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Washed);
			var brokers = this.BrokerSummaries;

			var stylePlain = sl.CreateStyle();
			stylePlain.Font.Bold = false;
			stylePlain.Font.FontColor = Color.Black;
			stylePlain.Font.FontSize = _baseFont;

			var styleWashHead = sl.CreateStyle();
			styleWashHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleWashHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleWashHead.Font.Bold = true;
			styleWashHead.Font.FontSize = _baseFont;
			styleWashHead.Fill.SetPattern(PatternValues.Solid, _colorQ_Head, Color.Transparent);
			styleWashHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleWashHead.Border.TopBorder.Color = Color.Black;
			styleWashHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleWashHead.Border.BottomBorder.Color = Color.Black;

			var styleMoney = sl.CreateStyle();
			styleMoney.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var styleDate = sl.CreateStyle();
			styleDate.FormatCode = "mm/dd/yyyy";

			SLD_ResizeColumn(sl, "A", 16);

			var row = 1;
			foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
				if (row > 1) {
					row = row + 2;
				}
				sl.SetCellStyle($"A{row}", $"I{row}", stylePlain);
				sl.SetCellValue($"A{row}", b.BrokerIdentity.ToString());
				sl.SetCellValue($"B{row}", b.AccountIdentity);

				row++;

				var washCount = 0;
				foreach (var qr in b.QuarterRows) {
					washCount = washCount + qr.WashMatches.Count;

					foreach (var match in qr.WashMatches) {
						var ticker = match.Ticker;
						var washes = match.WashDetails;
						var glr = match.GainLossRow;
						var proportionLoss = match.ProportionLoss;
						var lotCount = match.LotCount;

						var washShares = washes.Sum(x => x.Quantity);
						var fracAllowed = 1 - (washShares < glr.Quantity ? (washShares / glr.Quantity) : 1);
						var lossAllowed = fracAllowed * glr.GainLoss / proportionLoss;
						var adjProportionLost = fracAllowed / proportionLoss;
						var adjustment = -1 * (glr.GainLoss - lossAllowed);

						var washRange = 5 + washes.Count; // pad out 5 rows beyond the head
						sl.SetCellStyle(row, 1, (row + washRange), 10, stylePlain);

						SLD_ResizeRow(sl, row, 18);

						sl.SetCellStyle($"A{row}", $"I{row}", styleWashHead);

						sl.SetCellValue($"A{row}", ticker);

						sl.SetCellValue($"B{row}", match.GainLossRow.DateOpened);
						sl.SetCellStyle($"B{row}", styleDate);

						sl.SetCellValue($"C{row}", match.GainLossRow.GainLossType.ToString());
						sl.SetCellValue($"D{row}", match.GainLossRow.Quantity.ToString());

						sl.SetCellValue($"E{row}", match.GainLossRow.Proceeds);
						sl.SetCellStyle($"E{row}", styleMoney);

						sl.SetCellValue($"F{row}", match.GainLossRow.DateClosed);
						sl.SetCellStyle($"F{row}", styleDate);

						sl.SetCellValue($"G{row}", match.GainLossRow.GainLoss);
						sl.SetCellStyle($"G{row}", styleMoney);

						SLD_ResizeColumn(sl, "B", 18);
						SLD_ResizeColumn(sl, "C", 18);
						SLD_ResizeColumn(sl, "D", 18);
						SLD_ResizeColumn(sl, "E", 18);
						SLD_ResizeColumn(sl, "F", 18);
						SLD_ResizeColumn(sl, "G", 18);

						var washMsg = $"{washShares} alternate shares purchased,"
								+ (lossAllowed == 0 ? $" entire loss disallowed" :
								" loss limited to " + (lotCount == 1 ? $"{fracAllowed:P2}" : $"{adjProportionLost:P2} ({fracAllowed:P2} adjusted by {proportionLoss:P2} due to {lotCount} lots)"))
								+ $" - {lossAllowed:C2} max loss, add back {adjustment:C2} ";

						row++;
						sl.SetCellValue($"A{row}", washMsg);
						sl.SetCellStyle($"A{row}", stylePlain);
						SLD_ResizeRow(sl, row, 18);

						foreach (var w in washes.OrderBy(x => x.SecuritySymbol).OrderBy(x => x.TransactionDate).OrderBy(x => x.AccountIdentity)) {
							row++;
							sl.SetCellValue($"B{row}", w.AccountIdentity);
							sl.SetCellValue($"C{row}", w.SecuritySymbol);

							sl.SetCellValue($"D{row}", w.TransactionDate);
							sl.SetCellStyle($"D{row}", styleDate);

							sl.SetCellValue($"E{row}", w.Quantity);

							sl.SetCellValue($"F{row}", w.UnitPrice);
							sl.SetCellStyle($"F{row}", styleMoney);
						}

						row++;
					}
				}

				if (washCount < 1) {
					sl.SetCellValue($"A{row}", "Account has no detected wash sales");
					sl.SetCellStyle($"A{row}", stylePlain);
					SLD_ResizeRow(sl, row, 18);

					row++;
				}
			}

			return sl;
		}

		protected SLDocument CreateQuarterlyData(SLDocument sl, string sheetName) {
			bool isWashedSheet = (Sheet_WashedSheet == sheetName);
			sl.SelectWorksheet(sheetName);
			var brokers = this.BrokerSummaries;

			int brokerCount = brokers.Count();
			var year = brokers.Max(x => x.Year);

			var stylePlain = sl.CreateStyle();
			stylePlain.Font.Bold = false;
			stylePlain.Font.FontColor = Color.Black;
			stylePlain.Font.FontSize = _baseFont;

			var styleRowHead = sl.CreateStyle();
			styleRowHead.Alignment.Horizontal = HorizontalAlignmentValues.Right;
			styleRowHead.Font.Bold = true;
			styleRowHead.Font.FontColor = Color.Black;
			styleRowHead.Font.FontSize = _baseFont;

			var styleSubTot = sl.CreateStyle();
			styleSubTot.Alignment.Horizontal = HorizontalAlignmentValues.Right;
			styleSubTot.Font.Bold = true;
			styleSubTot.Font.FontColor = Color.Black;
			styleSubTot.Font.FontSize = _baseFont;
			styleSubTot.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleSubTot.Border.TopBorder.Color = Color.Black;
			styleSubTot.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleSubTot.Border.BottomBorder.Color = Color.Black;

			var styleQuarterHead = sl.CreateStyle();
			styleQuarterHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleQuarterHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleQuarterHead.Font.Bold = true;
			styleQuarterHead.Font.FontSize = _baseFont;
			styleQuarterHead.Fill.SetPattern(PatternValues.Solid, _colorQ_Head, Color.Transparent);

			var styleYearHead = sl.CreateStyle();
			styleYearHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleYearHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleYearHead.Font.Bold = true;
			styleYearHead.Font.FontSize = _baseFont;
			styleYearHead.Fill.SetPattern(PatternValues.Solid, _colorY_Head, Color.Transparent);

			var styleMainHead = sl.CreateStyle();
			styleMainHead.Font.Bold = true;
			styleMainHead.Font.FontColor = Color.Black;
			styleMainHead.Font.FontSize = _baseFont + 4;
			styleMainHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thin;
			styleMainHead.Border.BottomBorder.Color = Color.Black;

			var styleMoney = sl.CreateStyle();
			styleMoney.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var styleDate = sl.CreateStyle();
			styleDate.FormatCode = "mm/dd/yyyy";

			var starterRow = 4; // starting row
			var quarterGap = 12;  // rows per quarter
			var subhead = starterRow;
			var quarter = 1;
			var lastCol = (brokerCount + 2);
			var colA = GetColIndex('A');
			var colB = GetColIndex('B');

			var colBrkrLast = GetColIndex(brokerCount + 1);
			var colSubTot = GetColIndex(brokerCount + 2);

			sl.SetCellStyle(1, 1, 60, lastCol, stylePlain);

			var incTypes = new IncomeType[] { IncomeType.LongTermCG, IncomeType.ShortTermGG, IncomeType.Dividend, IncomeType.Interest };

			while (quarter <= 5) {
				sl.SetCellStyle((subhead - 1), colA, subhead, lastCol, styleQuarterHead);

				if (quarter <= 4) {
					var qMonth = quarter * 3;
					int qMonthDays = DateTime.DaysInMonth(year, qMonth);
					var qEndDate = new DateTime(year, qMonth, qMonthDays);

					sl.SetCellValue($"A{subhead - 1}", $"Q {quarter}");
					sl.SetCellValue($"A{subhead}", qEndDate);
					sl.SetCellStyle($"A{subhead}", styleDate);
				} else {
					sl.SetCellStyle((subhead - 1), colA, subhead, lastCol, styleYearHead);
					sl.SetCellValue($"A{subhead - 1}", "Year");
					sl.SetCellValue($"A{subhead}", $"{year}");
				}

				sl.SetCellStyle((subhead + 1), colA, (subhead + 4), colA, styleRowHead);
				sl.SetCellValue($"A{subhead + 1}", "LT GG");
				sl.SetCellValue($"A{subhead + 2}", "ST GG");
				sl.SetCellValue($"A{subhead + 3}", "Dividend");
				sl.SetCellValue($"A{subhead + 4}", "Interest");

				sl.SetCellStyle((subhead + 5), colA, (subhead + 5), lastCol, styleSubTot);
				sl.SetCellValue($"A{subhead + 5}", "subtotal");

				// isWashedSheet

				sl.SetCellValue($"{colSubTot}{subhead}", "Totals");

				for (int r = 1; r <= 4; r++) {
					string formulaSubtotalRow = $"=SUM(B{subhead + r}:{colBrkrLast}{subhead + r})";
					sl.SetCellValue($"{colSubTot}{subhead + r}", formulaSubtotalRow);
					sl.SetCellStyle($"{colSubTot}{subhead + r}", styleMoney);
				}

				string formultotalCol = $"=SUM({colSubTot}{subhead + 1}:{colSubTot}{subhead + 4})";
				sl.SetCellValue((subhead + 5), lastCol, formultotalCol);
				sl.SetCellStyle((subhead + 5), lastCol, styleMoney);

				// start in col B and move from there
				var col = colB;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					sl.SetCellValue(subhead, col, b.BrokerIdentity.ToString());
					var tots = b.QuarterRows.Where(x => x.Quarter == quarter).FirstOrDefault();

					var colLetter = GetColIndex(col);
					string formulaSubtotalCol = $"=SUM({colLetter}{subhead + 1}:{colLetter}{subhead + 4})";
					sl.SetCellValue((subhead + 5), col, formulaSubtotalCol);
					sl.SetCellStyle((subhead + 5), col, styleMoney);

					if (quarter == 5) {
						// for each tallied quarter & row within the quarter
						for (int qq = 1; qq <= 4; qq++) {
							var qFormulaIdx = qq + starterRow;
							var yrTotalFormula = $"={colLetter}{qFormulaIdx}";

							for (int qr = 1; qr <= 3; qr++) {
								qFormulaIdx = qFormulaIdx + quarterGap;
								yrTotalFormula = yrTotalFormula + $"+{colLetter}{qFormulaIdx}";
							}

							sl.SetCellValue((subhead + qq), col, yrTotalFormula);
							sl.SetCellStyle((subhead + qq), col, styleMoney);
						}
					}

					if (tots != null && quarter <= 4) {
						var incRow = subhead + 1;
						foreach (var inc in incTypes) {
							var income = tots.QuarterlyTotalRows.Where(x => x.IncomeType == inc).FirstOrDefault();
							sl.SetCellStyle(incRow, col, stylePlain);
							sl.SetCellStyle(incRow, col, styleMoney);
							if (income != null) {
								if (isWashedSheet) {
									sl.SetCellValue(incRow, col, income.TotalIncome);
								} else {
									sl.SetCellValue(incRow, col, income.Income);
								}
							} else {
								sl.SetCellValue(incRow, col, 0);
							}
							incRow++;
						}

						if (tots.QuarterlyTotalRows.Any(x => x.Adjustment != 0)) {
							if (isWashedSheet) {
								sl.SetCellValue($"A{subhead + 6}", "* wash sales reflected in above totals");
							} else {
								sl.SetCellValue($"A{subhead + 6}", "* wash sales may affect above totals");
							}
						}
					}
					col++;
				}

				quarter++;
				subhead = subhead + quarterGap;
			}

			SLD_ResizeColumn(sl, "A", 20);

			for (int c = 2; c <= lastCol; c++) {
				SLD_ResizeColumn(sl, c, 18);
			}

			if (isWashedSheet) {
				sl.SetCellValue("A1", $"Quarterly Income & Tax For {year} (with washes)");
			} else {
				sl.SetCellValue("A1", $"Quarterly Income & Tax For {year}");
			}

			sl.SetCellStyle(1, colA, 1, brokerCount + 2, styleMainHead);
			SLD_ResizeRow(sl, 1, 20);

			return sl;
		}

		protected int GetColIndex(char letter) {
			char col = char.ToUpper(letter);
			return (col - 'A' + 1);
		}

		protected char GetColIndex(int idx) {
			return (char)('A' + idx - 1);
		}

		protected void SLD_ResizeColumn(SLDocument sl, string colName, double colMinWidth) {
			sl.AutoFitColumn(colName);
			double width = sl.GetColumnWidth(colName);

			if (width < colMinWidth) {
				sl.SetColumnWidth(colName, colMinWidth);
			} else {
				sl.SetColumnWidth(colName, width + 2);
			}
		}

		protected void SLD_ResizeColumn(SLDocument sl, int colIdx, double colMinWidth) {
			sl.AutoFitColumn(colIdx);
			double width = sl.GetColumnWidth(colIdx);

			if (width < colMinWidth) {
				sl.SetColumnWidth(colIdx, colMinWidth);
			} else {
				sl.SetColumnWidth(colIdx, width + 2);
			}
		}

		protected void SLD_ResizeRow(SLDocument sl, int rowNbr, double colMinHeight) {
			sl.AutoFitRow(rowNbr);
			double height = sl.GetRowHeight(rowNbr);

			if (height < colMinHeight) {
				sl.SetRowHeight(rowNbr, colMinHeight);
			} else {
				sl.SetRowHeight(rowNbr, height + 2);
			}
		}
	}
}