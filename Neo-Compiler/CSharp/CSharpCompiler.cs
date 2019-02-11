using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Numerics;
using Neo.Compiler.AVM;

namespace Neo.Compiler
{
    public class CSharpCompiler
    {
        private static string GenerateMainMethod(TypeDefinition t)
        {
            var code = new List<string>();
            code.Add("\tpublic static Object Main(string operation, params object[] args) {");

            code.Add("\tif (Runtime.Trigger == TriggerType.Verification) {");
            code.Add("\t\tif (Owner.Length == 20) {");            
            code.Add("\t\t\treturn Runtime.CheckWitness(Owner); // if param Owner is script hash");
            code.Add("\t\t}");
            code.Add("\telse if (Owner.Length == 33) {");            
            code.Add("\tbyte[] signature = operation.AsByteArray(); // if param Owner is public key");
            code.Add("\treturn VerifySignature(signature, Owner);");
            code.Add("\t\t}");
            code.Add("\t}");

            var methods = t.Methods;
            foreach (var method in methods)
            {
                if (!method.IsStatic)
                {
                    continue;
                }

                if (method.Name == ".cctor")
                {
                    continue;
                }

                if (method.HasParameters)
                {
                    var first = method.Parameters[0];
                    var argType = first.ParameterType;
                    if (first.Name == "value" && argType.Name.Contains("Action`"))
                    {
                        continue;
                    }
                }

                var parameters = method.Parameters;
                var parameterDescriptions = string.Join(", ", method.Parameters.Select(x => x.ParameterType + " " + x.Name).ToArray());

                Console.WriteLine("{0} {1} ({2})",
                                  method.ReturnType,
                                  method.Name,
                                  parameterDescriptions);
            }
            code.Add("}");

            var output = string.Join("\n", code.ToArray());
            return output;
        }

        public static bool Execute(string fileName, ILogger logger)
        {
            var code = File.ReadAllText(fileName);

            var options = new CSharpParseOptions(kind: SourceCodeKind.Regular, languageVersion: LanguageVersion.Latest);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, options, fileName,  System.Text.Encoding.UTF8);

            // note - This path must patch the .NET framework version used to build the Neo.SmartContract.Framework.dll
            var coreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

            var libLocation = typeof(object).GetTypeInfo().Assembly.Location;
            var libFolder = Path.GetDirectoryName(libLocation);

            var neededAssemblies = new string[]
            {
                "System.dll",
                "System.Numerics.dll",
                "System.Private.CoreLib.dll",
                "netstandard.dll",
                "System.Runtime.dll",
                "System.Runtime.Numerics.dll"
            };

            List<MetadataReference> references = neededAssemblies.Select(x => MetadataReference.CreateFromFile(Path.Combine(libFolder, x))).ToList<MetadataReference>();

            var exePath = Directory.GetCurrentDirectory();
            references.Add(MetadataReference.CreateFromFile(Path.Combine(exePath, "Neo.SmartContract.Framework.dll")));

            var contractName = Path.GetFileNameWithoutExtension(fileName);
            var assemblyName = Path.Combine(exePath, contractName+".dll");

            var compileOptions = new CSharpCompilationOptions(
                           OutputKind.DynamicallyLinkedLibrary,
                           optimizationLevel: OptimizationLevel.Debug,
                           allowUnsafe: true);

            Compilation compilation = CSharpCompilation.Create("Contract", options: compileOptions, references: references, syntaxTrees: new[] { syntaxTree });

            var pdbName = assemblyName.Replace(".dll", ".pdb");

            var assemblyStream = new FileStream(assemblyName, FileMode.Create);
            var pdbStream = new FileStream(pdbName, FileMode.Create);
            var emitResult = compilation.Emit(assemblyStream, pdbStream);

            assemblyStream.Close();
            pdbStream.Close();

            if (emitResult.Success)
            {
                /*assemblyStream.Seek(0, SeekOrigin.Begin);
                AssemblyDefinition assemblyInfo = AssemblyDefinition.ReadAssembly(assemblyStream);

                var mod = assemblyInfo.Modules.FirstOrDefault();
                foreach (var t in mod.Types)
                {
                    if (!t.Name.StartsWith("<"))
                    {
                        GenerateMainMethod(t);
                    }
                }
                */

                return AVMCompiler.Execute(assemblyName, pdbName, logger);
            }
            else
            {
                foreach (var d in emitResult.Diagnostics)
                {
                    var lineSpan = d.Location.GetLineSpan();
                    var startLine = lineSpan.StartLinePosition.Line;
                    logger.Log(string.Format("Line {0}: {1}", startLine, d.GetMessage()));
                }
                return false;
            }
        }

    }
}
