using Neo.Emulation;
using Neo.Emulation.API;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.Lux.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using Neo.Lux.Cryptography;
using LunarLabs.Parser.JSON;
using NEO_Emulator.SmartContractTestSuite;

namespace Neo.Debugger.Forms
{
    public partial class RunForm : Form
    {
        private ABI _abi;
        private SmartContractTestSuite _testSuite;
        private string currentContractName = "";
        private bool editMode = false;
        private int editRow;
        private bool lockDate;

        public AVMFunction currentMethod { get; private set; }

        public DebugParameters DebugParameters { get; private set; }
        public static String SelectedTestSequence { get; private set; }

        private string _defaultPrivateKey;
        private Dictionary<string, string> _defaultParams;

        public RunForm(ABI abi, SmartContractTestSuite tests, string contractName, string defaultPrivateKey, Dictionary<string, string> defaultParams, string defaultFunction)
        {
            InitializeComponent();
            _testSuite = tests;
            _abi = abi;
            currentContractName = contractName;

            //Defaults.
            if (defaultPrivateKey != null)
                _defaultPrivateKey = defaultPrivateKey;
            if (defaultParams != null)
                _defaultParams = defaultParams;

            assetComboBox.Items.Clear();
            assetComboBox.Items.Add("None");
            foreach (var entry in Asset.Entries)
            {
                assetComboBox.Items.Add(entry.name);
            }
            assetComboBox.SelectedIndex = 0;

            triggerComboBox.SelectedIndex = 0;
            witnessComboBox.SelectedIndex = 0;

            paramsList.Items.Clear();

            foreach (var f in _abi.functions.Values)
            {
                paramsList.Items.Add(f.name);
            }

            if (string.IsNullOrEmpty(defaultFunction))
            {
                defaultFunction = _abi.entryPoint.name;
            }

            int mainItem = paramsList.FindString(defaultFunction);
            if (mainItem >= 0) paramsList.SetSelected(mainItem, true);

            testCasesList.Items.Clear();
            foreach (var entry in _testSuite.cases.Keys)
            {
                testCasesList.Items.Add(entry);
            }

            foreach (var entry in _testSuite.sequences.Keys)
            {
                testSequenceList.Items.Add(entry);
            }
        }

        private string ConvertArray(string s)
        {
            if (DebuggerUtils.IsHex(s))
            {
                var bytes = s.HexToBytes();
                s = DebuggerUtils.BytesToString(bytes);
            }
            else if (DebuggerUtils.IsValidWallet(s))
            {
                var scriptHash = s.AddressToScriptHash();
                s = DebuggerUtils.BytesToString(LuxUtils.ReverseHex(scriptHash.ByteToHex()).HexToBytes());
            }
            else
            {
                return null;
            }

            return  s;
        }

