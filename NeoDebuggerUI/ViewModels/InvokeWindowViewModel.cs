using System.Linq;
using Neo.Debugger.Core.Models;
using ReactiveUI;
using System.Reactive.Linq;
using LunarLabs.Parser;
using NeoDebuggerUI.Models;

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

        public InvokeWindowViewModel()
        {
            if(DebuggerStore.instance.Tests != null && DebuggerStore.instance.Tests.cases.Count > 0) {
                _selectedTestCase = DebuggerStore.instance.Tests.cases.ElementAt(0).Key;
            }
        }

        public DataNode SelectedTestCaseParams => SelectedTestCase != null ? DebuggerStore.instance.Tests.cases[SelectedTestCase].args : null;

        public DebugParameters DebugParams { get; set; } = new DebugParameters();

        public void Run()
        {
            
        }
    }
}