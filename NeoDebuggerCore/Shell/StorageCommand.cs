using Neo.Emulation.Utils;
using System;

namespace Neo.Debugger.Shell
{
    internal class StorageCommand : Command
    {
        public override string Name => "storage";
        public override string Help => "View content of storage";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            var storage = Shell.Debugger.Emulator.currentAccount.storage;
            foreach (var entry in storage.entries)
            {
                output(ShellMessageType.Default, FormattingUtils.OutputData(entry.Key, false) + " => " + FormattingUtils.OutputData(entry.Value, false));
            }
        }
    }
}
