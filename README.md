# Income Parser

Income Parser is a C# .NET 10.0 tool designed to aggregate brokerage transaction data, identify potential wash sales across multiple accounts, and generate detailed financial reports for tax research and discovery.

## Use Cases

*   **Loss Harvesting & Tax Planning**: Visualize potential capital gains/losses throughout the year to make informed trading decisions.
*   **Multi-Broker Aggregation**: Consolidate trade data and income (dividends, interest) from various brokerage firms into a single unified view.
*   **Wash Sale Detection**: Identify potential wash sales by scanning transactions across different accounts and symbols within the IRS-defined 61-day window (30 days before/after a loss).
*   **Quarterly Tax Estimation**: Manage quarterly tax rates and estimated payments to visualize potential liabilities throughout the fiscal year.
*   **Automated Reporting**: Generate professional Excel workbooks featuring:
*   **Multi-Broker Aggregation**: Consolidate trade data and income (dividends, interest) from various brokerage firms into a single unified view.
*   **Wash Sale Detection**: Identify potential wash sales by scanning transactions across different accounts and symbols within the IRS-defined 61-day window (30 days before/after a loss).
*   **Quarterly Tax Estimation**: Manage quarterly tax rates and estimated payments to visualize potential liabilities throughout the fiscal year.
*   **Automated Reporting**: Generate professional Excel workbooks featuring:
    *   Quarterly income summaries.
    *   Detailed sale histories (Long-term vs. Short-term).
    *   Visualized monthly income and sales trends using Clustered Column charts.
    *   Wash sale adjustments and audit trails.

## Getting Started

