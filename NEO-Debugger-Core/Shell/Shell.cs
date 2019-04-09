using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Debugger.Shell
{
    public abstract class Command
    {
        public DebuggerShell Shell { get; set; }

        public abstract string Name { get; }
        public abstract string Help { get; }

        public abstract void Execute(string[] args, Action<ShellMessageType, string> output);
    }

    public enum ShellMessageType
    {
        Default,
        Success,
        Error,
    }

    public class DebuggerShell
    {
        public List<Command> commands = new List<Command>();

        public string avmPath;

        public readonly DebugManager Debugger;
        private DebuggerSettings _settings;

        public DebuggerShell(DebugManager debugger) 
        {
            this.Debugger = debugger;
            this._settings = debugger.Settings;

            AddCommand(new HelpCommand());            
            AddCommand(new LoadCommand());
            AddCommand(new ContractCommand());
            AddCommand(new InvokeCommand());
            AddCommand(new StorageCommand());
            AddCommand(new BreakpointCommand());
            AddCommand(new FileCommand());
            AddCommand(new RunCommand());
            AddCommand(new StepCommand());
        }

        public void AddCommand(Command cmd)
        {
            commands.Add(cmd);
            cmd.Shell = this;
        }

        internal static string[] ParseArgs(string input)
        {
            var args = new List<string>();

            int pos = 0;
            var sb = new StringBuilder();

            char prev = '\0';

            while (pos < input.Length)
            {
                var c = input[pos];
                pos++;

                if (c == '"')
                {
                    sb.Append(c);
                    while (pos < input.Length)
                    {
                        c = input[pos];
                        pos++;

                        sb.Append(c);
                        if (c == '"') break;
                    }

                    args.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                if (c == '[')
                {
                    int temp = pos;

                    pos = input.Length - 1;
                    while (pos > temp)
                    {
                        c = input[pos];
                        if (c == ']') break;
                        pos--;
                    }


                    sb.Append('[');
                    while (temp<pos)
                    {
                        c = input[temp];
                        temp++;
                        sb.Append(c);
                    }
                    sb.Append(']');

                    pos = temp + 1;

                    args.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    if (c == ' ' || c==',')
                    {
                        if (sb.Length > 0)
                        {
                            args.Add(sb.ToString());
                            sb.Length = 0;
                        }
                    }
                    else
                        sb.Append(c);
                }

                prev = c;
            }

            if (sb.Length>0)
            {
                args.Add(sb.ToString());
            }

            return args.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        public bool Execute(string input, Action<ShellMessageType, string> output)
        {
            var temp = ParseArgs(input);
            var cmd = temp[0].ToLower();

            foreach (var entry in commands)
            {
                if (entry.Name == cmd)
                {
                    entry.Execute(temp, output);
                    return true;
                }
            }

            return false;
        }
    }

}
