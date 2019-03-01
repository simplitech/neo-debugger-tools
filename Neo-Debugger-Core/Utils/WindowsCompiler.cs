using Neo.Debugger.Core.Models;
using NeoDebuggerCore.Utils;

namespace Neo.Debugger.Core.Utils
{
    public class WindowsCompiler : NeonCompiler
    {
        public WindowsCompiler(DebuggerSettings settings) : base(settings)
        {
        }

        public override string Python3()
        {
            return "python.exe";
        }
    }
}
