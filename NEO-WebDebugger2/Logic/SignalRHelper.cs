using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Neo.WebDebugger.Logic
{
    public static class SignalRHelper
    {
        public static void LogEventMessage(string connectionId, string message)
        {
            GlobalHost.ConnectionManager.GetHubContext<DebuggerHub>().Clients.Client(connectionId).logEventMessage(message);
        }
    }
}