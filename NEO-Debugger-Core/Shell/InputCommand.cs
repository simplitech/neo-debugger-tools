using LunarParser;
using LunarParser.JSON;
using Neo.Emulation.API;
using Neo.Emulation.Utils;
using System;
using System.Numerics;

namespace Neo.Debugger.Shell
{
    public class InputCommand : Command
    {
        public override string Name => "input";
        public override string Help => "Set the input for a contract";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            DataNode inputs;

            try
            {
                inputs = JSONReader.ReadFromString(args[1]);
            }
            catch
            {
                output(ShellMessageType.Error, "Invalid arguments format. Must be valid JSON.");
                return;
            }

            if (args.Length >= 3)
            {
                bool valid = false;

                if (args[2].ToLower() == "with")
                {
                    if (args.Length >= 5)
                    {
                        var assetAmount = BigInteger.Parse(args[3]);
                        var assetName = args[4];

                        foreach (var entry in Asset.Entries)
                        {
                            if (entry.name == assetName)
                            {
                                output(ShellMessageType.Default, $"Attaching {assetAmount} {assetName} to transaction");
                                Shell.Debugger.Emulator.SetTransaction(entry.id, assetAmount);
                                break;
                            }
                        }

                        valid = true;
                    }
                }

                if (!valid)
                {
                    output(ShellMessageType.Error, "Invalid sintax.");
                    return;
                }

                output(ShellMessageType.Default, "Executing transaction...");

                Shell.Debugger.Emulator.Reset(inputs, null);
                Shell.Debugger.Emulator.Run();

                var val = Shell.Debugger.Emulator.GetOutput();

                Shell.Debugger.Blockchain.Save();

                output(ShellMessageType.Success, "Result: " + FormattingUtils.StackItemAsString(val));
                output(ShellMessageType.Default, "GAS used: " + Shell.Debugger.Emulator.usedGas);
            }
        }
    }
}
