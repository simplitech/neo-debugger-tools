using System.IO;
using LunarLabs.Parser;
using Neo.Emulation;
using Neo.Emulation.API;
using NUnit.Framework;

namespace ICO_Unit_Tests
{
    [TestFixture]
    public class ContractTests
    {
        private static Emulator emulator; 

        [OneTimeSetUp]
        public void Setup()
        {
            var path = TestContext.CurrentContext.TestDirectory.Replace("ICOUnitTests", "ICOTemplate");
            Directory.SetCurrentDirectory(path);
            var avmBytes = File.ReadAllBytes("ICOContract.avm");
            var chain = new Blockchain();
            emulator = new Emulator(chain);
            var address = chain.DeployContract("test", avmBytes);
            emulator.SetExecutingAccount(address);
        }

        [Test]
        public void TestSymbol()
        {
            var inputs = DataNode.CreateArray();
            inputs.AddValue("symbol");
            inputs.AddValue(null);

            var script = emulator.GenerateLoaderScriptFromInputs(inputs, null);
            emulator.Reset(script, null, null);
            emulator.Run();

            var result = emulator.GetOutput();
            Assert.NotNull(result);

            var symbol = result.GetString();
            Assert.IsTrue(symbol.Equals("DEMO"));
        }
    }   
}
