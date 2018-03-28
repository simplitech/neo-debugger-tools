using Neo.Debugger.Core.Utils;
using Neo.Debugger.Shell;
using System;

namespace NEO_DevShell
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NEO Developer Shell - "+ DebuggerUtils.DebuggerVersion);
            var shell = new DebuggerShell();

            if (args.Length>0)
            {
                shell.Execute("load " + args[0]);
            }

            while (true)
            {
                Console.Write(">");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (!shell.Execute(input))
                {
                    Console.WriteLine("Unknown command. Type HELP for list of commands.");
                }
            }
        }
    }
}
