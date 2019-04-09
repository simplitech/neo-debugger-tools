using LunarLabs.Parser;
using Neo.Emulation;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Debugger.Core.Models
{
    public class DebugParameters
    {
        public DebugParameters()
        {
            DefaultParams = new Dictionary<string, string>();
            Transaction = new Dictionary<byte[], BigInteger>();
        }

        public CheckWitnessMode WitnessMode { get; set; }

        public TriggerType TriggerType { get; set; }

        public Dictionary<string, string> DefaultParams { get; set; }

        public Dictionary<byte[], BigInteger> Transaction { get; set; }

        public string PrivateKey { get; set; }

        public DataNode ArgList { get; set; }

        public uint Timestamp { get; set; }

        public byte[] RawScript;
    }
}
