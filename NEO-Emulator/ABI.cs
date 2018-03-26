using LunarParser.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Emulator
{
    public class AVMInput
    {
        public string name;
        public NeoEmulator.Type type;
    }

    public class AVMFunction
    {
        public string name;
        public NeoEmulator.Type returnType;
        public List<AVMInput> inputs = new List<AVMInput>();
    }

    public class ABI
    {
        public Dictionary<string, AVMFunction> functions = new Dictionary<string, AVMFunction>();
        public AVMFunction entryPoint { get; private set; }
        public readonly string fileName;

        public ABI()
        {
            var f = new AVMFunction();
            f.name = "Main";
            f.inputs = new List<AVMInput>();
            f.inputs.Add(new AVMInput() { name = "args", type = NeoEmulator.Type.Array });

            this.functions[f.name] = f;
            this.entryPoint = functions.Values.FirstOrDefault();
        }

        public ABI(string fileName)
        {
            this.fileName = fileName;

            var json = File.ReadAllText(fileName);
            var root = JSONReader.ReadFromString(json);

            var fn = root.GetNode("functions");
            foreach (var child in fn.Children) {
                var f = new AVMFunction();
                f.name = child.GetString("name");
                if (!Enum.TryParse(child.GetString("returnType"), out f.returnType))
                {
                    f.returnType = NeoEmulator.Type.Unknown;
                }

                var p = child.GetNode("parameters");
                if (p != null && p.ChildCount > 0)
                {
                    f.inputs.Clear();
                    for (int i=0; i<p.ChildCount; i++)
                    {
                        var input = new AVMInput();
                        input.name = p[i].GetString("name");
                        if (!Enum.TryParse<NeoEmulator.Type>(p[i].GetString("type"), out input.type))
                        {
                            input.type = NeoEmulator.Type.Unknown;
                        }                         
                        f.inputs.Add(input);
                    }
                }
                else
                {
                    f.inputs = null;
                }

                functions[f.name] = f;
            }

            entryPoint = functions[root.GetString("entrypoint")];
        }
    }
}
