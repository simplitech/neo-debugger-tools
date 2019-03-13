using System.Linq;
using Neo.Debugger.Core.Models;
using ReactiveUI;
using System.Reactive.Linq;
using LunarLabs.Parser;
using NeoDebuggerUI.Models;
using System;
using System.Threading.Tasks;
using Neo.VM;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using Neo.Emulation.API;


namespace NeoDebuggerUI.ViewModels
{
    public class InvokeWindowViewModel : ViewModelBase
    {
        public IEnumerable<string> TestCases { get => DebuggerStore.instance.Tests.cases.Keys; }
        public IEnumerable<string> FunctionList { get => DebuggerStore.instance.manager.ABI.functions.Select(x => x.Value.name); }
        public List<string> AssetItems { get; } = new List<string>();

        public DataNode SelectedTestCaseParams => SelectedTestCase != null ? DebuggerStore.instance.Tests.cases[SelectedTestCase].args : null;
        public DebugParameters DebugParams { get; set; } = new DebugParameters();

        public delegate void SelectedTestChanged(string selectedTestCase);
        public event SelectedTestChanged EvtSelectedTestCaseChanged;

        public string[] Trigger { get => Enum.GetNames(typeof(Neo.Emulation.TriggerType)); }
        public string[] Witness { get => Enum.GetNames(typeof(Neo.Emulation.CheckWitnessMode)); }
        private bool lockDate;

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => this.RaiseAndSetIfChanged(ref _selectedDate, value);
        }

        private uint _timestamp;
        public uint Timestamp
        {
            get => _timestamp;
            set => this.RaiseAndSetIfChanged(ref _timestamp, value);
        }

        private string _selectedTestCase;
        public string SelectedTestCase
        {
            get => _selectedTestCase;
            set => this.RaiseAndSetIfChanged(ref _selectedTestCase, value);
        }

        private string _selectedFunction;
        public string SelectedFunction
        {
            get => _selectedFunction;
            set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
        }
        
        private string _selectedTrigger;
        public string SelectedTrigger
        {
            get => _selectedTrigger;
            set => this.RaiseAndSetIfChanged(ref _selectedTrigger, value);
        }

        private string _selectedWitness;
        public string SelectedWitness
        {
            get => _selectedWitness;
            set => this.RaiseAndSetIfChanged(ref _selectedWitness, value);
        }

        private string _selectedAsset;
        public string SelectedAsset
        {
            get => _selectedAsset;
            set => this.RaiseAndSetIfChanged(ref _selectedAsset, value);
        }

        private string _assetAmount;
        public string AssetAmount
        {
            get => _assetAmount;
            set => this.RaiseAndSetIfChanged(ref _assetAmount, value);
        }

        private ObservableCollection<string> _privateKeys;
        public ObservableCollection<string> PrivateKeys
        {
            get => _privateKeys;
            set => this.RaiseAndSetIfChanged(ref _privateKeys, value);
        }

        private string _selectedPrivateKey;
        public string SelectedPrivateKey
        {
            get => _selectedPrivateKey;
            set => this.RaiseAndSetIfChanged(ref _selectedPrivateKey, value);
        }

        private string _privateKeyAddress;
        public string PrivateKeyAddress
        {
            get => _privateKeyAddress;
            set => this.RaiseAndSetIfChanged(ref _privateKeyAddress, value);
        }

        private string _inputPrivateKey = "";
        public string InputPrivateKey
        {
            get => _inputPrivateKey;
            set => this.RaiseAndSetIfChanged(ref _inputPrivateKey, value);
        }

        public void NotifySelectedTestChangeEvt()
        {
            EvtSelectedTestCaseChanged?.Invoke(_selectedTestCase);
        }

        public void UpdateSelectedDate()
        {
            if(lockDate)
            {
                return;
            }
            SelectedDate = DateTimeOffset.FromUnixTimeSeconds((long)Timestamp).DateTime;
        }

        public void UpdateTimestamp()
        {
            lockDate = true;
            Timestamp = (uint)((DateTimeOffset)SelectedDate).ToUnixTimeSeconds();
            lockDate = false;
        }

        public void UpdatePrivateKeyAddress()
        {
            if(SelectedPrivateKey == "None")
            {
                PrivateKeyAddress = "(No key loaded)";
                return;
            }
            var keyPair = DebuggerStore.instance.GetKeyFromString(SelectedPrivateKey);
            PrivateKeyAddress = keyPair.address;
            Runtime.invokerKeys = keyPair;
        }

