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

            MenuItem newCSharp = this.FindControl<MenuItem>("MenuItemNewCSharp");
            newCSharp.Click += async (o, e) => { await NewCSharpFile(); };

            MenuItem newPython = this.FindControl<MenuItem>("MenuItemNewPython");
            newPython.Click += async (o, e) => { await NewPythonFile(); };

            MenuItem newNEP5 = this.FindControl<MenuItem>("MenuItemNewNEP5");
            newNEP5.Click += async (o, e) => { await NewPythonFile(); };


            this.ViewModel.EvtFileChanged += (fileName) => LoadFile(fileName);
            this.ViewModel.EvtFileToCompileChanged += () => ViewModel.SaveCurrentFileWithContent(_textEditor.Text);
            this.Activated += (o, e) => { ReloadCurrentFile(); };
              
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
                this.ViewModel.ResetWithNewFile(result);
            }
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

        private async void ReloadCurrentFile()
        {
            if (!string.IsNullOrEmpty(ViewModel.SelectedFile) && File.Exists(ViewModel.SelectedFile))
            {
                await Task.Run(() => LoadFile(ViewModel.SelectedFile));
            }
        }

    }
}
