using Neo.Emulation;
using Neo.Emulation.Utils;
using System;
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
                dataGridView1.Rows.Add(FormattingUtils.OutputData(entry.Key, false), FormattingUtils.OutputData(entry.Value, false, hintType));
            }
        }
    }
}
