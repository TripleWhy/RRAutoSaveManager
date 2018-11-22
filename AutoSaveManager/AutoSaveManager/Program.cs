namespace AutoSaveManager
{
	using System;

	class Program
	{
		private static void Main(string[] args)
		{
			using (AutoSaveManager asm = new AutoSaveManager())
			{
				asm.StartWatching();
				Console.WriteLine("Press \'q\' to quit.");
				while(Console.Read()!='q');
			}
		}
	}
}
