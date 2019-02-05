using System.Linq;
using Neo.Debugger.Core.Models;
using ReactiveUI;
using System.Reactive.Linq;
using LunarLabs.Parser;
using NeoDebuggerUI.Models;
using System;
using System.Threading.Tasks;
using Neo.VM;

namespace NeoDebuggerUI.ViewModels
{
    public class InvokeWindowViewModel : ViewModelBase
    {
        private string _selectedTestCase;
        public string SelectedTestCase
        {
            get => _selectedTestCase;
            set => this.RaiseAndSetIfChanged(ref _selectedTestCase, value);
        }

        public DataNode SelectedTestCaseParams => SelectedTestCase != null ? DebuggerStore.instance.Tests.cases[SelectedTestCase].args : null;
        public DebugParameters DebugParams { get; set; } = new DebugParameters();

        public InvokeWindowViewModel()
        {
            if(DebuggerStore.instance.Tests != null && DebuggerStore.instance.Tests.cases.Count > 0) {
                _selectedTestCase = DebuggerStore.instance.Tests.cases.ElementAt(0).Key;
            }
        }

        public void Run()
        {
            DebugParams.TriggerType = Neo.Emulation.TriggerType.Application;
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

            if(result != null)
            {
                OpenGenericSampleDialog(result.GetString(), "OK", "", false);
            }
            else
            {
                OpenGenericSampleDialog(errorMessage, "Error", "", false);
            }
        }
    }
}