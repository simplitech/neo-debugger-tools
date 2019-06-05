using Avalonia;
using Avalonia.Logging.Serilog;
using NEODebuggerUI.Views;

namespace NEODebuggerUI
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
