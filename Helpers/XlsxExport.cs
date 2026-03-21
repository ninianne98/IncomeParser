using Carrotware.IncomeParser.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using SpreadsheetLight;
using System.Data;
using System.Drawing;
using System.Text;
using Color = System.Drawing.Color;

namespace Carrotware.IncomeParser.Helpers {

	public class XlsxExport {

		public XlsxExport() { }

		public XlsxExport(IEnumerable<BrokerSummary> brokers) {
			this.BrokerSummaries = brokers;
		}

		public IEnumerable<BrokerSummary> BrokerSummaries { get; set; } = new List<BrokerSummary>();

		private const string Sheet_Washed = "Washed";
		private const string Sheet_Unwashed = "Unwashed";
		private const string Sheet_Washes = "Washes";

		private int _starterRowQuarters = 4; // starting row
		private int _quarterRowGap = 12;  // rows per quarter

		private int _maxIncomeRows = 62; // max rows on income

		private int _baseFont = 12;
		private Color _colorMedDarkHead = ColorTranslator.FromHtml("#AEC69D");
		private Color _colorDarkHead = ColorTranslator.FromHtml("#8A9E7C");

		private IncomeType[] _incomeTypes = [IncomeType.LongTermCG, IncomeType.ShortTermGG, IncomeType.Dividend, IncomeType.Interest];
		private string[] _taxCategories = ["Long Term CG", "Short Term CG", "Dividends", "Interest"];

		public void GenerateReport() {
			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;
			string fileName = Path.Join(settingFolder, ParserWorkerBee.OutputReportExcel);

			using (var ms = new MemoryStream()) {
				using (var sl = new SLDocument()) {
					sl.RenameWorksheet(SLDocument.DefaultFirstSheetName, Sheet_Washed);
					sl.AddWorksheet(Sheet_Unwashed);
					sl.AddWorksheet(Sheet_Washes);

					CreateQuarterlyData(sl, Sheet_Washed);
					CreateQuarterlyData(sl, Sheet_Unwashed);
					CreateWashData(sl);

					UpdateTaxInfo(sl);

					sl.SelectWorksheet(Sheet_Washed);
					sl.SaveAs(ms);
				}

				ms.Position = 0;
				using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
					ms.CopyTo(fs);
				}
			}
		}

