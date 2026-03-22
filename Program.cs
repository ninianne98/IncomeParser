internal class Program {

	private static void Main(string[] args) {
		Console.WriteLine("=====================================================");
		PrintDisclaimer();

		var bee = new ParserWorkerBee();
		bee.RunParser();

		Console.WriteLine("=====================================================");
		Console.WriteLine("                       DONE");
		Thread.Sleep(500);

		PrintDisclaimer();
		Thread.Sleep(15 * 1000);
	}

	public static void PrintDisclaimer() {
		Console.WriteLine();
		Console.WriteLine();
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("================================================================================");
		Console.WriteLine("                                  DISCLAIMER                                    ");
		Console.WriteLine("================================================================================");
		Console.ResetColor();

		Console.WriteLine("1. USE AT YOUR OWN RISK: This tool is provided 'as-is' without any warranties.");
		Console.WriteLine("2. NOT FINANCIAL/TAX ADVICE: The developer is not a financial advisor or tax");
		Console.WriteLine("   expert. This software is for informational purposes only.");
		Console.WriteLine("3. VERIFICATION REQUIRED: This tool is designed to help identify potential issues,");
		Console.WriteLine("   high level income tabulation, potential wash sales, etc. but it is NOT a substitute");
		Console.WriteLine("   for professional advice. The taxpayer is solely responsible for performing their");
		Console.WriteLine("   own due diligence and verifying all data.");
		Console.WriteLine("4. LIABILITY: The developer shall not be held liable for any financial losses,");
		Console.WriteLine("   tax penalties, or errors resulting from the use of this software.");

		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("================================================================================");
		Console.ResetColor();
		Console.WriteLine();
		Console.WriteLine();
	}
}