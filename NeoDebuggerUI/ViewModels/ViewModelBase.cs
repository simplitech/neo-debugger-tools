using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NeoDebuggerUI.Views;
using ReactiveUI;

namespace NeoDebuggerUI.ViewModels
{
	public class ViewModelBase : ReactiveObject
	{
        public async void OpenGenericSampleDialog(String text, String okText, String cancelText, bool showCancel)
        {
            var clickedOk = await GetGenericSampleDialogResult(text, okText, cancelText, showCancel);

            var wasOk = clickedOk;
        }

        public async Task<bool> GetGenericSampleDialogResult(String text, String okText, String cancelText, bool showCancel)
        {
            var clickedOk = await new GenericConfirmationWindow()
                .SetText(text)
                .SetOkText(okText)
                .SetCancelText(cancelText)
                .ShowCancel(showCancel)
                .Open();

            var wasOk = clickedOk;
            return wasOk;
        }

    }
}
