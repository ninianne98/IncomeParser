namespace Carrotware.IncomeParser.Helpers {

	public class RowHelper {
		private Dictionary<string, int> _dict = new Dictionary<string, int>();
		private string[] _rowData = new string[1];

		public RowHelper() { }

		public RowHelper(string[]? headerRow) {
			LoadHeader(headerRow);
		}

		public void LoadRow(string[]? row) {
			if (row != null) {
				if (_dict.Count < 1) {
					LoadHeader(row);
				}
				_rowData = row;
			}
		}

		public void CreateColumnNames(string[] row) {
			this.Reset();

			for (int i = 0; i < row.Length; i++) {
				string keyName = "Col" + (string.Format("000000{0}", (i + 1))).Right(3);

				keyName = KeyFormat(keyName);

				if (!Exists(keyName)) {
					_dict.Add(keyName, i);
				}
			}

			LoadRow(row);
		}

		private void LoadHeader(string[]? headerRow) {
			if (headerRow != null) {
				for (int i = 0; i < headerRow.Length; i++) {
					string keyName = headerRow[i];

					if (!string.IsNullOrEmpty(keyName)) {
						keyName = KeyFormat(keyName);

						if (!Exists(keyName)) {
							_dict.Add(keyName, i);
						}
					}
				}
			}
		}

		public string ReadEmptyCell(string keyName) {
			return ReadCell(keyName) ?? string.Empty;
		}

		public string ReadEmptyCell(int cellNum) {
			return ReadCell(cellNum) ?? string.Empty;
		}

		public string? ReadCell(string keyName) {
			string? cellValue = null;

			if (!string.IsNullOrEmpty(keyName)) {
				keyName = KeyFormat(keyName);

				if (Exists(keyName)) {
					int cellNum = _dict[keyName];

					if (cellNum < _rowData.Length) {
						cellValue = _rowData[cellNum];
					}
				} else {
					throw new Exception($"Attempt to read column '{keyName}' which does not exist.");
				}
			}

			return cellValue;
		}

		public string? ReadCell(int cellNum) {
			string? cellValue = null;

			if (cellNum < _rowData.Length) {
				cellValue = _rowData[cellNum];
			} else {
				throw new Exception($"Attempt to read column index {cellNum} which does not exist.");
			}

			return cellValue;
		}

		public bool Exists(string keyName) {
			if (!string.IsNullOrEmpty(keyName)) {
				keyName = KeyFormat(keyName);

				if (_dict.ContainsKey(keyName)) {
					return true;
				}
			}

			return false;
		}

		private string KeyFormat(string? keyName) {
			if (!string.IsNullOrEmpty(keyName)) {
				return keyName.ToLowerInvariant().Trim();
			}

			return string.Empty;
		}

		public void ResetCurrentRow() {
			_rowData = new string[1];
		}

		public void Reset() {
			_rowData = new string[1];
			_dict = new Dictionary<string, int>();
		}
	}
}