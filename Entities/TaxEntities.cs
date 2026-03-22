using Carrotware.IncomeParser.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carrotware.IncomeParser.Entities {

	public class TaxDataCollector {

		public TaxDataCollector() { }

		public TaxYearData Fetch(int year) {
			if (year < 1970) {
				year = DateTime.Now.Year;
			}

			var taxData = new TaxYearData(year);

			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;
			var filePath = Directory.GetFiles(settingFolder, $"TaxYear*{year}.json").FirstOrDefault();

			if (string.IsNullOrEmpty(filePath) == false && File.Exists(filePath)) {
				var jsonString = File.ReadAllText(filePath);
				taxData = JsonSerializer.Deserialize<TaxYearData>(jsonString);
			}

			if (taxData == null) {
				taxData = new TaxYearData(year);
			}

			return taxData;
		}

		public void Init(IEnumerable<IBrokerSummary> brokers) {
			var year = brokers.Max(x => x.Year);
			if (year < 1970) {
				year = DateTime.Now.Year;
			}

			bool hasChanges = false;

			var taxData = Fetch(year);

			string settingFolder = ParserWorkerBee.Configuration["MainDocumentFolder"] ?? string.Empty;
			var filePath = Directory.GetFiles(settingFolder, $"TaxYear*{year}.json").FirstOrDefault();

			if (string.IsNullOrEmpty(filePath)) {
				filePath = Path.Combine(settingFolder, $"TaxYear_{year}.json");
			}

			if (File.Exists(filePath)) {
				var jsonString = File.ReadAllText(filePath);
				taxData = JsonSerializer.Deserialize<TaxYearData>(jsonString);
			} else {
				hasChanges = true;
			}

			if (taxData == null) {
				taxData = new TaxYearData(year);
				hasChanges = true;
			}

			Console.WriteLine($"\n\n========= Processing Tax Year: {taxData.Year} =========\n\n");

			foreach (var q in taxData.Quarters.OrderBy(x => x.Quarter)) {
				bool bChangeQuarter = false;
				bool bQuarterUpdate = false;

				Console.WriteLine($"\n\n[Q {q.Quarter} : {year}] \n\tEst Payment: {q.EstPayment:C} \n\tPayroll: {q.Payroll:C} \n\tDate: {q.DateOfPayment:d}");

				Console.Write("Add/Update values? (Enter to accept shown values. Type 'change' to alter values.) : ");
				var revise = Console.ReadLine();
				if (revise != null && revise.ToUpperInvariant() == "CHANGE") {
					bChangeQuarter = true;
				}

				if (bChangeQuarter) {
					string? inputText = null;
					inputText = HandleConsolePrompt("Est Payment (enter when done): ");

					if (inputText != null) {
						var ret = decimal.TryParse(inputText, out decimal newPay);
						if (q.EstPayment != newPay) {
							q.EstPayment = newPay;
							hasChanges = true;
							bQuarterUpdate = true;
						}
					}

					inputText = null;
					inputText = HandleConsolePrompt("Payroll (enter when done): ");

					if (inputText != null) {
						var ret = decimal.TryParse(inputText, out decimal newRoll);
						if (q.Payroll != newRoll) {
							q.Payroll = newRoll;
							hasChanges = true;
							bQuarterUpdate = true;
						}
					}

					inputText = null;
					inputText = HandleConsolePrompt("Payment Date (enter when done): ");

					if (inputText != null) {
						if (q.PaymentDate != inputText) {
							q.SetDate(inputText);
							hasChanges = true;
							bQuarterUpdate = true;
						}
					}

					if (bQuarterUpdate) {
						Console.ForegroundColor = ConsoleColor.DarkCyan;
						Console.WriteLine($"\n\n[REVISED Q {q.Quarter} : {year}] \n\tEst Payment: {q.EstPayment:C} \n\tPayroll: {q.Payroll:C} \n\tDate: {q.DateOfPayment:d}");
						Console.ResetColor();
					}
				}
			}

			if (hasChanges) {
				if (File.Exists(filePath)) {
					var backupPath = $"{filePath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
					File.Copy(filePath, backupPath, true);
				}

				var options = new JsonSerializerOptions { WriteIndented = true };
				File.WriteAllText(filePath, JsonSerializer.Serialize(taxData, options));
				Console.WriteLine($"\n[SUCCESS] Backup created and {Path.GetFileName(filePath)} updated.\n\n");
			}

			Console.WriteLine("\n\n\n");
		}

		private string? HandleConsolePrompt(string prompt) {
			var inputText = string.Empty;

			Console.Write(prompt);
			inputText = Console.ReadLine();

			if (string.IsNullOrEmpty(inputText)) {
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write("Blank response detected, type 'OK' to accept empty value, enter when done: ");
				inputText = Console.ReadLine() ?? string.Empty;
				if (inputText.ToUpperInvariant() == "OK") {
					inputText = string.Empty;
				} else {
					inputText = null;
				}
				Console.ResetColor();
			}

			return inputText;
		}
	}

	//======================
	public class TaxYearData {

		public TaxYearData() {
			LoadQuarters();
		}

		public TaxYearData(int year) {
			this.Year = year;
			LoadQuarters();
		}

		protected void LoadQuarters() {
			if (this.Quarters == null) {
				this.Quarters = new List<QuarterInfo>();
			}

			for (var q = 1; q <= 4; q++) {
				if (this.Quarters.Where(x => x.Quarter == q).Any() == false) {
					var qi = new QuarterInfo(q);
					this.Quarters.Add(qi);
				}
			}
		}

		public int Year { get; set; } = DateTime.Now.Year;
		public List<QuarterInfo> Quarters { get; set; } = new List<QuarterInfo>();
	}

	//======================

	public class QuarterInfo {

		public QuarterInfo() { }

		public QuarterInfo(int quarter) {
			this.Quarter = quarter;
		}

		public int Quarter { get; set; }
		public decimal EstPayment { get; set; }
		public decimal Payroll { get; set; }
		public string? PaymentDate { get; set; }

		[JsonIgnore]
		public DateTime? DateOfPayment {
			get {
				var date = this.PaymentDate;
				return SetDate(date);
			}
		}

		public DateTime? SetDate(string? dateInput) {
			var date = DateTime.MinValue;
			if (string.IsNullOrEmpty(dateInput) == false) {
				DateTime.TryParse(dateInput, out date);
				if (date != DateTime.MinValue) {
					this.PaymentDate = date.ToString("yyyy-MM-dd");
					return date;
				}
			}

			this.PaymentDate = string.Empty;
			return null;
		}
	}
}