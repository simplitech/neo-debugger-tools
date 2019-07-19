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

#pragma warning disable RECS0061 // Warns when a culture-aware 'EndsWith' call is used by default.
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
            _textEditor.TextChanged += (object sender, EventArgs e) => { ViewModel.EditorFileContent = _textEditor.Text; };

            _breakpointMargin = new BreakpointMargin();
            _breakpointMargin.Width = 20;
            _breakpointMargin.Tapped += (o, e) => SetBreakpointState();

            _textEditor.TextArea.LeftMargins.Insert(0, _breakpointMargin);

            MenuItem newCSharp = this.FindControl<MenuItem>("MenuItemNewCSharp");
            newCSharp.Click += async (o, e) => { await NewCSharpFile(); };

            MenuItem newPython = this.FindControl<MenuItem>("MenuItemNewPython");
            newPython.Click += async (o, e) => { await NewPythonFile(); };

            MenuItem newNEP5 = this.FindControl<MenuItem>("MenuItemNewNEP5");
            newNEP5.Click += async (o, e) => { await NewPythonFile(); };


            this.ViewModel.EvtFileChanged += async (fileName) => await LoadFile(fileName);
            //this.ViewModel.EvtFileToCompileChanged += async () => await ViewModel.SaveCurrentFileWithContent(_textEditor.Text);
            //this.Activated += (o, e) => { ReloadCurrentFile(); };

            Dispatcher.UIThread.InvokeAsync(async () => {
                await RenderVMStack(ViewModel.EvaluationStack, ViewModel.AltStack, ViewModel.StackIndex);
                await RenderVariableStack(ViewModel.VariableNames, ViewModel.VariableValues, ViewModel.VariableTypes);
            });
            this.ViewModel.EvtVMStackChanged += async (eval, alt, index) => await RenderVMStack(eval, alt, index);
            this.ViewModel.EvtVariablesChanged += async (name, value, type) => await RenderVariableStack(name, value, type);
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
            var result = await dialog.ShowAsync(this);
            await ViewModel.ResetWithNewFile(result);
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
            if (File.Exists(filename))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {

                    if (filename.EndsWith(".avm"))
                    {
                        _textEditor.Text = ViewModel.DisassembleAVMFile(_textEditor.Text);
                    }
                    else
                    {
                        FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                        _textEditor.Load(fs);
                    }
                    _textEditor.IsReadOnly = filename.EndsWith(".avm");
                });
            }
        }

        public void SetBreakpointState()
        {
            int lineIndex;

            var caret = _textEditor.CaretOffset;

            // when clicking on the margin, is selecting the whole line + 1 character of the next line
            // this is to get the correct clicked line
            if (_textEditor.SelectionLength > 0)
            {
                caret -= 1;
            }

            try
            {
                var documentLine = _textEditor.Document.GetLineByOffset(caret);
                lineIndex = documentLine.LineNumber;

                _textEditor.Select(documentLine.Offset, 0);

                ViewModel.SetBreakpoint(lineIndex);
            }
            catch (Exception e)
            {
                // if there are no documents open, ocurrs NullPointerException
                Console.WriteLine(e.Message);
            }
        }

        public async Task UpdateBreakpoint(int line)
        {
            // update ui
            await Dispatcher.UIThread.InvokeAsync(() => _breakpointMargin.UpdateBreakpointMargin(ViewModel.Breakpoints, line));

            // fix gui bug when inserting breakpoint in the same line of the caret
            if (_textEditor.Document.GetLineByOffset(_textEditor.CaretOffset).LineNumber == line)
            {
                var offset = _textEditor.Document.GetOffset(line, 0);
                _textEditor.CaretOffset = offset < _textEditor.Text.Length ? offset : 0;
            }
        }


        public async Task HighlightOnBreakpoint(bool isOnBreakpoint, int currentLine)
        {
            if (currentLine <= 0 || currentLine > _textEditor.LineCount)
            {
                return;
            }

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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task RenderVariableStack(List<string> name, List<string> value, List<string> type)
        {
            var grid = this.FindControl<Grid>("VariablesGrid");
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            var rowHeader = new RowDefinition { Height = new GridLength(20) };
            grid.RowDefinitions.Add(rowHeader);

            var nameHeader = new TextBlock { Text = "Name", FontWeight = FontWeight.Bold };
            Grid.SetRow(nameHeader, 0);
            Grid.SetColumn(nameHeader, 0);
            grid.Children.Add(nameHeader);

            var valueHeader = new TextBlock { Text = "Value", FontWeight = FontWeight.Bold, Margin = Thickness.Parse("5, 0, 0, 0") };
            Grid.SetRow(valueHeader, 0);
            Grid.SetColumn(valueHeader, 1);
            grid.Children.Add(valueHeader);

            var typeHeader = new TextBlock { Text = "Type", FontWeight = FontWeight.Bold, Margin = Thickness.Parse("5, 0, 0, 0") };
            Grid.SetRow(typeHeader, 0);
            Grid.SetColumn(typeHeader, 2);
            grid.Children.Add(typeHeader);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                for (int i = 0; i < name.Count; i++)
                {
                    RenderLine(grid, i + 1, name[i], value[i], type[i]);
                }
            });
        }

        private async Task RenderVMStack(List<string> evalStack, List<string> altStack, int index)
        {
            var grid = this.FindControl<Grid>("VMStackGrid");
            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            var rowHeader = new RowDefinition { Height = new GridLength(20) };
            grid.RowDefinitions.Add(rowHeader);

            var indexHeader = new TextBlock { Text = "Index", FontWeight = FontWeight.Bold };
            Grid.SetRow(indexHeader, 0);
            Grid.SetColumn(indexHeader, 0);
            grid.Children.Add(indexHeader);

            var evalHeader = new TextBlock { Text = "Eval", FontWeight = FontWeight.Bold, Margin = Thickness.Parse("5, 0, 0, 0") };
            Grid.SetRow(evalHeader, 0);
            Grid.SetColumn(evalHeader, 1);
            grid.Children.Add(evalHeader);

            var altHeader = new TextBlock { Text = "Alt", FontWeight = FontWeight.Bold, Margin = Thickness.Parse("5, 0, 0, 0") };
            Grid.SetRow(altHeader, 0);
            Grid.SetColumn(altHeader, 2);
            grid.Children.Add(altHeader);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                for (int i = 0; i <= index; i++)
                {
                    RenderLine(grid, i + 1, (index - i).ToString(), evalStack[i], altStack[i]);
                }
            });
        }

        private void RenderLine(Grid grid, int rowCount, string row0, string row1, string row2)
        {
            var rowView = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(rowView);

            var indexView = new TextBlock { Text = row0 };
            Grid.SetRow(indexView, rowCount);
            Grid.SetColumn(indexView, 0);
            grid.Children.Add(indexView);

            var evalView = new TextBlock { Text = row1, Margin = Thickness.Parse("5, 0, 0, 0") };
            Grid.SetRow(evalView, rowCount);
            Grid.SetColumn(evalView, 1);
            grid.Children.Add(evalView);

            var altView = new TextBlock { Text = row2, Margin = Thickness.Parse("5, 0, 0, 0") };
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
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.R, Avalonia.Input.InputModifiers.Control),
                Command = runControl.Command,
                CommandParameter = runControl.CommandParameter
            };
            keyBindings.Add(runKeyBinding);

            var stepControl = this.FindControl<MenuItem>("StepContract");
            var stepKeyBinding = new Avalonia.Input.KeyBinding()
            {
                // hotkey: F10
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.D, Avalonia.Input.InputModifiers.Control),
                Command = stepControl.Command,
                CommandParameter = stepControl.CommandParameter
            };
            keyBindings.Add(stepKeyBinding);

            var stopKeyBinding = new Avalonia.Input.KeyBinding()
            {
                // hotkey: Shift + F5
                Gesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.P, Avalonia.Input.InputModifiers.Control),
                Command = this.FindControl<MenuItem>("StopContract").Command
            };
            keyBindings.Add(stopKeyBinding);
        }
    }
}
