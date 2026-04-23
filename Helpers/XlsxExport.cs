using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Interfaces;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using SpreadsheetLight.Charts;
using System.Data;
using System.Drawing;
using System.Text;
using Color = System.Drawing.Color;

namespace Carrotware.IncomeParser.Helpers {

	public class XlsxExport {

		public XlsxExport() { }

		public XlsxExport(IEnumerable<IBrokerSummary> brokers) {
			this.BrokerSummaries = brokers;
		}

		public IEnumerable<IBrokerSummary> BrokerSummaries { get; set; } = new List<IBrokerSummary>();

		private const string Sheet_Washed = "Washed";
		private const string Sheet_Unwashed = "Unwashed";
		private const string Sheet_Washes = "Washes";
		private const string Sheet_Sales = "Sales";
		private const string Sheet_SalesTotal = "Sales Total";
		private const string Sheet_Income = "Income";
		private const string Sheet_IncomeTotal = "Income Totals";

		private int _maxIncomeRows = 72; // max rows on income (4 quarters + year)
		private int _starterRowQuarters = 4; // starting row
		private int _quarterRowGap = 14;  // rows per quarter

		private int _baseFont = 12;

		// #AEC69D	#85AE93	#62958A	#497B7E	#39616D	#2F4858
		// #AEC69D	#83B694	#54A591	#089292	#007F95	#006A95
		// #8A9E7C	#6B8F78	#517E74	#3F6C6E	#345A65	#2F4858
		// #8A9E7C	#6B9679	#478C7B	#0F8282	#00768A	#006990

		private Color _color1 = ColorTranslator.FromHtml("#AEC69D");
		private Color _color2 = ColorTranslator.FromHtml("#83B694");
		private Color _color3 = ColorTranslator.FromHtml("#54A591");
		private Color _color4 = ColorTranslator.FromHtml("#089292");
		private Color _color5 = ColorTranslator.FromHtml("#007F95");
		private Color _color6 = ColorTranslator.FromHtml("#006A95");

		private Color _colorShort = ColorTranslator.FromHtml("#FFE4E0");
		private Color _colorLong = ColorTranslator.FromHtml("#E2F7D3");

		private IncomeType[] _incomeTypes = [IncomeType.LongTermCG, IncomeType.ShortTermGG, IncomeType.Dividend, IncomeType.Interest];

		public int Year { get; set; } = DateTime.Now.Year;

		public SLStyle StylePlain(SLDocument sl) {
			var style = sl.CreateStyle();
			style.Font.FontSize = _baseFont;
			style.Font.Bold = false;
			style.Font.FontColor = Color.Black;

			return style;
		}

		public SLStyle StyleMoney(SLDocument sl) {
			var style = sl.CreateStyle();
			style.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

			return style;
		}

		public SLStyle StyleDate(SLDocument sl) {
			var style = sl.CreateStyle();
			style.FormatCode = "mm/dd/yyyy";

			return style;
		}

		public SLStyle StylePercent(SLDocument sl) {
			var style = sl.CreateStyle();
			style.FormatCode = "0.00%";

			return style;
		}

		public SLStyle StyleRowHead(SLDocument sl) {
			var style = StylePlain(sl);
			style.Alignment.Horizontal = HorizontalAlignmentValues.Right;
			style.Font.Bold = true;

			return style;
		}

		public SLStyle StyleHeadBase(SLDocument sl) {
			var style = StyleCenteredBackground(sl, _color1);

			return style;
		}

		public SLStyle StyleH2(SLDocument sl) {
			var style = StylePlain(sl);
			style.Font.Bold = true;
			style.Font.FontColor = _color6;
			style.Font.FontSize = _baseFont + 4;
			style.Border.BottomBorder.BorderStyle = BorderStyleValues.Medium;
			style.Border.BottomBorder.Color = _color6;

			return style;
		}

		public SLStyle StyleCenteredBackground(SLDocument sl, Color color) {
			var style = StylePlain(sl);

			style.Font.Bold = true;
			style.Alignment.Horizontal = HorizontalAlignmentValues.Center;
			style.Alignment.Vertical = VerticalAlignmentValues.Bottom;
			style.Fill.SetPattern(PatternValues.Solid, color, Color.Transparent);

			return style;
		}

		public void GenerateReport() {
			var year = this.BrokerSummaries.Max(x => x.Year);
			if (year <= 1970) {
				year = DateTime.Now.Year;
			}
			this.Year = year;

			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;
			//string fileName = Path.Join(settingFolder, ParserWorkerBee.OutputReportExcel);
			string fileName = Path.Join(settingFolder, ParserWorkerBee.OutputReportExcelYear(year));

			using (var ms = new MemoryStream()) {
				using (var sl = new SLDocument()) {
					Console.WriteLine("Creating workbook ==========");

					sl.RenameWorksheet(SLDocument.DefaultFirstSheetName, Sheet_Washed);
					sl.AddWorksheet(Sheet_Unwashed);
					sl.AddWorksheet(Sheet_Washes);
					sl.AddWorksheet(Sheet_Sales);
					sl.AddWorksheet(Sheet_SalesTotal);
					sl.AddWorksheet(Sheet_Income);
					sl.AddWorksheet(Sheet_IncomeTotal);

					Console.WriteLine("Creating quarterly reports ============");
					CreateQuarterlyData(sl, Sheet_Washed);
					CreateQuarterlyData(sl, Sheet_Unwashed);

					Console.WriteLine("Creating wash report ==============");
					CreateWashData(sl);
					Console.WriteLine("Adding tax estimates ================");
					UpdateTaxInfo(sl);

					Console.WriteLine("Creating sale history ==================");
					ReportSales(sl);
					Console.WriteLine("Creating sale totals ==================");
					ReportSalesTotal(sl);

					Console.WriteLine("Creating income history ==================");
					ReportIncome(sl);
					Console.WriteLine("Creating income totals ==================");
					ReportIncomeTotal(sl);

					sl.SelectWorksheet(Sheet_Washed);
					Console.WriteLine("Saving workbook ========================");
					sl.SaveAs(ms);
				}

				ms.Position = 0;
				using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
					ms.CopyTo(fs);
				}
			}
		}

