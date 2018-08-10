// NOTE - For this project to run it is necessary to download Electron and copy its contents to folder "Output/electron"
using LunarParser;
using LunarParser.JSON;
using SynkServer.Core;
using SynkServer.HTTP;
using SynkServer.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.Debugger.Electron
{
    public class FileEntry
    {
        public string id;
        public string name;
        public string path;
        public string url;
        public bool active;
        public string content;

        public HashSet<int> breakpoints = new HashSet<int>();
    }

    public class Backend
    {
        private string activeDocumentID;
        private Dictionary<string, FileEntry> projectFiles = new Dictionary<string, FileEntry>();

        private int docAllocID  = 100;

        private FileEntry LoadFile(string filePath, string content = null)
        {
            if (content == null)
            {
                if (File.Exists(filePath))
                {
                    content = File.ReadAllText(filePath);
                }
                else
                {
                    return null;
                }
            }

            var entry = new FileEntry() { path = filePath, name = Path.GetFileName(filePath), active = false, id = docAllocID.ToString(), content = content};

            projectFiles[entry.id] = entry;

            if (activeDocumentID == null)
            {
                activeDocumentID = entry.id;
            }

            docAllocID++;

            return entry;
        }

        private bool LoadContract(string targetPath)
        {
            targetPath = targetPath.Replace("\\", "/");
            projectFiles.Clear();

            activeDocumentID = null;

            var extension = Path.GetExtension(targetPath);
            if (extension != ".avm")
            {
                var language = LanguageSupport.DetectLanguage(targetPath);
                if (language == SourceLanguage.Other)
                {
                    return false;
                }

                var mainEntry = LoadFile(targetPath);
                if (mainEntry == null)
                {
                    return false;
                }

                var sourceCode = mainEntry.content;

                var avmFilePath = targetPath.Replace(extension, ".avm");

                if (!File.Exists(avmFilePath))
                {
                    return false;
                }

                if (!_shell.Debugger.LoadContract(avmFilePath))
                {
                    return false;
                }
               
                LoadFile(avmFilePath, _shell.Debugger.avmDisassemble.ToString());
            }
            
            return true;
        }

        private Dictionary<string, object> GetContext()
        {
            var context = new Dictionary<string, object>();

            foreach (var entry in projectFiles.Values)
            {
                entry.active = (entry.id == activeDocumentID);
            }

            context["files"] = this.projectFiles.Values;

            context["activeDocumentID"] = activeDocumentID;

            return context;
        }

        public Backend(ServerSettings serverSettings)
        {
            var curPath = Directory.GetCurrentDirectory();

            // initialize a logger
            var log = new SynkServer.Core.Logger();

            var server = new HTTPServer(log, serverSettings);

            var templateEngine = new TemplateEngine(server, "../views");

            // instantiate a new site, the second argument is the file path where the public site contents will be found
            var site = new Site(server, "../public");

            LoadContract(curPath+"/contracts/DefaultContract.cs");

            site.Get("/", (request) =>
            {
                var context = GetContext();

                return templateEngine.Render(site, context, new string[] { "index" });
            });

            site.Post("/switch", (request) =>
            {
                var code = request.args["code"];
                projectFiles[this.activeDocumentID].content = code;

                this.activeDocumentID = request.args["id"];

                var context = GetContext();

                return "ok";
            });

            site.Get("/content", (request) =>
            {
                var content = projectFiles[activeDocumentID].content;
                return content;
            });

            site.Get("/breakpoint/list", (request) =>
            {
                var breakpoints = projectFiles[activeDocumentID].breakpoints;
                var node = DataNode.CreateArray();
                foreach (var line in breakpoints)
                {
                    var item = DataNode.CreateValue(line);
                    node.AddNode(item);
                }
                return node;
            });

            site.Post("/breakpoint/add", (request) =>
            {
                int line = int.Parse(request.args["line"]);

                var file = projectFiles[activeDocumentID];
                if (_debugger.AddBreakpoint(line, file.path))
                {
                    var breakpoints = file.breakpoints;
                    breakpoints.Add(line);

                    return "ok";
                }

                return "fail";
            });

            site.Post("/breakpoint/remove", (request) =>
            {
                int line = int.Parse(request.args["line"]);

                var file = projectFiles[activeDocumentID];
                var breakpoints = file.breakpoints;

                breakpoints.Remove(line);

                _debugger.RemoveBreakpoint(line, file.path);

                return "ok";
            });

            site.Post("/compile", (request) =>
            {
                var code = request.args["code"];
                request.session.Set("code", code);

                if (Compile(serverSettings, code))
                {
                    return "OK";
                }

                return "FAIL";
            });

            site.Post("/shell", (request) =>
            {
                var input = request.args["input"];
                var output = DataNode.CreateObject();

                var lines = DataNode.CreateArray("lines");
                output.AddNode(lines);

                if (!_shell.Execute(input, (type, text) =>
                {
                    lines.AddValue(text);
                }))
                {
                    output.AddValue("Invalid command");
                }

                string filePath;
                var curLine = _shell.Debugger.ResolveLine(_shell.Debugger.Info.offset, true, out filePath);

                output.AddField("state", _shell.Debugger.Info.state);
                output.AddField("offset", _shell.Debugger.Info.offset);
                output.AddField("line", curLine);
                output.AddField("path", filePath);

                if (_shell.Debugger.Info.state == Emulation.DebuggerState.State.Finished)
                {
                    var val = _debugger.Emulator.GetOutput();

                    _debugger.Blockchain.Save();

                    var methodName = _debugger.Emulator.currentMethod;
                    var hintType = !string.IsNullOrEmpty(methodName) && _debugger.ABI != null && _debugger.ABI.functions.ContainsKey(methodName) ? _debugger.ABI.functions[methodName].returnType : Emulator.Type.Unknown;

                    var temp = FormattingUtils.StackItemAsString(val, false, hintType);
                    output.AddField("result", temp);
                    output.AddField("gas", _debugger.Emulator.usedGas);
                }

                var json = JSONWriter.WriteToString(output);
                return json;
            });

            server.Run();
        }

        public static bool Compile(ServerSettings settings, string code)
        {
            return false;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var settings = ServerSettings.Parse(args);
            settings.port = 7799;

            var backend = new Backend(settings);
        }
    }

}
