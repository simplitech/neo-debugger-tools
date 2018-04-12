using LunarParser;
using LunarParser.JSON;
using Neo.Debugger.Core.Utils;
using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Emulation.Utils;
using System;
using System.Numerics;

namespace Neo.Debugger.Shell
{
    public static class ShellRunner
    {
        public static void Run(DebuggerShell Shell, Action<ShellMessageType, string> output)
        {
            Shell.Debugger.Run();
            var state = Shell.Debugger.State;

            switch (state.state)
            {
                case DebuggerState.State.Finished: OnFinished(Shell, output); break;

                case DebuggerState.State.Break:
                    {
                        string filePath;
                        int lineNumber;
                        lineNumber = Shell.Debugger.ResolveLine(state.offset, true, out filePath);

                        lineNumber++;
                        output(ShellMessageType.Default, $"Breakpoint hit, line {lineNumber} in {filePath}");

                        int count = 0;

                        foreach (var entry in Shell.Debugger.Emulator.Variables)
                        {
                            if (count == 0)
                            {
                                output(ShellMessageType.Default, $"Variable values:");
                            }

                            var val = FormattingUtils.StackItemAsString(entry.value, true, entry.type);
                            output(ShellMessageType.Default, $"\t{entry.name} = {val}");
                            count++;
                        }
                        break;
                    }

                default:
                    {
                        output(ShellMessageType.Default, "VM state: " + state.state);
                        break;
                    }
            }
        }

        private static void OnFinished(DebuggerShell Shell, Action<ShellMessageType, string> output)
        {
            var val = Shell.Debugger.Emulator.GetOutput();

            Shell.Debugger.Blockchain.Save();

            var methodName = Shell.Debugger.Emulator.currentMethod;
            var hintType = !string.IsNullOrEmpty(methodName) && Shell.Debugger.ABI != null && Shell.Debugger.ABI.functions.ContainsKey(methodName) ? Shell.Debugger.ABI.functions[methodName].returnType : Emulator.Type.Unknown;

            output(ShellMessageType.Success, "Result: " + FormattingUtils.StackItemAsString(val, false, hintType));
            output(ShellMessageType.Default, "GAS used: " + Shell.Debugger.Emulator.usedGas);
        }
    }

    public class InvokeCommand : Command
    {
        public override string Name => "invoke";
        public override string Help => "Invokes a contract";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            if (Shell.Debugger.IsSteppingOrOnBreakpoint)
            {
                output(ShellMessageType.Error, $"Please finish debugging the current invoke: {Shell.Debugger.Emulator.currentMethod}");
                return;
            }

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
            Shell.Debugger.Emulator.Reset(inputs, Shell.Debugger.ABI);

            ShellRunner.Run(Shell, output);
        }

    }
}
