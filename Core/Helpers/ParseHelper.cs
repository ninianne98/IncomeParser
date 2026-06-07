using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Carrotware.IncomeParser.Helpers {

	public static class ParseHelper {

		public static string GetDescription<T>(this T source) {
			if (source == null) return string.Empty;

			MemberInfo? memberInfo;
			var type = source.GetType();

			if (type.IsEnum) {
				memberInfo = type.GetField(source.ToString() ?? "");
			} else {
				memberInfo = type;
			}

			var attribute = memberInfo?
								.GetCustomAttributes(typeof(DescriptionAttribute), false)
								.FirstOrDefault() as DescriptionAttribute;

			return attribute?.Description ?? source.ToString() ?? typeof(T).ToString() ?? string.Empty;
		}

		public const int MIN_YEAR = 1970;

		public static bool IsAlphaNumeric(this string text) {
			return !string.IsNullOrEmpty(text) && text.All(Char.IsLetterOrDigit);
		}

		public static bool IsAlphabetic(this string text) {
			return !string.IsNullOrEmpty(text) && text.All(Char.IsLetter);
		}

		public static bool HasLetters(this string text) {
			return !string.IsNullOrEmpty(text) && text.Any(Char.IsLetter);
		}

		public static bool HasDigits(this string text) {
			return !string.IsNullOrEmpty(text) && text.Any(Char.IsDigit);
		}

		public static string? StringSafeTrim(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				return val.Trim();
			}

			return null;
		}

		public static decimal? StringToDecimal(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				val = val.MoneyStringClean();

				if (!string.IsNullOrEmpty(val)) {
					return Convert.ToDecimal(val);
				}
			}

			return null;
		}

		public static double? StringToDouble(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				return Convert.ToDouble(val);
			}

			return null;
		}

		public static string MoneyStringClean(this string? val) {
			string v2 = val.StringSafeTrim() ?? string.Empty;

			if (!string.IsNullOrEmpty(v2)) {
				if (v2 == "-" || v2 == "--" || v2 == "–" || v2 == "—") {
					v2 = string.Empty;
				}

				if (v2.Contains("(") && v2.Contains(")")) {
					v2 = v2.Replace("(", string.Empty);
					v2 = v2.Replace(")", string.Empty);
					v2 = string.Format("-{0}", v2);
				}

				if (v2.Contains("$")) {
					v2 = v2.Replace("$", string.Empty);
				}

				if (v2.Contains(",")) {
					v2 = v2.Replace(",", string.Empty);
				}

				v2 = v2.Trim();

				if (v2 == "0.00") {
					v2 = "0";
				}

				return v2.Trim();
			}

			return string.Empty;
		}

		public static double? StringToDecimalToDouble(this string? val, bool roundUp) {
			var d = val.StringToDecimal();

			if (d.HasValue) {
				if (!roundUp) {
					return (double)Math.Floor(d.Value);
				} else {
					return (double)Math.Ceiling(d.Value);
				}
			}

			return null;
		}

		public static DateTime? StringYYYYMMDDtoDate(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				return DateTime.ParseExact(val.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture);
			}

			return null;
		}

		public static DateTime? StringMMDDYYYYtoDate(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				if (val.Trim().Length < 8) {
					val = string.Format("0{0}", val.Trim());
				}

				return DateTime.ParseExact(val.Trim(), "MMddyyyy", CultureInfo.InvariantCulture);
			}

			return null;
		}

		public static DateTime? StringMMDDYYtoDate(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				if (val.Trim().Length < 6) {
					val = string.Format("0{0}", val.Trim());
				}

				return DateTime.ParseExact(val.Trim(), "MMddyy", CultureInfo.InvariantCulture);
			}

			return null;
		}

		public static DateTime? StringToDate(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				if (val.ToLowerInvariant().Contains(" ")) {
					val = val.Substring(0, val.ToLowerInvariant().IndexOf(" "));
				}

				return Convert.ToDateTime(val.Trim());
			}

			return null;
		}

		public static DateTime? SmartDateTime(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				if (val.Contains("/") || val.Contains("-")) {
					return Convert.ToDateTime(val);
				}
				if (val.Length == 6) {
					return val.StringMMDDYYtoDate();
				}
				if (val.Length == 8) {
					return val.StringMMDDYYYYtoDate();
				}
			}

			return null;
		}

		public static int? StringToInt(this string? val) {
			int v = 0;

			val = val.StringSafeTrim();

			if (!string.IsNullOrEmpty(val)) {
				if (int.TryParse(val, out v)) {
					return v;
				}
			}

			return null;
		}

		public static string? TrimLeadingZeros(this string? val) {
			val = val.StringSafeTrim();

			if (!string.IsNullOrEmpty(val) && val.Length > 1) {
				while (val.StartsWith("0") && val.Length > 1) {
					val = val.Substring(1);
				}
			}

			return val;
		}

		public static string? Left(this string? val, int length) {
			if (!string.IsNullOrEmpty(val) && length > 0) {
				if (val.Length <= length) {
					return val;
				} else {
					return val.Substring(0, length);
				}
			}

			return val;
		}

		public static string? Right(this string? val, int length) {
			if (string.IsNullOrEmpty(val)) {
				return val;
			} else if (val.Length > length) {
				return val.Substring(val.Length - length, length);
			}

			return val;
		}

		public static string? TakeSafeSubstring(string? val, int start, int length) {
			if (!string.IsNullOrEmpty(val) && length > 0) {
				if (val.Length <= (length + start + 1)) {
					return val;
				} else {
					return val.Substring(start, length);
				}
			}

			return val;
		}

		public static string QuoteForCSV(this string? val, bool trim) {
			if (!string.IsNullOrEmpty(val)) {
				val = val.Replace("\r\n", " ");
				val = val.Replace("\r", " ");
				val = val.Replace("\n", " ");
				val = val.Replace("\"", "\"\"");

				if (trim) {
					return string.Format("\"{0}\"", val.Trim());
				} else {
					return string.Format("\"{0}\"", val);
				}
			}

			return string.Empty;
		}

		public static string QuoteForCSV(this decimal? val) {
			return val.QuoteForCSV(string.Empty);
		}

		public static string QuoteForCSV(this decimal? val, string pattern) {
			return val.QuoteForCSV(pattern, true);
		}

		public static string QuoteForCSV(this decimal? val, string pattern, bool trim) {
			if (val.HasValue) {
				if (string.IsNullOrEmpty(pattern)) {
					pattern = "{0:F}";
				}

				return string.Format(pattern, val).QuoteForCSV(trim);
			}

			return string.Empty;
		}

		public static string QuoteForCSV(this string? val) {
			return val.QuoteForCSV(true);
		}

		public static void WriteLineNewFile(this string data, string fileName) {
			WriteLineFile(data, fileName, false);
		}

		public static void WriteLineFile(this string data, string fileName) {
			WriteLineFile(data, fileName, true);
		}

		public static void WriteLineFile(this string data, string fileName, bool append) {
			using (var sw = new StreamWriter(fileName, append)) {
				sw.WriteLine(data);
				sw.Flush();
			}
		}

		public static void WriteNewFile(this StringBuilder sb, string fileName) {
			WriteFile(sb, fileName, false);
		}

		public static void WriteFile(this StringBuilder sb, string fileName) {
			WriteFile(sb, fileName, true);
		}

		public static void WriteFile(this StringBuilder sb, string fileName, bool append) {
			using (var sw = new StreamWriter(fileName, append)) {
				sw.Write(sb.ToString());
				sw.Flush();
			}
		}

		public static string ToYMDString(this DateTime date) {
			return date.ToString("yyyy-MM-dd");
		}

		public static DateTime GetStartDateByNumber(int year, int month) {
			return new DateTime(year, month, 1);
		}

		public static DateTime GetEndDateByNumber(int year, int month) {
			int days = DateTime.DaysInMonth(year, month);

			return new DateTime(year, month, days, 23, 59, 59);
		}

		public static DateTime GetEndOfMonthByDate(DateTime month) {
			int days = DateTime.DaysInMonth(month.Year, month.Month);

			return new DateTime(month.Year, month.Month, days, 23, 59, 59);
		}

		public static string ReadFirst2KB(string filePath) {
			return ReadFirstXKB(2, filePath);
		}

		public static string ReadFirstXKB(int kb, string filePath) {
			int bytesToRead = kb * 1024;
			byte[] buffer = new byte[bytesToRead];

			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				int bytesRead = fs.Read(buffer, 0, bytesToRead);

				return Encoding.UTF8.GetString(buffer, 0, bytesRead);
			}
		}
	}
}