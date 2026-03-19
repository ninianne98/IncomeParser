using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using SpreadsheetLight;
using Color = System.Drawing.Color;

public class ParserWorkerBee {
	protected static IConfiguration _configuration; //= new ConfigurationBuilder().Build();
	protected static DateTime _date = DateTime.Now;

	public static IConfiguration Configuration {
		get {
			LoadConfig();

			return _configuration;
		}
	}

	private static void LoadConfig() {
		if (_configuration == null) {
			_configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.Build();
		}
	}

	public static DateTime AppDateTime {
		get {
			return _date;
		}
	}

	public static string OutputCSV {
		get {
			return string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.csv", AppDateTime);
		}
	}

	public static string OutputReport {
		get {
			return string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.txt", AppDateTime);
		}
	}

	public static string OutputReportExcel {
		get {
			return string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.xlsx", AppDateTime);
		}
	}

	public ParserWorkerBee() {
		LoadConfig();

		_date = DateTime.Now;
	}

	public void RunParser() {
		string settingFolder = _configuration["MainDocumentFolder"] ?? string.Empty;
		Console.WriteLine($"Main Document Folder : {settingFolder}");

		var files = Directory.GetFiles(settingFolder, "*.csv", SearchOption.AllDirectories).ToList()
						.Select(x => new FileInfo(x))
						.Where(x => x.Name.StartsWith("Statement_") == false && x.DirectoryName != settingFolder).ToList();

		var documents = new List<IFileCoreData>();
		var factory = new BrokerFileFactory();

		foreach (var file in files) {
			//Console.WriteLine($"\t\tFile : {file.FullName}");
			var fd = factory.GenerateFileData(file);
			fd.ParseFile();
			documents.Add(fd);
		}

		//PrintOutput(documents);

		var brokers = documents.Where(x => (x is IAccountGainLoss))
						.Select(x => new BrokerSummary(x.BrokerIdentity, x.AccountIdentity)).ToList();

		foreach (var b in brokers) {
			b.LoadData(documents);
		}

		Thread.Sleep(250);
		string fileNameCSV = Path.Join(settingFolder, OutputCSV);
		string fileNameTxt = Path.Join(settingFolder, OutputReport);

		File.WriteAllText(fileNameCSV, string.Empty);
		File.WriteAllText(fileNameTxt, string.Empty);
		Thread.Sleep(250);

		foreach (var b in brokers) {
			Console.WriteLine("=====================================================");

			b.PrintOutput();
		}

		GenerateReport(brokers);

		Console.WriteLine("=====================================================");
	}

