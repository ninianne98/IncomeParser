using Carrotware.IncomeParser.Core;
using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.Extensions.Logging;

/*
* Carrotware Income Parser
* http://www.carrotware.com/
*
* Copyright 2025 Samantha Copeland
* Licensed under the MIT license.
*
* Date: July 2025
*/

namespace Carrotware.IncomeParser {

	public class ParserWorkerBee {

		public ParserWorkerBee() {
		}

		public void RunParser() {
			var factory = new BrokerFileFactory();
			var documents = new List<IFileCoreData>();

			string settingFolder = CoreConfig.Configuration["MainDocumentFolder"] ?? string.Empty;
			Console.WriteLine($"Main Document Folder : {settingFolder}");

			var files = Directory.GetFiles(settingFolder, "*.csv", SearchOption.AllDirectories).ToList()
							.Select(x => new FileInfo(x))
							.Where(x => x.Name.StartsWith("Statement_") == false && x.DirectoryName != settingFolder).ToList();

			CoreConfig.Logger.LogInformation("Discovered {Count} files matching criteria for processing.", files.Count);

			foreach (var file in files) {
				try {
					CoreConfig.Logger.LogDebug("Processing file: {File}", file.FullName);
					var fcd = factory.GenerateFileData(file);
					if (fcd != null) {
						fcd.ParseFile();
						documents.Add(fcd);
					} else {
						CoreConfig.Logger.LogDebug("File skipped. No compatible parser found for: {File}", file.Name);
					}
				} catch (Exception ex) {
					CoreConfig.Logger.LogError(ex, "Error processing broker file: {File}", file.FullName);
				}
			}

			CoreConfig.Logger.LogInformation("Successfully parsed {Count} documents. Proceeding to report generation.", documents.Count);
			var brokers = factory.LoadBrokerDocuments(documents);

			Thread.Sleep(250);

			var year = brokers.Max(x => x.Year);
			//string fileNameTxt = Path.Join(settingFolder, OutputReport);
			string fileNameTxt = Path.Join(settingFolder, CoreConfig.OutputReportYear(year));
			CoreConfig.Logger.LogInformation("Writing text report to: {File}", fileNameTxt);
			File.WriteAllText(fileNameTxt, string.Empty);

			Thread.Sleep(250);

			factory.PrintOutput(brokers);

			Thread.Sleep(500);

			CoreConfig.PrintDisclaimer();
			CoreConfig.Logger.LogInformation("Collecting Tax Data...");
			var tax = new TaxDataCollector(brokers);
			tax.Run();

			CoreConfig.Logger.LogInformation("Generating XLSX report...");
			var report = new XlsxExport(brokers);
			report.GenerateReport();

			CoreConfig.Logger.LogInformation("ParserWorkerBee process completed successfully.");

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
}