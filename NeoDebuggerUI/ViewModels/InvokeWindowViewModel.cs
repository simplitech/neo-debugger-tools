using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using ReactiveUI;
using ReactiveUI.Legacy;
using System.Reactive.Linq;
using Avalonia.Layout;
using Avalonia.Media;
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
            _selectedTestCase = DebuggerStore.instance.Tests.cases.ElementAt(0).Key;
        }

        public DataNode SelectedTestCaseParams => DebuggerStore.instance.Tests.cases[SelectedTestCase].args;

        public DebugParameters DebugParams { get; set; } = new DebugParameters();

        public void Run()
        {
            
        }
    }
}