		public DateTime GetEndOfMonthByDate(DateTime month) {
			int days = DateTime.DaysInMonth(month.Year, month.Month);
			return new DateTime(month.Year, month.Month, days, 23, 59, 59);
		}

		protected SLDocument ReportIncomeTotal(SLDocument sl) {
			sl.SelectWorksheet(Sheet_IncomeTotal);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			int row = 0;

			var stylePlain = StylePlain(sl);
			var styleMainHead = StyleH2(sl);

			var styleHead = StyleHeadBase(sl);
			styleHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleHead.Border.BottomBorder.Color = Color.Black;

			var styleRowHead = StyleRowHead(sl);

			var styleMoney = StyleMoney(sl);

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'N';
			var colMaxIdx = ColLetterToNumber(colMax);

			var income = new List<TransactionDetail>();

			foreach (var b in brokers) {
				Console.WriteLine($"\tCreating income totals {b.AccountIdentity} ==========");

				foreach (var q in b.QuarterRows) {
					income = income.Union(q.IncomeDetails).ToList();
				}
			}

			row = 1;
			sl.SetCellValue($"A{row}", "Income By Month");
			sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
			SLD_ResizeRow(sl, row, 20);

			row++;
			sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleHead);
			sl.SetCellValue($"B{row}", "Account");
			SLD_ResizeRow(sl, row, 18);

			row++;
			foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", stylePlain);

				sl.SetCellValue($"A{row}", b.BrokerIdentity);
				sl.SetCellValue($"B{row}", b.AccountIdentity);

				sl.SetCellStyle($"A{row}", styleRowHead);
				sl.SetCellStyle($"C{row}", $"N{row}", styleMoney);