        private bool InitInvoke()
        {
            var key = paramsList.Text;
            var f = _abi.functions[key];

            DebugParameters = new DebugParameters();

            //Get the private key used
            DebugParameters.PrivateKey = privateKeyInput.Text;

            //Get the witness mode
            CheckWitnessMode witnessMode;
            var ws = witnessComboBox.SelectedItem.ToString().Replace(" ", "");

            if (!Enum.TryParse<CheckWitnessMode>(ws, out witnessMode))
            {
                return false;
            }
            DebugParameters.WitnessMode = witnessMode;

            //Get the trigger type
            TriggerType type;
            var ts = triggerComboBox.SelectedItem.ToString().Replace(" ", "");

            if (!Enum.TryParse<TriggerType>(ts, out type))
            {
                return false;
            }
            DebugParameters.TriggerType = type;

            var HasRawScript = RawScriptText.Text.Length != 0;

            //Get the arguments list
            if (!HasRawScript)
            {
                var argList = "";
                if (f.inputs != null)
                {
                    int index = 0;
                    foreach (var p in f.inputs)
                    {
                        var temp = ($"{key}_{f.name}").ToLower();
                        var name = inputGrid.Rows[index].Cells[0].Value;

                        object val;

                        // detect placeholder
                        if (inputGrid.Rows[index].Cells[1].Style.ForeColor == Color.Gray)
                        {
                            val = "";
                        }
                        else
                        {
                            val = ReadCellVal(index, 1);
                        }

                        if (val == null)
                        {
                            val = ""; // temporary hack, necessary to avoid VM crash
                        }

                        if (val != null && !val.Equals(""))
                        {
                            var param_key = (currentContractName + "_" + f.name + "_" + p.name).ToLower();
                            //Add our default running parameters for next time
                            DebugParameters.DefaultParams[param_key] = val.ToString();
                        }

                        if (index > 0)
                        {
                            argList += ",";
                        }

                        if (p.type == Emulator.Type.Array || p.type == Emulator.Type.ByteArray)
                        {
                            var s = val.ToString();

                            if (s.StartsWith("[") && s.EndsWith("]"))
                            {
                                val = s;
                            }
                            else
                            if (s.StartsWith("\"") && s.EndsWith("\""))
                            {
                                s = s.Substring(1, s.Length - 2);
                                val = ConvertArray(s);

                                if (val == null && p.type == Emulator.Type.ByteArray)
                                {
                                    val = DebuggerUtils.BytesToString(LuxUtils.ReverseHex(LuxUtils.ByteToHex(System.Text.Encoding.UTF8.GetBytes(s))).HexToBytes());
                                }
                                else
                                {
                                    if (val == null)
                                    {
                                        ShowArgumentError(f, index, val);
                                        return false;
                                    }
                                }

                            }
                            else
                            {
                                ShowArgumentError(f, index, val);
                                return false;
                            }

                            var items = JSONReader.ReadFromString(val.ToString());

                            val = "";
                            int itemIndex = 0;
                            foreach (var item in items)
                            {
                                if (itemIndex >0)
                                {
                                    val += ",";
                                }

                                if (item.Kind == LunarLabs.Parser.NodeKind.String)
                                {
                                    var vv = ConvertArray(item.Value);
                                    if (vv != null)
                                    {
                                        val += vv;
                                    }
                                    else
                                    {
                                        val += "\""+item.Value+"\"";
                                    }
                                }
                                else
                                {
                                    val += item.Value;
                                }

                                itemIndex++;
                            }

                            val = $"[{val}]";
                        }
                        else
                            switch (p.type)
                            {
                                case Emulator.Type.String:
                                    {
                                        var s = val.ToString();
                                        if (!s.StartsWith("\"") || !s.EndsWith("\""))
                                        {
                                            ShowArgumentError(f, index, val);
                                            return false;
                                        }

                                        break;
                                    }

                                case Emulator.Type.Integer:
                                    {
                                        BigInteger n;
                                        var s = val.ToString();
                                        if (string.IsNullOrEmpty(s) || !BigInteger.TryParse(s, out n))
                                        {
                                            ShowArgumentError(f, index, val);
                                            ResetTabs();
                                            return false;
                                        }
                                        break;
                                    }

                                case Emulator.Type.Boolean:
                                    {
                                        switch (val.ToString().ToLower())
                                        {
                                            case "true": val = true; break;
                                            case "false": val = false; break;
                                            default:
                                                {
                                                    ShowArgumentError(f, index, val);
                                                    ResetTabs();
                                                    return false;
                                                }

                                        }
                                        break;
                                    }
                            }

                        argList += val;
                        index++;
                    }
                }
                if (key != _abi.entryPoint.name)
                {
                    if (f.inputs == null || f.inputs.Count == 0)
                    {
                        argList = "[]";
                    }
                    var operation = Char.ToLowerInvariant(key[0]) + key.Substring(1);
                    argList = $"\"{operation}\", [{argList}]";
                }

                //Set the arguments list
                try
                {
                    DebugParameters.ArgList = DebuggerUtils.GetArgsListAsNode(argList);
                }
                catch
                {
                    MessageBox.Show("Error parsing input!");
                    ResetTabs();
                    return false;
                }
            }

            if (assetComboBox.SelectedIndex > 0)
            {
                foreach (var entry in Asset.Entries)
                {
                    if (entry.name == assetComboBox.SelectedItem.ToString())
                    {
                        BigInteger amount;
                        BigInteger.TryParse(assetAmount.Text, out amount);
                        if (amount > 0)
                        {
                            lastSentSymbol = entry.name;
                            lastSentAmount = assetAmount.Text;

                            amount *= Asset.Decimals; // fix decimals

                            //Add the transaction info
                            DebugParameters.Transaction.Add(entry.id, amount);
                        }
                        else
                        {
                            MessageBox.Show(entry.name + " amount must be greater than zero");
                            return false;
                        }

                        break;
                    }
                }
            }

            uint timestamp;
            if (!uint.TryParse(timestampBox.Text, out timestamp))
            {
                MessageBox.Show("Invalid timestamp");
                return false;
            }
            else
            {
                DebugParameters.Timestamp = timestamp;
            }

            DebugParameters.RawScript = HasRawScript ? RawScriptText.Text.HexToBytes() : null;

            return true;
        }

