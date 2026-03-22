using Carrotware.IncomeParser.Entities;

namespace Carrotware.IncomeParser.Interfaces {

	public interface IBrokerSummary {
		string AccountIdentity { get; set; }
		string BrokerIdentity { get; }
		string[] BrokerPathFragments { get; }
		List<GainLossRow> GainLossRows { get; set; }
		decimal GrandTotal { get; }
		List<QuarterRow> QuarterRows { get; set; }
		List<TransactionRow> TransactionRows { get; set; }
		int Year { get; set; }

		void SetAccountIdentity(string acct);

		void LoadData(List<IFileCoreData> documents);

		IFileCoreData? LoadFileCoreData(FileInfo file, List<string> rows);

		IFileCoreData? LoadFileCoreData(IFileCoreData filedata);
	}
}