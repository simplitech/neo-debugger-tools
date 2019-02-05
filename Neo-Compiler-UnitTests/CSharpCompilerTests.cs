using System;
using System.IO;
using Neo.Compiler;
using NUnit.Framework;

namespace Neo_Compiler_UnitTests
{
    [TestFixture]
    public class CSharpCompilerTests : ILogger
    {
        public void Log(string log)
        {
            Console.WriteLine(log);
        }

        [Test]
        public void TestSharpCompiler()
        {
            var path = TestContext.CurrentContext.TestDirectory;
            Directory.SetCurrentDirectory(path);
            var compiled = CSharpCompiler.Execute(Path.Combine(path, "SampleContract.cs"), this);
            Assert.IsTrue(compiled);
        }



    }
}
