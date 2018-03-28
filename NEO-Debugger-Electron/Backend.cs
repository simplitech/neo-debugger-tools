using LunarParser;
using Neo.Debugger.Core.Data;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using Neo.Debugger.Shell;
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

        private DebuggerShell _shell;

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
                    if (!_shell.Debugger.CompileContract(sourceCode, language, avmFilePath))
                    {
                        return false;
                    }
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
            var settings = new DebuggerSettings(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            var curPath = Directory.GetCurrentDirectory();

            if (!settings.compilerPaths.ContainsKey(SourceLanguage.CSharp))
            {
                settings.compilerPaths[SourceLanguage.CSharp] = curPath + "/compilers";
            }

            if (!settings.compilerPaths.ContainsKey(SourceLanguage.Python))
            {
                settings.compilerPaths[SourceLanguage.Python] = curPath + "/compilers";
            }

            _shell = new DebuggerShell(settings);

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

                var breakpoints = projectFiles[activeDocumentID].breakpoints;
                breakpoints.Add(line);

                return "ok";
            });

            site.Post("/breakpoint/remove", (request) =>
            {
                int line = int.Parse(request.args["line"]);

                var breakpoints = projectFiles[activeDocumentID].breakpoints;
                breakpoints.Remove(line);

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
                var output = new StringBuilder();

                _shell.Execute(input, (type, text) =>
                {
                    output.AppendLine(text);
                });

                return output.ToString();
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
