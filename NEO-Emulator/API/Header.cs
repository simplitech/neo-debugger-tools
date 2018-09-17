using Neo.VM;
using System;
using System.Numerics;

namespace Neo.Emulation.API
{
    public class Header: IInteropInterface
    {
        public uint timestamp;
		public BigInteger consensusData = 0;

        public Header(uint timestamp, uint consensusData)
        {
            this.timestamp = timestamp;
			this.consensusData = consensusData;
		}

        [Syscall("Neo.Header.GetHash")]
        public static bool GetHash(ExecutionEngine engine)
        {
            // Header
            //returns  byte[] 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Header.GetVersion")]
        public static bool GetVersion(ExecutionEngine engine)
        {
            // Header
            //returns uint 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Header.GetPrevHash")]
        public static bool GetPrevHash(ExecutionEngine engine)
    {
            // Header
            //returns byte[]
            throw new NotImplementedException();
        }

        [Syscall("Neo.Header.GetMerkleRoot")]
        public static bool GetMerkleRoot(ExecutionEngine engine)
        {
            // Header
            //returns  byte[] 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Header.GetTimestamp")]
        public static bool GetTimestamp(ExecutionEngine engine)
        {
            var obj = engine.EvaluationStack.Pop() as VM.Types.InteropInterface;

            if (obj == null)
            {
                return false;
            }

            var header = obj.GetInterface<Header>();

            engine.EvaluationStack.Push(header.timestamp);
            return true;
        }

        [Syscall("Neo.Header.GetConsensusData")]
        public static bool GetConsensusData(ExecutionEngine engine)
        {
			var obj = engine.EvaluationStack.Pop() as VM.Types.InteropInterface;

			if (obj == null)
			{
				return false;
			}

			var header = obj.GetInterface<Header>();
			engine.EvaluationStack.Push(header.consensusData);
			// Header
			//returns ulong 
			return true;
        }

        [Syscall("Neo.Header.GetNextConsensus")]
        public static bool GetNextConsensus(ExecutionEngine engine)
        {
            // Header
            //returns byte[] 
            throw new NotImplementedException();
        }
    }
}
