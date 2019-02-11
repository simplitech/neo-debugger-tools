using System;
using System.IO;
using Neo.Debugger.Core.Models;
using NeoDebuggerCore.Utils;
using NUnit.Framework;

namespace Neo_Debugger_UI_UnitTests
{
    [TestFixture]
    public class ViewModelTests
    {

        [Test]
        public void TestCanCompileTemplate()
        {
            var path = TestContext.CurrentContext.TestDirectory;
            Directory.SetCurrentDirectory(path);
            var compiler = NeonCompiler.GetInstance(new DebuggerSettings());
            var fullFilePath = Path.Combine(path, "ContractTemplate.cs");
            var sourceCode = File.ReadAllText(fullFilePath);
            Assert.NotNull(sourceCode);
            var compilerFolder = Path.Combine(Path.Combine(Directory.GetParent(path).Parent.Parent.Parent.FullName, "Output"), "neo-compiler");
            var compiled = compiler.CompileContract(sourceCode, fullFilePath, Neo.Debugger.Core.Data.SourceLanguage.CSharp, compilerFolder);
            Assert.IsTrue(compiled);
        }

        [Test]
        public void TestCanLoadTemplate()
        { 

        }


    }
}
