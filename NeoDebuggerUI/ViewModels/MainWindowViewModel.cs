using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using ReactiveUI;
using ReactiveUI.Legacy;
using System.Reactive.Linq;
using NeoDebuggerUI.Models;
using NeoDebuggerUI.Views;

namespace NeoDebuggerUI.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public ReactiveList<string> ProjectFiles { get; } = new ReactiveList<string>();

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

		private string _fileFolder;
		private DateTime _lastModificationDate;


		public MainWindowViewModel()
		{
			Log = "Debugger started";
			Neo.Emulation.API.Runtime.OnLogMessage = SendLogToPanel;
			DebuggerStore.instance.manager.SendToLog += (o, e) => { SendLogToPanel(e.Message); };

			var fileChanged = this.WhenAnyValue(vm => vm.SelectedFile).ObserveOn(RxApp.MainThreadScheduler);
			fileChanged.Subscribe(file => LoadSelectedFile());
		}

		private Unit LoadSelectedFile()
		{
			EvtFileChanged?.Invoke(_selectedFile);
			return Unit.Default;
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

				SelectedFile = avmFilePath;
			}

			return true;
		}

		public void SendLogToPanel(string s)
		{
			Log += s + "\n";
		}

		public void ClearLog()
		{
			Log = "";
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

			if (result != null)
			{
				LoadContract(result[0]);
			}
		}

		public static async void OpenRunDialog()
		{
			var modalWindow = new InvokeWindow();
			var task = modalWindow.ShowDialog();
			await Task.Run(()=> task.Wait());
		}

		public static async void OpenGenericSampleDialog()
		{
			// TODO: this is a sample, rename it
			var clickedOk = await new GenericConfirmationWindow()
				.SetText("Do you want candy?")
				.SetOkText("Sure, I love candy!")
				.SetCancelText("Not today, I am on a diet :(")
				.Open();

			var wasOk = clickedOk;
		}

	}
}
