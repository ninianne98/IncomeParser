using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Interfaces {

	public interface IFileCoreData {
		public FileInfo FileInfo { get; set; }
		public List<string> Rows { get; set; }
		public string BrokerIdentity { get; set; }
		public FileExtractType FileExtractType { get; set; }
		public string AccountIdentity { get; set; }

		public void ParseFile();

		public void SetFileType();
	}
}