using Neo.Emulation.Utils;
using System;

namespace Neo.Debugger.Shell
{
    class BreakpointCommand : Command
    {
        public override string Name => "breakpoint";
        public override string Help => "List, set or remove breakpoins";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            switch (args[1].ToLower())
            {
                case "list":
                    {
                        int count = 0;
                        foreach (var entry in Shell.Debugger.Breakpoints)
                        {
                            output(ShellMessageType.Default, $"Line {entry.lineNumber}, {entry.filePath}" );
                            count++;
                        }

                        if (count == 0)
                        {
                            output(ShellMessageType.Default, "No breakpoints set.");
                        }

                        break;
                    }

                case "set":
                    {
                        if (args.Length >= 3)
                        {
                            int lineNumber = int.Parse(args[2]);
                            var fileName = args[3];
                            
                            Shell.Debugger.AddBreakpoint(lineNumber - 1, fileName);
                            output(ShellMessageType.Default, $"Breakpoint set in line {lineNumber} of {fileName}");
                        }
                        else
                        {
                            output(ShellMessageType.Default, "Line and file not specified.");
                        }

                        break;
                    }

                case "remove":
                    {
                        if (args.Length >= 3)
                        {
                            int lineNumber = int.Parse(args[2]);
                            var fileName = args[3];
                            Shell.Debugger.RemoveBreakpoint(lineNumber - 1, fileName);
                            output(ShellMessageType.Default, $"Breakpoint removd from line {lineNumber} of {fileName}");
                        }
                        else
                        {
                            output(ShellMessageType.Default, "Line and file not specified.");
                        }

                        break;
                    }

                default:
                    {
                        output(ShellMessageType.Default, "Invalid option.");
                        break;
                    }
            }

        }
    }
}

