using System.Web;
using System.Web.Optimization;

namespace Neo.WebDebugger
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-3.3.1.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/signalr").Include(
                      "~/Scripts/jquery.signalR-2.2.3.min.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/site.css",
                      "~/Scripts/SemanticUI/semantic.min.css"));

            bundles.Add(new ScriptBundle("~/bundles/scripts").Include(
                      "~/Scripts/SemanticUI/semantic.min.js",
                      "~/Scripts/Ace/ace.js"));
        }
    }
}
