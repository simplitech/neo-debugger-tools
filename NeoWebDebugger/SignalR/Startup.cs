using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Neo.WebDebugger.Startup))]

namespace Neo.WebDebugger
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            // Wire up the SignalR hub
            app.MapSignalR();
        }
    }
}
