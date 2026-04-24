using Carrotware.IncomeParser.Interfaces;

/*
* Carrotware Income Parser
* http://www.carrotware.com/
*
* Copyright 2025 Samantha Copeland
* Licensed under the MIT license.
*
* Date: July 2025
*/

namespace Carrotware.IncomeParser.Entities {

	public class FidelityBrokerSummary : BrokerSummary {

		public FidelityBrokerSummary() : base() {
			_brokerIdent = BROKER_SUMMARY_IDENTITY;
		}

		public FidelityBrokerSummary(string acct) : this() {
			this.AccountIdentity = acct;
		}

		public const string BROKER_SUMMARY_IDENTITY = "Fidelity";

		public override string[] BrokerPathFragments { get { return new string[] { "fidelity", "netbenefits" }; } }

		public override IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows) {
			var filePath = file.FullName.ToLowerInvariant();
			bool hasMatch = this.BrokerPathFragments.Any(x => filePath.ToLowerInvariant().Contains(x));

			if (hasMatch && rows.Count >= 2) {
				if (filePath.Contains("portfolio_closed_lots")) {
					return new FidelityGainLoss(file, rows);
				}
				if (filePath.Contains("history_for_account")) {
					return new FidelityTransaction(file, rows);
				}
			}

			return null;
		}
	}
}