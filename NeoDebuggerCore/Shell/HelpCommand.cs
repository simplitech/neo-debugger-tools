using System;

namespace Neo.Debugger.Shell
{
    internal class HelpCommand : Command
    {
        public override string Name => "help";
        public override string Help => "Prints this list of commands";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            foreach (var cmd in Shell.commands)
            {
                output(ShellMessageType.Default, cmd.Name + ": " + cmd.Help);
            }
        }
    }
}
