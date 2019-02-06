using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using NeoDebuggerUI.ViewModels;
using NeoDebuggerUI.Views;

namespace NeoDebuggerUI
{
	class Program
	{
		static void Main(string[] args)
		{
			BuildAvaloniaApp().Start<MainWindow>();
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.UseReactiveUI()
				.LogToDebug();
	}
}
