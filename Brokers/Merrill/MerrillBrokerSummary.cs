using Carrotware.IncomeParser.Helpers;
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

	public class MerrillBrokerSummary : BrokerSummary {

		public MerrillBrokerSummary() : base() {
			_brokerIdent = BROKER_SUMMARY_IDENTITY;
		}

		public MerrillBrokerSummary(string acct) : this() {
			this.AccountIdentity = acct;
		}

		public const string BROKER_SUMMARY_IDENTITY = "Merrill Edge";

		public override string[] BrokerPathFragments { get { return new string[] { "merrill", "ml-edge" }; } }

		public override void LoadData(List<IFileCoreData> documents) {
			base.LoadData(documents);

			// Merrill uses gibberish symbols...
			// must patch, cross check with transaction logs
			if (this.TransactionRows.Any() && this.GainLossRows.Any()) {
				foreach (var g in this.GainLossRows.OrderBy(x => x.SecurityDescription)) {
					if (g.SecuritySymbol.IsAlphaNumeric() && g.SecuritySymbol.HasDigits()) {
						var securityRows = this.TransactionRows
											.Where(x => x.ActionText.ToUpperInvariant()
											.Contains(g.SecurityDescription.ToUpperInvariant()));

						var tickerOld = g.SecuritySymbol.ToUpperInvariant();
						var tickerNew = tickerOld;

						var dateClosed = g.DateClosed;
						var dateOpened = g.DateOpened;

						var tickerSale = securityRows.Where(x => x.TransactionType == TransactionType.Sell)
												.Where(x => (x.SettlementDate >= dateClosed.AddDays(-4)
																		&& x.SettlementDate <= dateClosed.AddDays(4))
														|| (x.TransactionDate >= dateClosed.AddDays(-4)
																	&& x.TransactionDate <= dateClosed.AddDays(4))
												).FirstOrDefault();

						if (tickerSale != null) {
							tickerNew = tickerSale.SecuritySymbol.ToUpperInvariant();
						} else {
							var tickerOpen = securityRows.Where(x => x.TransactionType == TransactionType.Buy)
										.Where(x => x.TransactionDate >= dateOpened.AddDays(-30)
												&& x.TransactionDate <= dateOpened.AddDays(30));

							var tickerAction = tickerOpen.Where(x => (x.SettlementDate >= dateOpened.AddDays(-4)
																		&& x.SettlementDate <= dateOpened.AddDays(4))
													|| (x.TransactionDate >= dateOpened.AddDays(-4)
																&& x.TransactionDate <= dateOpened.AddDays(4))
												).FirstOrDefault();

							if (tickerAction == null) {
								tickerOpen = securityRows.Where(x => x.TransactionType == TransactionType.Buy
														|| x.TransactionType == TransactionType.Dividend
														|| x.TransactionType == TransactionType.Interest)
												.Where(x => x.TransactionDate >= dateClosed.AddDays(-100)
														&& x.TransactionDate <= dateClosed.AddDays(100));

								tickerAction = tickerOpen.FirstOrDefault();
							}

							if (tickerAction != null) {
								tickerNew = tickerAction.SecuritySymbol.ToUpperInvariant();
							}
						}

						if (tickerOld != tickerNew) {
							this.GainLossRows.Where(s => s.SecuritySymbol.ToUpperInvariant() == tickerOld.ToUpperInvariant())
										.ToList().ForEach(s => s.SecuritySymbol = tickerNew.ToUpperInvariant());
						}
					}
				}
			}
		}

		public override IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows) {
			var filePath = file.FullName.ToLowerInvariant();
			bool hasMatch = this.BrokerPathFragments.Any(x => filePath.ToLowerInvariant().Contains(x));

			if (hasMatch) {
				if (rows[0].ToLowerInvariant().Contains("acquisition date")
					&& rows[0].ToLowerInvariant().Contains("liquidation date")
					&& rows[0].ToLowerInvariant().Contains("security description")
					&& rows[0].ToLowerInvariant().Contains("acquisition price")
					&& rows[0].ToLowerInvariant().Contains("liquidation price")) {
					return new MerrillGainLoss(file, rows);
				} else {
					return new MerrillTransaction(file, rows);
				}
			}

			return null;
		}
	}
}