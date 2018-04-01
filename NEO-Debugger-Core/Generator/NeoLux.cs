using Neo.Emulation;
using System;
using System.IO;
using System.Text;

namespace Neo.Debugger.Core.Generator
{
    public static class NeoLux
    {
        public  static string ConvertType(Emulator.Type type)
        {
            switch (type)
            {
                case Emulator.Type.Boolean: return "bool";
                case Emulator.Type.String: return "string";

                case Emulator.Type.Integer: return "BigInteger";
                case Emulator.Type.ByteArray: return "byte[]";
                case Emulator.Type.Array: return "object[]";

                default: throw new ArgumentException("Invalid type: " + type);
            }
        }

        public static string GenerateInterface(ABI abi)
        {
            var code = new StringBuilder();

            code.AppendLine("using NeoLux;");
            code.AppendLine("using Neo.Cryptography;");
            code.AppendLine("using System.Numerics;");
            code.AppendLine();


            var contractName = abi.fileName;
            while (contractName.Contains("."))
            {
                contractName = Path.GetFileNameWithoutExtension(contractName);
            }

            code.AppendLine("public class " + contractName + " {");
            code.AppendLine();

            code.AppendLine("\tpublic string ContractHash { get; private set; }");
            code.AppendLine();

            code.AppendLine("\tpublic " + contractName + "(string contractHash) {");
            code.AppendLine("\t\tthis.ContractHash = contractHash;");
            code.AppendLine("\t}");


            foreach (var entry in abi.functions)
            {
                if (entry.Key == abi.entryPoint.name)
                {
                    continue;
                }

                var method = entry.Value;

                bool isPure = false;

                var returnType = ConvertType(method.returnType);
                code.AppendLine();

                code.Append("\tpublic " + returnType + " " + entry.Key + "(");

                int count = 0;

                if (!isPure)
                {
                    code.Append("KeyPair from_key");
                    count++;
                }

                foreach (var arg in method.inputs)
                {
                    if (count > 0) code.Append(",");

                    var argType = ConvertType(arg.type);
                    code.Append(argType+" " +arg.name);
                    count++;
                }

                code.AppendLine(") {");

                code.Append("\t\tvar response = api.CallContract(from_key, contractHash, \"" + method.name + "\", new object[] { ");

                count = 0;
                if (!isPure)
                {
                    code.Append("from_key");
                    count++;
                }


                foreach (var arg in method.inputs)
                {
                    if (count > 0) code.Append(",");

                    code.Append(arg.name);
                    count++;
                }

                code.AppendLine("});");

                switch (method.returnType)
                {
                    case Emulator.Type.Integer: code.AppendLine("\t\tvar result = new BigInteger((byte[])response.result[0]);"); break;
                    case Emulator.Type.String: code.AppendLine("\t\tvar result = System.Text.Encoding.UTF8.GetString((byte[])response.result[0]);"); break;
                    case Emulator.Type.Boolean: code.AppendLine("\t\tvar result = (bool)response.result[0];"); break;
                    case Emulator.Type.ByteArray: code.AppendLine("\t\tvar result = (byte[])response.result[0];"); break;
                    case Emulator.Type.Array: code.AppendLine("\t\tvar result = response.result[0];"); break;
                    default: throw new ArgumentException("Invalid type: " + method.returnType);
                }
                

                code.AppendLine("\t\treturn result;");

                code.AppendLine("\t}");
            }

            code.AppendLine("}");

            return code.ToString();
        }
    }
}
