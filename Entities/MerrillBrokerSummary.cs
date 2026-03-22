using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;

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

			if (this.TransactionRows.Any() && this.GainLossRows.Any()) {
				foreach (var g in this.GainLossRows.OrderBy(x => x.SecurityDescription)) {
					if (g.SecuritySymbol.IsAlphaNumeric() && g.SecuritySymbol.HasDigits()) {
						var securityRows = this.TransactionRows
											.Where(x => x.ActionText.ToUpperInvariant().Contains(g.SecurityDescription.ToUpperInvariant()));

						var tickerOld = g.SecuritySymbol.ToUpperInvariant();
						var tickerNew = tickerOld;
						var dateClosed = g.DateClosed;
						var dateOpened = g.DateOpened;

						var tickerSale = securityRows.Where(x => x.TransactionType == TransactionType.Sell)
												.Where(x => (x.SettlementDate >= dateClosed.AddDays(-3)
																		&& x.SettlementDate <= dateClosed.AddDays(3))
														|| (x.TransactionDate >= dateClosed.AddDays(-3)
																	&& x.TransactionDate <= dateClosed.AddDays(3))
												).FirstOrDefault();

						if (tickerSale != null) {
							tickerNew = tickerSale.SecuritySymbol.ToUpperInvariant();
						} else {
							var tickerOpen = securityRows.Where(x => x.TransactionType == TransactionType.Buy
													|| x.TransactionType == TransactionType.Dividend
													|| x.TransactionType == TransactionType.Interest)
										.Where(x => x.TransactionDate >= dateOpened.AddDays(-30)
												&& x.TransactionDate <= dateOpened.AddDays(30));

							var tickerAction = tickerOpen.Where(x => (x.SettlementDate >= dateOpened.AddDays(-3)
																		&& x.SettlementDate <= dateOpened.AddDays(3))
													|| (x.TransactionDate >= dateOpened.AddDays(-3)
																&& x.TransactionDate <= dateOpened.AddDays(3))
												).FirstOrDefault();

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