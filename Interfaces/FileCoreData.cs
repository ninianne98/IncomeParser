using Carrotware.IncomeParser.Helpers;

namespace Carrotware.IncomeParser.Interfaces {

	public class FileCoreData : IFileCoreData {

		public FileCoreData() {
			this.Rows = new List<string>();
			this.SetFileType();
		}

		public FileCoreData(FileInfo file, string[] rows) : this() {
			this.FileInfo = file;
			this.Rows = rows.ToList();
			this.SetFileType();
			this.AccountIdentity = Path.GetFileNameWithoutExtension(this.FileInfo.FullName);
		}

		public FileCoreData(FileInfo file, List<string> rows) : this() {
			this.FileInfo = file;
			this.Rows = rows;
			this.SetFileType();
			this.AccountIdentity = Path.GetFileNameWithoutExtension(this.FileInfo.FullName);
		}

		public virtual void ParseFile() {
			//throw new NotImplementedException();
		}

		public virtual void SetFileType() {
			this.FileExtractType = FileExtractType.Unknown;
		}

		public FileInfo FileInfo { get; set; } // dont set a default!
		public List<string> Rows { get; set; } = new List<string>();
		public string BrokerIdentity { get; set; } = "Unknown";
		public FileExtractType FileExtractType { get; set; }
		public string AccountIdentity { get; set; } = string.Empty;
	}
}