        public InvokeWindowViewModel()
        {
            if(DebuggerStore.instance.Tests != null && DebuggerStore.instance.Tests.cases.Count > 0) {
                _selectedTestCase = DebuggerStore.instance.Tests.cases.ElementAt(0).Key;
            }
            
            _selectedFunction = DebuggerStore.instance.manager.ABI.entryPoint.name;
            _selectedTrigger = DebuggerStore.instance.manager.Emulator.currentTrigger.ToString();
            _selectedWitness = DebuggerStore.instance.manager.Emulator.checkWitnessMode.ToString();
            _selectedDate = DateTime.UtcNow;

            AssetItems.Add("None");
            foreach (var entry in Asset.Entries)
            {
                AssetItems.Add(entry.name);
            }
            _selectedAsset = AssetItems.ElementAt(0);
            _assetAmount = "0";

            LoadPrivateKeys();

            var selectedTestChanged = this.WhenAnyValue(x => x.SelectedTestCase);
            selectedTestChanged.Subscribe(test => NotifySelectedTestChangeEvt());

            var selectedDateChanged = this.WhenAnyValue(x => x.SelectedDate);
            selectedDateChanged.Subscribe(time => UpdateTimestamp());

            var timestampChanged = this.WhenAnyValue(x => x.Timestamp);
            timestampChanged.Subscribe(date => UpdateSelectedDate());

            var selectedPrivateKeyChanged = this.WhenAnyValue(x => x.SelectedPrivateKey);
            selectedPrivateKeyChanged.Subscribe(time => UpdatePrivateKeyAddress());
        }

        public void Run()
        {
            DebuggerStore.instance.manager.ConfigureDebugParameters(DebugParams);
            DebuggerStore.instance.manager.Run();
            StackItem result = null;
            string errorMessage = null;
            try
            {
                result = DebuggerStore.instance.manager.Emulator.GetOutput();
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (result != null)
            {
                OpenGenericSampleDialog("Execution finished.\nGAS cost: " + DebuggerStore.instance.UsedGasCost + "\nResult: " + result.GetString(), "OK", "", false, new Window());
            }
            else
            {
                OpenGenericSampleDialog(errorMessage, "Error", "", false, new Window());
            }
            DebuggerStore.instance.PrivateKeysList = PrivateKeys.ToList();
        }

        public void LoadPrivateKeys()
        {
            if (DebuggerStore.instance.PrivateKeysList?.Count > 0)
            {
                _privateKeys = new ObservableCollection<string>(DebuggerStore.instance.PrivateKeysList);
            }
            else
            {
                _privateKeys = new ObservableCollection<string>() { "None" };
            }

            var lastPrivateKey = DebuggerStore.instance.manager.Settings.lastPrivateKey;
            if (!string.IsNullOrEmpty(lastPrivateKey) )
            {
                if (!PrivateKeys.Contains(lastPrivateKey))
                {
                    PrivateKeys.Add(lastPrivateKey);
                }
                _selectedPrivateKey = lastPrivateKey;
            }
            else
            {
                _selectedPrivateKey = PrivateKeys.ElementAt(0);
            }
        }

        public void AddPrivateKey()
        {
            if (PrivateKeys.Contains(InputPrivateKey))
            {
                SelectedPrivateKey = InputPrivateKey;
                InputPrivateKey = "";
                OpenGenericSampleDialog("This private key is already loaded", "OK", "", false, new Window());
                return;
            }

            var keyAddress = DebuggerStore.instance.GetKeyAddressFromString(InputPrivateKey);
            if (keyAddress != null)
            {
                PrivateKeys.Add(InputPrivateKey);
                SelectedPrivateKey = InputPrivateKey;
                InputPrivateKey = "";
            }
            else
            {
                OpenGenericSampleDialog("Invalid private key, length should be 52 or 64", "OK", "", false, new Window());
            }
        }

        public void RemovePrivateKey()
        {
            if(SelectedPrivateKey != PrivateKeys.ElementAt(0))
            {
                var i = PrivateKeys.IndexOf(SelectedPrivateKey);

                SelectedPrivateKey = PrivateKeys.ElementAt(i - 1);
                PrivateKeys.RemoveAt(i);
            }
        }
    }
}