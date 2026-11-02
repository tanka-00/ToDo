using System.Web.Optimization;

namespace toDo
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap.bundle.js"));

            bundles.Add(new StyleBundle("~/Content/base").Include(
                        "~/Content/bootstrap.css",
                        "~/Content/css/site.css"));

            bundles.Add(new StyleBundle("~/Content/user").Include(
                        "~/Content/css/user.css"));

            bundles.Add(new StyleBundle("~/Content/admin").Include(
                        "~/Content/css/admin.css"));

            bundles.Add(new ScriptBundle("~/bundles/common").Include(
                        "~/Scripts/app/common.js"));

            bundles.Add(new ScriptBundle("~/bundles/auth").Include(
                        "~/Scripts/app/auth.js"));

            bundles.Add(new ScriptBundle("~/bundles/user").Include(
                        "~/Scripts/app/user.js"));

            bundles.Add(new ScriptBundle("~/bundles/admin").Include(
                        "~/Scripts/app/admin.js"));
        }
    }
}
