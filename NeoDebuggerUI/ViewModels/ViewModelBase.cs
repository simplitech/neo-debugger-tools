using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using NeoDebuggerUI.Views;
using ReactiveUI;

namespace NeoDebuggerUI.ViewModels
{
	public class ViewModelBase : ReactiveObject
	{
        public async Task<bool> OpenGenericSampleDialog(String text, String okText, String cancelText, bool showCancel)
        {
            var clickedOk = await new GenericConfirmationWindow()
                .SetText(text)
                .SetOkText(okText)
                .SetCancelText(cancelText)
                .ShowCancel(showCancel)
                .Open(Application.Current.MainWindow);

            var wasOk = clickedOk;
            return wasOk;
        }

    }
}
