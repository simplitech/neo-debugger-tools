using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Neo.WebDebugger
{
    public class DebuggerHub : Hub
    {
        public void Send(string connectionId, string message)
        {
            // Call the broadcastMessage method to update clients.

        }
    }
}