using Carrotware.IncomeParser;
using Carrotware.IncomeParser.Core;

/*
* Carrotware Income Parser
* http://www.carrotware.com/
*
* Copyright 2025 Samantha Copeland
* Licensed under the MIT license.
*
* Date: July 2025
*/

internal class Program {

	private static void Main(string[] args) {
		CoreConfig.SetLogger();

		CoreConfig.PrintAppName();

		if (!CoreConfig.PrintDisclaimer(true)) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("\nTerms not accepted. Application will exit.\n");
			Console.ResetColor();

			Thread.Sleep(15 * 1000);
			return;
		}

		var bee = new ParserWorkerBee();
		bee.RunParser();
		Thread.Sleep(250);

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine();
		Console.WriteLine("=====================================================");
		Console.WriteLine("                       DONE");
		Console.ResetColor();
		Thread.Sleep(500);

		CoreConfig.PrintDisclaimer();

		CoreConfig.PrintAppName();

		Thread.Sleep(15 * 1000);
	}
}