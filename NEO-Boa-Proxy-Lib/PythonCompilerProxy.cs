using System;
using System.Diagnostics;
using System.Linq;

namespace Neo_Boa_Proxy_Lib
{
    public class PythonCompilerProxy
    {
        public static bool Execute(string outputFilePath, string pythonExecutableName, Action<string> logCallback)
        {
            bool success = false;
            var proc = new Process();
            var info = new ProcessStartInfo();

            var loadCode = $"from boa.compiler import Compiler;Compiler.load_and_save('{outputFilePath}')";
            info.FileName = pythonExecutableName;
            info.Arguments = $"-c \"{loadCode}\"";

            info.UseShellExecute = false;
            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;

            proc.StartInfo = info;

            try
            {
                logCallback("Starting compilation...");

                proc.Start();
                proc.WaitForExit();

                var log = FetchLog(proc.StandardOutput.ReadToEnd());
                string last = null;
                foreach (var temp in log)
                {
                    var line = temp.Replace("\r", "");
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    logCallback(line);
                    last = line;
                }

                if (log.Length == 0)
                {
                    success = true;
                }

                log = FetchLog(proc.StandardError.ReadToEnd());
                foreach (var line in log)
                {
                    logCallback(line);
                }

                if (log.Length > 0)
                {
                    success = false;
                }

                if (proc.ExitCode != 0 || !success)
                {
                    logCallback("Error during compilation.");
                }
                else
                {
                    success = true;
                    logCallback("Compilation successful.");
                }
            }
            catch (Exception ex)
            {
                logCallback(ex.Message);
            }

            return success;
        }

        private static string[] FetchLog(string content)
        {
            return content.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

    }
}