	public void GenerateReport(IEnumerable<BrokerSummary> brokers) {
		int brokerCount = brokers.Count();
		var year = brokers.Max(x => x.Year);
		int baseFont = 12;

		string settingFolder = _configuration["MainDocumentFolder"] ?? string.Empty;
		string fileName = Path.Join(settingFolder, OutputReportExcel);

		using (var ms = new MemoryStream()) {
			using (var sl = new SLDocument()) {
				var stylePlain = sl.CreateStyle();
				stylePlain.Font.Bold = false;
				stylePlain.Font.FontColor = Color.Black;
				stylePlain.Font.FontSize = baseFont;

				var styleRowHead = sl.CreateStyle();
				styleRowHead.Alignment.Horizontal = HorizontalAlignmentValues.Right;
				styleRowHead.Font.Bold = true;
				styleRowHead.Font.FontColor = Color.Black;
				styleRowHead.Font.FontSize = baseFont;

				var styleSubTot = sl.CreateStyle();
				styleSubTot.Alignment.Horizontal = HorizontalAlignmentValues.Right;
				styleSubTot.Font.Bold = true;
				styleSubTot.Font.FontColor = Color.Black;
				styleSubTot.Font.FontSize = baseFont;
				styleSubTot.Border.TopBorder.BorderStyle = BorderStyleValues.Thin;
				styleSubTot.Border.TopBorder.Color = Color.Black;
				styleSubTot.Border.BottomBorder.BorderStyle = BorderStyleValues.Thick;
				styleSubTot.Border.BottomBorder.Color = Color.Black;

				var styleQuarterHead = sl.CreateStyle();
				styleQuarterHead.Alignment.Horizontal = HorizontalAlignmentValues.Center;
				styleQuarterHead.Alignment.Vertical = VerticalAlignmentValues.Bottom;
				styleQuarterHead.Font.Bold = true;
				styleQuarterHead.Font.FontSize = baseFont;
				styleQuarterHead.Fill.SetPattern(PatternValues.Solid, Color.DarkSeaGreen, Color.Transparent);

				var styleMainHead = sl.CreateStyle();
				styleMainHead.Font.Bold = true;
				styleMainHead.Font.FontColor = Color.Black;
				styleMainHead.Font.FontSize = baseFont + 4;
				styleMainHead.Border.BottomBorder.BorderStyle = BorderStyleValues.Thin;
				styleMainHead.Border.BottomBorder.Color = Color.Black;

				var styleMoney = sl.CreateStyle();
				styleMoney.FormatCode = "$#,##0.00;[Red]($#,##0.00)";

				var subhead = 4;
				var quarter = 1;
				var colA = GetColIndex('A');
				var colB = GetColIndex('B');

				sl.SetCellStyle(1, 1, 60, (brokerCount + 2), stylePlain);

				var incTypes = new IncomeType[] { IncomeType.LongTermCG, IncomeType.ShortTermGG, IncomeType.Dividend, IncomeType.Interest };

				while (subhead <= 48) {
					sl.SetCellStyle((subhead - 1), colA, subhead, (brokerCount + 2), styleQuarterHead);

					sl.SetCellStyle((subhead + 1), colA, (subhead + 4), colA, styleRowHead);
					sl.SetCellValue($"A{subhead + 1}", "LT GG");
					sl.SetCellValue($"A{subhead + 2}", "ST GG");
					sl.SetCellValue($"A{subhead + 3}", "Dividend");
					sl.SetCellValue($"A{subhead + 4}", "Interest");

					sl.SetCellStyle((subhead + 5), colA, (subhead + 5), (brokerCount + 2), styleSubTot);
					sl.SetCellValue($"A{subhead + 5}", "subtotal");

					// start in col B and move from there
					var col = colB;

					foreach (var b in brokers) {
						var tots = b.QuarterRows.Where(x => x.Quarter == quarter).FirstOrDefault();
						sl.SetCellValue(subhead, col, b.BrokerIdentity.ToString());

						var colLetter = GetColIndex(col);
						string formulaSubtotal = $"=SUM({colLetter}{subhead + 1}:{colLetter}{subhead + 4})";
						sl.SetCellValue((subhead + 5), col, formulaSubtotal);
						sl.SetCellStyle((subhead + 5), col, styleMoney);

						if (tots != null) {
							var incRow = subhead + 1;
							foreach (var inc in incTypes) {
								var income = tots.QuarterlyTotalRows.Where(x => x.IncomeType == inc).FirstOrDefault();
								sl.SetCellStyle(incRow, col, stylePlain);
								sl.SetCellStyle(incRow, col, styleMoney);
								if (income != null) {
									sl.SetCellValue(incRow, col, income.Income);
								} else {
									sl.SetCellValue(incRow, col, 0);
								}
								incRow++;
							}
						}
						col++;
					}
					quarter++;

					subhead = subhead + 12;
				}

				SLD_ResizeColumn(sl, "A", 20);

				for (int c = 2; c < (brokerCount + 2); c++) {
					SLD_ResizeColumn(sl, c, 25);
				}

				sl.SetCellValue("A1", $"Quarterly Income & Tax For {year}");
				sl.SetCellStyle(1, GetColIndex('A'), 1, brokerCount + 2, styleMainHead);
				SLD_ResizeRow(sl, 1, 20);

				sl.SaveAs(ms);
			}

			ms.Position = 0;

			// return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
				ms.CopyTo(fs);
			}
		}
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

	protected void PrintOutput(List<IFileCoreData> documents) {
		Console.WriteLine("=====================================================");

		foreach (var file in documents.OrderBy(x => x.FileExtractType.ToString())
							.OrderBy(x => x.AccountIdentity.ToString())
							.OrderBy(x => x.BrokerIdentity.ToString())) {
			Console.WriteLine($"\t\tFile data:  {file.BrokerIdentity} - {file.FileExtractType} - {file.AccountIdentity} ");

			if (file is IAccountTransaction) {
				var account = (IAccountTransaction)file;

				if (account.TransactionRows.Any()) {
					var dividends = account.TransactionRows.Where(x => x.TransactionType == TransactionType.Dividend).Sum(x => x.TransactionAmount);
					var interest = account.TransactionRows.Where(x => x.TransactionType == TransactionType.Interest).Sum(x => x.TransactionAmount);
					var ltg = account.TransactionRows.Where(x => x.TransactionType == TransactionType.DistributionLT).Sum(x => x.TransactionAmount);
					var stg = account.TransactionRows.Where(x => x.TransactionType == TransactionType.DistributionST).Sum(x => x.TransactionAmount);

					Console.WriteLine($"\t\t\tDividends:  {dividends:C2} ");
					Console.WriteLine($"\t\t\tInterest:  {interest:C2} ");
					Console.WriteLine($"\t\t\tLTG Distribution:  {ltg:C2} ");
					Console.WriteLine($"\t\t\tSTG Distribution:  {stg:C2} ");
				}
			}

			if (file is IAccountGainLoss) {
				var account = (IAccountGainLoss)file;

				if (account.GainLossRows.Any()) {
					var ltg = account.GainLossRows.Where(x => x.GainLossType == GainLossType.Long).Sum(x => x.GainLoss);
					var stg = account.GainLossRows.Where(x => x.GainLossType == GainLossType.Short).Sum(x => x.GainLoss);

					Console.WriteLine($"\t\t\tLong Term:  {ltg:C2} ");
					Console.WriteLine($"\t\t\tShort Term:  {stg:C2} ");
					Console.WriteLine($"\t\t\tTotal Gains:  {(stg + ltg):C2} ");
				}
			}
		}
	}
}