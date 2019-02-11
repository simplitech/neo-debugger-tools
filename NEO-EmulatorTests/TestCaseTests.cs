
using NEO_Emulator.SmartContractTestSuite;
using System;
using System.IO;
using Neo.Emulation.API;
using NUnit.Framework;


namespace Neo.Emulation.Tests
{
	[TestFixture]
	public class TestCaseTests
	{
		[OneTimeSetUp]
		public void Setup()
		{
			var path = TestContext.CurrentContext.TestDirectory;
			Directory.SetCurrentDirectory(path);
		}

		[Test]
		public void FromNodeTest()
		{
			string testData = File.ReadAllText("testFileSample.json");
			SmartContractTestSuite ts = new SmartContractTestSuite();
			ts.LoadFromJson(testData);
			Assert.AreEqual(2, ts.cases.Keys.Count);
			Assert.AreEqual(1, ts.sequences.Count);
			Assert.AreEqual(2, ts.sequences["Method1Method2"].Items.Count);
			Console.WriteLine(testData);
		}
	}
}