using LunarParser;
using LunarParser.JSON;
using Neo.Debugger.Core.Data;
using Neo.Emulator.API;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Debugger.Core.Models
{
    public class Settings
    {
        public string lastOpenedFile;
        public string lastPrivateKey;

        public string lastFunction;

        public Dictionary<SourceLanguage, string> compilerPaths = new Dictionary<SourceLanguage, string>();

        public Dictionary<string, string> lastParams = new Dictionary<string, string>();

        private string fileName;
        public readonly string path;


        public Settings(string settingsFolderPath)
        {
            this.path = settingsFolderPath + @"\Neo Contracts";

            this.fileName = path + @"\debugger.settings.json";

            if (File.Exists(fileName))
            {
                var json = File.ReadAllText(fileName);
                var root = JSONReader.ReadFromString(json);
                root = root["settings"];

                this.lastOpenedFile = root.GetString("lastfile");
                this.lastPrivateKey = root.GetString("lastkey", Blockchain.InitialPrivateWIF);

                var paramsNode = root.GetNode("lastparams");
                this.lastParams.Clear();

                if (paramsNode != null)
                {
                    foreach (var child in paramsNode.Children)
                    {
                        var key = child.GetString("key");
                        var value = child.GetString("value");
                        this.lastParams[key] = value;
                    }
                }

                var compilersNode = root.GetNode("compilers");
                if (compilersNode != null)
                {
                    foreach (var child in compilersNode.Children)
                    {
                        var key = child.GetString("language");
                        var value = child.GetString("path");

                        SourceLanguage language;
                        
                        if (Enum.TryParse<SourceLanguage>(key, out language))
                        {
                            this.compilerPaths[language] = value;
                        }                        
                    }
                }
            }
        }

        public void Save()
        {
            var root = DataNode.CreateObject("settings");
            root.AddField("lastfile", this.lastOpenedFile);
            root.AddField("lastkey", this.lastPrivateKey);

            var paramsNode = DataNode.CreateArray("lastparams");
            foreach (var entry in lastParams)
            {
                var node = DataNode.CreateObject();
                node.AddField("key", entry.Key);
                node.AddField("value", entry.Value);
                paramsNode.AddNode(node);
            }
            root.AddNode(paramsNode);

            var compilersNode = DataNode.CreateArray("compilers");
            foreach (var entry in this.compilerPaths)
            {
                var node = DataNode.CreateObject();
                node.AddField("language", entry.Key);
                node.AddField("path", entry.Value);
                compilersNode.AddNode(node);
            }
            root.AddNode(compilersNode);

            var json = JSONWriter.WriteToString(root);

            Directory.CreateDirectory(this.path);

            File.WriteAllText(fileName, json);
        }
    }
}
