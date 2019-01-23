using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using ReactiveUI;

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
		private string _fileFolder;
		private DebugManager _debugger;
		private DebuggerSettings _settings;
		private DateTime _lastModificationDate;


		public MainWindowViewModel()
		{
			_settings = new DebuggerSettings(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			_debugger = new DebugManager(_settings);
		}

		private bool LoadContract(string avmFilePath)
		{
			if (!_debugger.LoadContract(avmFilePath))
			{
				return false;
			}

			_lastModificationDate = File.GetLastWriteTime(_debugger.AvmFilePath);
			_debugger.Emulator.ClearAssignments();

			if (_debugger.IsMapLoaded)
			{
				ProjectFiles.Clear();
				_fileFolder = Path.GetDirectoryName(avmFilePath);
				ProjectFiles.Add(Path.GetFileName(avmFilePath));
				foreach (var path in _debugger.Map.FileNames)
				{
					_debugger.LoadAssignmentsFromContent(path);
					var fileName = Path.GetFileName(path);
					ProjectFiles.Add(fileName);
				}
				
				SelectedFile = avmFilePath;
			}


			//ReloadTextArea(_debugger.CurrentFilePath);

			return true;
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


	}
}
