using System.Globalization;
using System.Text;

namespace Carrotware.IncomeParser.Helpers {

	public static class ParseHelper {

		public static string? StringSafeTrim(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				return val.Trim();
			}

			return null;
		}

		public static decimal? StringToDecimal(this string? val) {
			if (!string.IsNullOrEmpty(val)) {
				val = val.MoneyStringClean();

				return Convert.ToDecimal(val);
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
			if (!string.IsNullOrEmpty(val)) {
				val = val.StringSafeTrim();

				if (val == "-") {
					val = string.Empty;
				}

				if (val.Contains("(") && val.Contains(")")) {
					val = val.Replace("(", string.Empty);
					val = val.Replace(")", string.Empty);
					val = string.Format("-{0}", val);
				}

				if (val.Contains("$")) {
					val = val.Replace("$", string.Empty);
				}

				if (val.Contains(",")) {
					val = val.Replace(",", string.Empty);
				}

				val = val.Trim();

				if (val == "0.00") {
					val = "0";
				}

				return val.Trim();
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
				if (val.ToLowerInvariant().Contains("as of")) {
					val = val.Substring(0, val.ToLowerInvariant().IndexOf("as of"));
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

		public static string Left(this string? val, int length) {
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
	}
}