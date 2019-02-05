using Neo.WebDebugger.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Humanizer;
using Neo.Debugger.Core.Data;

namespace Neo.WebDebugger.Controllers
{
    public class ContractController : Controller
    {
        // GET: Contracts
        public ActionResult Index()
        {
            ViewData["CSharpTemplates"] = ContractsHelper.GetContractList(SourceLanguage.CSharp);
            ViewData["PythonTemplates"] = ContractsHelper.GetContractList(SourceLanguage.Python);
            return View();
        }

        public ContentResult GetContent(string fileName)
        {
            return Content(ContractsHelper.GetContractContent(fileName));
        }
    }
}