        #region Main Form

        private static string lastSentSymbol = "";
        private static string lastSentAmount = "0";

        private void RunForm_Shown(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.None;

            inputGrid.AllowUserToAddRows = false;

            assetComboBox.SelectedIndex = 0;
            for (int i=0; i< assetComboBox.Items.Count; i++)
            {
                if (assetComboBox.Items[i].ToString() == lastSentSymbol)
                {
                    assetComboBox.SelectedIndex = i;
                    break;
                }
            }

            assetAmount.Text = lastSentAmount;
            assetAmount.Enabled = assetComboBox.SelectedIndex > 0;
            timestampBox.Text = DateTime.UtcNow.ToTimestamp().ToString();

            if (Runtime.invokerKeys == null && File.Exists("last.key"))
            {
                var privKey = File.ReadAllBytes("last.key");
                if (privKey.Length == 32)
                {
                    Runtime.invokerKeys = new KeyPair(privKey);
                }
            }

            if (Runtime.invokerKeys != null)
            {
                addressLabel.Text = Runtime.invokerKeys.address;
            }
            else
            {
                addressLabel.Text = "(No key loaded)";
            }

            privateKeyInput.Text = _defaultPrivateKey;
        }

        private void ResetTabs()
        {
            this.runTabs.SelectedTab = methodTab;
        }

        #endregion

        #region Input Lists Handlers

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var key = paramsList.Text;
            LoadFunction(key);
        }

