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
                        var filePath = args[1];

                        if (File.Exists(filePath))
                        {
                            Shell.avmPath = filePath;

                            var bytes = File.ReadAllBytes(filePath);

                            var avmName = Path.GetFileName(filePath);
                            output(ShellMessageType.Success, $"Loaded {avmName} ({bytes.Length} bytes)");

                            string contractName;

                            var mapFile = avmName.Replace(".avm", ".debug.json");
                            if (File.Exists(mapFile))
                            {
                                var map = new NeoMapFile();
                                map.LoadFromFile(mapFile, bytes);

                                contractName = map.contractName;
                            }
                            else
                            {
                                contractName = avmName.Replace(".avm", "");
                            }

                            var address = Shell.Debugger.Blockchain.FindAddressByName(contractName);

                            if (address == null)
                            {
                                address = Shell.Debugger.Blockchain.DeployContract(contractName, bytes);
                                output(ShellMessageType.Success, $"Deployed {contractName} at address {address.keys.address}");
                            }
                            else
                            {
                                output(ShellMessageType.Default, $"Updated {contractName} at address {address.keys.address}");
                            }

                            Runtime.OnLogMessage = (x => output(ShellMessageType.Default, x));
                        }
                        else
                        {
                            output(ShellMessageType.Error, "File not found.");
                        }

                        break;
                    }

                case "input":
                    {

                        break;
                    }
            }


        }

    }

}
