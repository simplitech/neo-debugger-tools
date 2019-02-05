using System;
using Neo.Debugger.Core.Models;

namespace NeoDebuggerCore.Utils
{
    public class UnixCompiler : Compiler
    {
        public UnixCompiler(DebuggerSettings settings) : base(settings)
        {
        }

        public override string PythonCompilerExecutableName()
        {
            return "python3";
        }
    }
}
