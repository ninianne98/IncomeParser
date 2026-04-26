using Carrotware.IncomeParser.Core;
using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;
using Microsoft.Extensions.Configuration;

/*
* Carrotware Income Parser
* http://www.carrotware.com/
*
* Copyright 2025 Samantha Copeland
* Licensed under the MIT license.
*
* Date: July 2025
*/

namespace Carrotware.IncomeParser.Interfaces {

	public class BrokerFileFactory {
		protected List<IFileCoreData> _documents = new List<IFileCoreData>();

		protected string _rptFileNameTxt = string.Empty;

		public BrokerFileFactory() {
		}

		public int Year { get; set; } = DateTime.Now.Year;

		protected void SetTextReportFile(string fileName) {
			_rptFileNameTxt = fileName;
		}

		protected void ConsoleWriter() {
			ConsoleWriter(string.Empty);
		}

		protected void ConsoleWriter(string data) {
			Console.WriteLine(data);

			data.WriteLineFile(_rptFileNameTxt);
		}

		private List<IBrokerSummary?> _brokers = new List<IBrokerSummary?>();

		public List<IBrokerSummary?> GetBrokerClasses() {
			if (_brokers == null || _brokers.Count <= 0) {
				_brokers = new List<IBrokerSummary?>();

				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var brokerType = typeof(IBrokerSummary);

				var brokerTypes = CoreConfig.ScanForBrokers();

				var implementations = assemblies
									.SelectMany(a => a.GetTypes())
									.Where(t => brokerType.IsAssignableFrom(t) && t.IsClass
												&& !t.IsInterface && !t.IsAbstract)
									.Union(brokerTypes)
									.ToList();

				var instances = implementations.Select(x => Activator.CreateInstance(x)).ToList();

				_brokers = instances.Where(x => x != null).Select(x => (IBrokerSummary?)x).ToList();
			}

			return _brokers;
		}

		public IFileCoreData? GenerateFileData(FileInfo file) {
			var instances = GetBrokerClasses();

			var rows = File.ReadAllLines(file.FullName).ToList();

			if (rows.Any() && rows.Count >= 1) {
				foreach (var inst in instances) {
					if (inst != null) {
						var data = inst.LoadFileCoreData(file, rows);

						if (data != null) {
							return data;
						}
					}
				}
			}

			return null;
		}

		public List<IBrokerSummary> LoadBrokerDocuments(List<IFileCoreData> documents) {
			_documents = documents;

			var brokers = new List<IBrokerSummary>();
			var instances = GetBrokerClasses();

			foreach (var inst in instances) {
				if (inst != null) {
					Type brokerType = inst.GetType();
					var docs = documents.Where(x => (x is IAccountGainLoss))
								.Where(x => x.BrokerIdentity == inst.BrokerIdentity)
								.ToList();

					if (docs.Count > 0) {
						var accts = docs.Where(x => string.IsNullOrEmpty(x.AccountIdentity) == false)
										.Select(x => x.AccountIdentity).Distinct().ToList();

						foreach (var acct in accts) {
							var instance = (IBrokerSummary)Activator.CreateInstance(brokerType);

							if (instance != null) {
								instance.SetAccountIdentity(acct);
								instance.LoadData(documents);

								brokers.Add(instance);
							}
						}
					}
				}
			}

			var year = brokers.Max(x => x.Year);
			if (year <= ParseHelper.MIN_YEAR) {
				year = DateTime.Now.Year;
			}

			this.Year = year;

			return brokers.OrderBy(x => x.AccountIdentity).OrderBy(x => x.BrokerIdentity).ToList();
		}

		public void PrintOutput(List<IFileCoreData> documents, List<IBrokerSummary> brokers) {
			_documents = documents;

			PrintOutput(brokers);
		}

		public void PrintOutput(List<IBrokerSummary> brokers) {
			var year = brokers.Max(x => x.Year);
			if (year <= ParseHelper.MIN_YEAR) {
				year = DateTime.Now.Year;
			}
			this.Year = year;

			string settingFolder = CoreConfig.Configuration["MainDocumentFolder"] ?? string.Empty;

			string fileNameTxt = Path.Join(settingFolder, CoreConfig.OutputReportYear(this.Year));
			SetTextReportFile(fileNameTxt);

			foreach (var b in brokers.OrderBy(x => x.AccountIdentity).OrderBy(x => x.BrokerIdentity).OrderByDescending(x => x.GrandTotal)) {
				Console.WriteLine("=====================================================");
				PrintOutput(b);
			}
		}

