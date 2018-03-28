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
            if (args.Length < 2) return;

            switch (args[1].ToLower())
            {

                case "load":
                    {
                        var filePath = args[2];

                        var blockchain = Shell.Debugger.Blockchain;
                        if (blockchain.Load(filePath))
                        {                           
                            output(ShellMessageType.Error, $"Loaded blockchain, ({blockchain.currentHeight} blocks, {blockchain.AddressCount} addresses)");
                        }
                        else
                        {
                            output(ShellMessageType.Error, "File not found.");
                        }

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
