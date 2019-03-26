using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using NEODebuggerUI.ViewModels;

namespace NEODebuggerUI.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private TextEditor _textEditor;
        private BreakpointMargin _breakpointMargin;

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

            _breakpointMargin = new BreakpointMargin();
            _breakpointMargin.Width = 20;
            _breakpointMargin.PointerPressed += (o, e) => SetBreakpointState(e.GetPosition(_textEditor));
            _textEditor.TextArea.LeftMargins.Insert(0, _breakpointMargin);

            MenuItem newCSharp = this.FindControl<MenuItem>("MenuItemNewCSharp");
            newCSharp.Click += async (o, e) => { await NewCSharpFile(); };

            MenuItem newPython = this.FindControl<MenuItem>("MenuItemNewPython");
            newPython.Click += async (o, e) => { await NewPythonFile(); };

            MenuItem newNEP5 = this.FindControl<MenuItem>("MenuItemNewNEP5");
            newNEP5.Click += async (o, e) => { await NewPythonFile(); };


            this.ViewModel.EvtFileChanged += async (fileName) => await LoadFile(fileName);
            this.ViewModel.EvtFileToCompileChanged += async () => await ViewModel.SaveCurrentFileWithContent(_textEditor.Text);
            //this.Activated += (o, e) => { ReloadCurrentFile(); };

            Task.Run(() => RenderVMStack(ViewModel.EvaluationStack, ViewModel.AltStack, ViewModel.StackIndex));
            this.ViewModel.EvtVMStackChanged += async (eval, alt, index) => await RenderVMStack(eval, alt, index);
            this.ViewModel.EvtDebugCurrentLineChanged += async (isOnBreakpoint, line) => await HighlightOnBreakpoint(isOnBreakpoint, line);
            this.ViewModel.EvtBreakpointStateChanged += async (line, addBreakpoint) => await UpdateBreakpoint(line);

            this.SetHotKeys();
        }

        public async Task NewCSharpFile()
        {
            this.ViewModel.SendLogToPanel("New CSharp File 1"); 
            var dialog = new SaveFileDialog();
            var filters = new List<FileDialogFilter>();
            var filteredExtensions = new List<string>(new string[] { "cs" });
            var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "C# File" };
            filters.Add(filter);
            dialog.Filters = filters;
            var result = dialog.ShowAsync(this);
            this.ViewModel.SendLogToPanel("New CSharp File 2 ");
            if (result != null)
            {
                await ViewModel.ResetWithNewFile(result.Result);
            }
        }

        public async Task NewPythonFile()
        {
            var dialog = new SaveFileDialog();
            var filters = new List<FileDialogFilter>();
            var filteredExtensions = new List<string>(new string[] { "py" });
            var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "Python File" };
            filters.Add(filter);
            dialog.Filters = filters;
            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                await this.ViewModel.ResetWithNewFile(result);
            }
        }

        private async Task LoadFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            await Dispatcher.UIThread.InvokeAsync(() => _textEditor.Load(fs) );
        }

        public void SetBreakpointState(Point clickPosition)
        {
            int lineIndex;

            var textPosition = _textEditor.GetPositionFromPoint(clickPosition);
            var maxLine = _textEditor.Document.LineCount;
            if (textPosition?.Line == null || textPosition?.Line > maxLine)
            {
                //click was not in a valid line
                return;
            }
            lineIndex = textPosition?.Line ?? 0;

            ViewModel.SetBreakpoint(lineIndex);
        }

        public async Task UpdateBreakpoint(int line)
        {
            // update ui
            await Dispatcher.UIThread.InvokeAsync(()=>_breakpointMargin.UpdateBreakpointMargin(ViewModel.Breakpoints, line));

            // fix gui bug when inserting breakpoint in the same line of the caret
            if (_textEditor.Document.GetLineByOffset(_textEditor.CaretOffset).LineNumber == line)
            {
                var offset = _textEditor.Document.GetOffset(line, 0);
                _textEditor.CaretOffset = offset < _textEditor.Text.Length ? offset : 0;
            }
        }


        public async Task HighlightOnBreakpoint(bool isOnBreakpoint, int currentLine)
        {
            if (isOnBreakpoint)
            {
                // highlight the line when stopped on a breakpoint
                var currentDocumentLine = _textEditor.Document.GetLineByNumber(currentLine);

                var lineText = _textEditor.Document.GetText(currentDocumentLine.Offset, currentDocumentLine.Length);
                var offset = Regex.Match(lineText, @"\S").Index;
                var regex = Regex.Match(lineText, @"(?<=^\s*)([^\s\/]|[^\s\/]\s)+(?=\s*$|\s*\/\/)").Value;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _breakpointMargin.UpdateBreakpointView(ViewModel.Breakpoints, currentLine, offset, regex.Length);
                    // change selection to fix gui bug to update
                    _textEditor.SelectionStart = currentDocumentLine.NextLine.Offset - 1;
                    _textEditor.SelectionLength = 1; // there must be a selection to update textview
                });

            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // clear highlight of the last stopped line 
                    _breakpointMargin.UpdateBreakpointView(ViewModel.Breakpoints, 0);

                    // change selection to fix gui bug to update
                    _textEditor.SelectionStart = _textEditor.CaretOffset > 0 ? _textEditor.CaretOffset - 1 : 0;
                    // there must be a modification to update textview
                });

            }
            _textEditor.IsReadOnly = isOnBreakpoint;
        }

        private void ReloadCurrentFile() 
        {
            if (!string.IsNullOrEmpty(ViewModel.SelectedFile) && File.Exists(ViewModel.SelectedFile))
            {
                Task.Run(() => LoadFile(ViewModel.SelectedFile));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task RenderVMStack(List<string> evalStack, List<string> altStack, int index)
        {
            var grid = this.FindControl<Grid>("VMStackGrid");
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            var rowHeader = new RowDefinition { Height = new GridLength(20) };
            grid.RowDefinitions.Add(rowHeader);

            var indexHeader = new TextBlock
            {
                Text = "Index",
                FontWeight = FontWeight.Bold,
                Margin = Thickness.Parse("0, 0, 5, 0")
            };
            Grid.SetRow(indexHeader, 0);
            Grid.SetColumn(indexHeader, 0);
            grid.Children.Add(indexHeader);

            var evalHeader = new TextBlock { Text = "Eval", FontWeight = FontWeight.Bold };
            Grid.SetRow(evalHeader, 0);
            Grid.SetColumn(evalHeader, 1);
            grid.Children.Add(evalHeader);

            var altHeader = new TextBlock { Text = "Alt", FontWeight = FontWeight.Bold };
            Grid.SetRow(altHeader, 0);
            Grid.SetColumn(altHeader, 2);
            grid.Children.Add(altHeader);

            await Dispatcher.UIThread.InvokeAsync(() => {
                for (int i = 0; i <= index; i++)
                {
                    RenderLine(grid, i + 1, index - i, evalStack[i], altStack[i]);
                }
            });

        }

        private void RenderLine(Grid grid, int rowCount, int index, string eval, string alt)
        {
            var rowView = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(rowView);

            var indexView = new TextBlock { Text = index.ToString() };
            Grid.SetRow(indexView, rowCount);
            Grid.SetColumn(indexView, 0);
            grid.Children.Add(indexView);

            var evalView = new TextBlock { Text = eval };
            Grid.SetRow(evalView, rowCount);
            Grid.SetColumn(evalView, 1);
            grid.Children.Add(evalView);

            var altView = new TextBlock { Text = alt };
            Grid.SetRow(altView, rowCount);
            Grid.SetColumn(altView, 2);
            grid.Children.Add(altView);

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


        public void SetHotKeys()
        {
            var keyBindings = this.KeyBindings;

            var runControl = this.FindControl<MenuItem>("RunContract");
            var runKeyBinding = new Avalonia.Input.KeyBinding()
            {
                // hotkey: F5
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.F5),
                Command = runControl.Command,
                CommandParameter = runControl.CommandParameter
            };
            keyBindings.Add(runKeyBinding);

            var stepControl = this.FindControl<MenuItem>("StepContract");
            var stepKeyBinding = new Avalonia.Input.KeyBinding()
            {
                // hotkey: F10
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.F10),
                Command = stepControl.Command,
                CommandParameter = stepControl.CommandParameter
            };
            keyBindings.Add(stepKeyBinding);

            var stopKeyBinding = new Avalonia.Input.KeyBinding()
            {
                // hotkey: Shift + F5
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.F5, Avalonia.Input.InputModifiers.Shift),
                Command = this.FindControl<MenuItem>("StopContract").Command
            };
            keyBindings.Add(stopKeyBinding);
        }

    }
}
