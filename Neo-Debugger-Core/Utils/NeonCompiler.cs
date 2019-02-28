using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Neo.Compiler;
using Neo.Debugger.Core.Data;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.DebuggerCompiler;
using Neo_Boa_Proxy_Lib;

namespace NeoDebuggerCore.Utils
{
    public abstract class NeonCompiler : ILogger
    {
        internal DebuggerSettings _settings;
        public event CompilerLogEventHandler SendToLog;
        public delegate void CompilerLogEventHandler(object sender, CompilerLogEventArgs e);
		public abstract string PythonCompilerExecutableName();
		
        public NeonCompiler(DebuggerSettings settings)
        {
            _settings = settings;
        }

        public static NeonCompiler GetInstance(DebuggerSettings settings)
        {
            var platform = Environment.OSVersion.Platform;
            NeonCompiler compiler;
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


        public bool CompileCSharpContract(string sourceCode, string outputFilePath)
        {
            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException(nameof(outputFilePath));

            File.WriteAllText(outputFilePath, sourceCode);

            if (CSharpCompiler.Execute(outputFilePath, this))
            {
                SendToLog?.Invoke(this, new CompilerLogEventArgs() { Message = "SUCC" });
            }
            else
            {
                SendToLog?.Invoke(this, new CompilerLogEventArgs() { Message = "Compilation failed" });
            }

            return true;
        }

        public bool CompilePythonContract(string sourceCode, string outputFilePath, string compilerPath = null)
        {
            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException(nameof(outputFilePath));

            File.WriteAllText(outputFilePath, sourceCode);

            if (PythonCompilerProxy.Execute(outputFilePath, PythonCompilerExecutableName(), (m) => { SendToLog?.Invoke(this, new CompilerLogEventArgs() { Message = m }); }, compilerPath))
            {
                SendToLog?.Invoke(this, new CompilerLogEventArgs() { Message = "SUCC" });
            }
            else
            {
                SendToLog?.Invoke(this, new CompilerLogEventArgs() { Message = "Compilation failed" });
            }

            return true;
        }

        public bool CompileContract(string sourceCode, string outputFilePath, SourceLanguage language, string compilerPath = null)
        {
            if (string.IsNullOrEmpty(outputFilePath))
                throw new ArgumentNullException(nameof(outputFilePath));

            File.WriteAllText(outputFilePath, sourceCode);

            if (language == SourceLanguage.CSharp)
            {
                return CompileCSharpContract(sourceCode, outputFilePath);
            }
            else if (language == SourceLanguage.Python)
            {
                return CompilePythonContract(sourceCode, outputFilePath, compilerPath);
            }
            else 
            {
                throw new NotSupportedException(); 
            }

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
