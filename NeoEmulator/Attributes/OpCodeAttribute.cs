using Neo.Lux.Core;
using System;

namespace Neo.Emulation
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class OpCodeAttribute : Attribute
    {
        public Lux.VM.OpCode OpCode { get; }

        public OpCodeAttribute(Lux.VM.OpCode opcode)
        {
            this.OpCode = opcode;
        }
    }
}
