using System;
using Neo.Debugger.Core.Models;

namespace NeoDebuggerCore.Utils
{
    public class UnixCompiler : NeonCompiler
    {
        public UnixCompiler(DebuggerSettings settings) : base(settings)
        {
        }

        public override string Python3()
        {
            return "python3";
        }
    }
}
