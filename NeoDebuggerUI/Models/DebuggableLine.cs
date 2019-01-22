using System;
using System.Collections.Generic;
using System.Text;

namespace NeoDebuggerUI.Models
{
	public class DebuggableLine
	{
		public int LineNumber { get; set; }
		public bool ActiveBreakpoint { get; set; }
		public string LineContent { get; set; }

		public DebuggableLine(int lineNumber, bool activeBreakpoint, string lineContent)
		{
			LineNumber = lineNumber;
			ActiveBreakpoint = activeBreakpoint;
			LineContent = lineContent;
		}
	}
}
