using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using NeoDebuggerUI.Views;
using ReactiveUI;

namespace NeoDebuggerUI.ViewModels
{
	public class ViewModelBase : ReactiveObject
	{
        public async void OpenGenericSampleDialog(String text, String okText, String cancelText, bool showCancel, Window owner)
        {
            var clickedOk = await new GenericConfirmationWindow()
                .SetText(text)
                .SetOkText(okText)
                .SetCancelText(cancelText)
                .ShowCancel(showCancel)
                .Open(owner);

            var wasOk = clickedOk;
        }

    }
}
