using Carrotware.IncomeParser.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

/*
* Carrotware Income Parser
* http://www.carrotware.com/
*
* Copyright 2025 Samantha Copeland
* Licensed under the MIT license.
*
* Date: July 2025
*/

namespace Carrotware.IncomeParser.Core {

	public static class CoreConfig {
		private static IConfiguration? _configuration = null;
		private static DateTime _date = DateTime.MinValue;
		private static ILogger _logger = NullLogger.Instance;

		public static void SetLogger() {
			if (_logger == null || _logger == NullLogger.Instance) {
				using var loggerFactory = LoggerFactory.Create(builder => {
					builder.AddConfiguration(Configuration.GetSection("Logging"));
					builder.AddConsole();
				});

				_logger = loggerFactory.CreateLogger("IncomeParser") ?? NullLogger.Instance;
			}
		}

		public static ILogger Logger {
			get {
				if (_logger == null) {
					SetLogger();
				}

				return _logger;
			}
		}

		public static IConfiguration Configuration {
			get {
				if (_configuration == null) {
					_configuration = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
						.Build();
				}

				return _configuration;
			}
		}

		public static DateTime AppDateTime {
			get {
				if (_date == DateTime.MinValue) {
					_date = DateTime.Now;
				}

				return _date;
			}
		}

		public static string OutputCSV => string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.csv", AppDateTime);

		public static string OutputCSV_Year(int year) =>
			string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.csv", year, AppDateTime);

		public static string OutputReport => string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.txt", AppDateTime);

		public static string OutputReportYear(int year) =>
			string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.txt", year, AppDateTime);

		public static string OutputReportExcel => string.Format("Statement_{0:yyMMdd}_{0:HHmmss}.xlsx", AppDateTime);

		public static string OutputReportExcelYear(int year) =>
			string.Format("Statement_{0}_{1:yyMMdd}_{1:HHmmss}.xlsx", year, AppDateTime);

		private static List<string> GetAssemblies() {
			var fldr = AppDomain.CurrentDomain.BaseDirectory ?? AppDomain.CurrentDomain.RelativeSearchPath ?? "./";

			var files = new List<string>();
			try {
				files = Directory.GetFiles(fldr, "*.dll", SearchOption.AllDirectories).ToList();
			} catch (Exception ex) {
				Logger.LogError(ex, "Failed to retrieve assembly files from folder: {Folder}", fldr);
			}

			return files;
		}

		public static ConcurrentBag<Type> ScanForBrokers() {
			var files = GetAssemblies();
			var typeList = new ConcurrentBag<Type>();
			var nsp = typeof(CoreConfig)?.Namespace?.ToLowerInvariant() ?? string.Empty;

			foreach (string file in files) {
				try {
					var assembly = Assembly.LoadFrom(file);
					var types = assembly.GetTypes();

					// discover all types implementing IBrokerSummary in one pass
					var modules = types.Where(t => t.GetInterface(nameof(IBrokerSummary)) != null).ToList();

					foreach (var m in modules.Where(x => x.Namespace != null)) {
						if (string.IsNullOrWhiteSpace(m.Namespace) == false
								&& !m.Namespace.ToLowerInvariant().StartsWith(nsp)
									&& m.IsClass
									&& !m.IsAbstract
									&& !m.Name.ToLowerInvariant().Contains("anonymoustype")) {
							typeList.Add(m);
						}
					}
				} catch (ReflectionTypeLoadException ex) {
					foreach (var loaderEx in ex.LoaderExceptions) {
						if (loaderEx != null)
							Logger.LogError(loaderEx, "Reflection error loading types from {File}", file);
					}
				} catch (Exception ex) {
					Logger.LogError(ex, "Unexpected error scanning assembly for brokers: {File}", file);
				}
			}

			return typeList;
		}

		public static void PrintDisclaimer() {
			Console.WriteLine();
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("================================================================================");
			Console.WriteLine("                                  DISCLAIMER                                    ");
			Console.WriteLine("================================================================================");
			Console.ResetColor();

			Console.WriteLine("1. AS-IS WARRANTY: THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY");
			Console.WriteLine("   KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT");
			Console.WriteLine("   HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN");
			Console.WriteLine("   ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN");
			Console.WriteLine("   CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
			Console.WriteLine("2. NOT AN EXPERT: The developer is NOT a tax professional, CPA, or financial");
			Console.WriteLine("   advisor. This software is for informational purposes only.");
			Console.WriteLine("3. RESEARCH & DISCOVERY TOOL: This application is intended solely as a");
			Console.WriteLine("   research or discovery tool to assist with high-level income tabulation");
			Console.WriteLine("   and identifying potential wash sales. It is NOT a substitute for");
			Console.WriteLine("   professional advice or official tax documentation.");
			Console.WriteLine("4. CONSULT A PROFESSIONAL: If you have questions regarding your tax situation");
			Console.WriteLine("   or do not understand the output of this tool, you must consult a qualified");
			Console.WriteLine("   financial or tax expert.");
			Console.WriteLine("5. VERIFICATION REQUIRED: The user is solely responsible for performing their");
			Console.WriteLine("   own due diligence and verifying all data generated by this tool.");

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("================================================================================");
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine();
		}
	}
}