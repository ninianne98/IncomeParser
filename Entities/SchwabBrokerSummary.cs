using Carrotware.IncomeParser.Interfaces;

namespace Carrotware.IncomeParser.Entities {

	public class SchwabBrokerSummary : BrokerSummary {

		public SchwabBrokerSummary() : base() {
			_brokerIdent = BROKER_SUMMARY_IDENTITY;
		}

		public SchwabBrokerSummary(string acct) : this() {
			this.AccountIdentity = acct;
		}

		public const string BROKER_SUMMARY_IDENTITY = "Schwab";

		public override string[] BrokerPathFragments { get { return new string[] { "schwab", "chuck" }; } }

		public override IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows) {
			var filePath = file.FullName.ToLowerInvariant();
			bool hasMatch = this.BrokerPathFragments.Any(x => filePath.ToLowerInvariant().Contains(x));

			if (hasMatch && rows.Count >= 2) {
				if (filePath.Contains("gainloss")) {
					return new SchwabGainLoss(file, rows);
				}
				if (filePath.Contains("transactions")) {
					return new SchwabTransaction(file, rows);
				}

				if (rows[0].ToLowerInvariant().Contains("realized gain/loss")
					&& rows[1].ToLowerInvariant().Contains("closed date")
					&& rows[1].ToLowerInvariant().Contains("opened date")
					&& rows[1].ToLowerInvariant().Contains("cost basis")) {
					return new SchwabGainLoss(file, rows);
				} else if (rows[0].ToLowerInvariant().Contains("gain/loss (%)")
					&& rows[0].ToLowerInvariant().Contains("closed date")
					&& rows[0].ToLowerInvariant().Contains("opened date")
					&& rows[0].ToLowerInvariant().Contains("cost basis")) {
					return new SchwabGainLoss(file, rows);
				} else {
					return new SchwabTransaction(file, rows);
				}
			}

			return null;
		}
	}
}