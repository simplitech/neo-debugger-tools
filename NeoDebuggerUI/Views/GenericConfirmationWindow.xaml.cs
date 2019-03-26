using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NeoDebuggerUI.ViewModels;

namespace NeoDebuggerUI.Views
{
    public class GenericConfirmationWindow : ReactiveWindow<GenericConfirmationWindowViewModel>
    {
        public GenericConfirmationWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            ViewModel.ButtonClicked += ok => Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public GenericConfirmationWindow SetText(string text)
        {
            ViewModel.Text = text;
            return this;
        }

        public GenericConfirmationWindow SetOkText(string text)
        {
            ViewModel.OkText = text;
            return this;
        }

        public GenericConfirmationWindow SetCancelText(string text)
        {
            ViewModel.CancelText = text;
            return this;
        }

        public GenericConfirmationWindow ShowCancel(bool show)
        {
            ViewModel.ShowCancel = show;
            return this;
        }

        public async Task<bool> Open()
        {
            var task = ShowDialog(new Window());
            await Task.Run(()=> task.Wait());
            return ViewModel.ClickedOk == true;
        }
    }
}