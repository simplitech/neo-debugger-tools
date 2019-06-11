﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Legacy;
using System.Reactive.Linq;
using NEODebuggerUI.Models;
using NEODebuggerUI.Views;
using System.Reactive;
using NeoDebuggerCore.Utils;
using Avalonia;
using Neo.Debugger.Core.Utils;
using Avalonia.Threading;

#pragma warning disable RECS0061 // Warns when a culture-aware 'EndsWith' call is used by default.
namespace NEODebuggerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ReactiveList<string> ProjectFiles { get; } = new ReactiveList<string>();


        private bool _isStopped = false;
        public bool IsStopped
        {
            get => _isStopped;
            set => this.RaiseAndSetIfChanged(ref _isStopped, value);
        }


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

        public String EditorFileContent;

        public HashSet<int> Breakpoints { get => GetBreakpointHashSet(); }

        private string _selectedProjectFile;
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

        private void LoadSelectedFile()
        {
            if (_selectedFile != null)
            {
                if (_selectedFile.EndsWith(".avm"))
                {
                    EvtFileChanged?.Invoke(DebuggerStore.instance.manager.AvmFilePath);
                }
                else
                {
                    if (_selectedFile.EndsWith("py") || _selectedFile.EndsWith("cs"))
                    {
                        _selectedProjectFile = _selectedFile;
                    }
                    EvtFileChanged?.Invoke(_selectedFile);
                }
                UpdateBreakpointView(1, false);
            }
        }

        public string DisassembleAVMFile(string avmSourceCode)
        {
            string content = null;
            if (SelectedFile.EndsWith(".avm"))
            {
                // SelectedFile is not the path of .avm nor a valid file path
                content = DebuggerStore.instance.manager.GetContentFor(DebuggerStore.instance.manager.AvmFilePath);
            }

            return content;
        }


        private async Task LoadAvm(string avmFilePath)
        {
            bool loaded = DebuggerStore.instance.manager.LoadContract(avmFilePath);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (loaded)
                {
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
                        SelectedFile = avmFilePath;
                    }
                }
            });
        }

        private async Task LoadContract(string file)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SelectedFile = file;
                if (file.EndsWith("avm"))
                {
                    await LoadAvm(file);
                }
                else
                {
                    await CompileCurrentFile();
                }
            });

        }

        internal async Task ResetWithNewFile(string result)
        {
            if (!result.EndsWith(".cs", StringComparison.Ordinal) && !result.EndsWith(".py", StringComparison.Ordinal))
            {
                SendLogToPanel("File not supported. Please use .cs or .py extension.");
                return;
            }

            if (!File.Exists(result))
            {
                LoadTemplate(result);
            }
            SendLogToPanel("Resetting with new file");

            this.ProjectFiles.Clear();
            this.ProjectFiles.Add(result);
            this.SelectedFile = ProjectFiles[0];
            await CompileCurrentFile();
        }

        public async Task SaveAndRebuild()
        {
            await Task.Run(async () =>
            {
                File.WriteAllText(SelectedFile, EditorFileContent);
                await CompileCurrentFile();
            });
        }

        //Current compiler does not support multiple files
        public async Task CompileCurrentFile()
        {
            if (_selectedProjectFile != null)
            {
                EvtFileToCompileChanged?.Invoke();
                var sourceCode = File.ReadAllText(_selectedProjectFile);

                await Task.Run(async () =>
                {
                    SendLogToPanel("Compiling project...");
                    try
                    {
                        bool compiled = DebuggerStore.instance.manager.CompileContract(sourceCode, LanguageSupport.DetectLanguage(this.SelectedFile), this.SelectedFile);
                        if (compiled)
                        {
                            string avmFile = this.SelectedFile.Replace(LanguageSupport.GetExtension(LanguageSupport.DetectLanguage(this.SelectedFile)), ".avm");
                            SendLogToPanel("Compiled file located at " + avmFile);
                            await LoadAvm(avmFile);
                        }
                    }
                    catch(Exception ex)
                    {
                        SendLogToPanel("Compiler error: " + ex.Message);
                    }

                });
            }
        }

        private void LoadTemplate(string result)
        {
            SendLogToPanel("Loading template. ");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            string fullFilePath = null;
            if (result.EndsWith("cs", StringComparison.Ordinal))
            {
                SendLogToPanel("Loading CS Template");
                fullFilePath = Path.Combine(path, "ContractTemplate.cs");
            }
            else if (result.EndsWith("py", StringComparison.Ordinal))
            {
                fullFilePath = Path.Combine(path, "NEP5.py");
            }

            var sourceCode = File.ReadAllText(fullFilePath);
            File.WriteAllText(result, sourceCode);
        }

        public async Task Step()
        {
            if (DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint)
            {
                var previousLine = DebuggerStore.instance.manager.CurrentLine;
                do
                {
                    DebuggerStore.instance.manager.Step();
                } while (previousLine == DebuggerStore.instance.manager.CurrentLine);

                await CheckResults();
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
            var filteredExtensions = new List<string>(new string[] { "avm", "cs", "py" });
            var filter = new FileDialogFilter { Extensions = filteredExtensions, Name = "NEO AVM files" };
            filters.Add(filter);
            dialog.Filters = filters;
            dialog.AllowMultiple = false;

            var result = await dialog.ShowAsync(Application.Current.MainWindow);

            if (result != null && result.Length > 0)
            {
                await LoadContract(result[0]);
            }
        }

        public async Task TryRun()
        {
            if (DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint)
            {
                await RunDebugger();
            }
            else
            {
                await OpenInvokeWindow();
            }
        }

        public async Task CheckResults()
        {
            ConsumedGas = DebuggerStore.instance.UsedGasCost;

            UpdateCurrentLineView();
            UpdateStackPanel();

            if (!DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint)
            {
                IsStopped = false;
                Neo.VM.StackItem result = null;
                string errorMessage = null;
                try
                {
                    result = DebuggerStore.instance.manager.Emulator.GetOutput();
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                if (result != null)
                {
                    await OpenGenericSampleDialog("Execution finished.\nGAS cost: " + DebuggerStore.instance.UsedGasCost + "\nResult: " + result.GetString(), "OK", "", false);
                }
                else
                {
                    await OpenGenericSampleDialog(errorMessage, "Error", "", false);
                }
            }
            else
            {
                IsStopped = true;
            }
        }

        public async Task OpenInvokeWindow()
        {
            var invokeWindow = new InvokeWindow();
            await invokeWindow.ShowDialog(Application.Current.MainWindow);
            await RunDebugger();
        }

        public async Task RunDebugger()
        {
            if(!DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint)
            {
                DebuggerStore.instance.manager.ConfigureDebugParameters(DebuggerStore.instance.DebugParams);
            }

            DebuggerStore.instance.manager.Run();
            
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await CheckResults();
            });
        }

        private HashSet<int> GetBreakpointHashSet()
        {
            //  when selected file is .avm needs to you disassembler
            if (!SelectedFile.EndsWith(".avm", StringComparison.Ordinal))
            {
                return DebuggerStore.instance.manager.Emulator.Breakpoints.Select(x => DebuggerStore.instance.manager.ResolveLine(x, true, out _selectedFile) + 1).ToHashSet();
            }
            return DebuggerStore.instance.manager.Emulator.Breakpoints.Select(x => DebuggerStore.instance.manager.avmDisassemble.ResolveLine(x) + 1).ToHashSet();
        }

        public void AddBreakpoint(int line)
        {
            // SelectedFile is not the path of .avm nor a valid file path
            if (SelectedFile.EndsWith(".avm"))
            {
                DebuggerStore.instance.manager.AddBreakpoint(line - 1, DebuggerStore.instance.manager.AvmFilePath);
            }
            else
            {
                DebuggerStore.instance.manager.AddBreakpoint(line - 1, SelectedFile);
            }
            UpdateBreakpointView(line, true);
        }

        public void RemoveBreakpoint(int line)
        {
            // SelectedFile is not the path of .avm nor a valid file path
            if (SelectedFile.EndsWith(".avm"))
            {
                DebuggerStore.instance.manager.RemoveBreakpoint(line - 1, DebuggerStore.instance.manager.AvmFilePath);
            }
            else
            {
                DebuggerStore.instance.manager.RemoveBreakpoint(line - 1, SelectedFile);
            }
            UpdateBreakpointView(line, false);
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
                    if (!SelectedFile.EndsWith(".avm"))
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
                    else
                    {
                        // add breakpoint in the line of the disassembled avm file
                        AddBreakpoint(line);
                    }
                }
            }
        }

        public void StopDebugging()
        {
            if (DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint)
            {
                DebuggerStore.instance.manager.Emulator.Stop();
                DebuggerStore.instance.manager.Run();
                UpdateCurrentLineView();
            }
        }

        private void UpdateStackPanel()
        {
            if (DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint) {
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
            }
            else
            {
                EvaluationStack.Clear();
                AltStack.Clear();
                StackIndex = -1;
            }

            EvtVMStackChanged?.Invoke(EvaluationStack, AltStack, StackIndex);

        }

        public void UpdateCurrentLineView()
        {

            if (SelectedFile.EndsWith(".avm"))
            {
                var offset = DebuggerStore.instance.manager.Offset;
                EvtDebugCurrentLineChanged?.Invoke(DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint, DebuggerStore.instance.manager.avmDisassemble.ResolveLine(offset) + 1);
            }
            else
            {
                EvtDebugCurrentLineChanged?.Invoke(DebuggerStore.instance.manager.IsSteppingOrOnBreakpoint, DebuggerStore.instance.manager.CurrentLine + 1);
            }

        }

        public void UpdateBreakpointView(int line, bool addBreakpoint)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                EvtBreakpointStateChanged?.Invoke(line, addBreakpoint);
                UpdateCurrentLineView();
            });
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

            var result = await dialog.ShowAsync(Application.Current.MainWindow);

            if (result != null && result.Length > 0)
            {
                DebuggerStore.instance.manager.Blockchain.Load(result[0]);
            }
        }

        public async void ResetBlockchain()
        {
            if (!DebuggerStore.instance.manager.BlockchainLoaded)
            {
                await OpenGenericSampleDialog("No blockchain loaded yet!", "Ok", "", false);
                return;
            }

            if (DebuggerStore.instance.manager.Blockchain.currentHeight > 1)
            {
                if (!(await OpenGenericSampleDialog("The current loaded Blockchain already has some transactions.\n" +
                    "This action can not be reversed, are you sure you want to reset it?", "Yes", "No", true)))
                {
                    return;
                }
            }

            DebuggerStore.instance.manager.Blockchain.Reset();
            DebuggerStore.instance.manager.Blockchain.Save();

            SendLogToPanel("Reset to virtual blockchain at path: " + DebuggerStore.instance.manager.Blockchain.fileName);
        }

        public void OpenKeyTool()
        {
            var keyToolWindow = new KeyToolWindow();
            keyToolWindow.ShowDialog(Application.Current.MainWindow);
        }
    }
}
