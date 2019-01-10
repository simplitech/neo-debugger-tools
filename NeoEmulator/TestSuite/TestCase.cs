
using System.Collections.Generic;
using System.IO;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;

namespace Neo.Emulation
{
    public class TestCase
    {
        public readonly string name;
        public readonly string method;
        public readonly DataNode args;

        public TestCase(string name, string method, DataNode args)
        {
            this.name = name;
            this.method = method;
            this.args = args;
        }

        public static TestCase FromNode(DataNode node)
        {
            var name = node.GetString("name");
            var method = node.GetString("method", null);
            var args = new List<string>();
            var argNode = node.GetNode("params");
            return new TestCase(name, method, argNode);
        }
    }
	
}
