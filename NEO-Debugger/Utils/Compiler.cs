using Neo.Debugger.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Debugger.Utils
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

        public bool CompileContract(string sourceCode, string outputFilePath)
        {
            bool success = false;
            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException("outputFilePath");

            File.WriteAllText(outputFilePath, sourceCode);

            var proc = new Process();
            proc.StartInfo.FileName = "neon.exe";
            proc.StartInfo.Arguments = "\"" + outputFilePath + "\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;

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
