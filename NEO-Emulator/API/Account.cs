using LunarParser;
using Neo.Cryptography;
using Neo.Emulation.Utils;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Emulation.API
{
    public class Account
    {
        public string name;
        public KeyPair keys;

        public byte[] byteCode;

        public Storage storage = new Storage();

        public Dictionary<string, decimal> balances = new Dictionary<string, decimal>();

        public bool HasNonZeroBalance
        {
            get
            {
                foreach (var amount in balances.Values)
                {
                    if (amount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal bool Load(DataNode root)
        {
            this.name = root.GetString("name");
            this.byteCode = root.GetString("code").HexToByte();

            var privKey = root.GetString("key").HexToByte();

            if (privKey.Length != 32)
            {
                return false;
            }

            this.keys = new KeyPair(privKey);

            var storageNode = root.GetNode("storage");

            if (storageNode != null)
            {
                this.storage.Load(storageNode);
            }
            else
            {
                this.storage = null;
            }

            this.balances.Clear();
            var balanceNode = root.GetNode("balance");
            if (balanceNode != null)
            {
                foreach (var child in balanceNode.Children)
                {
                    if (child.Name == "entry")
                    {
                        var symbol = child.GetString("symbol");
                        var amount = child.GetDecimal("amount");

                        balances[symbol] = amount;
                    }
                }
            }

            return true;
        }

        public DataNode Save()
        {
            var result = DataNode.CreateObject("address");

            if (this.name != null)
            {
                result.AddField("name", this.name);
            }

            result.AddField("hash", this.keys.PrivateKey.ByteToHex());
            result.AddField("key", this.keys.PrivateKey.ByteToHex());
            if (this.byteCode != null)
            {
                result.AddField("code", this.byteCode.ByteToHex());
            }

            if (this.storage != null)
            {
                result.AddNode(this.storage.Save());
            }

            return result;
        }

        [Syscall("Neo.Account.GetScriptHash")]
        public static bool ScriptHash(ExecutionEngine engine)
        {
            //return byte[] 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Account.GetVotes")]
        public static bool GetVotes(ExecutionEngine engine)
        {
            //byte[][] 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Account.SetVotes", 1)]
        public static bool SetVotes(ExecutionEngine engine)
        {
            //byte[][] 
            throw new NotImplementedException();
        }

        [Syscall("Neo.Account.GetBalance")]
        public static bool GetBalance(ExecutionEngine engine)
        {
            //byte[] asset_id
            //return long
            throw new NotImplementedException();
        }
    }
}
