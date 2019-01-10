using Microsoft.AspNetCore.Mvc;

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