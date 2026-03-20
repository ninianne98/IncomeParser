using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Carrotware.IncomeParser.Entities {

	public class BrokerSummary {
		private List<IFileCoreData> _documents = new List<IFileCoreData>();

		public BrokerSummary() { }

		public BrokerSummary(BrokerIdentity brokerIdent, string acct) {
			this.BrokerIdentity = brokerIdent;
			this.AccountIdentity = acct;
		}

		public BrokerIdentity BrokerIdentity { get; set; } = BrokerIdentity.Unknown;
		public string AccountIdentity { get; set; } = string.Empty;

		public int Year { get; set; } = DateTime.Now.Year;

		public decimal GrandTotal {
			get {
				decimal _total = 0;

				if (this.QuarterRows != null) {
					foreach (var q in this.QuarterRows) {
						_total = _total + q.QuarterlyTotalRows.Sum(x => x.Income);
						_total = _total + q.QuarterlyTotalRows.Sum(x => x.Adjustment);
					}
				}

				return _total;
			}
		}

		public List<GainLossRow> GainLossRows { get; set; } = new List<GainLossRow>();

		public List<TransactionRow> TransactionRows { get; set; } = new List<TransactionRow>();

		public List<QuarterRow> QuarterRows { get; set; } = new List<QuarterRow>();

		public void LoadData(List<IFileCoreData> documents) {
			_documents = documents;

			var transactions = _documents.Where(x => x.BrokerIdentity == this.BrokerIdentity
					&& x.AccountIdentity == this.AccountIdentity
					&& (x is IAccountTransaction))
					.Select(x => (IAccountTransaction)x)
					.FirstOrDefault();

			var gains = _documents.Where(x => x.BrokerIdentity == this.BrokerIdentity
								&& x.AccountIdentity == this.AccountIdentity
								&& (x is IAccountGainLoss))
								.Select(x => (IAccountGainLoss)x)
								.FirstOrDefault();

			if (transactions != null) {
				this.TransactionRows = transactions.TransactionRows;
			}
			if (gains != null) {
				this.GainLossRows = gains.GainLossRows;
			}
		}

		public void PrintOutput() {
			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;

			//string fileNameCSV = Path.Join(settingFolder, ParserWorkerBee.OutputCSV);
			string fileNameTxt = Path.Join(settingFolder, ParserWorkerBee.OutputReport);
			SetReportFile(fileNameTxt);

			var sb = new StringBuilder();
			ConsoleWriter("-----------------------------------------------------------------------");

			var securityAliases = ParserWorkerBee.Configuration.GetSection("SecurityAliases");
			var aliasesEntries = securityAliases.Get<List<string>>();
			var aliases = aliasesEntries != null ? aliasesEntries.Select(x => x.Split(',')
											.Select(x => x.ToUpperInvariant()).ToList()).ToList() : new List<List<string>>();

			decimal adjYearLong = 0;
			decimal adjYearShort = 0;

			sb.AppendLine($"Account: {this.AccountIdentity} - {this.BrokerIdentity}".QuoteForCSV());
			sb.AppendLine($"Generated: {ParserWorkerBee.AppDateTime}".QuoteForCSV());
			// sb.WriteFile(fileNameCSV);
			sb.Clear();

			ConsoleWriter();
			ConsoleWriter($"Account: {this.AccountIdentity} - {this.BrokerIdentity}");
			ConsoleWriter("-----------------------------");
			ConsoleWriter($"Transactions: {this.TransactionRows.Count}");
			ConsoleWriter($"Gain/Losses: {this.GainLossRows.Count}");
			ConsoleWriter("-----------------------------");

			var dividends = this.TransactionRows.Where(x => x.TransactionType == TransactionType.Dividend).Sum(x => x.TransactionAmount);
			var interest = this.TransactionRows.Where(x => x.TransactionType == TransactionType.Interest).Sum(x => x.TransactionAmount);

			var ltgT = this.TransactionRows.Where(x => x.TransactionType == TransactionType.DistributionLT).Sum(x => x.TransactionAmount);
			var stgT = this.TransactionRows.Where(x => x.TransactionType == TransactionType.DistributionST).Sum(x => x.TransactionAmount);
			var ltgGL = this.GainLossRows.Where(x => x.GainLossType == GainLossType.Long).Sum(x => x.GainLoss);
			var stgGL = this.GainLossRows.Where(x => x.GainLossType == GainLossType.Short).Sum(x => x.GainLoss);

			var ltg = ltgT + ltgGL;
			var stg = stgT + stgGL;

			ConsoleWriter($"Total Dividends:\t{dividends:C2} ");
			ConsoleWriter($"Total Interest:\t{interest:C2} ");
			ConsoleWriter($"Total LTG Distribution and Gains/Losses:\t{ltg:C2} ");
			ConsoleWriter($"Total STG Distribution and Gains/Losses:\t{stg:C2} ");

			sb.Append(",");
			sb.Append("Dividends".QuoteForCSV() + ",");
			sb.Append("Interest".QuoteForCSV() + ",");
			sb.Append("LT CG".QuoteForCSV() + ",");
			sb.Append("ST CG".QuoteForCSV() + ",");
			sb.AppendLine();
			// sb.WriteFile(fileNameCSV);
			sb.Clear();

			sb.Append(",");
			sb.Append($"{dividends:C2}".QuoteForCSV() + ",");
			sb.Append($"{interest:C2}".QuoteForCSV() + ",");
			sb.Append($"{ltg:C2}".QuoteForCSV() + ",");
			sb.Append($"{stg:C2}".QuoteForCSV() + ",");
			sb.AppendLine();
			// sb.WriteFile(fileNameCSV);
			sb.Clear();

			this.Year = DateTime.Now.Year;
			// to capture wash sales, including small amounts of prior or future dates is appropriate, use dominant year
			// ex checking a Jan or Dec set of trades after the fact, include additional dates flanking the affected period
			if (this.TransactionRows.Any()) {
				this.Year = this.TransactionRows.Select(d => d.TransactionDate)
						   .GroupBy(y => y.Year)
						   .OrderByDescending(g => g.Count())
						   .Select(g => g.Key)
						   .FirstOrDefault();
			}
			if (this.Year < 1970) {
				this.Year = DateTime.Now.Year;
			}

			sb.AppendLine(",");
			sb.Append(",");
			sb.Append("Quarter".QuoteForCSV() + ",");
			sb.Append("Dividends".QuoteForCSV() + ",");
			sb.Append("Interest".QuoteForCSV() + ",");
			sb.Append("LT CG".QuoteForCSV() + ",");
			sb.Append("ST CG".QuoteForCSV() + ",");
			sb.Append("LT CG Adjusted".QuoteForCSV() + ",");
			sb.Append("LT CG Adjustment".QuoteForCSV() + ",");
			sb.Append("ST CG Adjusted".QuoteForCSV() + ",");
			sb.Append("ST CG Adjustment".QuoteForCSV() + ",");
			sb.AppendLine();
			// sb.WriteFile(fileNameCSV);
			sb.Clear();

			var year = this.Year;

			for (int q = 1; q <= 4; q++) {
				var startMonth = ((q - 1) * 3 + 1);
				var endMonth = q * 3;
				int endMonthEndDate = DateTime.DaysInMonth(year, endMonth);

				var startDate = new DateTime(year, startMonth, 1);
				var endDate = new DateTime(year, endMonth, endMonthEndDate);

				var quarter = new QuarterRow(q, year, startDate, endDate);
				this.QuarterRows.Add(quarter);

				ConsoleWriter("-----------------------------");
				ConsoleWriter($"Quarter {q} {year} : {startDate:d} - {endDate:d}");
				ConsoleWriter("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

				if (startDate <= DateTime.Now.Date.AddDays(7)) {
					// only show date ranges that are historical
					var transactions = this.TransactionRows.Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);
					var gains = this.GainLossRows.Where(x => x.DateClosed >= startDate && x.DateClosed <= endDate);

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

					sb.Append(",");
					sb.Append($"Q{q} {year}".QuoteForCSV() + ",");
					sb.Append($"{dividendsQ:C2}".QuoteForCSV() + ",");
					sb.Append($"{interestQ:C2}".QuoteForCSV() + ",");
					sb.Append($"{ltgQ:C2}".QuoteForCSV() + ",");
					sb.Append($"{stgQ:C2}".QuoteForCSV() + ",");
					sb.AppendLine();
					// sb.WriteFile(fileNameCSV);
					sb.Clear();

					var quarterDiv = new QuarterlyTotalRow(IncomeType.Dividend, q, dividendsQ);
					var quarterInt = new QuarterlyTotalRow(IncomeType.Interest, q, interestQ);
					var quarterLong = new QuarterlyTotalRow(IncomeType.LongTermCG, q, ltgQ);
					var quarterShort = new QuarterlyTotalRow(IncomeType.ShortTermGG, q, stgQ);

					quarter.QuarterlyTotalRows.Add(quarterDiv);
					quarter.QuarterlyTotalRows.Add(quarterInt);
					quarter.QuarterlyTotalRows.Add(quarterLong);
					quarter.QuarterlyTotalRows.Add(quarterShort);

					var losses = gains.Where(x => x.GainLoss < 0 && x.Quantity != 0);

					foreach (var glr in losses) {
						var washes = new List<WashDetail>();
						var washStart = glr.DateClosed.AddDays(-31);
						var washEnd = glr.DateClosed.AddDays(31);
						var ticker = glr.SecuritySymbol.ToUpperInvariant();

						// as there are sometimes several lots, proportionally share any wash sales across lots
						var lotCount = losses.Where(x => x.SecuritySymbol == ticker).Count();
						var totalQuantityLost = losses.Where(x => x.SecuritySymbol == ticker).Sum(x => x.Quantity);
						var proportionLoss = glr.Quantity / totalQuantityLost;

						var alternates = aliases.Where(x => x.Select(y => y.ToUpperInvariant()).Contains(ticker)).FirstOrDefault();
						if (alternates == null) {
							alternates = new List<string>();
						}
						if (alternates.Any() == false) {
							alternates.Add(ticker);
						}

						foreach (var d in _documents.Where(x => x is IAccountTransaction)) {
							var doc = (IAccountTransaction)d;
							var washesFound = doc.TransactionRows.Where(x => alternates.Contains(x.SecuritySymbol.ToUpperInvariant())
												&& x.TransactionType == TransactionType.Buy
												&& x.TransactionDate >= washStart && x.TransactionDate <= washEnd)
											.Select(x => new WashDetail(d, x));

							washes = washes.Union(washesFound).ToList();
						}

						if (washes.Any()) {
							var wash = new WashMatch(ticker, alternates, glr, washes);
							wash.TotalQuantityLost = totalQuantityLost;
							wash.LotCount = lotCount;
							wash.ProportionLoss = proportionLoss;
							quarter.WashMatches.Add(wash);

							var washShares = washes.Sum(x => x.Quantity);
							var fracAllowed = 1 - (washShares < glr.Quantity ? (washShares / glr.Quantity) : 1);
							var lossAllowed = fracAllowed * glr.GainLoss / proportionLoss;
							var adjProportionLost = fracAllowed / proportionLoss;

							var adjustment = -1 * (glr.GainLoss - lossAllowed);

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

							foreach (var w in washes.OrderBy(x => x.SecuritySymbol).OrderBy(x => x.TransactionDate).OrderBy(x => x.AccountIdentity)) {
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

						sb.Append(",");
						sb.Append($"Q{q} {year} ***".QuoteForCSV() + ",");
						sb.Append($"{dividendsQ:C2}".QuoteForCSV() + ",");
						sb.Append($"{interestQ:C2}".QuoteForCSV() + ",");
						sb.Append($"{ltgQ:C2}".QuoteForCSV() + ",");
						sb.Append($"{stgQ:C2}".QuoteForCSV() + ",");
						sb.Append($"{ltgQ_Adj:C2}".QuoteForCSV() + ",");
						sb.Append($"{adjQL:C2}".QuoteForCSV() + ",");
						sb.Append($"{stgQ_Adj:C2}".QuoteForCSV() + ",");
						sb.Append($"{adjQS:C2}".QuoteForCSV() + ",");
						sb.AppendLine();
						// sb.WriteFile(fileNameCSV);
						sb.Clear();
					} else {
						ConsoleWriter("\tNo detected wash sales, no quarterly adjustment");
					}
				} else {
					ConsoleWriter($"\tFuture Dates Out Of Range");
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

				sb.Append(",");
				sb.AppendLine();

				sb.Append(",");
				sb.Append("Dividends".QuoteForCSV() + ",");
				sb.Append("Interest".QuoteForCSV() + ",");
				sb.Append("LT CG".QuoteForCSV() + ",");
				sb.Append("ST CG".QuoteForCSV() + ",");
				sb.Append("LT CG Adjusted".QuoteForCSV() + ",");
				sb.Append("LT CG Adjustment".QuoteForCSV() + ",");
				sb.Append("ST CG Adjusted".QuoteForCSV() + ",");
				sb.Append("ST CG Adjustment".QuoteForCSV() + ",");
				sb.AppendLine();
				// sb.WriteFile(fileNameCSV);
				sb.Clear();

				sb.Append(",");
				sb.Append($"{dividends:C2}".QuoteForCSV() + ",");
				sb.Append($"{interest:C2}".QuoteForCSV() + ",");
				sb.Append($"{ltg:C2}".QuoteForCSV() + ",");
				sb.Append($"{stg:C2}".QuoteForCSV() + ",");
				sb.Append($"{adjL:C2}".QuoteForCSV() + ",");
				sb.Append($"{adjYearLong:C2}".QuoteForCSV() + ",");
				sb.Append($"{adjS:C2}".QuoteForCSV() + ",");
				sb.Append($"{adjYearShort:C2}".QuoteForCSV() + ",");
				sb.AppendLine();
				// sb.WriteFile(fileNameCSV);
				sb.Clear();
			} else {
				ConsoleWriter("\tNo detected wash sales, no annual adjustment");
			}

			sb.Append(",");
			sb.AppendLine();
			// sb.WriteFile(fileNameCSV);
			sb.Clear();

			ConsoleWriter("-----------------------------------------------------------------------");
		}

		private string _rptFileName = string.Empty;

		private void SetReportFile(string fileName) {
			_rptFileName = fileName;
		}

		private void ConsoleWriter() {
			ConsoleWriter(string.Empty);
		}

		private void ConsoleWriter(string data) {
			Console.WriteLine(data);

			data.WriteLineFile(_rptFileName);
		}
	}
}