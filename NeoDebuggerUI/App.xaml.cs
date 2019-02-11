using Avalonia;
using Avalonia.Markup.Xaml;

namespace NeoDebuggerUI
{
	public class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
