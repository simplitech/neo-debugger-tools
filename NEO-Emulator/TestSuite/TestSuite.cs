using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LunarLabs.Parser.JSON;
using Neo.Emulation;

namespace NEO_Emulator.SmartContractTestSuite
{
	public class SmartContractTestSuite
	{
		public Dictionary<string, TestCase> cases = new Dictionary<string, TestCase>();
		public Dictionary<string, TestSequence> sequences = new Dictionary<string, TestSequence>();

		public SmartContractTestSuite()
		{

		}

		public SmartContractTestSuite(string fileName)
		{
			fileName = fileName.Replace(".avm", ".test.json");

			if (File.Exists(fileName))
			{
				var json = File.ReadAllText(fileName);
				LoadFromJson(json);
			}
		}
		
		public void LoadFromJson(string json)
		{
			var root = JSONReader.ReadFromString(json);

			var casesNode = root["cases"];
			foreach (var node in casesNode.Children)
			{
				var entry = TestCase.FromNode(node);
				cases[entry.name] = entry;
			}

			if (root.HasNode("sequences"))
			{
				var sequencesNode = root["sequences"];
				foreach (var node in sequencesNode.Children)
				{
					var entry = new TestSequence(node);
					sequences[entry.SequenceName] = entry;
				}
				
			}
		}
	}
}
