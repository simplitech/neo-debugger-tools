using Neo.Debugger.Core.Data;
using Neo.Debugger.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Debugger.Core.Utils
{
    public class Compiler
    {
        //Logging event handler
        public event CompilerLogEventHandler SendToLog;
        public delegate void CompilerLogEventHandler(object sender, CompilerLogEventArgs e);

        private Settings _settings;

        public Compiler(Settings settings)
        {
            _settings = settings;
        }

        public bool CompileContract(string sourceCode, string outputFilePath, SourceLanguage language)
        {
            bool success = false;
            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException("outputFilePath");

            File.WriteAllText(outputFilePath, sourceCode);

            var proc = new Process();

            var info = new ProcessStartInfo();

            switch (language)
            {
                case SourceLanguage.CSharp:
                    {
                        info.FileName = "neon.exe";
                        break;
                    }

                default:
                    {
                        Log("Unsupported languuge...");
                        return false;
                    }
            }

            info.Arguments = "\"" + outputFilePath + "\"";
            info.UseShellExecute = false;
            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;

            proc.StartInfo = info;

            try
            {
                Log("Starting compilation...");

                proc.Start();
                proc.WaitForExit();

                var log = proc.StandardOutput.ReadToEnd().Split('\n');
                string last = null;
                foreach (var temp in log)
                {
                    var line = temp.Replace("\r", "");
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    Log(line);
                    last = line;
                }

                if (proc.ExitCode != 0 || last != "SUCC")
                {
                    log = proc.StandardError.ReadToEnd().Split('\n');
                    foreach (var line in log)
                    {
                        Log(line);
                    }

                    Log("Error during compilation.");
                }
                else
                {
                    success = true;
                    Log("Compilation sucessful.");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            return success;
        }

        public void Log(string message)
        {
            SendToLog?.Invoke(this, new CompilerLogEventArgs
            {
                Message = message
            });
        }
    }
}
