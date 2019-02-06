using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using NeoDebuggerUI.ViewModels;

namespace NeoDebuggerUI.Views
{
	public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private TextEditor _textEditor;

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

        private void LoadFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _textEditor.Load(fs);
        }


        private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
