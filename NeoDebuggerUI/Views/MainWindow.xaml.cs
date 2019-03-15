using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
            _textEditor.Options.ConvertTabsToSpaces = true;
            _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            _textEditor.TextArea.IndentationStrategy = new AvaloniaEdit.Indentation.CSharp.CSharpIndentationStrategy(_textEditor.Options);
            _textEditor.PointerHover += (o, e) => SetTip(e.GetPosition(_textEditor));
            _textEditor.PointerHoverStopped += (o, e) => ToolTip.SetIsOpen(_textEditor, false);

            MenuItem newCSharp = this.FindControl<MenuItem>("MenuItemNewCSharp");
            newCSharp.Click += async (o, e) => { await NewCSharpFile(); };

            this.ViewModel.EvtFileChanged += (fileName) => LoadFile(fileName);
            this.ViewModel.EvtFileToCompileChanged += () => ViewModel.SaveCurrentFileWithContent(_textEditor.Text);

        }

        public async Task NewCSharpFile()
        {
            var dialog = new SaveFileDialog();
            var filters = new List<FileDialogFilter>();
            var filteredExtensions = new List<string>(new string[] { "cs" });
            var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "C# File" };
            filters.Add(filter);
            dialog.Filters = filters;
            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                this.ViewModel.ResetWithNewFile(result);
            }
        }

        private void LoadFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _textEditor.Load(fs);
        }

        public void SetTip(Point mousePosition)
        {
            var textPosition = _textEditor.GetPositionFromPoint(mousePosition);

            var lineIndex = textPosition?.Line;
            var columnIndex = textPosition?.Column;

            ToolTip.SetTip(_textEditor, $"This is just a test on position {lineIndex}, {columnIndex}");
            ToolTip.SetIsOpen(_textEditor, true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
