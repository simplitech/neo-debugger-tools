using Neo.Debugger.Core.Utils;
using ReactiveUI;
using System;

namespace NEODebuggerUI.ViewModels
{
    public class KeyToolWindowViewModel : ViewModelBase
    {
        public delegate void PrivateKeyToDecodeChanged(string privateKey);
        public event PrivateKeyToDecodeChanged EvtPrivateKeyToDecodeChanged;

        private string _privateKey = "";
        public string PrivateKey
        {
            get => _privateKey;
            set => this.RaiseAndSetIfChanged(ref _privateKey, value);
        }

        public KeyToolWindowViewModel()
        {
            // fix bug not updating private key after click random twice
            var privateKeyChanged = this.WhenAnyValue(x => x.PrivateKey);
        }

        public async void DecodeKey()
        {
            try
            {
                if (DebuggerUtils.GetKeyFromString(PrivateKey) != null)
                {
                    EvtPrivateKeyToDecodeChanged?.Invoke(PrivateKey);
                }
                else
                {
                    await OpenGenericSampleDialog("Invalid key input, must be 52 or 64 hexadecimal characters.", "OK", "", false);
                }
            }
            catch
            {
                // any exception on GetKeyFromString is handled as invalid key input
                await OpenGenericSampleDialog("Invalid key input, must be 52 or 64 hexadecimal characters.", "OK", "", false);
            }
        }

        public void GenerateRandomKey()
        {
            var bytes = new byte[32];
            var rnd = new Random();
            rnd.NextBytes(bytes);
            PrivateKey = DebuggerUtils.ToHexString(bytes);
        }
    }
}