### Prerequisites
*   [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Installation
1. Clone the repository.
2. **Build the solution:**
   *   **Command Line:** Run `dotnet build` from the root directory.
   *   **Visual Studio:** Open the solution file (`.sln`) and select **Build > Build Solution** (or press `Ctrl+Shift+B`).
3. **Deploy Broker Modules:** Ensure that the compiled brokerage `.dll` files (e.g., `BrokerSchwab.dll`, `BrokerFidelity.dll`) are located in the same directory as the `IncomeParser.exe` executable and settings file `appsettings.json` for the plugin system to discover them.
4. **Data Setup:**
    *   Place your brokerage CSV files in the directory specified by `MainDocumentFolder` in `appsettings.json`.
    *   The application includes an interactive menu (driven by `TaxDataCollector`) to configure quarterly estimated tax payments and tax rates.
    *   Any changes made through this menu will be saved to a `TaxYear_YYYY.json` file (e.g., `TaxYear_2024.json`) within the `MainDocumentFolder` specified in `appsettings.json`. This file stores your customized tax year data.


## Configuration

The application utilizes an `appsettings.json` file for configuration. 

### Key Settings
*   `MainDocumentFolder`: The root directory where brokerage CSV files are stored and reports are generated.
*   `TaxRatesPercent`: Default tax rates for different income types (Dividend, Interest, LT/ST Capital Gains).
*   `SecurityAliases`: Map different ticker symbols that represent "substantially identical" securities for wash sale tracking.  Some common tickers have been added, however, this is not exhaustive.  The user should perform their own due diligence.
    *   **User Awareness**: The application cannot automatically determine which different tickers are equivalent (e.g., stock share classes like `GOOG`/`GOOGL` or `BRK.A`/`BRK.B`, alternate share classes of the same mutual fund like `WSHFX`/`WSHCX`/`AWSHX`, mutual funds like `SWPPX`/`VFIAX`/`FXAIX`/`PREIX` which all track the `S&P 500 Index`,  or ETFs like `SPY`/`SPYM`/`VOO`/`IVV` which all track the `S&P 500 Index`). You must have awareness of your holdings to build this list.
    *   **Identification Strategy**: The most effective way to build this list is to review the **net loss transactions** in your generated reports. If you identify a net loss in a security, it is suggested to research alternate share classes or alternate funds using the same index, to add those tickers to this list.  Many fund sponsors and on-line databases can aid in this research.

### Example appsettings.json

```json
{
  "MainDocumentFolder": "C:\\Users\\Username\\Documents\\TaxData",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "IncomeParser.Core": "Debug"
    }
  },
  "TaxRatesPercent": {
    "ShortTerm": 0.24,
    "LongTerm": 0.15,
    "Dividend": 0.15,
    "Interest": 0.24
  },
  "SecurityAliases": [
    "QQQ,QQQM,USNQX",
	"SWPPX,VOO,SPY,SPYM,IVV",
    "WSHFX,WSHCX,AWSHX"
  ]
}

```

### Broker File Naming Conventions
The application identifies brokerage files based on keywords found in their filenames or directory paths. These keywords are defined by each broker's `BrokerPathFragments` property.

For example, within `appsettings.json` the setting for `MainDocumentFolder` points at `C:\Users\Username\Documents\TaxData\` and there are sub directories for each brokerage type.

Any brokerage specific parser you build can leverage the example classes provided, and will be subject to any rules you establish.

*   **Schwab**: Paths should contain "schwab" or "chuck" (e.g., `C:\Users\Username\Documents\TaxData\Schwab\Mine XXX777_GainLoss_Realized_Details_20260102.csv`).
*   **Fidelity**: Paths should contain "fidelity" or "netbenefits" (e.g., `C:\Users\Username\Documents\TaxData\Fidelity\BrokerageLink 77XXXX_History_for_Account_2025_77777777.csv`).
*   **Merrill Edge**: Paths should contain "merrill" or "ml-edge" (e.g., `C:\Users\Username\Documents\TaxData\Merrill\SettledActivity_052025_052025.csv`).
*   **Chase**: Paths should contain "chase" or "jpmorgan" (e.g., `C:\Users\Username\Documents\TaxData\Chase\realizedGainOrLoss_260131.csv`).

**Note on File Pairing**: Schwab and Fidelity do not consistently include the account # or nickname in either the transaction or gain/loss export file contents or filename. Be sure to name the corresponding files for these two brokerages so that they have the same naming prefix.


## Architecture & Extensibility

Income Parser is built with a modular, plugin-based architecture:

*   **Core**: Contains the central engine, configuration logic, and shared interfaces.
*   **Main**: The command-line interface (cli) entry point.
*   **Brokers**: Individual projects (e.g., `Schwab`, `Fidelity`, `Merrill`, `Chase`) that implement broker-specific parsing logic.

### Adding a New Broker
The system uses reflection to automatically discover broker modules at runtime. To add a new broker:
1. Create a new class library project.
2. Implement the `IBrokerSummary` interface.
3. Ensure the class is not abstract and has a valid namespace.
4. Place the compiled `.dll` in the application's base directory. The `CoreConfig.ScanForBrokers()` method will automatically register it.

## Output Files

Reports are generated with standardized naming conventions for easy tracking:

| Format | Naming Convention |
| :--- | :--- |
| **Excel** | `Statement_YEAR_YYMMDD_HHMMSS.xlsx` |
| **Text** | `Statement_YEAR_YYMMDD_HHMMSS.txt` |

*Note: When a specific tax year is detected, based on the majority of dates in the loaded data, the filename will be prefixed with the year (e.g., `Statement_2024_...`).*

## Disclaimer

**1. AS-IS WARRANTY:** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

**2. NOT AN EXPERT:** The developer is **NOT** a tax professional, CPA, or financial advisor. This software is for informational purposes only.

**3. RESEARCH & DISCOVERY TOOL:** This application is intended solely as a research or discovery tool to assist with high-level income tabulation and identifying potential wash sales. It is **NOT** a substitute for professional advice or official tax documentation.

**4. CONSULT A PROFESSIONAL:** If you have questions regarding your tax situation or do not understand the output of this tool, you must consult a qualified financial or tax expert.

**5. VERIFICATION REQUIRED:** The user is solely responsible for performing their own due diligence and verifying all data generated by this tool.

## License

The MIT License (MIT)

Copyright (c) 2025 Samantha Copeland

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
