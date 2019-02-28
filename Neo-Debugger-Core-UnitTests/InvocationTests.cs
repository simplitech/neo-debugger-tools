using System;
using System.IO;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using NeoDebuggerCore.Utils;
using NUnit.Framework;

namespace Neo
{
	[TestFixture]
	public class DebuggerCoreUnitTests
	{

		private string _compilerFolder = Path.Combine(Path.Combine(
													  Directory.GetParent(TestContext.CurrentContext.TestDirectory).Parent.Parent.Parent.FullName,
													  "Output"), "neo-compiler");

        private NeonCompiler compiler = NeonCompiler.GetInstance(new DebuggerSettings());

        [Test]
		public void TestParameterParsing()
		{
			var argList = DebuggerUtils.GetArgsListAsNode("\"symbol\"");
			Assert.NotNull(argList);
			Assert.IsTrue(argList.ChildCount == 1);
		}

		[Test]
		public void TestParameterParsing2()
		{
			var argList = DebuggerUtils.GetArgsListAsNode("\"symbol\", []");
			Assert.NotNull(argList);
			Assert.IsTrue(argList.ChildCount == 2);
		}

		[Test]
		public void TestUseCompilerFromOutputFolder()
		{
			var path = TestContext.CurrentContext.TestDirectory;
			Console.WriteLine(_compilerFolder);
			Assert.IsTrue(File.Exists(_compilerFolder + "Neo-Compiler.dll"));
		}

		[Test]
		public void TestCSharpCompiler()
		{
			var path = TestContext.CurrentContext.TestDirectory;
			Directory.SetCurrentDirectory(path);
            var fullFilePath = Path.Combine(path, "SampleContract.cs");
            var sourceCode = File.ReadAllText(fullFilePath);
			Assert.NotNull(sourceCode);
			var compilerFolder = Path.Combine(Path.Combine(Directory.GetParent(path).Parent.Parent.Parent.FullName, "Output"), "neo-compiler");
			var compiled = compiler.CompileContract(sourceCode, fullFilePath, Neo.Debugger.Core.Data.SourceLanguage.CSharp, compilerFolder);
			Assert.IsTrue(compiled);
		}

        [Test]
        public void TestPythonCompiler()
        {
            var path = TestContext.CurrentContext.TestDirectory;
            Directory.SetCurrentDirectory(path);
            var fullFilePath = Path.Combine(path, "NEP5.py");
            var sourceCode = File.ReadAllText(fullFilePath);
            Assert.NotNull(sourceCode);
            var compiled = compiler.CompileContract(sourceCode, fullFilePath, Neo.Debugger.Core.Data.SourceLanguage.Python, "/Users/ricardmprado/Workspace/neo-boa/");
            Assert.IsTrue(compiled);
        }

        [Test]
		[Ignore("It won't pass in all OS")]
        public void TestGetCompilerInstance()
        {
            var compilerInstance = NeonCompiler.GetInstance(new DebuggerSettings());
            //Assert.IsInstanceOf(typeof(UnixCompiler), compilerInstance);
        }

    }
}
