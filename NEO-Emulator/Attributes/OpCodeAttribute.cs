using Neo.Lux.Core;
using System;
using Neo.VM;

namespace Neo.Emulation
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class OpCodeAttribute : Attribute
    {
        public OpCode OpCode { get; }

        public OpCodeAttribute(OpCode opcode)
        {
            this.OpCode = opcode;
        }
    }
}
