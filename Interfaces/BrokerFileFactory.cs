using Carrotware.IncomeParser.Entities;

namespace Carrotware.IncomeParser.Interfaces {

	public class BrokerFileFactory {

		public BrokerFileFactory() {
		}

		public IFileCoreData GenerateFileData(FileInfo file) {
			var filePath = file.FullName.ToLowerInvariant();
			var rows = File.ReadAllLines(file.FullName).ToList();

			IFileCoreData filedata = new FileCoreData(file, rows);

			if (rows.Any() && rows.Count >= 1) {
				if (rows.Count >= 3) {
					if (filePath.Contains("schwab") || filePath.Contains("chuck")) {
						if (rows[0].ToLowerInvariant().Contains("realized gain/loss")
							&& rows[1].ToLowerInvariant().Contains("closed date")
							&& rows[1].ToLowerInvariant().Contains("opened date")
							&& rows[1].ToLowerInvariant().Contains("cost basis")) {
							filedata = new SchwabGainLoss(file, rows);
						} else {
							filedata = new SchwabTransaction(file, rows);
						}

						return filedata;
					}
				}

				if (filePath.Contains("merrill")) {
					if (rows[0].ToLowerInvariant().Contains("acquisition date")
						&& rows[0].ToLowerInvariant().Contains("liquidation date")
						&& rows[0].ToLowerInvariant().Contains("acquisition price")
						&& rows[0].ToLowerInvariant().Contains("liquidation price")) {
						filedata = new MerrillGainLoss(file, rows);
					} else {
						filedata = new MerrillTransaction(file, rows);
					}

					return filedata;
				}

				if (filePath.Contains("chase") || filePath.Contains("morgan") || filePath.Contains("jpmc")) {
					if (rows[0].ToLowerInvariant().Contains("acquired date")
						&& rows[0].ToLowerInvariant().Contains("sale date")
						&& rows[0].ToLowerInvariant().Contains("unit sale price")
						&& rows[0].ToLowerInvariant().Contains("total realized gain loss")) {
						filedata = new ChaseGainLoss(file, rows);
					} else {
						filedata = new ChaseTransaction(file, rows);
					}
				}
			}

			return filedata;
		}
	}
}