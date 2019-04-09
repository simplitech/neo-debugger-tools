using LunarLabs.Parser;
using Neo.Lux.Cryptography;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Emulation.API
{
    public class Block : Header
    {
        public uint height;

        private List<Transaction> _transactions = new List<Transaction>();
        public IEnumerable<Transaction> Transactions { get { return _transactions; } }

        public int TransactionCount { get { return _transactions.Count; } }

        public Block(uint height, uint timestamp, uint consensusData, UInt256 hash) : base(timestamp, consensusData, hash)
        {
            this.height = height;
        }

        internal void AddTransaction(Transaction tx)
        {
            if (_transactions.Contains(tx))
            {
                return;
            }

            _transactions.Add(tx);
        }

        public Transaction GetTransactionByIndex(int index)
        {
            return _transactions[index];
        }

        internal bool Load(DataNode root)
        {
            this.timestamp = root.GetUInt32("timestamp");
			this.consensusData = root.GetUInt32("consensusData");
			this.hash = UInt256.Parse(root.GetString("hash"));


			this._transactions.Clear();

            foreach (var child in root.Children)
            {
                if (child.Name == "transaction")
                {
                    var tx = new Transaction(this);
                    tx.Load(child);
                    _transactions.Add(tx);
                }
            }

            return true;
        }

        public DataNode Save()
        {
            var result = DataNode.CreateObject("block");
            foreach (var tx in _transactions)
            {
                result.AddNode(tx.Save());
            }

            result.AddField("timestamp", timestamp);
			result.AddField("consensusData", consensusData);
			result.AddField("hash", hash.ToString());

            return result;
        }

        [Syscall("Neo.Block.GetTransactionCount")]
        public bool GetTransactionCount(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            var obj = context.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            context.EvaluationStack.Push(block._transactions.Count);
            return true;
        }

        [Syscall("Neo.Block.GetTransactions")]
        public bool GetTransactions(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            var obj = context.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            // returns Transaction[]

            var txs = new StackItem[block._transactions.Count];

            int index = 0;
            foreach (var tx in block.Transactions)
            {
                txs[index] = new VM.Types.InteropInterface<Transaction>(tx);
                index++;
            }

            var array = new VM.Types.Array(txs);

            throw new NotImplementedException();
        }

        
        [Syscall("Neo.Block.GetTransaction")]
        public bool GetTransaction(ExecutionEngine engine)
        {
            var context = engine.CurrentContext;
            var index = (int)context.EvaluationStack.Pop().GetBigInteger();
            var obj = context.EvaluationStack.Pop();
            var block = ((VM.Types.InteropInterface)obj).GetInterface<Block>();

            if (block == null)
                return false;

            if (index<0 || index>=block._transactions.Count)
            {
                return false;
            }

            var tx = block.GetTransactionByIndex(index);
            context.EvaluationStack.Push(new VM.Types.InteropInterface<Transaction>(tx));
            return true;
        }

    }
}
