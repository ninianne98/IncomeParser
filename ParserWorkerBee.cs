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
		//string fileNameCSV = Path.Join(settingFolder, OutputCSV);
		//File.WriteAllText(fileNameCSV, string.Empty);
		string fileNameTxt = Path.Join(settingFolder, OutputReport);
		File.WriteAllText(fileNameTxt, string.Empty);

		Thread.Sleep(250);

		foreach (var b in brokers.OrderBy(x => x.AccountIdentity).OrderBy(x => x.BrokerIdentity)) {
			Console.WriteLine("=====================================================");
			b.PrintOutput();
		}

		Thread.Sleep(250);

		var report = new XlsxExport(brokers);
		report.GenerateReport();

		Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
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