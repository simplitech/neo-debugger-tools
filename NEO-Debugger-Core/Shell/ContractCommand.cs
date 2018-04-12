using Neo.Debugger.Dissambler;
using Neo.Emulation.API;
using System;
using System.IO;

namespace Neo.Debugger.Shell
{
    internal class ContractCommand : Command
    {
        public override string Name => "contract";
        public override string Help => "Loads a NEO smart contract from a file";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            if (args.Length < 2) return;

            switch (args[1].ToLower())
            {
                case "load":
                    {
                        var filePath = args[2];

                        if (File.Exists(filePath))
                        {
                            if (!Shell.Debugger.LoadContract(filePath))
                            {
                                output(ShellMessageType.Error, $"Error loading contract.");
                                return;
                            }

                            output(ShellMessageType.Success, $"Loaded {Shell.Debugger.AvmFilePath} ({Shell.Debugger.Emulator.ContractByteCode.Length} bytes)");

                            Runtime.OnLogMessage = (x => output(ShellMessageType.Default, x));

                        }                    
                        else
                        {
                            output(ShellMessageType.Error, $"File not found: {filePath}");
                        }

                        break;
                    }

                case "compile":
                    {

                        break;
                    }
            }


        }

    }

}
