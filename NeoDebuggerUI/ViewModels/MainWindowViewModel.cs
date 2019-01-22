using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using NeoDebuggerUI.Models;

namespace NeoDebuggerUI.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public string[] FileNames => new string[] { "File1.cs", "File2.cs", "File3.cs", "File4.cs" };
		public List<DebuggableLine> FileContent { get; set; }

		public MainWindowViewModel()
		{
			FileContent = new List<DebuggableLine>();
			for (int i = 0; i < 2; i++)
			{
				var line = new DebuggableLine(i, false, "Line " + i);
				FileContent.Add(line);
			}
		}
	}
}
