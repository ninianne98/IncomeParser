internal class Program {

	private static void Main(string[] args) {
		Console.WriteLine("=====================================================");

		var bee = new ParserWorkerBee();
		bee.RunParser();


		Console.WriteLine("=====================================================");
		Console.WriteLine("                       DONE");
		Thread.Sleep(15 * 1000);
	}
}