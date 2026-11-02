using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace toDo
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var identity = Context.User.Identity as FormsIdentity;

            if (identity == null)
            {
                return;
            }

            string[] parts = identity.Ticket.UserData.Split('|');

            if (parts.Length > 1)
            {
                Context.User = new GenericPrincipal(identity, new[] { parts[1] });
            }
        }
    }
}
