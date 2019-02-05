using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.Debugger.Shell;
using System;
using System.IO;

namespace NEO_DevShell
{
    internal class ExitCommand : Command
    {
        public override string Name => "exit";
        public override string Help => "Exits the shell";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            Environment.Exit(0);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEO Developer Shell - " + DebuggerUtils.DebuggerVersion);
            var settings = new DebuggerSettings(Directory.GetCurrentDirectory());
            var debugger = new DebugManager(settings);
            var shell = new DebuggerShell(debugger);
            shell.AddCommand(new ExitCommand());

            if (args.Length > 0)
            {
                var input = args[0].Replace("'", "\"");
                if (input.StartsWith("\"") && input.EndsWith("\""))
                {
                    input = input.Substring(1, input.Length - 2);
                }

                var lines = input.Split(';');
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine(line);
                        shell.Execute(line, OutputMessage);
                    }
                }
            }

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(">");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (!shell.Execute(input, OutputMessage))
                {
                    Console.WriteLine("Unknown command. Type HELP for list of commands.");
                }
            }
        }

        private static void OutputMessage(ShellMessageType type, string text)
        {
            switch (type)
            {
                case ShellMessageType.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case ShellMessageType.Success: Console.ForegroundColor = ConsoleColor.Green; break;
                case ShellMessageType.Default: Console.ForegroundColor = ConsoleColor.DarkGray; break;
            }
            Console.WriteLine(text);
        }
    }
}
