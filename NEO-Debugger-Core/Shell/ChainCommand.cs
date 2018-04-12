using System;
using System.IO;

namespace Neo.Debugger.Shell
{

    internal class LoadCommand : Command
    {
        public override string Name => "chain";
        public override string Help => "Loads a virtual blockchain from a file";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            switch (args[1].ToLower())
            {

                case "load":
                    {
                        if (args.Length < 2)
                        {
                            output(ShellMessageType.Error, "File name not specified.");
                        }
                        else
                        {
                            var filePath = args[2];

                            var blockchain = Shell.Debugger.Blockchain;
                            if (blockchain.Load(filePath))
                            {
                                output(ShellMessageType.Error, $"Loaded virtual blockchain, ({blockchain.currentHeight} blocks, {blockchain.AddressCount} addresses)");
                            }
                            else
                            {
                                output(ShellMessageType.Error, "File not found.");
                            }
                        }

                        break;
                    }

                case "create":
                    {
                        if (args.Length < 2)
                        {
                            output(ShellMessageType.Error, "File name not specified.");
                        }
                        else
                        {
                            var filePath = args[2];
                            if (Shell.Debugger.LoadEmulator(filePath))
                            {
                                output(ShellMessageType.Success, "A new virtual blockchain was created at " + filePath);
                            }
                            else
                            {
                                output(ShellMessageType.Error, "Error creating virtual blockchain.");
                            }
                        }
                        break;
                    }

                case "reset":
                    {
                        var blockchain = Shell.Debugger.Blockchain;
                        blockchain.Reset();
                        output(ShellMessageType.Default, "Virtual blockchain was reset.");
                        break;
                    }

                default:
                    {
                        output(ShellMessageType.Error, "Invalid sub command.");
                        break;
                    }
            }


        }

    }
}