				row++;
			}

			for (int m = 1; m <= 12; m++) {
				row = 2;
				var colMonthIdx = colFirstIdx + m + 1;
				var colMonth = ColNumberToLetter(colMonthIdx);

				var monthStart = new DateTime(year, m, 1);
				var monthEnd = GetEndOfMonthByDate(monthStart);

				//income for the month
				var monthIncome = income.Where(x => x.TransactionDate >= monthStart && x.TransactionDate <= monthEnd).ToList();
				//var totalIncome = monthIncome.Sum(x => x.TransactionAmount);

				Console.WriteLine($"\tTabulating income totals for {monthStart.ToString("MMMM yyyy")} ==========");

				sl.SetCellValue($"{colMonth}{row}", $"{monthStart.ToString("MMM yyyy")}");

				row++;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					var accountIncome = monthIncome.Where(x => x.BrokerIdentity == b.BrokerIdentity && x.AccountIdentity == b.AccountIdentity);
					var acctIncome = accountIncome.Sum(x => x.TransactionAmount);

					sl.SetCellValue($"{colMonth}{row}", acctIncome);
					sl.SetCellStyle($"{colMonth}{row}", styleMoney);

					row++;
				}

				SLD_ResizeColumn(sl, colMonth, 16);
			}

			SLD_ResizeColumn(sl, "A", 16);
			SLD_ResizeColumn(sl, "B", 18);

			var accountColors = new Color[] { _color2, _color4, _color6, _color1, _color3, _color5 };

			int lastRow = 2 + brokerCount;

			var options = new SLCreateChartOptions() { RowsAsDataSeries = true };
			var chart = sl.CreateChart(2, 2, lastRow, colMaxIdx, options);

			for (int i = 0; i < brokerCount; i++) {
				int seriesIndex = i + 1;
				var color = accountColors[i % accountColors.Length];

				var seriesOptions = chart.GetDataSeriesOptions(seriesIndex);

				seriesOptions.Fill.SetSolidFill(color, 0);

				chart.SetDataSeriesOptions(seriesIndex, seriesOptions);
			}

			chart.SetChartType(SLColumnChartType.ClusteredColumn);
			chart.ShowChartLegend(LegendPositionValues.Right, false);

			chart.SetChartPosition((lastRow + 2), 1, (lastRow + 25), 12);
			sl.InsertChart(chart);

			return sl;
		}

		protected SLDocument ReportIncomeTotal_2(SLDocument sl) {
			sl.SelectWorksheet(Sheet_IncomeTotal);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			var stylePlain = StylePlain(sl);
			var styleMainHead = StyleH2(sl);
			var styleHead = StyleHeadBase(sl);
			var styleRowHead = StyleRowHead(sl);

			var styleMoney = StyleMoney(sl);
			var styleDate = StyleDate(sl);

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'D';
			var colMaxIdx = ColLetterToNumber(colMax);

			var income = new List<TransactionDetail>();

			foreach (var b in brokers) {
				Console.WriteLine($"\tCreating income totals {b.AccountIdentity} ==========");

				foreach (var q in b.QuarterRows) {
					income = income.Union(q.IncomeDetails).ToList();
				}
			}

			int row = 1;

			for (int m = 1; m <= 12; m++) {
				var monthStart = new DateTime(year, m, 1);
				var monthEnd = GetEndOfMonthByDate(monthStart);

				//income for the month
				var monthIncome = income.Where(x => x.TransactionDate >= monthStart && x.TransactionDate <= monthEnd).ToList();
				var totalIncome = monthIncome.Sum(x => x.TransactionAmount);

				Console.WriteLine($"\tTabulating income totals for {monthStart.ToString("MMMM yyyy")} ==========");

				sl.SetCellValue($"{colFirst}{row}", $"{monthStart.ToString("MMMM yyyy")}");
				sl.SetCellValue($"B{row}", " ");
				sl.SetCellValue($"C{row}", " ");
				sl.SetCellValue($"{colMax}{row}", 0);

				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
				//sl.SetCellStyle($"{colMax}{row}", styleMoney);

				SLD_ResizeRow(sl, row, 20);
				row++;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", stylePlain);

					sl.SetCellValue($"B{row}", b.BrokerIdentity);
					sl.SetCellValue($"C{row}", b.AccountIdentity);

					var accountIncome = monthIncome.Where(x => x.BrokerIdentity == b.BrokerIdentity && x.AccountIdentity == b.AccountIdentity);
					var acctIncome = accountIncome.Sum(x => x.TransactionAmount);

					sl.SetCellValue($"{colMax}{row}", acctIncome);

					sl.SetCellStyle($"B{row}", styleRowHead);
					sl.SetCellStyle($"{colMax}{row}", styleMoney);

					row++;
				}
				row++;
			}

			SLD_ResizeColumn(sl, "A", 16);
			SLD_ResizeColumn(sl, "B", 18);
			SLD_ResizeColumn(sl, "C", 18);
			SLD_ResizeColumn(sl, "D", 18);

			return sl;
		}

		protected SLDocument ReportIncome(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Income);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			var stylePlain = StylePlain(sl);
			var styleMainHead = StyleH2(sl);
			var styleHead = StyleHeadBase(sl);
			var styleRowHead = StyleRowHead(sl);

			var styleMoney = StyleMoney(sl);
			var styleDate = StyleDate(sl);

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'I';
			var colMaxIdx = ColLetterToNumber(colMax);

			SLD_ResizeColumn(sl, "A", 14);
			SLD_ResizeColumn(sl, "B", 18);

			var income = new List<TransactionDetail>();

			foreach (var b in brokers) {
				Console.WriteLine($"\tCreating income history {b.AccountIdentity} ==========");

				foreach (var q in b.QuarterRows) {
					income = income.Union(q.IncomeDetails).ToList();
				}
			}

			int row = 1;

			for (int m = 1; m <= 12; m++) {
				var monthStart = new DateTime(year, m, 1);
				var monthEnd = GetEndOfMonthByDate(monthStart);

				Console.WriteLine($"\tTabulating income for {monthStart.ToString("MMMM yyyy")} ==========");

				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
				sl.SetCellValue($"{colFirst}{row}", $"{monthStart.ToString("MMMM yyyy")}");
				SLD_ResizeRow(sl, row, 20);
				row++;

				//income for the month
				var monthIncome = income.Where(x => x.TransactionDate >= monthStart && x.TransactionDate <= monthEnd).ToList();

				var sortedIncome = monthIncome
										.GroupBy(i => i.SecuritySymbol)
										.Select(g => new {
											Ticker = g.Key,
											TotalAmount = g.Sum(i => i.TransactionAmount)
										})
										.OrderByDescending(x => x.TotalAmount)
										.ToList();

				foreach (var inc in sortedIncome) {
					sl.SetCellValue($"A{row}", inc.Ticker);
					sl.SetCellValue($"I{row}", inc.TotalAmount);

					sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleHead);
					sl.SetCellStyle($"I{row}", styleMoney);

					SLD_ResizeColumn(sl, "I", 18);

					row++;

					var tickerIncome = monthIncome.Where(x => x.SecuritySymbol.ToUpperInvariant() == inc.Ticker.ToUpperInvariant());

					var rowct = tickerIncome.Count();
					sl.SetCellStyle(row, colFirstIdx, (row + rowct + 3), colMaxIdx, stylePlain);

					foreach (var mi in tickerIncome.OrderBy(x => x.BrokerIdentity)
											.OrderBy(x => x.AccountIdentity)
											.OrderBy(x => x.TransactionType)
											.OrderBy(x => x.TransactionDate)
											.OrderByDescending(x => x.TransactionAmount)) {
						sl.SetCellValue($"B{row}", mi.BrokerIdentity);
						sl.SetCellValue($"C{row}", mi.AccountIdentity);
						sl.SetCellValue($"D{row}", mi.SecuritySymbol);

						sl.SetCellValue($"F{row}", mi.TransactionType.GetDescription());
						sl.SetCellValue($"G{row}", mi.TransactionDate);
						sl.SetCellValue($"H{row}", mi.TransactionAmount);

						sl.SetCellStyle($"G{row}", styleDate);
						sl.SetCellStyle($"H{row}", styleMoney);

						row++;
					}
				}

				row = row + 3;
			}

			SLD_ResizeColumn(sl, "B", 18);
			SLD_ResizeColumn(sl, "C", 18);
			SLD_ResizeColumn(sl, "D", 18);
			SLD_ResizeColumn(sl, "E", 18);
			SLD_ResizeColumn(sl, "F", 18);
			SLD_ResizeColumn(sl, "G", 18);
			SLD_ResizeColumn(sl, "H", 18);
			SLD_ResizeColumn(sl, "I", 18);

			return sl;
		}

		protected SLDocument ReportSalesTotal(SLDocument sl) {
			sl.SelectWorksheet(Sheet_SalesTotal);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			int row = 0;

			var stylePlain = StylePlain(sl);
			var styleMainHead = StyleH2(sl);

			var styleHead = StyleHeadBase(sl);
			styleHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleHead.Border.BottomBorder.Color = Color.Black;

			var styleRowHead = StyleRowHead(sl);

			var styleMoney = StyleMoney(sl);

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'N';
			var colMaxIdx = ColLetterToNumber(colMax);

			row = 1;
			sl.SetCellValue($"A{row}", "Sales By Month");
			sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
			SLD_ResizeRow(sl, row, 20);

			row++;
			sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleHead);
			sl.SetCellValue($"B{row}", "Account");
			SLD_ResizeRow(sl, row, 18);

			row++;
			foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", stylePlain);

				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", stylePlain);

				sl.SetCellValue($"A{row}", b.BrokerIdentity);
				sl.SetCellValue($"B{row}", b.AccountIdentity + " (gains)");

				sl.SetCellStyle($"A{row}", styleRowHead);
				sl.SetCellStyle($"C{row}", $"N{row}", styleMoney);

				row++;

				sl.SetCellValue($"A{row}", b.BrokerIdentity);
				sl.SetCellValue($"B{row}", b.AccountIdentity + " (proceeds)");

				sl.SetCellStyle($"A{row}", styleRowHead);
				sl.SetCellStyle($"C{row}", $"N{row}", styleMoney);

				row++;

				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", stylePlain);

				sl.SetCellValue($"A{row}", b.BrokerIdentity);
				sl.SetCellValue($"B{row}", b.AccountIdentity + " (cost)");

				sl.SetCellStyle($"A{row}", styleRowHead);
				sl.SetCellStyle($"C{row}", $"N{row}", styleMoney);

				row++;
			}

			for (int m = 1; m <= 12; m++) {
				row = 2;
				var colMonthIdx = colFirstIdx + m + 1;
				var colMonth = ColNumberToLetter(colMonthIdx);

				var monthStart = new DateTime(year, m, 1);
				var monthEnd = GetEndOfMonthByDate(monthStart);

				Console.WriteLine($"\tTabulating sales totals for {monthStart.ToString("MMMM yyyy")} ==========");

				sl.SetCellValue($"{colMonth}{row}", $"{monthStart.ToString("MMM yyyy")}");

				row++;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					var accountIncome = b.GainLossRows.Where(x => x.DateClosed >= monthStart && x.DateClosed <= monthEnd).ToList();

					var gainLoss = accountIncome.Sum(x => x.GainLoss);
					var proceeds = accountIncome.Sum(x => x.Proceeds);
					var costBasis = accountIncome.Sum(x => x.CostBasis);

					sl.SetCellValue($"{colMonth}{row}", gainLoss);
					sl.SetCellStyle($"{colMonth}{row}", styleMoney);
					row++;

					sl.SetCellValue($"{colMonth}{row}", proceeds);
					sl.SetCellStyle($"{colMonth}{row}", styleMoney);
					row++;

					sl.SetCellValue($"{colMonth}{row}", costBasis);
					sl.SetCellStyle($"{colMonth}{row}", styleMoney);
					row++;
				}

				SLD_ResizeColumn(sl, colMonth, 16);
			}

			SLD_ResizeColumn(sl, "A", 16);
			SLD_ResizeColumn(sl, "B", 18);

			var accountColors = new Color[] { _color2, _color4, _color6, _color1, _color3, _color5 };
			var colorGold = new string[] { "#ffa600", "#ed9200", "#da7f00", "#c76c01", "#b35a00", "#9f4900" };
			var colorBlues = new string[] { "#04198f", "#382e96", "#53439c", "#6958a3", "#7e6ea9", "#9185af" };
			var colorGreens = new string[] { "#3a9c2d", "#569e47", "#6ba05e", "#7ea274", "#90a38a" };
			var colorReds = new string[] { "#ae2d19", "#c34931", "#d86349", "#eb7d61", "#ff967b" };

			int lastRow = 2 + (brokerCount * 3);

			var options = new SLCreateChartOptions() { RowsAsDataSeries = true };
			var chart = sl.CreateChart(2, 2, lastRow, colMaxIdx, options);

			chart.SetChartType(SLColumnChartType.ClusteredColumn);

			for (int i = 1; i <= brokerCount; i++) {
				var color = accountColors[i % accountColors.Length];

				var ccBlue = colorBlues[i % colorBlues.Length];
				//var ccGreen = colorGreens[i % colorGreens.Length];
				//var ccRed = colorReds[i % colorReds.Length];
				var ccGold = colorGold[i % colorGold.Length];

				var blue = ColorTranslator.FromHtml(ccBlue);
				//var green = ColorTranslator.FromHtml(ccGreen);
				//var red = ColorTranslator.FromHtml(ccRed);
				var gold = ColorTranslator.FromHtml(ccGold);

				var colorLine = gold;

				int ixGain = (i * 3) - 2;
				var optGain = chart.GetDataSeriesOptions(ixGain);
				optGain.Fill.SetSolidFill(colorLine, 0);
				optGain.Line.SetSolidLine(colorLine, 0);
				optGain.Line.Width = (decimal)2;
				optGain.Marker.Fill.SetSolidFill(colorLine, 0);
				optGain.Marker.Line.SetSolidLine(colorLine, 0);
				optGain.Marker.Symbol = MarkerStyleValues.Diamond;
				chart.SetDataSeriesOptions(ixGain, optGain);

				int ixProc = (i * 3) - 1;
				var optProc = chart.GetDataSeriesOptions(ixProc);
				optProc.Fill.SetSolidFill(color, 0);
				optProc.Line.SetSolidLine(color, 0);
				chart.SetDataSeriesOptions(ixProc, optProc);

				int ixCost = (i * 3) - 0;
				var optCost = chart.GetDataSeriesOptions(ixCost);
				optCost.Fill.SetSolidFill(blue, 0);
				optCost.Line.SetSolidLine(blue, 0);
				chart.SetDataSeriesOptions(ixCost, optCost);

				chart.PlotDataSeriesAsSecondaryLineChart(ixGain, SLChartDataDisplayType.Normal, false);
			}

			chart.ShowChartLegend(LegendPositionValues.Right, false);

			chart.SetChartPosition((lastRow + 2), 1, (lastRow + 25), 12);
			sl.InsertChart(chart);

			return sl;
		}

		protected SLDocument ReportSales(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Sales);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			var stylePlain = StylePlain(sl);

			var styleShort = StyleCenteredBackground(sl, _colorShort);
			var styleLong = StyleCenteredBackground(sl, _colorLong);

			var styleHead = StyleHeadBase(sl);
			styleHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleHead.Border.BottomBorder.Color = Color.Black;

			var styleMainHead = StyleH2(sl);

			var styleRowHead = StyleRowHead(sl);

			var styleMoney = StyleMoney(sl);

			var styleDate = StyleDate(sl);

			var colFirst = 'A';
			var colFirstIdx = ColLetterToNumber(colFirst);
			var colMax = 'J';
			var colMaxIdx = ColLetterToNumber(colMax);

			SLD_ResizeColumn(sl, "A", 14);
			SLD_ResizeColumn(sl, "B", 18);

			var row = 1;
			foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
				if (row > 1) {
					//create a gap between brokerages
					row = row + 2;
				}
				var tickers = b.GainLossRows.Select(x => x.SecuritySymbol.ToUpperInvariant()).OrderBy(x => x).Distinct().ToList();

				Console.WriteLine($"\tCreating sale history {b.AccountIdentity} ==========");

				sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleMainHead);
				sl.SetCellValue($"{colFirst}{row}", $"{b.BrokerIdentity} : {b.AccountIdentity}");
				SLD_ResizeRow(sl, row, 20);

				row++;

				foreach (var ticker in tickers) {
					var saleRows = b.GainLossRows.Where(x => x.SecuritySymbol.ToUpperInvariant() == ticker)
						.OrderBy(x => x.Quantity)
						.OrderBy(x => x.DateOpened)
						.OrderBy(x => x.DateClosed).ToList();
					//var tranRows = b.TransactionRows
					//		.Where(x => x.SecuritySymbol == ticker && (x.TransactionType == TransactionType.Journal || x.TransactionType == TransactionType.Sell))
					//		.OrderBy(x => x.TransactionDate).ToList();

					var rowct = saleRows.Count();

					sl.SetCellStyle(row, colFirstIdx, (row + rowct + 3), colMaxIdx, stylePlain);

					var desc = string.Empty;
					var det = saleRows.FirstOrDefault();
					desc = ((ticker.Length >= 6 || ticker.HasDigits()) && det != null) ? det.SecurityDescription : string.Empty;

					sl.SetCellValue($"A{row}", ticker);
					sl.SetCellValue($"B{row}", desc);
					sl.SetCellValue($"C{row}", "Date Opened");
					sl.SetCellValue($"D{row}", "Date Closed");
					sl.SetCellValue($"E{row}", "Quantity");
					sl.SetCellValue($"F{row}", "Unit Cost");
					sl.SetCellValue($"G{row}", "Unit Proceeds");
					sl.SetCellValue($"H{row}", "Proceeds");
					sl.SetCellValue($"I{row}", "Gain/Loss");
					sl.SetCellValue($"J{row}", "Term");

					sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleHead);

					row++;
					foreach (var g in saleRows) {
						sl.SetCellValue($"B{row}", g.SecuritySymbol);
						sl.SetCellStyle($"B{row}", styleRowHead);

						sl.SetCellValue($"C{row}", g.DateOpened);
						sl.SetCellStyle($"C{row}", styleDate);
						sl.SetCellValue($"D{row}", g.DateClosed);
						sl.SetCellStyle($"D{row}", styleDate);

						sl.SetCellValue($"E{row}", g.Quantity);

						sl.SetCellValue($"F{row}", g.UnitCost);
						sl.SetCellStyle($"F{row}", styleMoney);

						sl.SetCellValue($"G{row}", g.UnitProceeds);
						sl.SetCellStyle($"G{row}", styleMoney);

						sl.SetCellValue($"H{row}", g.Proceeds);
						sl.SetCellStyle($"H{row}", styleMoney);

						sl.SetCellValue($"I{row}", g.GainLoss);
						sl.SetCellStyle($"I{row}", styleMoney);

						sl.SetCellValue($"J{row}", g.GainLossType.GetDescription());
						var stLongShort = g.GainLossType == GainLossType.Long ? styleLong : styleShort;
						sl.SetCellStyle($"J{row}", stLongShort);

						row++;
					}

					SLD_ResizeColumn(sl, "C", 18);
					SLD_ResizeColumn(sl, "D", 18);
					SLD_ResizeColumn(sl, "E", 18);
					SLD_ResizeColumn(sl, "F", 18);
					SLD_ResizeColumn(sl, "G", 18);
					SLD_ResizeColumn(sl, "H", 18);
					SLD_ResizeColumn(sl, "I", 18);
					SLD_ResizeColumn(sl, "J", 18);

					row++;
				}
			}

			return sl;
		}

		protected SLDocument UpdateTaxInfo(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Washed);
			var brokers = this.BrokerSummaries;
			int brokerCount = brokers.Count();
			var year = this.Year;

			var taxYearData = TaxYearData.Load(year);

			if (taxYearData == null
						|| taxYearData.Quarters.Count == 0
						|| taxYearData.TaxRates.Count == 0) {
				return sl;
			}

			var stylePlain = StylePlain(sl);
			stylePlain.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.TopBorder.Color = Color.Black;
			stylePlain.Border.BottomBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.BottomBorder.Color = Color.Black;
			stylePlain.Border.LeftBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.LeftBorder.Color = Color.Black;
			stylePlain.Border.RightBorder.BorderStyle = BorderStyleValues.Thin;
			stylePlain.Border.RightBorder.Color = Color.Black;

			var styleHead = StyleHeadBase(sl);
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;

			var styleRowHead = StyleRowHead(sl);

			var styleDate = StyleDate(sl);

			var styleMoney = StyleMoney(sl);

			var styleMoneyAttention = StyleMoney(sl);
			styleMoneyAttention.Font.Italic = true;

			var stylePerc = StylePercent(sl);

			var colLastBrIdx = brokerCount + 1;
			var colSubTotIdx = brokerCount + 2;
			var colTaxIdx = brokerCount + 3;
			var colLastBr = ColNumberToLetter(colLastBrIdx);
			var colSubTot = ColNumberToLetter(colSubTotIdx);
			var colTax = ColNumberToLetter(colTaxIdx);

			var colTaxRateLblIdx = colTaxIdx + 4;
			var colTaxValue1Idx = colTaxIdx + 5;
			var colTaxValue2Idx = colTaxIdx + 6;
			var colTaxRateLbl = ColNumberToLetter(colTaxRateLblIdx);
			var colTaxValue1 = ColNumberToLetter(colTaxValue1Idx);
			var colTaxValue2 = ColNumberToLetter(colTaxValue2Idx);

			var widthSubTot = sl.GetColumnWidth(colSubTotIdx);
			sl.SetColumnWidth(colTaxIdx, widthSubTot);

			for (int i = 1; i <= _maxIncomeRows; i++) {
				var styleRowTmp = sl.GetCellStyle(i, colSubTotIdx);
				sl.SetCellStyle(i, colTaxIdx, styleRowTmp);
			}

			for (int c = 1; c <= 3; c++) {
				var colSpacerIdx = colTaxIdx + c;
				var colSpacer = ColNumberToLetter(colSpacerIdx);
				sl.SetColumnWidth(colSpacer, 6);
			}

			var subhead = _starterRowQuarters;

			sl.SetCellStyle(subhead, colTaxRateLblIdx, subhead, colTaxValue1Idx, styleHead);
			sl.SetCellStyle((subhead + 1), colTaxRateLblIdx, (subhead + 4), colTaxValue1Idx, stylePlain);
			sl.SetCellValue($"{colTaxRateLbl}{subhead}", "Rates");

			if (taxYearData.TaxRates.Any()) {
				double rt = 0.25;
				int taxRateRow = _starterRowQuarters + 1;

				foreach (var incType in _incomeTypes) {
					var keyDesc = incType.GetDescription();
					rt = 0.25;

					var taxRate = taxYearData.TaxRates.Where(x => x.IncomeType == incType).FirstOrDefault();

					if (taxRate != null) {
						var rateNbr = taxRate.Percentage ?? 20;
						rt = (rateNbr > 1.00) ? (rateNbr / 100.00) : rateNbr;
					}

					sl.SetCellValue($"{colTaxRateLbl}{taxRateRow}", keyDesc);
					sl.SetCellValue($"{colTaxValue1}{taxRateRow}", rt);

					sl.SetCellStyle($"{colTaxRateLbl}{taxRateRow}", styleRowHead);
					sl.SetCellStyle($"{colTaxValue1}{taxRateRow}", stylePerc);

					taxRateRow++;
				}

				// 4 quarters + annual
				for (int q = 1; q <= 5; q++) {
					var quarterTaxRow = _starterRowQuarters + 1 + ((q - 1) * _quarterRowGap);
					sl.SetCellValue($"{colTax}{quarterTaxRow - 1}", "Tax Est.");

					for (int r = 1; r <= _incomeTypes.Count(); r++) {
						taxRateRow = _starterRowQuarters + r;
						string formulaTax = $"={colSubTot}{quarterTaxRow}*{colTaxValue1}${taxRateRow}";
						sl.SetCellValue($"{colTax}{quarterTaxRow}", formulaTax);
						sl.SetCellStyle($"{colTax}{quarterTaxRow}", styleMoney);
						quarterTaxRow++;
					}

					string formultotalCol = $"=SUM({colTax}{quarterTaxRow - 4}:{colTax}{quarterTaxRow - 1})";
					sl.SetCellValue($"{colTax}{quarterTaxRow}", formultotalCol);
				}
			}

			var taxSpacing = 8;
			var taxRateRow1 = 0;
			var taxRateRow2 = 0;
			var subhead1 = _starterRowQuarters + taxSpacing;
			var subhead2 = subhead1 + taxSpacing;

			sl.SetCellStyle(subhead1, colTaxRateLblIdx, subhead1, colTaxValue2Idx, styleHead);
			sl.SetCellStyle((subhead1 + 1), colTaxRateLblIdx, (subhead1 + 5), colTaxValue2Idx, stylePlain);
			sl.SetCellValue($"{colTaxRateLbl}{subhead1}", $"Prepaid Est Tax {year}");
			sl.SetCellValue($"{colTaxValue1}{subhead1}", "Date");
			sl.SetCellValue($"{colTaxValue2}{subhead1}", "Amount");

			sl.SetCellStyle(subhead2, colTaxRateLblIdx, subhead2, colTaxValue1Idx, styleHead);
			sl.SetCellStyle((subhead2 + 1), colTaxRateLblIdx, (subhead2 + 5), colTaxValue1Idx, stylePlain);
			sl.SetCellValue($"{colTaxRateLbl}{subhead2}", $"Payroll {year}");
			sl.SetCellValue($"{colTaxValue1}{subhead2}", "Amount");

			if (taxYearData != null && taxYearData.Quarters.Count() == 4) {
				DateTime? paydate = null;
				decimal est = 0;
				decimal pay = 0;

				for (int q = 1; q <= 4; q++) {
					var yearQuarter = taxYearData.Quarters.Where(x => x.Quarter == q).FirstOrDefault();
					taxRateRow1 = subhead1 + q;
					taxRateRow2 = subhead2 + q;

					if (yearQuarter != null) {
						paydate = yearQuarter.DateOfPayment;
						est = yearQuarter.EstPayment;
						pay = yearQuarter.Payroll;
					} else {
						paydate = null;
						est = 0;
						pay = 0;
					}

					sl.SetCellValue($"{colTaxRateLbl}{taxRateRow1}", $"Q{q}");
					if (paydate != null && paydate != DateTime.MinValue) {
						sl.SetCellValue($"{colTaxValue1}{taxRateRow1}", (DateTime)paydate);
					}
					sl.SetCellValue($"{colTaxValue2}{taxRateRow1}", est);

					sl.SetCellStyle($"{colTaxRateLbl}{taxRateRow1}", styleRowHead);
					sl.SetCellStyle($"{colTaxValue1}{taxRateRow1}", styleDate);
					sl.SetCellStyle($"{colTaxValue2}{taxRateRow1}", styleMoney);

					sl.SetCellValue($"{colTaxRateLbl}{taxRateRow2}", $"Q{q}");
					sl.SetCellValue($"{colTaxValue1}{taxRateRow2}", pay);

					sl.SetCellStyle($"{colTaxRateLbl}{taxRateRow2}", styleRowHead);
					sl.SetCellStyle($"{colTaxValue1}{taxRateRow2}", styleMoney);

					taxRateRow1++;
					taxRateRow2++;

					if (q == 4) {
						string formula1 = $"=SUM({colTaxValue2}{taxRateRow1 - 4}:{colTaxValue2}{taxRateRow1 - 1})";
						string formula2 = $"=SUM({colTaxValue1}{taxRateRow2 - 4}:{colTaxValue1}{taxRateRow2 - 1})";

						sl.SetCellValue($"{colTaxValue1}{taxRateRow1}", "Total");
						sl.SetCellValue($"{colTaxValue2}{taxRateRow1}", formula1);

						sl.SetCellStyle($"{colTaxValue1}{taxRateRow1}", styleRowHead);
						sl.SetCellStyle($"{colTaxValue2}{taxRateRow1}", styleMoney);

						sl.SetCellValue($"{colTaxRateLbl}{taxRateRow2}", "Total");
						sl.SetCellValue($"{colTaxValue1}{taxRateRow2}", formula2);

						sl.SetCellStyle($"{colTaxRateLbl}{taxRateRow2}", styleRowHead);
						sl.SetCellStyle($"{colTaxValue1}{taxRateRow2}", styleMoney);
					}
				}

				// 4 quarters + annual
				for (int q = 1; q <= 5; q++) {
					taxRateRow1 = subhead1 + q;
					taxRateRow2 = subhead2 + q;
					var quarterTaxRow = _starterRowQuarters + 6 + ((q - 1) * _quarterRowGap);

					sl.SetCellValue($"{colSubTot}{quarterTaxRow}", "Prepaid");
					sl.SetCellStyle($"{colSubTot}{quarterTaxRow}", styleRowHead);
					sl.SetCellValue($"{colSubTot}{quarterTaxRow + 1}", "Payroll");
					sl.SetCellStyle($"{colSubTot}{quarterTaxRow + 1}", styleRowHead);

					if (q <= 4) {
						sl.SetCellValue($"{colSubTot}{quarterTaxRow + 2}", $"Est Q {q} Tax");
					} else {
						sl.SetCellValue($"{colSubTot}{quarterTaxRow + 2}", "Est Annual Tax");
					}
					sl.SetCellStyle($"{colSubTot}{quarterTaxRow + 2}", styleRowHead);

					if (q > 1 && q < 5) {
						sl.SetCellValue($"{colSubTot}{quarterTaxRow + 3}", "Running");
						sl.SetCellStyle($"{colSubTot}{quarterTaxRow + 3}", styleRowHead);
					}

					string formula1 = $"={colTaxValue2}${taxRateRow1}";
					string formula2 = $"={colTaxValue1}${taxRateRow2}";
					string formula3 = $"={colTax}{quarterTaxRow - 1}-({colTax}{quarterTaxRow}+{colTax}{quarterTaxRow + 1})";

					sl.SetCellValue($"{colTax}{quarterTaxRow}", formula1);
					sl.SetCellStyle($"{colTax}{quarterTaxRow}", styleMoney);

					sl.SetCellValue($"{colTax}{quarterTaxRow + 1}", formula2);
					sl.SetCellStyle($"{colTax}{quarterTaxRow + 1}", styleMoney);

					sl.SetCellValue($"{colTax}{quarterTaxRow + 2}", formula3);
					sl.SetCellStyle($"{colTax}{quarterTaxRow + 2}", styleMoney);

					if (q > 1 && q < 5) {
						string formula4 = $"={colTax}{quarterTaxRow + 2}";
						for (int qq = 1; qq < q; qq++) {
							formula4 = formula4 + $"+{colTax}{quarterTaxRow + 2 - (_quarterRowGap * qq)}";
						}
						sl.SetCellValue($"{colTax}{quarterTaxRow + 3}", formula4);
						sl.SetCellStyle($"{colTax}{quarterTaxRow + 3}", styleMoney);
					}
				}
			}

			SLD_ResizeColumn(sl, colTax, 18);
			SLD_ResizeColumn(sl, colTaxRateLbl, 20);
			SLD_ResizeColumn(sl, colTaxValue1, 16);
			SLD_ResizeColumn(sl, colTaxValue2, 16);

			return sl;
		}

		protected SLDocument CreateWashData(SLDocument sl) {
			sl.SelectWorksheet(Sheet_Washes);
			var brokers = this.BrokerSummaries;

			var stylePlain = StylePlain(sl);

			var styleHead = StyleHeadBase(sl);
			styleHead.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleHead.Border.TopBorder.Color = Color.Black;
			styleHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleHead.Border.BottomBorder.Color = Color.Black;

			var styleMainHead = StyleH2(sl);

			var styleMoney = StyleMoney(sl);
			var stylePercent = StylePercent(sl);

			var styleDate = StyleDate(sl);

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
					var quarterWash = qr.WashMatches
								.OrderByDescending(x => x.GainLossRow.Proceeds)
								.OrderByDescending(x => x.GainLossRow.Quantity)
								.OrderBy(x => x.GainLossRow.DateClosed)
								.OrderBy(x => x.GainLossRow.SecuritySymbol);

					sl.SetCellStyle(row, colFirstIdx, (row + 3), colMaxIdx, stylePlain);

					foreach (var match in quarterWash) {
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

						sl.SetCellValue($"C{row}", match.GainLossRow.DateClosed);
						sl.SetCellStyle($"C{row}", styleDate);

						sl.SetCellValue($"D{row}", match.GainLossRow.GainLossType.ToString());

						sl.SetCellValue($"E{row}", match.GainLossRow.Quantity.ToString());

						sl.SetCellValue($"F{row}", match.GainLossRow.GainLoss);
						sl.SetCellStyle($"F{row}", styleMoney);

						sl.SetCellValue($"G{row}", lossAllowed);
						sl.SetCellStyle($"G{row}", styleMoney);

						sl.SetCellValue($"H{row}", fracAllowed);
						sl.SetCellStyle($"H{row}", stylePercent);

						sl.SetCellValue($"I{row}", adjustment);
						sl.SetCellStyle($"I{row}", styleMoney);

						sl.SetCellStyle($"{colFirst}{row}", $"{colMax}{row}", styleHead);
						SLD_ResizeRow(sl, row, 18);

						SLD_ResizeColumn(sl, "B", 18);
						SLD_ResizeColumn(sl, "C", 18);
						SLD_ResizeColumn(sl, "D", 18);
						SLD_ResizeColumn(sl, "E", 12);
						SLD_ResizeColumn(sl, "F", 18);
						SLD_ResizeColumn(sl, "G", 12);
						SLD_ResizeColumn(sl, "H", 12);
						SLD_ResizeColumn(sl, "I", 12);

						sl.SetCellStyle(row, colFirstIdx, (row + washes.Count + 5), colMaxIdx, stylePlain);

						var washMsg = $"{washShares} alternate shares purchased,"
								+ (lossAllowed == 0 ? $" entire loss disallowed" :
								" loss limited to " + (lotCount == 1 ? $"{fracAllowed:P2}" : $"{adjProportionLost:P2} ({fracAllowed:P2} adjusted by {proportionLoss:P2} due to {lotCount} lots)"))
								+ $" - {lossAllowed:C2} max loss, add back {adjustment:C2} ";

						row++;
						sl.SetCellValue($"{colFirst}{row}", washMsg);
						sl.SetCellStyle($"{colFirst}{row}", stylePlain);
						SLD_ResizeRow(sl, row, 18);

						foreach (var w in washes.OrderBy(x => x.AccountIdentity).OrderBy(x => x.BrokerIdentity)) {
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
			var year = this.Year;

			var stylePlain = StylePlain(sl);

			var stylePlainAttention = StylePlain(sl);
			stylePlainAttention.Font.Italic = true;

			var styleRowHead = StyleRowHead(sl);

			var styleSubTot = sl.CreateStyle();
			styleSubTot.Alignment.Horizontal = HorizontalAlignmentValues.Right;
			styleSubTot.Font.Bold = true;
			styleSubTot.Font.FontColor = Color.Black;
			styleSubTot.Font.FontSize = _baseFont;
			styleSubTot.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
			styleSubTot.Border.TopBorder.Color = Color.Black;
			styleSubTot.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
			styleSubTot.Border.BottomBorder.Color = Color.Black;

			var styleQuarterHead = StyleCenteredBackground(sl, _color1);

			var styleYearHead = StyleCenteredBackground(sl, _color3);

			var styleMainHead = StyleH2(sl);

			var styleDate = StyleDate(sl);

			var styleMoney = StyleMoney(sl);

			var styleMoneyAttention = StyleMoney(sl);
			styleMoneyAttention.Font.Italic = true;

			var quarter = 1;
			var subhead = _starterRowQuarters;

			var colSubTotIdx = brokerCount + 2;
			var colBrkrLastIdx = brokerCount + 1;
			var colBrkrLast = ColNumberToLetter(colBrkrLastIdx);
			var colSubTot = ColNumberToLetter(colSubTotIdx);
			var colA = ColLetterToNumber('A');
			var colB = ColLetterToNumber('B');

			SLD_ResizeColumn(sl, "A", 22);

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

				for (var i = 0; i < _incomeTypes.Count(); i++) {
					sl.SetCellValue($"A{subhead + 1 + i}", _incomeTypes[i].GetDescription());
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
				var colSumIdx = colB;

				foreach (var b in brokers.OrderByDescending(x => x.GrandTotal)) {
					sl.SetCellValue((subhead - 1), colSumIdx, b.BrokerIdentity);
					sl.SetCellValue(subhead, colSumIdx, b.AccountIdentity);
					var totals = b.QuarterRows.Where(x => x.Quarter == quarter).FirstOrDefault();

					var colSum = ColNumberToLetter(colSumIdx);
					string formulaSubtotalCol = $"=SUM({colSum}{subhead + 1}:{colSum}{subhead + 4})";
					sl.SetCellValue((subhead + 5), colSumIdx, formulaSubtotalCol);
					sl.SetCellStyle((subhead + 5), colSumIdx, styleMoney);

					if (quarter == 5) {
						// for each tallied quarter & row within the quarter
						for (int qq = 1; qq <= 4; qq++) {
							var qFormulaIdx = qq + _starterRowQuarters;
							var yrTotalFormula = $"={colSum}{qFormulaIdx}";

							for (int qr = 1; qr <= 3; qr++) {
								qFormulaIdx = qFormulaIdx + _quarterRowGap;
								yrTotalFormula = yrTotalFormula + $"+{colSum}{qFormulaIdx}";
							}

							sl.SetCellValue((subhead + qq), colSumIdx, yrTotalFormula);
							sl.SetCellStyle((subhead + qq), colSumIdx, styleMoney);
						}
					}

					if (totals == null) {
						totals = new QuarterRow();
					}

					if (totals != null && quarter <= 4) {
						if (totals.QuarterStartDate != DateTime.MinValue) {
							if (totals.QuarterStartDate > DateTime.Now.Date) {
								sl.SetCellValue($"A{subhead + 6}", "* Future Dates Out Of Range");
								sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
							}
							if (totals.QuarterStartDate <= DateTime.Now.Date && totals.QuarterEndDate >= DateTime.Now.Date) {
								sl.SetCellValue($"A{subhead + 6}", "* Quarter Not Closed");
								sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
							}
						}

						var rowIncome = subhead + 1;
						foreach (var inc in _incomeTypes) {
							var income = totals.QuarterlyTotalRows.Where(x => x.IncomeType == inc).FirstOrDefault();
							sl.SetCellStyle(rowIncome, colSumIdx, stylePlain);
							sl.SetCellStyle(rowIncome, colSumIdx, styleMoney);
							if (income != null) {
								if (isWashedSheet) {
									if (income.Adjustment != 0) {
										sl.SetCellStyle(rowIncome, colSumIdx, styleMoneyAttention);
									}
									sl.SetCellValue(rowIncome, colSumIdx, income.TotalIncome);
								} else {
									sl.SetCellValue(rowIncome, colSumIdx, income.Income);
								}
							} else {
								sl.SetCellValue(rowIncome, colSumIdx, 0);
							}
							rowIncome++;
						}

						if (totals.QuarterlyTotalRows.Any(x => x.Adjustment != 0)) {
							if (isWashedSheet) {
								sl.SetCellValue($"A{subhead + 6}", "* Wash sales reflected in above totals");
							} else {
								sl.SetCellValue($"A{subhead + 6}", "* Wash sales may affect above totals");
							}
							sl.SetCellStyle($"A{subhead + 6}", stylePlainAttention);
						}
					}
					colSumIdx++;
				}

				quarter++;
				subhead = subhead + _quarterRowGap;
			}

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