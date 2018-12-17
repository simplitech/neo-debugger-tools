
using Neo.Debugger.Core.Utils;
using NEO_Emulator.SmartContractTestSuite;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Emulation.Utils;


namespace Neo.Debugger.Core.Utils.Tests
{
	[TestFixture]
	public class DebugManagerTests
	{
		DebugManager debugManager = new DebugManager();

		[OneTimeSetUp]
		public void Setup()
		{
			var path = TestContext.CurrentContext.TestDirectory;
			Directory.SetCurrentDirectory(path);
			debugManager.LoadContract("testFile.avm");
		}

		[Test]
		public void LoadSequenceTest()
		{
			List<object> resultList = debugManager.RunSequence("Method1Method2");
			Assert.AreEqual(2, resultList.Count);
			string name = FormattingUtils.StackItemAsString((VM.StackItem)resultList[0]);
			string symbol = FormattingUtils.StackItemAsString((VM.StackItem)resultList[1]);
			Assert.AreEqual("BNDES Token", name);
			Assert.AreEqual("BNDT", symbol);
		}
	}
}