        private void paramsList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            button1_Click(null, null);
        }

        private void testCasesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var key = testCasesList.Text;
            var testCase = _testSuite.cases[key];
            var methodName = testCase.method != null ? testCase.method : _abi.entryPoint.name;

            for (int i = 0; i < paramsList.Items.Count; i++)
            {
                if (paramsList.Items[i].ToString() == methodName)
                {
                    paramsList.SelectedIndex = i;

                    for (int j = 0; j < inputGrid.RowCount; j++)
                    {
                        string val;

                        if (testCase.args != null && j < testCase.args.ChildCount)
                        {
                            var node = testCase.args[j];
                            val = DebuggerUtils.ParseNode(node, j);
                        }
                        else
                        {
                            val = "";
                        }

                        inputGrid.Rows[j].Cells[1].Value = val;
                        inputGrid.Rows[j].Cells[1].Style.ForeColor = Color.Black;
                    }

                    break;
                }
            }
        }

        private void inputGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (editMode)
            {
                editMode = false;
                VerifyPlaceholderAt(editRow, 1);
            }

        }

        private void inputGrid_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (!editMode)
            {
                VerifyPlaceholderAt(e.RowIndex, e.ColumnIndex);
            }
        }

        private void inputGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && currentMethod != null)
            {
                var col = inputGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor;
                if (col == Color.Gray)
                {
                    DisablePlaceholderText(e.RowIndex, e.ColumnIndex);
                }
            }
        }

        private void inputGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            editMode = true;
            editRow = e.RowIndex;
        }

        private void inputGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            editMode = false;
            VerifyPlaceholderAt(e.RowIndex, e.ColumnIndex);
        }

        private string ReadCellVal(int row, int col)
        {
            var temp = inputGrid.Rows[row].Cells[col].Value;
            var val = temp != null ? temp.ToString() : null;
            return val;
        }

        private void EnablePlaceholderText(int row, int col, AVMInput p)
        {
            var s = p.type.ToString();

            if (p.type == Emulator.Type.Array || p.type == Emulator.Type.ByteArray)
            {
                s += " (Eg: [1, 2, \"something\"]";
            }

            var curContent = ReadCellVal(row, col);

            if (curContent != s && !string.IsNullOrEmpty(curContent))
            {
                return;
            }

            inputGrid.Rows[row].Cells[col].Style.ForeColor = Color.Gray;
            inputGrid.Rows[row].Cells[col].Value = s;
        }

        private void DisablePlaceholderText(int row, int col)
        {
            inputGrid.Rows[row].Cells[col].Style.ForeColor = Color.Black;
            inputGrid.Rows[row].Cells[col].Value = "";
        }

        private void VerifyPlaceholderAt(int row, int col)
        {
            if (col == 1 && currentMethod != null && !editMode)
            {
                var val = ReadCellVal(row, col);

                if (string.IsNullOrEmpty(val))
                {
                    var p = currentMethod.inputs[row];
                    EnablePlaceholderText(row, col, p);
                }
                else
                {
                    inputGrid.Rows[row].Cells[col].Style.ForeColor = Color.Black;
                }
            }

        }

        private void LoadFunction(string key)
        {
            if (_abi.functions.ContainsKey(key))
            {
                currentMethod = _abi.functions[key];

                inputGrid.Rows.Clear();

                if (currentMethod.inputs != null)
                {
                    foreach (var p in currentMethod.inputs)
                    {
                        var param_key = (currentContractName + "_" + currentMethod.name + "_" + p.name).ToLower();
                        object val = "";

                        bool isEmpty = true;

                        if (_defaultParams.ContainsKey(param_key))
                        {
                            val = _defaultParams[param_key];
                            isEmpty = false;
                        }

                        inputGrid.Rows.Add(new object[] { p.name, val });

                        int rowIndex = inputGrid.Rows.Count - 1;

                        if (isEmpty)
                        {
                            EnablePlaceholderText(rowIndex, 1, p);
                        }
                    }
                }

                button1.Enabled = true;
            }
        }

        #endregion

        #region Button Click Handlers

        private void button1_Click(object sender, EventArgs e)
        {
            if (runTabs.SelectedIndex != 3)
            {
                SelectedTestSequence = null;
                if (InitInvoke())
                {
                    this.DialogResult = DialogResult.OK;
                }
            }else
            {
                if(SelectedTestSequence != null)
                {
                    this.DialogResult = DialogResult.OK;
                }
            }
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var keyPair = DebuggerUtils.GetKeyFromString(privateKeyInput.Text);
            if (keyPair != null)
            {
                Runtime.invokerKeys = keyPair;
                addressLabel.Text = Runtime.invokerKeys.address;
            }
            else
            {
                MessageBox.Show("Invalid private key, length should be 52 or 64");
            }
        }

        #endregion

        #region Helpers

        private void ShowArgumentError(AVMFunction f, int index, object val)
        {
            string error;

            if (val == null || string.IsNullOrEmpty(val.ToString()))
            {
                error = "Missing";
            }
            else
            {
                error = "Invalid format in ";
            }

            MessageBox.Show($"{error} argument #{index + 1} (\"{f.inputs[index].name}\") of {f.name} method");
            ResetTabs();
        }

        #endregion

        private void timestampBox_TextChanged(object sender, EventArgs e)
        {
            if (lockDate) return;

            uint timestamp;
            if (uint.TryParse(timestampBox.Text, out timestamp))
            {
                dateTimePicker1.Value = timestamp.ToDateTime();
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            lockDate = true;
            timestampBox.Text = dateTimePicker1.Value.ToTimestamp().ToString();
            lockDate = false;
        }

        private void assetComboBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            assetAmount.Enabled = assetComboBox.SelectedIndex > 0;
        }

        private void testSequenceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedTestSequence = (string)testSequenceList.SelectedItem;
        }
    }
}
