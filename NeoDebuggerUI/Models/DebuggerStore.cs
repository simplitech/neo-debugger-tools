using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using NEO_Emulator.SmartContractTestSuite;
using ReactiveUI;

namespace NeoDebuggerUI.Models
{
    public class DebuggerStore : ReactiveObject
    {
        public static readonly DebuggerStore instance;

        static DebuggerStore()
        {
            instance = new DebuggerStore();
        }
        
        public readonly DebugManager manager;

        private DebuggerStore()
        {
            var settings = new DebuggerSettings(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            manager = new DebugManager(settings);
        }

        public SmartContractTestSuite Tests
        {
            get
            {
                if (manager.Tests == null)
                {
                    manager.LoadTests();   
                }

                return manager.Tests;
            }
        }
    }
}