		protected void PrintOutput(IBrokerSummary broker) {
			string settingFolder = CoreConfig.Configuration["MainDocumentFolder"] ?? string.Empty;

			ConsoleWriter("-----------------------------------------------------------------------");

			var securityAliases = CoreConfig.Configuration.GetSection("SecurityAliases");
			var aliasesEntries = securityAliases.Get<List<string>>();
			var aliases = aliasesEntries != null ? aliasesEntries.Select(x => x.Split(',')
											.Select(x => x.ToUpperInvariant()).ToList()).ToList() : new List<List<string>>();

			decimal adjYearLong = 0;
			decimal adjYearShort = 0;

			Console.ForegroundColor = ConsoleColor.DarkCyan;
			ConsoleWriter();
			ConsoleWriter($"Account: {broker.AccountIdentity} - {broker.BrokerIdentity}");
			ConsoleWriter("-----------------------------");
			ConsoleWriter($"Transactions: {broker.TransactionRows.Count}");
			ConsoleWriter($"Gain/Losses: {broker.GainLossRows.Count}");
			ConsoleWriter("-----------------------------");
			Console.ResetColor();

			var dividends = broker.TransactionRows.Where(x => x.TransactionDate.Year == broker.Year && x.TransactionType == TransactionType.Dividend).Sum(x => x.TransactionAmount);
			var interest = broker.TransactionRows.Where(x => x.TransactionDate.Year == broker.Year && x.TransactionType == TransactionType.Interest).Sum(x => x.TransactionAmount);

			var ltgT = broker.TransactionRows.Where(x => x.TransactionDate.Year == broker.Year && x.TransactionType == TransactionType.DistributionLT).Sum(x => x.TransactionAmount);
			var stgT = broker.TransactionRows.Where(x => x.TransactionDate.Year == broker.Year && x.TransactionType == TransactionType.DistributionST).Sum(x => x.TransactionAmount);
			var ltgGL = broker.GainLossRows.Where(x => x.DateClosed.Year == broker.Year && x.GainLossType == GainLossType.Long).Sum(x => x.GainLoss);
			var stgGL = broker.GainLossRows.Where(x => x.DateClosed.Year == broker.Year && x.GainLossType == GainLossType.Short).Sum(x => x.GainLoss);

			var ltg = ltgT + ltgGL;
			var stg = stgT + stgGL;

			ConsoleWriter($"Total Dividends:\t{dividends:C2} ");
			ConsoleWriter($"Total Interest:\t{interest:C2} ");
			ConsoleWriter($"Total LTG Distribution and Gains/Losses:\t{ltg:C2} ");
			ConsoleWriter($"Total STG Distribution and Gains/Losses:\t{stg:C2} ");

			var year = broker.Year;

			for (int q = 1; q <= 4; q++) {
				var startMonth = (q - 1) * 3 + 1;
				var endMonth = q * 3;
				int endMonthEndDate = DateTime.DaysInMonth(year, endMonth);

				var startDate = new DateTime(year, startMonth, 1);
				var endDate = new DateTime(year, endMonth, endMonthEndDate);

				var quarter = new QuarterRow(q, year, startDate, endDate);
				broker.QuarterRows.Add(quarter);

				ConsoleWriter("-----------------------------");
				ConsoleWriter($"Quarter {q} {year} : {startDate:d} - {endDate:d}");
				ConsoleWriter("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

				if (startDate <= DateTime.Now.Date.AddDays(2)) {
					// only show date ranges that are historical
					var transactions = broker.TransactionRows.Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);
					var gains = broker.GainLossRows.Where(x => x.DateClosed >= startDate && x.DateClosed <= endDate);

					var dividendsQ = transactions.Where(x => x.TransactionType == TransactionType.Dividend).Sum(x => x.TransactionAmount);
					var interestQ = transactions.Where(x => x.TransactionType == TransactionType.Interest).Sum(x => x.TransactionAmount);

					var ltgTQ = transactions.Where(x => x.TransactionType == TransactionType.DistributionLT).Sum(x => x.TransactionAmount);
					var stgTQ = transactions.Where(x => x.TransactionType == TransactionType.DistributionST).Sum(x => x.TransactionAmount);

					var ltgGLQ = gains.Where(x => x.GainLossType == GainLossType.Long).Sum(x => x.GainLoss);
					var stgGLQ = gains.Where(x => x.GainLossType == GainLossType.Short).Sum(x => x.GainLoss);

					var ltgQ = ltgTQ + ltgGLQ;
					var stgQ = stgTQ + stgGLQ;

					decimal adjQL = 0;
					decimal adjQS = 0;

					ConsoleWriter($"\tQ{q} Dividends:\t{dividendsQ:C2} ");
					ConsoleWriter($"\tQ{q} Interest:\t{interestQ:C2} ");
					ConsoleWriter($"\tQ{q} LT CG:\t{ltgQ:C2} ");
					ConsoleWriter($"\tQ{q} ST CG:\t{stgQ:C2} ");
					ConsoleWriter();

					var quarterDiv = new QuarterlyTotalRow(IncomeType.Dividend, q, dividendsQ);
					var quarterInt = new QuarterlyTotalRow(IncomeType.Interest, q, interestQ);
					var quarterLong = new QuarterlyTotalRow(IncomeType.LongTermCG, q, ltgQ);
					var quarterShort = new QuarterlyTotalRow(IncomeType.ShortTermGG, q, stgQ);

					quarter.IncomeDetails = transactions.Where(x => x.TransactionType == TransactionType.Interest
											|| x.TransactionType == TransactionType.Dividend
											|| x.TransactionType == TransactionType.DistributionST
											|| x.TransactionType == TransactionType.DistributionLT)
									.Select(x => new TransactionDetail(broker, x)).ToList();

					quarter.SaleDetails = transactions.Where(x => x.TransactionType == TransactionType.Sell)
									.Select(x => new TransactionDetail(broker, x)).ToList();

					quarter.QuarterlyTotalRows.Add(quarterDiv);
					quarter.QuarterlyTotalRows.Add(quarterInt);
					quarter.QuarterlyTotalRows.Add(quarterLong);
					quarter.QuarterlyTotalRows.Add(quarterShort);

					var losses = gains.Where(x => x.GainLoss < 0 && x.Quantity != 0)
								.OrderByDescending(x => x.Proceeds)
								.OrderByDescending(x => x.Quantity)
								.OrderBy(x => x.DateClosed)
								.OrderBy(x => x.SecuritySymbol);

					foreach (var glr in losses) {
						var ticker = glr.SecuritySymbol.ToUpperInvariant();

						var alternates = aliases.Where(x => x.Select(y => y.ToUpperInvariant()).Contains(ticker)).FirstOrDefault();
						if (alternates == null) {
							alternates = new List<string>();
						}
						if (alternates.Any() == false) {
							alternates.Add(ticker);
						}

						var wash = new WashMatch(glr, alternates, gains);
						var washes = wash.ProcessDocuments(_documents);

						if (washes.Any()) {
							quarter.WashMatches.Add(wash);

							var totalQuantityLost = wash.TotalQuantityLost;
							var lotCount = wash.LotCount;
							var proportionLoss = wash.ProportionLoss;
							var washShares = wash.WashShares;
							var fracAllowed = wash.FracAllowed;
							var lossAllowed = wash.LossAllowed;
							var adjProportionLost = wash.AdjProportionLost;
							var adjustment = wash.Adjustment;

							if (glr.GainLossType == GainLossType.Long) {
								adjQL = adjQL + adjustment;
							}
							if (glr.GainLossType == GainLossType.Short) {
								adjQS = adjQS + adjustment;
							}

							ConsoleWriter($"\tPotential Wash: {ticker} : {glr.DateOpened:d} - {glr.GainLossType} - {glr.Quantity} shares @ {glr.UnitProceeds:C2} - {glr.DateClosed:d} - {glr.GainLoss:C2} ");

							var washMsg = $"\t\t{washShares} alternate shares purchased,"
										+ (lossAllowed == 0 ? $" entire loss disallowed" :
												" loss limited to " + (lotCount == 1 ? $"{fracAllowed:P2}" : $"{adjProportionLost:P2} ({fracAllowed:P2} adjusted by {proportionLoss:P2} due to {lotCount} lots)"))
										+ $" - {lossAllowed:C2} max loss, add back {adjustment:C2} ";
							ConsoleWriter(washMsg);

							foreach (var w in washes.OrderBy(x => x.AccountIdentity).OrderBy(x => x.BrokerIdentity)) {
								ConsoleWriter($"\t\t{w.AccountIdentity} :  {w.SecuritySymbol} - {w.TransactionDate:d} - {w.Quantity} @ {w.UnitPrice:C2}");
							}
						}
					}

					adjYearLong = adjYearLong + adjQL;
					adjYearShort = adjYearShort + adjQS;

					if (adjQL != 0 || adjQS != 0) {
						var ltgQ_Adj = ltgQ + adjQL;
						var stgQ_Adj = stgQ + adjQS;

						ConsoleWriter();
						ConsoleWriter($"\tQ{q} Dividends:\t{dividendsQ:C2} ");
						ConsoleWriter($"\tQ{q} Interest:\t{interestQ:C2} ");
						ConsoleWriter($"\tQ{q} ADJUSTED LT CG:\t{ltgQ_Adj:C2} \t- adding back {adjQL:C2}");
						ConsoleWriter($"\tQ{q} ADJUSTED ST CG:\t{stgQ_Adj:C2} \t- adding back {adjQS:C2}");

						quarterLong.Adjustment = adjQL;
						quarterShort.Adjustment = adjQS;
					} else {
						ConsoleWriter("\t† No detected wash sales, no quarterly adjustment");
					}

					if (DateTime.Now.Date < endDate) {
						ConsoleWriter($"\t* Quarter Not Closed");
					}
				} else {
					ConsoleWriter($"\t* Future Dates Out Of Range");
				}
			}

			if (adjYearLong != 0 || adjYearShort != 0) {
				var adjL = ltg + adjYearLong;
				var adjS = stg + adjYearShort;

				ConsoleWriter();
				ConsoleWriter($"Total Dividends:\t{dividends:C2} ");
				ConsoleWriter($"Total Interest:\t{interest:C2} ");
				ConsoleWriter($"Total ADJUSTED LTG Distribution and Gains/Losses:\t{adjL:C2} \t- adding back {adjYearLong:C2}");
				ConsoleWriter($"Total ADJUSTED STG Distribution and Gains/Losses:\t{adjS:C2} \t- adding back {adjYearShort:C2}");
			} else {
				ConsoleWriter("");
				ConsoleWriter("\t† No detected wash sales, no annual adjustment");
			}

			ConsoleWriter("-----------------------------------------------------------------------");
		}
	}
}