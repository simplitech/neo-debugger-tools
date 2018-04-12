using Neo.Emulation.Utils;
using System;
using System.Linq;

namespace Neo.Debugger.Shell
{
    class FileCommand : Command
    {
        public override string Name => "file";
        public override string Help => "List, view or add files";

        public override void Execute(string[] args, Action<ShellMessageType, string> output)
        {
            switch (args[1].ToLower())
            {
                case "list":
                    {
                        output(ShellMessageType.Default, Shell.Debugger.AvmFilePath);
                        foreach (var entry in Shell.Debugger.Map.FileNames)
                        {
                            output(ShellMessageType.Default, entry);
                        }

                        break;
                    }

                case "view":
                    {
                        var fileName = args[2];
                        string content;                        
                        try
                        {
                            content = Shell.Debugger.GetContentFor(fileName);
                        }
                        catch
                        {
                            content = null;
                        }

                        if (string.IsNullOrEmpty(content))
                        {
                            output(ShellMessageType.Error, "File not found");
                        }
                        else
                        {
                            var lines = content.Split('\n');
                            int curLine = 0;
                            var tab = new string(' ', 6);
                            foreach (var entry in lines)
                            {
                                curLine++;
                                var pad = curLine.ToString().PadRight(4);
                                string SS = (Shell.Debugger.HasBreakpoint(curLine, fileName)) ? "X" : " ";

                                var chunkSize = 73;
                                for (int i=0; i<=99; i++)
                                {
                                    var len = chunkSize;
                                    var ofs = chunkSize * i;
                                    if (ofs + len >= entry.Length)
                                    {
                                        len = entry.Length - ofs;
                                    }

                                    if (len <= 0)
                                    {
                                        break;
                                    }

                                    var chunk = entry.Substring(ofs, len);
                                    if (i == 0)
                                    {
                                        output(ShellMessageType.Default, $"{pad} {SS} \t{chunk}");
                                    }
                                    else
                                    {
                                        output(ShellMessageType.Default, $"{tab} \t{chunk}");
                                    }
                                }
                            }
                        }

                        break;
                    }

                default:
                    {
                        output(ShellMessageType.Default, "Invalid option.");
                        break;
                    }
            }

        }
    }
}