		protected SLDocument UpdateTaxInfo(SLDocument sl) {
			var taxRates = ParserWorkerBee.Configuration.GetSection("TaxRatesPercent").Get<Dictionary<string, object>>();

			sl.SelectWorksheet(Sheet_Washed);
			var brokers = this.BrokerSummaries;

			int brokerCount = brokers.Count();
			var year = brokers.Max(x => x.Year);

			if (year < 1970) {
				year = DateTime.Now.Year;
			}

			var stylePlain = sl.CreateStyle();
			stylePlain.Font = new SLFont();
			stylePlain.Font.FontSize = _baseFont;
			stylePlain.Font.Bold = false;
			stylePlain.Font.FontColor = Color.Black;
			stylePlain.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.TopBorder.Color = Color.Black;
			stylePlain.Border.BottomBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.BottomBorder.Color = Color.Black;
			stylePlain.Border.LeftBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.LeftBorder.Color = Color.Black;
			stylePlain.Border.RightBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.RightBorder.Color = Color.Black;

			var styleHead = sl.CreateStyle();
			styleHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleHead.Font.Bold = true;
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleHead.Font.FontSize = _baseFont;
			styleHead.Fill.SetPattern(PatternValues.Solid, _colorMedDarkHead, Color.Transparent);

			var styleRowHead = sl.CreateStyle();
			styleRowHead.Alignment.Horizontal = HorizontalAlignmentValues.Right;
			styleRowHead.Font.Bold = true;
			styleRowHead.Font.FontColor = Color.Black;
			styleRowHead.Font.FontSize = _baseFont;

			var styleMoney = sl.CreateStyle();
			styleMoney.Font = new SLFont();
			styleMoney.Font.FontSize = _baseFont;
			styleMoney.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var styleMoneyAttention = sl.CreateStyle();
			styleMoneyAttention.Font = new SLFont();
			styleMoneyAttention.Font.FontSize = _baseFont;
			styleMoneyAttention.Font.Italic = true;
			styleMoneyAttention.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var stylePerc = sl.CreateStyle();
			stylePerc.Font = new SLFont();
			stylePerc.Font.FontSize = _baseFont;
			stylePerc.FormatCode = "0.00%";

			var colSubTotIdx = brokerCount + 2;
			var colSubTot = ColNumberToLetter(colSubTotIdx);

			var colTaxIdx = colSubTotIdx + 1;
			var colTax = ColNumberToLetter(colTaxIdx);

			var colTaxRateLblIdx = colTaxIdx + 4;
			var colTaxRateIdx = colTaxRateLblIdx + 1;
			var colTaxRateLbl = ColNumberToLetter(colTaxRateLblIdx);
			var colTaxRate = ColNumberToLetter(colTaxRateIdx);

			for (int i = 1; i <= _maxIncomeRows; i++) {
				var styleRowTmp = sl.GetCellStyle(i, colSubTotIdx);
				sl.SetCellStyle(i, colTaxIdx, styleRowTmp);
			}

			for (int c = 1; c <= 3; c++) {
				var lastColSpace = colTaxIdx + c;
				var colSpace = ColNumberToLetter(lastColSpace);

				SLD_ResizeColumn(sl, colSpace, 5);
			}

			var subhead = _starterRowQuarters;

			sl.SetCellStyle(subhead, colTaxRateLblIdx, subhead, colTaxRateIdx, styleHead);
			sl.SetCellStyle((subhead + 1), colTaxRateLblIdx, (subhead + 4), colTaxRateIdx, stylePlain);

			sl.SetCellValue($"{colTaxRateLbl}{subhead}", "Rates");
			sl.SetCellValue($"{colTaxRate}{subhead}", "");

			if (taxRates != null) {
				double rt = 0.30;
				int taxRateRow = subhead + 1;

				foreach (var taxCat in _taxCategories) {
					var key = taxCat.Replace(" ", "");
					rt = 0.30;

					if (taxRates.ContainsKey(key)) {
						var rate = taxRates[key].ToString() ?? "20";
						var rateNbr = double.Parse(rate);
						rt = (rateNbr > 1.00) ? (rateNbr / 100.00) : rateNbr;
					}

					sl.SetCellValue($"{colTaxRateLbl}{taxRateRow}", taxCat);
					sl.SetCellValue($"{colTaxRate}{taxRateRow}", rt);

					sl.SetCellStyle($"{colTaxRateLbl}{taxRateRow}", styleRowHead);
					sl.SetCellStyle($"{colTaxRate}{taxRateRow}", stylePerc);

					taxRateRow++;
				}

				// 4 quarters + annual
				for (int q = 1; q <= 5; q++) {
					var quarterTaxRow = subhead + 1 + ((q - 1) * _quarterRowGap);
					sl.SetCellValue($"{colTax}{quarterTaxRow - 1}", "Tax Est.");

					for (int r = 1; r <= _taxCategories.Count(); r++) {
						taxRateRow = subhead + r;
						string formulaTax = $"={colSubTot}{quarterTaxRow}*{colTaxRate}${taxRateRow}";
						sl.SetCellValue($"{colTax}{quarterTaxRow}", formulaTax);
						sl.SetCellStyle($"{colTax}{quarterTaxRow}", styleMoney);
						quarterTaxRow++;
					}

					string formultotalCol = $"=SUM({colTax}{quarterTaxRow - 4}:{colTax}{quarterTaxRow - 1})";
					sl.SetCellValue($"{colTax}{quarterTaxRow}", formultotalCol);
				}

				SLD_ResizeColumn(sl, colTax, 18);
			}

			SLD_ResizeColumn(sl, colTaxRateLbl, 20);
			SLD_ResizeColumn(sl, colTaxRate, 16);

			return sl;
		}

		protected SLDocument CreateWashData(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Washes);
			var brokers = this.BrokerSummaries;

			var stylePlain = sl.CreateStyle();
			stylePlain.Font = new SLFont();
			stylePlain.Font.FontSize = _baseFont;
			stylePlain.Font.Bold = false;
			stylePlain.Font.FontColor = Color.Black;

