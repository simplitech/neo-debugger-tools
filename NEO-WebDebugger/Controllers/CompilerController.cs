using System;
using System.IO;
using System.IO.Compression;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using Neo.Debugger.Core.Models;
using Neo.Debugger.Core.Utils;

namespace Neo.WebDebugger.Controllers
{
    public class CompilerController : Controller
    {
        [HttpPost]
        public ActionResult Compile(string source)
        {
            //Use settings from the My Documents folder, in a hosted / multi-tenant environment, this will have to change.  This works for local machine for now
            var settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            Compiler compiler = new Compiler(settings);
            compiler.SendToLog += Compiler_SendToLog;

            Directory.CreateDirectory(settings.path);
            var fileName = Path.Combine(settings.path, "DebugContract.cs");

            bool success = compiler.CompileContract(source, fileName, Debugger.Core.Data.SourceLanguage.CSharp);

            if (success)
            {
                compiler.Log("Contract compiled successfully.");
            }

            return Json(success);
        }

        private void Compiler_SendToLog(object sender, Debugger.Core.Models.CompilerLogEventArgs e)
        {
            GlobalHost.ConnectionManager.GetHubContext<DebuggerHub>().Clients.All.logEventMessage(e.Message);
        }
    }
}