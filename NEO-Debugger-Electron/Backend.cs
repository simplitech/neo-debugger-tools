using Neo.Debugger.Core.Data;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;
using SynkServer.Core;
using SynkServer.HTTP;
using SynkServer.Templates;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Debugger.Electron
{
    public class FileEntry
    {
        public string name;
        public string path;
        public string url;
        public bool active;
    }

    public class Backend
    {
        private string activeFilePath;
        private Dictionary<string, FileEntry> projectFiles = new Dictionary<string, FileEntry>();
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();

        private DebugManager _debugger;

        private DebuggerSettings _settings;

        private bool LoadFile(string filePath, string content = null)
        {
            if (content == null)
            {
                content = File.ReadAllText(filePath);
            }

            fileContents[filePath] = content;

            var entry = new FileEntry() { path = filePath, name = Path.GetFileName(filePath), active = false };
            projectFiles[filePath] = entry;

            return true;
        }

        private bool LoadContract(string targetPath)
        {
            targetPath = targetPath.Replace("\\", "/");
            activeFilePath = targetPath;
            projectFiles.Clear();

            var extension = Path.GetExtension(targetPath);
            if (extension != ".avm")
            {
                var language = LanguageSupport.DetectLanguage(targetPath);
                if (language == SourceLanguage.Other)
                {
                    return false;
                }

                if (!LoadFile(targetPath))
                {
                    return false;
                }

                var sourceCode = fileContents[targetPath];

                var avmFilePath = targetPath.Replace(extension, ".avm");

                if (!File.Exists(avmFilePath))
                {
                    if (!_debugger.CompileContract(sourceCode, language, avmFilePath))
                    {
                        return false;
                    }
                }

                if (!_debugger.LoadContract(avmFilePath))
                {
                    return false;
                }
               
                LoadFile(avmFilePath, _debugger.avmDisassemble.ToString());
            }            

            return true;
        }

        private Dictionary<string, object> GetContext()
        {
            var context = new Dictionary<string, object>();

            foreach (var entry in projectFiles.Values)
            {
                entry.active = (entry.path == activeFilePath);
            }

            context["files"] = this.projectFiles.Values;

            return context;
        }

        public Backend(ServerSettings serverSettings)
        {
            _settings = new DebuggerSettings(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            var curPath = Directory.GetCurrentDirectory();

            if (!_settings.compilerPaths.ContainsKey(SourceLanguage.CSharp))
            {
                _settings.compilerPaths[SourceLanguage.CSharp] = curPath + "/compilers";
            }

            if (!_settings.compilerPaths.ContainsKey(SourceLanguage.Python))
            {
                _settings.compilerPaths[SourceLanguage.Python] = curPath + "/compilers";
            }

            _debugger = new DebugManager(_settings);

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
                fileContents[this.activeFilePath] = code;

                this.activeFilePath = request.args["path"];

                var context = GetContext();

                return "ok";
            });

            site.Get("/content", (request) =>
            {
                var content = fileContents[activeFilePath];
                return content;
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
