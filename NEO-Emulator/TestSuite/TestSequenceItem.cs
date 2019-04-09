using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LunarLabs.Parser;

namespace NEO_Emulator.SmartContractTestSuite
{
	public class TestSequenceItem
	{
		public string TestPrivateKey { get; private set; }
		public string TestName { get; private set; }
		public DataNode Result { get; private set; }
		

		public TestSequenceItem(DataNode itemNode)
		{
			this.TestPrivateKey = itemNode.GetString("testPrivateKey");
			this.TestName = itemNode.GetString("testName");
			this.Result = itemNode.GetNode("result");
		}
		
	}
}
