﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Legacy;
using System.Reactive.Linq;
using NeoDebuggerUI.Models;
using NeoDebuggerUI.Views;
using System.Reactive;
using NeoDebuggerCore.Utils;

namespace NeoDebuggerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveList<string> ProjectFiles { get; } = new ReactiveList<string>();
        public delegate void SelectedFileChanged(string selectedFilename);
        public event SelectedFileChanged EvtFileChanged;

        public delegate void FileToCompileChanged();
        public event FileToCompileChanged EvtFileToCompileChanged;

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

        private string _fileFolder;
		private DateTime _lastModificationDate;

		public MainWindowViewModel()
		{
			Log = "Debugger started\n";
			Neo.Emulation.API.Runtime.OnLogMessage = SendLogToPanel;
			DebuggerStore.instance.manager.SendToLog += (o, e) => { SendLogToPanel(e.Message); };

            var fileChanged = this.WhenAnyValue(vm => vm.SelectedFile);
			fileChanged.Subscribe(file => LoadSelectedFile());
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
                    AddBreakpoints(); // test
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
            Log += s + "\n";
        }

        public void ClearLog()
        {
            Log = "";
        }

        public void AddBreakpoints()
        {
            //TODO: add breakpoint from gui
            foreach (var entry in DebuggerStore.instance.manager.Map.Entries)
            {
                var line = entry.line - 1;
                DebuggerStore.instance.manager.AddBreakpoint(line, SelectedFile);
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

			var result = await dialog.ShowAsync();

			if (result != null && result.Length > 0)
			{
				LoadContract(result[0]);
			}
		}

		public async Task OpenRunDialog()
		{
            CompileCurrentFile();
            var modalWindow = new InvokeWindow();
            
            if (!IsSteppingOrOnBreakpoint)
            {
                var task = modalWindow.ShowDialog();
                await Task.Run(() => task.Wait());
            }
            
            // not using getters because the properties are updated on another thread and won't update the ui
            IsSteppingOrOnBreakpoint = DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint;
            ConsumedGas = DebuggerStore.instance.UsedGasCost;
        }

        public void StopDebugging()
        {
            if (IsSteppingOrOnBreakpoint)
            {
                DebuggerStore.instance.manager.Emulator.Stop();
                DebuggerStore.instance.manager.Run();

                // not using getter because the property are updated on another thread and won't update the ui
                IsSteppingOrOnBreakpoint = DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint;
                OpenGenericSampleDialog("Debug was stopped", "OK", "", false);
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
