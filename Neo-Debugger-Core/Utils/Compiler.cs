using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Neo.Debugger.Core.Data;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;

namespace NeoDebuggerCore.Utils
{
    public abstract class Compiler
    {
        internal DebuggerSettings _settings;
        public event CompilerLogEventHandler SendToLog;
        public delegate void CompilerLogEventHandler(object sender, CompilerLogEventArgs e);
		public abstract string PythonCompilerExecutableName();
		
        public Compiler(DebuggerSettings settings)
        {
            _settings = settings;
        }

        public static Compiler GetInstance(DebuggerSettings settings)
        {
            var platform = Environment.OSVersion.Platform;
            Compiler compiler;
            if(platform == PlatformID.Win32NT)
            {
                compiler = new WindowsCompiler(settings);
            }
            else
            {
                compiler = new UnixCompiler(settings);
            }

            return compiler;
        }


        private string[] FetchLog(string content)
        {
            return content.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        // #64
        public bool CompileContract(string sourceCode, string outputFilePath, SourceLanguage language, string compilerPath = null)
        {
            bool success = false;

            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException("outputFilePath");

            File.WriteAllText(outputFilePath, sourceCode);

            var proc = new Process();

            var info = new ProcessStartInfo();

            if (compilerPath == null)
            {
                info.WorkingDirectory = _settings.compilerPaths[language];
            }
            else
            {
                info.WorkingDirectory = compilerPath;
            }

            switch (language)
            {
                case SourceLanguage.CSharp:
                    {
                        var compilePath = Path.Combine(info.WorkingDirectory, NeonDotNetExecutableName());
                        if (!File.Exists(compilePath))
                            throw new FileNotFoundException("Compiler not found.\nExpected path: " + compilePath, compilePath);

						info.FileName = DotNetExecutableName();
                        info.Arguments = compilePath + " \"" + outputFilePath + "\"";
                        break;
                    }

                case SourceLanguage.Python:
                    {
                        outputFilePath = outputFilePath.Replace("\\", "/");
                        var loadCode = $"from boa.compiler import Compiler;Compiler.load_and_save('{outputFilePath}')";
                        info.FileName = PythonCompilerExecutableName();
                        info.Arguments = $"-c \"{loadCode}\"";
                        break;
                    }

                default:
                    {
                        Log("Unsupported language...");
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
                    Log("Compilation successful.");
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

		private string NeonDotNetExecutableName()
		{
			return "Neo-Compiler.dll";
		}

		private string DotNetExecutableName()
		{
			return "dotnet";
		}
	}
}
