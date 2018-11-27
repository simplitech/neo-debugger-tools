using Neo.Emulation;
using Neo.Emulation.Utils;
using Neo.Lux.Cryptography;
using Neo.Lux.Utils;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Neo.Debugger.Forms
{
    public partial class StorageForm : Form
    {
        private Emulator _debugger;

        public StorageForm(Emulator debugger)
        {
            InitializeComponent();
            _debugger = debugger;

            dataGridView1.Columns.Add("Key", "Key");
            dataGridView1.Columns.Add("Values", "Content");

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[0].FillWeight = 3;

            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[1].FillWeight = 4;
        }

        private void StorageForm_Load(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();

            var storage = _debugger.currentAccount.storage;
            foreach (var entry in storage.entries)
            {
                // TODO : Proper type detection?
                Emulator.Type hintType = entry.Key.Length == 20 ? Emulator.Type.Integer : Emulator.Type.Integer;

                string key = null;

                if (entry.Key.Length == 24)
                {
                    var prefixData = entry.Key.Take(4).ToArray();
                    var hash = entry.Key.Skip(4).ToArray();

                    //https://stackoverflow.com/questions/38199136/check-if-c-sharp-byte-array-contains-a-string
                    var isAscii = prefixData.All(b => b >= 32 && b <= 127);

                    if (isAscii)
                    {
                        var prefix = System.Text.Encoding.ASCII.GetString(prefixData);

                        var signatureHash = new UInt160(hash);

                        key = prefix + "." + CryptoUtils.ToAddress(signatureHash);
                    }
                }

                if (key == null)
                {
                    key = Emulation.Utils.FormattingUtils.OutputData(entry.Key, false);
                }

                dataGridView1.Rows.Add(key, Emulation.Utils.FormattingUtils.OutputData(entry.Value, false, hintType));
            }
        }
    }
}
