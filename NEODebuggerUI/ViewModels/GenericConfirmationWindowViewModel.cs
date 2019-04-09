using ReactiveUI;

namespace NeoDebuggerUI.ViewModels
{
    public class GenericConfirmationWindowViewModel : ViewModelBase
    {
        private string _text = "";
        private string _okText = "";
        private string _cancelText = "";
        private bool _showCancel = true;
        private bool? _clickedOk = null;
        
        public delegate void OnButtonClicked(bool wasOk);
        public event OnButtonClicked ButtonClicked;

        public void Cancel()
        {
            ClickedOk = false;
            ButtonClicked?.Invoke(false);
        }

        public void Ok()
        {
            ClickedOk = true;
            ButtonClicked?.Invoke(true);
        }
        
        public string Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }
        
        public string OkText
        {
            get => _okText;
            set => this.RaiseAndSetIfChanged(ref _okText, value);
        }
        
        public string CancelText
        {
            get => _cancelText;
            set => this.RaiseAndSetIfChanged(ref _cancelText, value);
        }
        
        public bool ShowCancel
        {
            get => _showCancel;
            set => this.RaiseAndSetIfChanged(ref _showCancel, value);
        }
        
        public bool? ClickedOk
        {
            get => _clickedOk;
            set => this.RaiseAndSetIfChanged(ref _clickedOk, value);
        }
    }
}