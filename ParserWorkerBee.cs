using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.Extensions.Configuration;

public class ParserWorkerBee {
	protected static IConfiguration _configuration;
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

	public static string OutputCSV_Year(int year) {
		return string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.csv", year, AppDateTime);
	}

	public static string OutputReport {
		get {
			return string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.txt", AppDateTime);
		}
	}

	public static string OutputReportYear(int year) {
		return string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.txt", year, AppDateTime);
	}

	public static string OutputReportExcel {
		get {
			return string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.xlsx", AppDateTime);
		}
	}

	public static string OutputReportExcelYear(int year) {
		return string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.xlsx", year, AppDateTime);
	}

	public ParserWorkerBee() {
		LoadConfig();

		_date = DateTime.Now;
	}

	public void RunParser() {
		var factory = new BrokerFileFactory();
		var documents = new List<IFileCoreData>();

		string settingFolder = _configuration["MainDocumentFolder"] ?? string.Empty;
		Console.WriteLine($"Main Document Folder : {settingFolder}");

		var files = Directory.GetFiles(settingFolder, "*.csv", SearchOption.AllDirectories).ToList()
						.Select(x => new FileInfo(x))
						.Where(x => x.Name.StartsWith("Statement_") == false && x.DirectoryName != settingFolder).ToList();

		foreach (var file in files) {
			//Console.WriteLine($"\t\tFile : {file.FullName}");
			var fcd = factory.GenerateFileData(file);
			fcd.ParseFile();
			documents.Add(fcd);
		}

		//PrintOutput(documents);
		var brokers = factory.LoadBrokerDocuments(documents);

		Thread.Sleep(250);
		//string fileNameCSV = Path.Join(settingFolder, OutputCSV);
		//File.WriteAllText(fileNameCSV, string.Empty);

		var year = brokers.Max(x => x.Year);
		//string fileNameTxt = Path.Join(settingFolder, OutputReport);
		string fileNameTxt = Path.Join(settingFolder, ParserWorkerBee.OutputReportYear(year));
		File.WriteAllText(fileNameTxt, string.Empty);

		Thread.Sleep(250);

		factory.PrintOutput(brokers);

		Thread.Sleep(500);

		Program.PrintDisclaimer();
		var tax = new TaxDataCollector();
		tax.Init(brokers);

		var report = new XlsxExport(brokers);
		report.GenerateReport();

		Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

		Thread.Sleep(500);
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