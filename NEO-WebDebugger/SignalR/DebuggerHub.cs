using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Neo.WebDebugger
{
    public class DebuggerHub : Hub
    {
        public void LogEventMessage(string message)
        {
            Clients.All.logEventMessage(message);
        }

        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            
        }
    }
}