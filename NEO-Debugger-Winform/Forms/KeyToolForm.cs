using Neo.Cryptography;
using System;
using System.Windows.Forms;
using Neo.Debugger.Core.Utils;

namespace Neo.Debugger.Forms
{
    public partial class KeyToolForm : Form
    {
        public KeyToolForm()
        {
            InitializeComponent();

            keyDataGrid.Columns.Add("Property", "Property");
            keyDataGrid.Columns.Add("Value", "Value");

            keyDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            keyDataGrid.RowHeadersVisible = false;
            keyDataGrid.Columns[0].ReadOnly = true;
            keyDataGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            keyDataGrid.Columns[0].FillWeight = 2;

            keyDataGrid.Columns[1].ReadOnly = true;
            keyDataGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            keyDataGrid.Columns[1].FillWeight = 4;


        }

        private void KeyToolForm_Shown(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            KeyPair keyPair;
            
            if (keyBox.Text.Length == 52)
            {
                keyPair = KeyPair.FromWIF(keyBox.Text);
            }
            else
            if (keyBox.Text.Length == 64)
            {
                var keyBytes = keyBox.Text.HexToBytes();
                keyPair = new KeyPair(keyBytes);
            }
            else
            {
                MessageBox.Show("Invalid key input, must be 52 or 64 hexdecimal characters.");
                return;
            }

            keyDataGrid.Rows.Clear();

            var scriptHash = Emulation.Helper.AddressToScriptHash(keyPair.address);

            keyDataGrid.Rows.Add(new object[] { "Address", keyPair.address });
            keyDataGrid.Rows.Add(new object[] { "Script Hash (RAW, hex) ", scriptHash.ToHexString() });
            keyDataGrid.Rows.Add(new object[] { "Script Hash (RAW, bytes) ", DebuggerUtils.ToReadableByteArrayString(scriptHash) });
            keyDataGrid.Rows.Add(new object[] { "Public Key (RAW, hex)", keyPair.PublicKey.ToHexString() });
            keyDataGrid.Rows.Add(new object[] { "Private Key (RAW, hex)", keyPair.PrivateKey.ToHexString() });
            keyDataGrid.Rows.Add(new object[] { "Private Key (WIF, hex)", keyPair.WIF });
            keyDataGrid.Rows.Add(new object[] { "Private Key (RAW, bytes)", DebuggerUtils.ToReadableByteArrayString(keyPair.PrivateKey) });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var bytes = new byte[32];
            var rnd = new Random();
            rnd.NextBytes(bytes);
            keyBox.Text = bytes.ToHexString();
        }
    }
}
