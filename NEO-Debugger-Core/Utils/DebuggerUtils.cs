
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using Neo.Lux.Cryptography;
using Neo.Lux.Utils;

namespace Neo.Debugger.Core.Utils
{
    public static class DebuggerUtils
    {
        public static string DebuggerVersion = "v1.1";

        public static DataNode GetArgsListAsNode(string argList)
        {
            var node = JSONReader.ReadFromString("{\"params\": [" + argList + "]}");
            return node.GetNode("params");
        }

        public static string FindExecutablePath(string exeName, IEnumerable<string> extraPaths = null)
        {
            string envPath = Environment.ExpandEnvironmentVariables("%PATH%");
            var paths = envPath.Split(';').ToList();

            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            paths.Add(exePath);

            if (extraPaths != null)
            {
                foreach (var path in extraPaths)
                {
                    paths.Add(path);
                }
            }

            foreach (var entry in paths)
            {
                var path = entry.Replace("\\", "/");
                if (!path.EndsWith("/")) path += "/";

                var fullPath = path + exeName;
                if (File.Exists(fullPath))
                {
                    return path;
                }
            }

            return null;
        }

        public static bool IsValidWallet(string address)
        {
            if (string.IsNullOrEmpty(address) || address[0] != 'A')
            {
                return false;
            }

            try
            {
                var buffer = address.Base58CheckDecode();
                return buffer != null && buffer.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHex(string chars)
        {
            if (string.IsNullOrEmpty(chars)) return false;
            if (chars.Length % 2 != 0) return false;

            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        public static KeyPair GetKeyFromString(string key)
        {
            if (key.Length == 52)
            {
                return KeyPair.FromWIF(key);
            }
            else
            if (key.Length == 64)
            {
                var keyBytes = key.HexToBytes();
                return new KeyPair(keyBytes);
            }
            else
            {
                return null;
            }
        }

        public static string ParseNode(DataNode node, int index)
        {
            string val;

            if (node.ChildCount > 0)
            {
                val = "";

                foreach (var child in node.Children)
                {
                    if (val.Length > 0) val += ", ";

                    val += ParseNode(child, -1);
                }
                val = $"[{val}]";
            }
            else
            if (node.Kind == NodeKind.Null)
            {
                val = "[]";
            }
            else
            if (node.Kind == NodeKind.Numeric || node.Kind == NodeKind.Boolean)
            {
                val = node.Value;
            }
            else
            if (node.Kind == NodeKind.String)
            {
                val = $"\"{node.Value}\"";

            }
            else
            {
                val = node.Value;
            }

            return val;
        }

        public static  string BytesToString(byte[] bytes)
        {
            var s = "";
            foreach (var b in bytes)
            {
                if (s.Length > 0) s += ",";
                s += b.ToString();
            }
            s = $"[{s}]";
            return s;
        }

        public static string ReverseHex(string hex)
        {
            string result = "";
            for (var i = hex.Length - 2; i >= 0; i -= 2)
            {
                result += hex.Substring(i, 2);
            }
            return result;
        }

        public static string ToReadableByteArrayString(byte[] bytes)
        {
            var output = "";
            foreach (var item in bytes)
            {
                if (output.Length > 0) output += ",";
                output += $"{item.ToString().PadLeft(3)}";
            }
            output = $"[{output}]";
            return output;
        }

        public static string ToHexString(byte[] bytes)
        {
            return bytes.ToHexString();
        }

        public static byte[] AddressToScriptHash(string address)
        {
            return address.AddressToScriptHash();
        }
    }
}
