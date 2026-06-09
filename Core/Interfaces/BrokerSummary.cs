using Carrotware.IncomeParser.Entities;
using Carrotware.IncomeParser.Helpers;

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

	public abstract class BrokerSummary : IBrokerSummary {
		protected List<IFileCoreData> _documents = new List<IFileCoreData>();

		public BrokerSummary() {
			_brokerIdent = "Generic";
		}

		public BrokerSummary(string acct) : this() {
			this.AccountIdentity = acct;
		}

		public void SetAccountIdentity(string acct) {
			this.AccountIdentity = acct;
		}

		protected string _brokerIdent = "GENERIC";

		public virtual string BrokerIdentity { get { return _brokerIdent; } }

		public virtual string[] BrokerPathFragments { get { return new string[] { "broker" }; } }

		public string AccountIdentity { get; set; } = string.Empty;

		public int Year { get; set; } = ParseHelper.MIN_YEAR;

		public decimal GrandTotal {
			get {
				decimal gainLoss = 0;
				decimal income = 0;

				if (this.GainLossRows != null) {
					gainLoss = this.GainLossRows.Sum(x => x.GainLoss);
				}
				if (this.TransactionRows != null) {
					income = this.TransactionRows
							.Where(x => x.TransactionType == TransactionType.Dividend
										|| x.TransactionType == TransactionType.Interest)
							.Sum(x => x.TransactionAmount);
				}

				return gainLoss + income;
			}
		}

		public List<GainLossRow> GainLossRows { get; set; } = new List<GainLossRow>();

		public List<TransactionRow> TransactionRows { get; set; } = new List<TransactionRow>();

		public List<QuarterRow> QuarterRows { get; set; } = new List<QuarterRow>();

		public virtual IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows) {
			return new FileCoreData(file, rows);
		}

		public virtual IFileCoreData? LoadFileCoreData(IFileCoreData filedata) {
			var file = filedata.FileInfo;
			var rows = filedata.Rows;

			return LoadFileCoreData(file, rows);
		}

		public virtual void LoadData(List<IFileCoreData> documents) {
			_documents = documents;

			var transactions = _documents.Where(x => x.BrokerIdentity == this.BrokerIdentity
									&& x.AccountIdentity == this.AccountIdentity
									&& x is IAccountTransaction)
							.Select(x => (IAccountTransaction)x);

			var gains = _documents.Where(x => x.BrokerIdentity == this.BrokerIdentity
										&& x.AccountIdentity == this.AccountIdentity
										&& x is IAccountGainLoss)
								.Select(x => (IAccountGainLoss)x);

			if (transactions != null) {
				var tRows = new List<TransactionRow>();

				foreach (var t in transactions) {
					tRows = tRows.Union(t.TransactionRows).OrderBy(x => x.ActionText).OrderBy(x => x.TransactionDate).ToList();
				}

				this.TransactionRows = tRows;
			}
			if (gains != null) {
				var gRows = new List<GainLossRow>();

				foreach (var g in gains) {
					gRows = gRows.Union(g.GainLossRows).OrderBy(x => x.RowText).OrderBy(x => x.DateClosed).ToList();
				}

				this.GainLossRows = gRows;
			}

			this.Year = DateTime.Now.Year;

			if (this.TransactionRows != null) {
				// to capture wash sales, including small amounts of prior or future dates is appropriate, use dominant year
				// ex checking a Jan or Dec set of trades after the fact, include additional dates flanking the affected period
				if (this.TransactionRows.Any()) {
					this.Year = this.TransactionRows.Select(d => d.TransactionDate)
							   .GroupBy(y => y.Year)
							   .OrderByDescending(g => g.Count())
							   .Select(g => g.Key)
							   .FirstOrDefault();
				}

				if (this.Year <= ParseHelper.MIN_YEAR) {
					this.Year = DateTime.Now.Year;
				}
			}
		}

		protected string _rptFileName = string.Empty;

		protected void SetReportFile(string fileName) {
			_rptFileName = fileName;
		}

		protected void ConsoleWriter() {
			ConsoleWriter(string.Empty);
		}

		protected void ConsoleWriter(string data) {
			Console.WriteLine(data);

			data.WriteLineFile(_rptFileName);
		}
	}
}