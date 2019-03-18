using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
            var word = GetWord(mousePosition);
            var info = ViewModel.GetVariableInformation(word);

            if (info != null)
            {
                ToolTip.SetTip(_textEditor, info);
                ToolTip.SetIsOpen(_textEditor, true);
            }
        }

        private string GetWord(Point mousePosition)
        {
            int lineIndex, columnIndex;

            var textPosition = _textEditor.GetPositionFromPoint(mousePosition);
            var maxLine = _textEditor.Document.LineCount;
            if (textPosition?.Line == null || textPosition?.Line > maxLine)
            {
                //mouse is not in a valid line
                return null;
            }
            lineIndex = textPosition?.Line ?? 0;
            var line = _textEditor.Document.GetLineByNumber(lineIndex);

            var maxColumn = _textEditor.Document.GetLineByNumber(lineIndex).Length;
            if (textPosition?.Column > maxColumn)
            {
                // mouse is not in a valid column of the line
                return null;
            }
            columnIndex = textPosition?.Column ?? 0;
            var lineOffset = columnIndex - 1;

            var textOffset = _textEditor.Document.GetOffset(lineIndex, columnIndex);
            var lineStr = _textEditor.Document.GetText(line);

            if (Regex.IsMatch(_textEditor.Document.GetText(0, textOffset), @"\/\*[^\*]*\*?[^\/]*$") ||
                Regex.IsMatch(lineStr.Substring(0, columnIndex), @"\/\/"))
            {
                // if there is a "/*" and there isn't a "*/" in the text
                // or there is a "//" in the line before the mouse position
                // mouse is in a comment
                return null;
            }

            var end = Regex.Match(lineStr.Substring(lineOffset), @"^\b[\w\d_]+\b").Value.Length;
            if (end <= 0)
            {
                // mouse is not on a word
                return null;
            }
            var start = Regex.Match(lineStr.Substring(0, columnIndex), @"\b[\w\d_]+\b$").Index;
            var length = end - start + lineOffset;

            return lineStr.Substring(start, length);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
