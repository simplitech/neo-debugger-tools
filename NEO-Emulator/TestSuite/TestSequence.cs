using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LunarLabs.Parser;

namespace NEO_Emulator.SmartContractTestSuite
{
	public class TestSequence
	{
		public string SequenceName { get; private set; }
		public bool ResetBlockchain { get; private set; }
		public List<TestSequenceItem> Items { get; private set; }

		public TestSequence(DataNode sequenceNode)
		{
			this.SequenceName = sequenceNode.GetString("sequenceName");
			this.ResetBlockchain = sequenceNode.GetBool("resetBlockchain");
			var sequenceItems = sequenceNode.GetNode("items");
			this.Items = new List<TestSequenceItem>();
			foreach (var children in sequenceItems.Children)
			{
				TestSequenceItem item = new TestSequenceItem(children);
				this.Items.Add(item);
			}
	
		}
		
	}
}
