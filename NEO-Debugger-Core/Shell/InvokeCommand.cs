using LunarParser;
using LunarParser.JSON;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Emulation.Utils;
using System;
using System.Numerics;

namespace Neo.Debugger.Shell
{
    public class InvokeCommand : Command
    {
        public override string Name => "invoke";
        public override string Help => "Invokes a contract";

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
            }

            output(ShellMessageType.Default, "Executing transaction...");

            Shell.Debugger.Emulator.Reset(inputs, null);
            Shell.Debugger.Emulator.Run();

            var val = Shell.Debugger.Emulator.GetOutput();

            Shell.Debugger.Blockchain.Save();

            string functionName = inputs.ChildCount>0 ? inputs[0].Value : null;
            var hintType = !string.IsNullOrEmpty(functionName) && Shell.Debugger.ABI != null && Shell.Debugger.ABI.functions.ContainsKey(functionName) ? Shell.Debugger.ABI.functions[functionName].returnType : Emulator.Type.Unknown;

            output(ShellMessageType.Success, "Result: " + FormattingUtils.StackItemAsString(val, false, hintType));
            output(ShellMessageType.Default, "GAS used: " + Shell.Debugger.Emulator.usedGas);
        }
    }
}