			var styleWashHead = sl.CreateStyle();
			styleWashHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleWashHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleWashHead.Font.Bold = true;
			styleWashHead.Font.FontSize = _baseFont;
			styleWashHead.Fill.SetPattern(PatternValues.Solid, _colorMedDarkHead, Color.Transparent);
			styleWashHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleWashHead.Border.TopBorder.Color = Color.Black;
			styleWashHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleWashHead.Border.BottomBorder.Color = Color.Black;

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

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'J';
			var colMaxIdx = ColLetterToNumber(colMax);

			SLD_ResizeColumn(sl, colFirstIdx, 14);

			var row = 1;
			foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
				if (row > 1) {
					//create a gap between brokerages
					row = row + 2;
				}
				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
				sl.SetCellValue($"{colFirst}{row}", $"{b.BrokerIdentity} : {b.AccountIdentity}");
				SLD_ResizeRow(sl, row, 20);

				row++;

				var washCount = 0;
				foreach (var qr in b.QuarterRows) {
					washCount = washCount + qr.WashMatches.Count;

					sl.SetCellStyle(row, colFirstIdx, (row + 3), colMaxIdx, stylePlain);

					foreach (var match in qr.WashMatches) {
						var glr = match.GainLossRow;
						var ticker = glr.SecuritySymbol.ToUpperInvariant();
						var washes = match.WashDetails;
						var totalQuantityLost = match.TotalQuantityLost;
						var lotCount = match.LotCount;
						var proportionLoss = match.ProportionLoss;
						var washShares = match.WashShares;
						var fracAllowed = match.FracAllowed;
						var lossAllowed = match.LossAllowed;
						var adjProportionLost = match.AdjProportionLost;
						var adjustment = match.Adjustment;

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

						sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleWashHead);
						SLD_ResizeRow(sl, row, 18);

						SLD_ResizeColumn(sl, "B", 18);
						SLD_ResizeColumn(sl, "C", 18);
						SLD_ResizeColumn(sl, "D", 18);
						SLD_ResizeColumn(sl, "E", 18);
						SLD_ResizeColumn(sl, "F", 18);
						SLD_ResizeColumn(sl, "G", 18);

						sl.SetCellStyle(row, colFirstIdx, (row + washes.Count + 5), colMaxIdx, stylePlain);

						var washMsg = $"{washShares} alternate shares purchased,"
								+ (lossAllowed == 0 ? $" entire loss disallowed" :
								" loss limited to " + (lotCount == 1 ? $"{fracAllowed:P2}" : $"{adjProportionLost:P2} ({fracAllowed:P2} adjusted by {proportionLoss:P2} due to {lotCount} lots)"))
								+ $" - {lossAllowed:C2} max loss, add back {adjustment:C2} ";

						row++;
						sl.SetCellValue($"{colFirst}{row}", washMsg);
						sl.SetCellStyle($"{colFirst}{row}", stylePlain);
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
					sl.SetCellValue($"{colFirst}{row}", "Account has no detected wash sales");
					sl.SetCellStyle($"{colFirst}{row}", stylePlain);
					SLD_ResizeRow(sl, row, 18);

					row++;
				}
			}

			return sl;
		}

		protected SLDocument CreateQuarterlyData(SLDocument sl, string sheetName) {
			bool isWashedSheet = (Sheet_Washed.ToUpperInvariant() == sheetName.ToUpperInvariant());
			sl.SelectWorksheet(sheetName);
			var brokers = this.BrokerSummaries;

			int brokerCount = brokers.Count();
			var year = brokers.Max(x => x.Year);

			if (year < 1970) {
				year = DateTime.Now.Year;
			}

			var stylePlain = sl.CreateStyle();
			stylePlain.Font = new SLFont();
			stylePlain.Font.FontSize = _baseFont;
			stylePlain.Font.Bold = false;
			stylePlain.Font.FontColor = Color.Black;

			var stylePlainAttention = sl.CreateStyle();
			stylePlainAttention.Font = new SLFont();
			stylePlainAttention.Font.FontSize = _baseFont;
			stylePlainAttention.Font.Italic = true;
			stylePlain.Font.FontColor = Color.Black;

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
			styleQuarterHead.Fill.SetPattern(PatternValues.Solid, _colorMedDarkHead, Color.Transparent);

			var styleYearHead = sl.CreateStyle();
			styleYearHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			styleYearHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			styleYearHead.Font.Bold = true;
			styleYearHead.Font.FontSize = _baseFont;
			styleYearHead.Fill.SetPattern(PatternValues.Solid, _colorDarkHead, Color.Transparent);

			var styleMainHead = sl.CreateStyle();
			styleMainHead.Font.Bold = true;
			styleMainHead.Font.FontColor = Color.Black;
			styleMainHead.Font.FontSize = _baseFont + 4;
			styleMainHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thin;
			styleMainHead.Border.BottomBorder.Color = Color.Black;

			var styleMoney = sl.CreateStyle();
			styleMoney.Font = new SLFont();
			styleMoney.Font.FontSize = _baseFont;
			styleMoney.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var styleMoneyAttention = sl.CreateStyle();
			styleMoneyAttention.Font = new SLFont();
			styleMoneyAttention.Font.FontSize = _baseFont;
			styleMoneyAttention.Font.Italic = true;
			styleMoneyAttention.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			var styleDate = sl.CreateStyle();
			styleDate.Font = new SLFont();
			styleDate.Font.FontSize = _baseFont;
			styleDate.FormatCode = "mm/dd/yyyy";

			var quarter = 1;
			var subhead = _starterRowQuarters;

			var colSubTotIdx = brokerCount + 2;
			var colBrkrLastIdx = brokerCount + 1;
			var colA = ColLetterToNumber('A');
			var colB = ColLetterToNumber('B');

			var colBrkrLast = ColNumberToLetter(colBrkrLastIdx);
			var colSubTot = ColNumberToLetter(colSubTotIdx);

			sl.SetCellStyle(1, colA, _maxIncomeRows, colSubTotIdx, stylePlain);

			while (quarter <= 5) {
				sl.SetCellStyle((subhead - 1), colA, subhead, colSubTotIdx, styleQuarterHead);

				if (quarter <= 4) {
					var qMonth = quarter * 3;
					int qMonthDays = DateTime.DaysInMonth(year, qMonth);
					var qEndDate = new DateTime(year, qMonth, qMonthDays);

					sl.SetCellValue($"A{subhead - 1}", $"Q {quarter}");
					sl.SetCellValue($"A{subhead}", qEndDate);
					sl.SetCellStyle($"A{subhead}", styleDate);
				} else {
					sl.SetCellStyle((subhead - 1), colA, subhead, colSubTotIdx, styleYearHead);
					sl.SetCellValue($"A{subhead - 1}", "Year");
					sl.SetCellValue($"A{subhead}", $"{year}");
				}

				sl.SetCellStyle((subhead + 1), colA, (subhead + 4), colA, styleRowHead);

				for (var i = 0; i < _taxCategories.Count(); i++) {
					sl.SetCellValue($"A{subhead + 1 + i}", _taxCategories[i]);
				}

				sl.SetCellStyle((subhead + 5), colA, (subhead + 5), colSubTotIdx, styleSubTot);
				sl.SetCellValue($"A{subhead + 5}", "subtotal");

				sl.SetCellValue($"{colSubTot}{subhead}", "Totals");

				for (int r = 1; r <= 4; r++) {
					string formulaSubtotalRow = $"=SUM(B{subhead + r}:{colBrkrLast}{subhead + r})";
					sl.SetCellValue($"{colSubTot}{subhead + r}", formulaSubtotalRow);
					sl.SetCellStyle($"{colSubTot}{subhead + r}", styleMoney);
				}

				string formultotalCol = $"=SUM({colSubTot}{subhead + 1}:{colSubTot}{subhead + 4})";
				sl.SetCellValue((subhead + 5), colSubTotIdx, formultotalCol);
				sl.SetCellStyle((subhead + 5), colSubTotIdx, styleMoney);

				// start in col B and move from there
				var col = colB;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					sl.SetCellValue(subhead, col, b.BrokerIdentity.ToString());
					var tots = b.QuarterRows.Where(x => x.Quarter == quarter).FirstOrDefault();

					var colLetter = ColNumberToLetter(col);
					string formulaSubtotalCol = $"=SUM({colLetter}{subhead + 1}:{colLetter}{subhead + 4})";
					sl.SetCellValue((subhead + 5), col, formulaSubtotalCol);
					sl.SetCellStyle((subhead + 5), col, styleMoney);

					if (quarter == 5) {
						// for each tallied quarter & row within the quarter
						for (int qq = 1; qq <= 4; qq++) {
							var qFormulaIdx = qq + _starterRowQuarters;
							var yrTotalFormula = $"={colLetter}{qFormulaIdx}";

							for (int qr = 1; qr <= 3; qr++) {
								qFormulaIdx = qFormulaIdx + _quarterRowGap;
								yrTotalFormula = yrTotalFormula + $"+{colLetter}{qFormulaIdx}";
							}

							sl.SetCellValue((subhead + qq), col, yrTotalFormula);
							sl.SetCellStyle((subhead + qq), col, styleMoney);
						}
					}

					if (tots == null) {
						tots = new QuarterRow();
					}

					if (tots != null && quarter <= 4) {
						if (tots.QuarterStartDate != DateTime.MinValue) {
							if (tots.QuarterStartDate > DateTime.Now.Date) {
								sl.SetCellValue($"A{subhead + 6}", "* Future Dates Out Of Range");
								sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
							}
							if (tots.QuarterStartDate <= DateTime.Now.Date && tots.QuarterEndDate >= DateTime.Now.Date) {
								sl.SetCellValue($"A{subhead + 6}", "* Quarter Not Closed");
								sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
							}
						}

						var incRow = subhead + 1;
						foreach (var inc in _incomeTypes) {
							var income = tots.QuarterlyTotalRows.Where(x => x.IncomeType == inc).FirstOrDefault();
							sl.SetCellStyle(incRow, col, stylePlain);
							sl.SetCellStyle(incRow, col, styleMoney);
							if (income != null) {
								if (isWashedSheet) {
									if (income.Adjustment != 0) {
										sl.SetCellStyle(incRow, col, styleMoneyAttention);
									}
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
								sl.SetCellValue($"A{subhead + 6}", "* Wash sales reflected in above totals");
							} else {
								sl.SetCellValue($"A{subhead + 6}", "* Wash sales may affect above totals");
							}
							sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
						}
					}
					col++;
				}

				quarter++;
				subhead = subhead + _quarterRowGap;
			}

			SLD_ResizeColumn(sl, "A", 20);

			for (int c = 2; c <= colSubTotIdx; c++) {
				SLD_ResizeColumn(sl, c, 18);
			}

			if (isWashedSheet) {
				sl.SetCellValue("A1", $"Quarterly Income & Tax For {year} (with washes)");
			} else {
				sl.SetCellValue("A1", $"Quarterly Income & Tax For {year}");
			}

			sl.SetCellStyle(1, colA, 1, colSubTotIdx, styleMainHead);
			SLD_ResizeRow(sl, 1, 20);

			return sl;
		}

		public int ColLetterToNumber(char col) {
			return ColLetterToNumber(col.ToString());
		}

		public int ColLetterToNumber(string col) {
			if (string.IsNullOrEmpty(col)) {
				throw new ArgumentException("Column name cannot be null or empty.", nameof(col));
			}

			col = col.ToUpperInvariant();
			int columnNumber = 0;
			foreach (char character in col) {
				columnNumber = (columnNumber * 26) + (character - 'A' + 1);
			}

			return columnNumber;
		}

		public string ColNumberToLetter(int colIdx) {
			if (colIdx <= 0) {
				throw new ArgumentException("Column number must be greater than zero.", nameof(colIdx));
			}

			StringBuilder columnName = new StringBuilder();
			while (colIdx > 0) {
				int modulo = (colIdx - 1) % 26;
				columnName.Insert(0, (char)('A' + modulo));
				colIdx = (colIdx - 1) / 26;
			}

			return columnName.ToString().ToUpperInvariant();
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