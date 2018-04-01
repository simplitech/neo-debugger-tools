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

        private DebuggerSettings _settings;

        public Compiler(DebuggerSettings settings)
        {
            _settings = settings;
        }

        private string[] FetchLog(string content)
        {
            return content.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        public bool CompileContract(string sourceCode, string outputFilePath, SourceLanguage language)
        {
            if (!_settings.compilerPaths.ContainsKey(language))
            {
                Log($"{language} compiler is not configured.");
                return false;
            }

            bool success = false;

            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException("outputFilePath");

            File.WriteAllText(outputFilePath, sourceCode);

            var proc = new Process();

            var info = new ProcessStartInfo();

            info.WorkingDirectory = _settings.compilerPaths[language];

            switch (language)
            {
                case SourceLanguage.CSharp:
                    {
                        var file = Path.Combine(info.WorkingDirectory, "neon.exe");
                        if (!File.Exists(file))
                            throw new FileNotFoundException("File not found", file);

                        info.FileName = file;
                        info.Arguments = "\"" + outputFilePath + "\"";
                        break;
                    }

                case SourceLanguage.Python:
                    {
                        outputFilePath = outputFilePath.Replace("\\", "/");
                        var loadCode = $"from boa.compiler import Compiler;Compiler.load_and_save('{outputFilePath}')";
                        info.FileName = "python.exe";
                        info.Arguments = $"-c \"{loadCode}\"";
                        break;
                    }

                default:
                    {
                        Log("Unsupported languuge...");
                        return false;
                    }
            }

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

                var log = FetchLog(proc.StandardOutput.ReadToEnd());
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

                switch (language)
                {
                    case SourceLanguage.CSharp:
                        {
                            if (last == "SUCC")
                            {
                                success = true;
                            }
                            break;
                        }

                    case SourceLanguage.Python:
                        {
                            if (log.Length == 0)
                            {
                                success = true;
                            }
                            break;
                        }
                }

                log = FetchLog(proc.StandardError.ReadToEnd());
                foreach (var line in log)
                {
                    Log(line);
                }

                switch (language)
                {
                    case SourceLanguage.Python:
                        {
                            if (log.Length > 0)
                            {
                                success = false;
                            }
                            break;
                        }
                }

                if (proc.ExitCode != 0 || !success)
                {
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
