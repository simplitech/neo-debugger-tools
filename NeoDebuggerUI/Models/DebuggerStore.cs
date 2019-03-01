using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.Lux.Cryptography;
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
            var settings = new DebuggerSettings();
            manager = new DebugManager(settings);
            manager.LoadTests();
            manager.LoadEmulator("chain.avm");
        }

        public SmartContractTestSuite Tests
        {
            get
            {
                return manager.Tests;
            }
        }

        public string UsedGasCost
        {
            get
            {
                return string.Format("{0:N4}", manager.UsedGasCost);
            }
        }

        public KeyPair GetKeyFromString(string privateKey)
        {
            return DebuggerUtils.GetKeyFromString(privateKey);
        }

        public string GetKeyAddressFromString(string privateKey)
        {
            var key = GetKeyFromString(privateKey);
            return key?.address;
        }

        public List<string> PrivateKeysList { get; set; }
    }
}