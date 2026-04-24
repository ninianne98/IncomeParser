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

		Console.WriteLine("=====================================================");
		CoreConfig.PrintDisclaimer();

		var bee = new ParserWorkerBee();
		bee.RunParser();

		Console.WriteLine("=====================================================");
		Console.WriteLine("                       DONE");
		Thread.Sleep(500);

		CoreConfig.PrintDisclaimer();
		Thread.Sleep(15 * 1000);
	}
}