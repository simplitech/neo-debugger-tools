﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Legacy;
using System.Reactive.Linq;
using NeoDebuggerUI.Models;
using NeoDebuggerUI.Views;
using System.Reactive;
using NeoDebuggerCore.Utils;
using Avalonia;

namespace NeoDebuggerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveList<string> ProjectFiles { get; } = new ReactiveList<string>();
        public delegate void SelectedFileChanged(string selectedFilename);
        public event SelectedFileChanged EvtFileChanged;

        public delegate void FileToCompileChanged();
        public event FileToCompileChanged EvtFileToCompileChanged;

        public delegate void VMStackChanged(List<string> evalStack, List<string> altStack, int index);
        public event VMStackChanged EvtVMStackChanged;

        public delegate void DebugCurrentLineChanged(bool isOnBreakpoint, int currentLine);
        public event DebugCurrentLineChanged EvtDebugCurrentLineChanged;

        public delegate void BreakpointStateChanged(int line, bool addBreakpoint);
        public event BreakpointStateChanged EvtBreakpointStateChanged;

        public HashSet<int> Breakpoints
        {
            get => DebuggerStore.instance.manager.Emulator.Breakpoints.Select(x => DebuggerStore.instance.manager.ResolveLine(x, true, out _selectedFile) + 1).ToHashSet();
        }

        private string _selectedFile;
		public string SelectedFile
		{
			get => _selectedFile;
			set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
		}

		private string _log;
		public string Log
		{
			get => _log;
			set => this.RaiseAndSetIfChanged(ref _log, value);
		}
        
        // not using getter because the property are updated on another thread and won't update the ui
        private string _consumedGas = DebuggerStore.instance.UsedGasCost;
        public string ConsumedGas
        {
            get => _consumedGas;
            set => this.RaiseAndSetIfChanged(ref _consumedGas, value);
        }

        // not using getter because the property are updated on another thread and won't update the ui
        private bool _isSteppingOrOnBreakpoint = DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint;
        public bool IsSteppingOrOnBreakpoint
        {
            get => _isSteppingOrOnBreakpoint;
            set => this.RaiseAndSetIfChanged(ref _isSteppingOrOnBreakpoint, value);
        }

        public List<string> EvaluationStack { get; set; }
        public List<string> AltStack { get; set; }
        public int StackIndex { get; set; } = -1;

        private string _fileFolder;
		private DateTime _lastModificationDate;

		public MainWindowViewModel()
		{
            Log = "Debugger started\n";
			Neo.Emulation.API.Runtime.OnLogMessage = SendLogToPanel;
			DebuggerStore.instance.manager.SendToLog += (o, e) => { SendLogToPanel(e.Message); };

            var fileChanged = this.WhenAnyValue(vm => vm.SelectedFile);
            fileChanged.Subscribe(file => LoadSelectedFile());

            EvaluationStack = new List<string>();
            AltStack = new List<string>();
		}

        private Unit LoadSelectedFile()
        {
            EvtFileChanged?.Invoke(_selectedFile);
            return Unit.Default;
        }

        public void SaveCurrentFileWithContent(string content)
        {
            File.WriteAllText(this.SelectedFile, content);
        }

        private bool LoadContract(string avmFilePath)
        {
            if (!DebuggerStore.instance.manager.LoadContract(avmFilePath))
            {
                return false;
            }

            _lastModificationDate = File.GetLastWriteTime(DebuggerStore.instance.manager.AvmFilePath);
            DebuggerStore.instance.manager.Emulator.ClearAssignments();

            if (DebuggerStore.instance.manager.IsMapLoaded)
            {
                ProjectFiles.Clear();
                _fileFolder = Path.GetDirectoryName(avmFilePath);
                ProjectFiles.Add(Path.GetFileName(avmFilePath));
                foreach (var path in DebuggerStore.instance.manager.Map.FileNames)
                {
                    DebuggerStore.instance.manager.LoadAssignmentsFromContent(path);
                    ProjectFiles.Add(path);
                }
                string cSharpFile = avmFilePath.Replace(".avm", ".cs");

                if (File.Exists(cSharpFile))
                {
                    SelectedFile = cSharpFile;
                }
                else
                {
                    SelectedFile = avmFilePath;
                }
            }

            return true;
        }

        internal void ResetWithNewFile(string result)
        {
            if (!result.EndsWith(".cs", StringComparison.Ordinal))
            {
                SendLogToPanel("File not supported. Please use .cs extension.");
                return;
            }

            if (!File.Exists(result))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                var fullFilePath = Path.Combine(path, "ContractTemplate.cs");
                var sourceCode = File.ReadAllText(fullFilePath);
                File.WriteAllText(result, sourceCode);
            }

            this.ProjectFiles.Clear();
            this.ProjectFiles.Add(result);
            this.SelectedFile = ProjectFiles[0];
            CompileCurrentFile();
        }

        //Current compiler does not support multiple files
        public void CompileCurrentFile()
        {
            EvtFileToCompileChanged?.Invoke();
            var sourceCode = File.ReadAllText(this.SelectedFile);
            var compiled = DebuggerStore.instance.manager.CompileContract(sourceCode, Neo.Debugger.Core.Data.SourceLanguage.CSharp ,this.SelectedFile);
            if(compiled)
            {
                DebuggerStore.instance.manager.LoadContract(this.SelectedFile.Replace(".cs", ".avm"));
            }
        }

        public void SendLogToPanel(string s)
        {
            Log = s + "\n" + Log;
        }

        public void ClearLog()
        {
            Log = "";
        }

        public string GetVariableInformation(string text)
        {
            if (text == null)
            {
                return null;
            }

            var variable = DebuggerStore.instance.manager.Emulator.GetVariable(text);
            if (variable == null)
            {
                return null;
            }

            try
            {
                return text + " = " + Neo.Emulation.Utils.FormattingUtils.StackItemAsString(variable.value, true, variable.type);
            }
            catch
            {
                // if some class of the vm throws an exception while trying to get the value of the variable
                return text + " = Exception";
            }
        }

        public async Task Open()
		{
			var dialog = new OpenFileDialog();
			var filters = new List<FileDialogFilter>();
			var filteredExtensions = new List<string>(new string[] { "avm" });
			var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "NEO AVM files" };
			filters.Add(filter);
			dialog.Filters = filters;
			dialog.AllowMultiple = false;

			var result = await dialog.ShowAsync(new Window());

			if (result != null && result.Length > 0)
			{
				LoadContract(result[0]);
			}
		}

		public async Task OpenRunDialog(bool stepping)
		{
            CompileCurrentFile();
            var modalWindow = new InvokeWindow(stepping);

            if (!IsSteppingOrOnBreakpoint)
            {
                var task = modalWindow.ShowDialog(new Window());
                await Task.Run(() => task.Wait());
            }

            // not using getters because the properties are updated on another thread and won't update the ui
            IsSteppingOrOnBreakpoint = DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint;
            ConsumedGas = DebuggerStore.instance.UsedGasCost;

            EvtDebugCurrentLineChanged?.Invoke(IsSteppingOrOnBreakpoint, DebuggerStore.instance.manager.CurrentLine + 1);
            if (IsSteppingOrOnBreakpoint)
            {
                UpdateStackPanel();
            }
        }

        public void AddBreakpoint(int line)
        {
            DebuggerStore.instance.manager.AddBreakpoint(line - 1, SelectedFile);
            EvtBreakpointStateChanged?.Invoke(line, true);
        }

        public void RemoveBreakpoint(int line)
        {
            DebuggerStore.instance.manager.RemoveBreakpoint(line - 1, SelectedFile);
            EvtBreakpointStateChanged?.Invoke(line, false);
        }

        public void SetBreakpoint(int line)
        {
            if (Breakpoints.Contains(line))
            {
                // remove breakpoint
                RemoveBreakpoint(line);
            }
            else
            {
                if (DebuggerStore.instance.manager.Map != null)
                {
                    var entries = DebuggerStore.instance.manager.Map.Entries.Select(x => x.line);
                    if (entries.Contains(line))
                    {
                        // add breakpoint in line
                        AddBreakpoint(line);
                    }
                    else
                    {
                        try
                        {
                            // add breakpoint in the next possible line
                            var nextLine = entries.Where(x => x > line).Min();
                            if (!Breakpoints.Contains(nextLine))
                            {
                                AddBreakpoint(nextLine);
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            // Min() method throws an Invalid Operation Exception cause Where() method returns an empty enumerable
                            // entries list is empty - breakpoint won't be added
                            Console.WriteLine(e.Message + '\n' + e.StackTrace);
                        }
                    }
                }
            }
        }

        public void StopDebugging()
        {
            if (IsSteppingOrOnBreakpoint)
            {
                DebuggerStore.instance.manager.Emulator.Stop();
                DebuggerStore.instance.manager.Run();

                // not using getter because the property are updated on another thread and won't update the ui
                IsSteppingOrOnBreakpoint = DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint;

                EvtDebugCurrentLineChanged?.Invoke(IsSteppingOrOnBreakpoint, DebuggerStore.instance.manager.CurrentLine + 1);
            }
        }

        private void UpdateStackPanel()
        {
            var evalStack = DebuggerStore.instance.manager.Emulator.GetEvaluationStack().ToArray();
            var altStack = DebuggerStore.instance.manager.Emulator.GetAltStack().ToArray();

            EvaluationStack.Clear();
            AltStack.Clear();

            int index = Math.Max(evalStack.Length, altStack.Length) - 1;
            StackIndex = index;

            while (index >= 0)
            {
                try
                {
                    EvaluationStack.Add(index < evalStack.Length ? Neo.Emulation.Utils.FormattingUtils.StackItemAsString(evalStack[index]) : "");
                }
                catch
                {
                    // if some class of the vm throws an exception while trying to get the value of the variable
                    EvaluationStack.Add("Exception");
                }

                try
                {
                    AltStack.Add(index < altStack.Length ? Neo.Emulation.Utils.FormattingUtils.StackItemAsString(altStack[index]) : "");
                }
                catch
                {
                    // if some class of the vm throws an exception while trying to get the value of the variable
                    AltStack.Add("Exception");
                }
                
                index--;
            }
            EvtVMStackChanged?.Invoke(EvaluationStack, AltStack, StackIndex);
        }

        public async Task LoadBlockchain()
        {
            var dialog = new OpenFileDialog();
            var filters = new List<FileDialogFilter>();
            var filteredExtensions = new List<string>(new string[] { "chain.json" });
            var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "Virtual blockchain files" };
            filters.Add(filter);
            dialog.Filters = filters;
            dialog.AllowMultiple = false;

            var result = await dialog.ShowAsync(new Window());

            if (result != null && result.Length > 0)
            {
                DebuggerStore.instance.manager.Blockchain.Load(result[0]);
            }
        }

        public async void ResetBlockchain()
        {
            if(!DebuggerStore.instance.manager.BlockchainLoaded)
            {
                OpenGenericSampleDialog("No blockchain loaded yet!", "Ok", "", false);
                return;
            }

            if (DebuggerStore.instance.manager.Blockchain.currentHeight > 1)
            {
                if(!(await OpenGenericSampleDialog("The current loaded Blockchain already has some transactions.\n" +
                    "This action can not be reversed, are you sure you want to reset it?", "Yes", "No", true)))
                {
                    return;
                }
            }

            DebuggerStore.instance.manager.Blockchain.Reset();
            DebuggerStore.instance.manager.Blockchain.Save();

            SendLogToPanel("Reset to virtual blockchain at path: " + DebuggerStore.instance.manager.Blockchain.fileName);
        }
    }
}
