using Neo.Emulator;
using Neo.Emulator.API;
using System;
using System.Windows.Forms;

namespace Neo.Debugger.Forms
{
    public partial class BlockchainForm : Form
    {
        private Blockchain _blockchain;
        public BlockchainForm(Blockchain blockchain)
        {
            InitializeComponent();
            _blockchain = blockchain;
        }

        private void BlockchainForm_Shown(object sender, System.EventArgs e)
        {
            var blockchain = _blockchain;

            treeView2.Nodes.Clear();
            foreach (var block in blockchain.Blocks)
            {
                var blockNode = treeView2.Nodes.Add("Block #" + block.height);
                var tsNode = blockNode.Nodes.Add("Timestamp");
                tsNode.Nodes.Add(block.timestamp.ToDateTime().ToString());

                var txsNode = blockNode.Nodes.Add("Transactions");

                foreach (var tx in block.Transactions)
                {
                    var txNode = txsNode.Nodes.Add(tx.hash.ByteToHex());

                    if (tx.inputs.Count > 0)
                    {
                        var inputNode = txNode.Nodes.Add("Inputs");
                        foreach (var input in tx.inputs)
                        {
                            var prevHashNode = inputNode.Nodes.Add(input.prevHash.ByteToHex());
                        }
                    }

                    if (tx.outputs.Count > 0)
                    {
                        var outputNode = txNode.Nodes.Add("Outputs");
                        foreach (var output in tx.outputs)
                        {
                            var hashLbNode = outputNode.Nodes.Add("Hash");
                            var hashNode = hashLbNode.Nodes.Add(output.hash.ToString());
                        }
                    }
                }
            }

            treeView1.Nodes.Clear();

            foreach (var address in blockchain.Addresses)
            {
                var node = treeView1.Nodes.Add(address.keys.address);

                if (!string.IsNullOrEmpty(address.name))
                {

                }

                if (address.balances.Count > 0)
                {
                    var balanceNode = node.Nodes.Add("Balances");
                    

                    foreach (var balance in address.balances)
                    {
                        var child = balanceNode.Nodes.Add(balance.Key);
                        var value = child.Nodes.Add(balance.Value.ToString());
                    }
                }
            }
        }

        private Transaction FindByHash(string hash)
        {
            var blockchain = _blockchain;
            foreach (var block in blockchain.Blocks)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.hash.ToHexString() == hash)
                    {
                        return tx;
                    }
                }
            }

            return null;
        }

    }
}
