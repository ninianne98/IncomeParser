using Carrotware.IncomeParser.Core;
using Carrotware.IncomeParser.Helpers;
using Carrotware.IncomeParser.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Carrotware.IncomeParser.Entities {

	public class TaxDataCollector {

		public TaxDataCollector() { }

		public TaxDataCollector(IEnumerable<IBrokerSummary> brokers) {
			var year = brokers.Max(x => x.Year);
			if (year <= 1970) {
				year = DateTime.Now.Year;
			}

			this.BrokerSummaries = brokers;
			this.Year = year;
		}

		public int Year { get; set; } = DateTime.Now.Year;

		public IEnumerable<IBrokerSummary> BrokerSummaries { get; set; } = new List<IBrokerSummary>();

		public void Run() {
			var year = this.Year;

			bool hasChanges = false;

			var taxData = TaxYearData.Load(year);
			string settingFolder = CoreConfig.Configuration["MainDocumentFolder"] ?? string.Empty;
			var filePath = Directory.GetFiles(settingFolder, $"TaxYear*{year}.json").FirstOrDefault();

			hasChanges = string.IsNullOrEmpty(filePath);

			if (string.IsNullOrEmpty(filePath)) {
				filePath = Path.Combine(settingFolder, $"TaxYear_{year}.json");
			}

			hasChanges = File.Exists(filePath) == false;

			if (taxData == null) {
				taxData = new TaxYearData(year);
				hasChanges = true;
			}

			var rateDict = new Dictionary<string, IncomeType>();
			foreach (var et in Enum.GetValues<IncomeType>().Where(x => x != IncomeType.Unknown)) {
				var menuKey = IncomeTypeMenu(et);
				rateDict[menuKey] = et;
			}

			bool keepGoing = true;
			Console.WriteLine("\n\n");

			while (keepGoing) {
				Console.WriteLine($"========= Tax Year: {this.Year} =========");

				var sortedQuarters = taxData.Quarters.OrderBy(x => x.Quarter).ToList();

				Console.WriteLine("\n   Quarters ------------------------");
				foreach (var q in sortedQuarters) {
					var payDate = (q.DateOfPayment == DateTime.MinValue || q.DateOfPayment == null) ? "N/A" : $"{q.DateOfPayment:d}";
					Console.WriteLine($"\t[ {q.Quarter} ]  Q{q.Quarter}: Est={q.EstPayment:C}, Payroll={q.Payroll:C}, Date={payDate}");
				}

				Console.WriteLine("\n   Tax Rates ------------------------");
				foreach (var tri in taxData.TaxRates) {
					var menuKey = IncomeTypeMenu(tri.IncomeType);
					Console.WriteLine($"\t[ {menuKey} ]  {tri.IncomeType.GetDescription()}: {tri.Percentage:P}");
				}

				var finishMsg = hasChanges ? "Finish and Save" : "Continue";

				Console.WriteLine($"\n\t[ 0 ]  {finishMsg}\n\n\n");

				Console.Write("Select item, 0 to finish :  ");

				var inputInt = -1;
				var input = Console.ReadLine();

				input = string.IsNullOrEmpty(input) ? input : input.ToUpperInvariant().Trim();
				var inRet = int.TryParse(input, out inputInt);

				if (inRet && inputInt == 0) {
					keepGoing = false;
				} else if (inRet && inputInt >= 1 && inputInt <= 4) {
					var item = taxData.Quarters.Where(x => x.Quarter == inputInt).First();
					hasChanges |= PromptUpdateQuarter(item);
				} else if (string.IsNullOrWhiteSpace(input) == false && input.Length == 1
							&& rateDict.ContainsKey(input)) {
					var selRate = rateDict[input];
					var item = taxData.TaxRates.Where(x => x.IncomeType == selRate).First();
					hasChanges |= PromptUpdateTaxRate(item);
				} else {
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("\n\n** Invalid Selection **\n");
					Console.ResetColor();
				}
			}

			// if changed or new file, save!
			if (hasChanges || File.Exists(filePath) == false) {
				if (File.Exists(filePath)) {
					var backupPath = $"{filePath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
					File.Copy(filePath, backupPath, true);
				}

				var options = new JsonSerializerOptions { WriteIndented = true };
				File.WriteAllText(filePath, JsonSerializer.Serialize(taxData, options));
				Console.WriteLine($"\n[SUCCESS] Backup created and {Path.GetFileName(filePath)} updated.\n\n");
			}

			Console.WriteLine("\n\n");
		}

		private bool PromptUpdateTaxRate(TaxRateInfo info) {
			bool updated = false;

			Console.WriteLine($"\nUpdate Tax Rate for {this.Year} {info.IncomeType.GetDescription()} (Current Percentage: {info.Percentage:P})");
			string? inputText = HandleConsolePrompt("New tax rate (e.g. 0.15 or 15)");

			if (inputText != null && double.TryParse(inputText, out double newRate)) {
				if (newRate > 1.0) { newRate /= 100.0; }
				if (info.Percentage != newRate) { info.Percentage = newRate; updated = true; }
			}

			if (updated) {
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine($"\n\nREVISED {this.Year} {info.IncomeType.GetDescription()} Percentage: {info.Percentage:P}");
				Console.WriteLine("\n");
				Console.ResetColor();
			}

			return updated;
		}

		private bool PromptUpdateQuarter(QuarterInfo info) {
			bool updated = false;
			Console.WriteLine($"\nUpdate Quarter {info.Quarter} of {this.Year}");
			Console.WriteLine($"\tCurrent Est: {info.EstPayment:C}, Payroll: {info.Payroll:C}, Date: {info.DateOfPayment:d}");

			string? input = HandleConsolePrompt("Est Payment");
			if (input != null && decimal.TryParse(input, out decimal newEst)) {
				if (info.EstPayment != newEst) { info.EstPayment = newEst; updated = true; }
			}

			input = HandleConsolePrompt("Payroll");
			if (input != null && decimal.TryParse(input, out decimal newPay)) {
				if (info.Payroll != newPay) { info.Payroll = newPay; updated = true; }
			}

			input = HandleConsolePrompt("Payment Date");
			if (input != null && info.PaymentDate != input) { info.SetDate(input); updated = true; }

			if (updated) {
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine($"\nREVISED {info.Quarter} of {this.Year} Est: {info.EstPayment:C}, Payroll: {info.Payroll:C}, Date: {info.DateOfPayment:d}");
				Console.WriteLine("\n");
				Console.ResetColor();
			}

			return updated;
		}

		private string IncomeTypeMenu(IncomeType income) {
			return income.ToString().Substring(0, 1).ToUpperInvariant();
		}

		private string? HandleConsolePrompt(string prompt) {
			var inputText = string.Empty;

			Console.Write(prompt.Trim() + ":  ");
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
			this.Year = 1970;
			LoadQuarters();
			LoadRates();
		}

		public TaxYearData(int year) : this() {
			this.Year = year;
		}

		public static TaxYearData Load(int year) {
			if (year <= 1970) {
				year = DateTime.Now.Year;
			}

			var taxData = new TaxYearData(year);

			string settingFolder = CoreConfig.Configuration["MainDocumentFolder"] ?? string.Empty;
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

		protected void LoadQuarters() {
			if (this.Quarters == null) {
				this.Quarters = new List<QuarterInfo>();
			}

			if (this.Quarters.Count != 4) {
				for (var q = 1; q <= 4; q++) {
					if (this.Quarters.Where(x => x.Quarter == q).Any() == false) {
						var qi = new QuarterInfo(q);
						this.Quarters.Add(qi);
					}
				}
			}

			foreach (var q in this.Quarters) {
				q.FormatDate();
			}
		}

		protected void LoadRates() {
			if (this.TaxRates == null) {
				this.TaxRates = new List<TaxRateInfo>();
			}

			var rates = Enum.GetValues<IncomeType>().Where(x => x != IncomeType.Unknown).ToList();

			if (this.TaxRates.Count != rates.Count) {
				var taxRates = CoreConfig.Configuration.GetSection("TaxRatesPercent").Get<Dictionary<string, object>>();
				double rt = 0.30;

				foreach (var r in rates) {
					var key = r.ToString();
					rt = 0.25;

					if (taxRates != null && taxRates.ContainsKey(key)) {
						var taxRate = taxRates[key].ToString() ?? "20";
						var rateNbr = double.Parse(taxRate);
						rt = (rateNbr > 1.00) ? (rateNbr / 100.00) : rateNbr;
					}

					var rate = this.TaxRates.Where(x => x.IncomeType == r).FirstOrDefault();

					if (rate == null) {
						rate = new TaxRateInfo(r, rt);
						this.TaxRates.Add(rate);
					}
					if (rate.Percentage > 1.0) {
						rate.Percentage = rate.Percentage / 100.0;
					}
				}
			} else {
				foreach (var rate in this.TaxRates.Where(x => x.Percentage > 1.0)) {
					rate.Percentage = rate.Percentage / 100.0;
				}
			}
		}

		public int Year { get; set; } = DateTime.Now.Year;

		public List<QuarterInfo> Quarters { get; set; } = new List<QuarterInfo>();

		public List<TaxRateInfo> TaxRates { get; set; } = new List<TaxRateInfo>();
	}

	//======================

	public class TaxRateInfo {

		public TaxRateInfo() { }

		public TaxRateInfo(IncomeType rateType) {
			this.IncomeType = rateType;
			this.Percentage = 0.10;
		}

		public TaxRateInfo(IncomeType rateType, double perc) {
			this.IncomeType = rateType;
			this.Percentage = perc;
		}

		[JsonConverter(typeof(JsonStringEnumConverter<IncomeType>))]
		public IncomeType IncomeType { get; set; } = IncomeType.Unknown;

		public double? Percentage { get; set; } = null;
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

		public void FormatDate() {
			var dateInput = this.PaymentDate;
			var date = ParseDateString(dateInput);

			if (date != DateTime.MinValue) {
				this.PaymentDate = date.ToString("yyyy-MM-dd");
			} else {
				this.PaymentDate = string.Empty;
			}
		}

		public DateTime? SetDate(string? dateInput) {
			var date = ParseDateString(dateInput);

			if (date != DateTime.MinValue) {
				this.PaymentDate = date.ToString("yyyy-MM-dd");
				return date;
			} else {
				this.PaymentDate = string.Empty;
				return null;
			}
		}

		private DateTime ParseDateString(string? dateInput) {
			var date = DateTime.MinValue;

			if (string.IsNullOrEmpty(dateInput) == false) {
				DateTime.TryParse(dateInput, out date);
				if (date != DateTime.MinValue) {
					this.PaymentDate = date.ToString("yyyy-MM-dd");
				}
			}

			return date;
		}
	}
}