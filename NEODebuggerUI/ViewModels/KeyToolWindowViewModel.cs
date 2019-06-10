using ReactiveUI;

namespace NEODebuggerUI.ViewModels
{
    public class KeyToolWindowViewModel : ViewModelBase
    {
        
        public delegate void PrivateKeyChanged(string privateKey);
        public event PrivateKeyChanged EvtPrivateKeyChanged;

        private string _privateKey = "00ceb54af832611cd80352a7190d44c9bec14a7d00677bdad2d6fe58009158a1";
        public string PrivateKey
        {
            get => _privateKey;
            set => this.RaiseAndSetIfChanged(ref _privateKey, value);
        }

        public void Test()
        {
            EvtPrivateKeyChanged?.Invoke(PrivateKey);
        }
    }
}