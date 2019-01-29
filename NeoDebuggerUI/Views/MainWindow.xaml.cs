using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NeoDebuggerUI.Views
{
	public class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

			_textEditor = this.FindControl<TextEditor>("Editor");
			_textEditor.Background = Brushes.WhiteSmoke;
			_textEditor.BorderBrush = Brushes.Gray;
			_textEditor.ShowLineNumbers = true;
			_textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
			_textEditor.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy();

			this.ViewModel.EvtFileChanged += (fileName) => LoadFile(fileName);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
