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

	public class ChaseBrokerSummary : BrokerSummary {

		public ChaseBrokerSummary() : base() {
			_brokerIdent = BROKER_SUMMARY_IDENTITY;
		}

		public ChaseBrokerSummary(string acct) : this() {
			this.AccountIdentity = acct;
		}

		public const string BROKER_SUMMARY_IDENTITY = "JPMorgan Chase";

		public override string[] BrokerPathFragments { get { return new string[] { "chase", "jpmorgan", "morgan", "jpmc" }; } }

		public override IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows) {
			var filePath = file.FullName.ToLowerInvariant();
			bool hasMatch = this.BrokerPathFragments.Any(x => filePath.ToLowerInvariant().Contains(x));

			if (hasMatch) {
				if (rows[0].ToLowerInvariant().Contains("acquired date")
					&& rows[0].ToLowerInvariant().Contains("sale date")
					&& rows[0].ToLowerInvariant().Contains("unit sale price")
					&& rows[0].ToLowerInvariant().Contains("total realized gain loss")) {
					return new ChaseGainLoss(file, rows);
				} else {
					return new ChaseTransaction(file, rows);
				}
			}

			return null;
		}
	}
}