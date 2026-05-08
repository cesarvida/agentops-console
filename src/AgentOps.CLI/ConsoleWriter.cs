using System;
namespace AgentOps.CLI
{
    public class ConsoleWriter : IConsoleWriter
    {
        public void WriteLine(string message) => Console.WriteLine(message);
        public void WriteSuccess(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }
        public void WriteError(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }
        public void WriteWarning(string message)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }
    }
}
