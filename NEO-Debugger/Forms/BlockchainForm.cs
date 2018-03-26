using Neo.Debugger.Utils;
using Neo.Emulation;
using Neo.Emulation.API;
using System.Collections.Generic;
using System.Numerics;
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

        private Account fromAccount;

        private Dictionary<TreeNode, Account> nodeMap = new Dictionary<TreeNode, Account>();

        private void BlockchainForm_Shown(object sender, System.EventArgs e)
        {
            fromAccount = null;
            Reload();
        }

        private void Reload()
        {
            nodeMap.Clear();

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
                        int index = 1;
                        foreach (var output in tx.outputs)
                        {
                            var outLbNode = outputNode.Nodes.Add("Output #" + index);

                            var hashLbNode = outLbNode.Nodes.Add("Hash");
                            var hashNode = hashLbNode.Nodes.Add(output.hash.ToString());

                            var address = Cryptography.Crypto.Default.ToAddress(output.hash);
                            var addrLbNode = outLbNode.Nodes.Add("Address");
                            var addrNode = addrLbNode.Nodes.Add(address);

                            var assetLbNode = outLbNode.Nodes.Add("Asset");
                            try
                            {
                                var assetName = Asset.GetAssetName(output.assetID);
                                var assetNode = assetLbNode.Nodes.Add(output.amount.ToString() + " " + assetName);
                            }
                            catch
                            {
                                var assetNode = assetLbNode.Nodes.Add("ERROR");
                            }

                            index++;
                        }
                    }
                }
            }

            treeView1.Nodes.Clear();

            foreach (var address in blockchain.Accounts)
            {
                var node = treeView1.Nodes.Add(address.keys.address);
                nodeMap[node] = address;


                if (!string.IsNullOrEmpty(address.name))
                {
                    var nameLbNode = node.Nodes.Add("Name");
                    nodeMap[nameLbNode] = address;

                    var nameNode = nameLbNode.Nodes.Add(address.name);
                    nodeMap[nameNode] = address;
                }

                var balanceNode = node.Nodes.Add("Balances");


                foreach (var entry in Asset.Entries)
                {
                    decimal val = address.balances.ContainsKey(entry.name) ? address.balances[entry.name] : 0;
                    var child = balanceNode.Nodes.Add(val.ToString() + " " + entry.name);
                    nodeMap[child] = address;
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

        private Account selectedAddress;

        private void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                selectedAddress = nodeMap.ContainsKey(e.Node) ? nodeMap[e.Node] : null;

                markAsFromToolStripMenuItem.Enabled = selectedAddress != null && fromAccount != selectedAddress && selectedAddress.HasNonZeroBalance;
                sendAssetsToolStripMenuItem.Enabled = fromAccount != null && fromAccount != selectedAddress;

                nEOToolStripMenuItem.Enabled = fromAccount != null && fromAccount.balances.ContainsKey("NEO") && fromAccount.balances["NEO"] > 0;
                gASToolStripMenuItem.Enabled = fromAccount != null && fromAccount.balances.ContainsKey("GAS") && fromAccount.balances["GAS"] > 0;

                contextMenuStrip1.Show(Cursor.Position);
            }
        }

        private void newAddressToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            string input = "";

            if (InputUtils.ShowInputDialog("Insert name of new address", ref input) == DialogResult.OK)
            {
                _blockchain.CreateAddress(input);
                Reload();
            }

        }

        private void markAsFromToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            fromAccount = selectedAddress;            
        }

        private void SendAssetPrompt(Account from, Account to, string symbol)
        {
            string input = "1";

            if (InputUtils.ShowInputDialog("Insert amount of ", ref input) == DialogResult.OK)
            {
                BigInteger amount;
                if (BigInteger.TryParse(input, out amount) && amount>0)
                {
                    var block = _blockchain.GenerateBlock();

                    var assetID = Asset.GetAssetId(symbol);

                    var bytes = Emulation.Helper.AddressToScriptHash(to.keys.address);
                    var hash = new Cryptography.UInt160(bytes);

                    var tx = new Transaction(block);
                    tx.outputs.Add(new TransactionOutput(assetID, amount, hash));

                    if (_blockchain.ConfirmBlock(block))
                    {
                        _blockchain.Save();
                        Reload();
                    }
                }
                else
                {
                    MessageBox.Show("Invalid amount");
                }

            }
        }

        private void nEOToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (fromAccount == null)
            {
                return;
            }

            SendAssetPrompt(fromAccount, selectedAddress, "NEO");
        }

        private void gASToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (fromAccount == null)
            {
                return;
            }

            SendAssetPrompt(fromAccount, selectedAddress, "GAS");
        }
    }
}
