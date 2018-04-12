using LunarParser;
using LunarParser.JSON;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Emulation.Utils;
using System;
using System.Numerics;

namespace Neo.Debugger.Shell
{
    public class DebugCommand : Command
    {
        public override string Name => "debug";
        public override string Help => "Debugs a contract";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            switch (args[1].ToLower())
            {
                case "run":
                    {
                        if (Shell.Debugger.IsSteppingOrOnBreakpoint)
                        {
                            output(ShellMessageType.Default, "Resuming invoke.");
                            ShellRunner.Run(Shell, output);
                        }
                        else
                        {
                            output(ShellMessageType.Error, "Start a transaction with invoke first.");
                        }
                        
                        break;
                    }
            }
        }
    }

}
