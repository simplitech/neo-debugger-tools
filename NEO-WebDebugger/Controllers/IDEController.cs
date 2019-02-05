using Neo.Debugger.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Neo.WebDebugger.Controllers
{
    public class IDEController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public void SetConnectionId(string connectionId)
        {
            Session["ConnectionId"] = connectionId;
        } 

        public bool ToggleBreakpoint(int line)
        {
            return true;
        }
    }
}