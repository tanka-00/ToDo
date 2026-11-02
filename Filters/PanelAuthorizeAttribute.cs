using System;
using System.Web;
using System.Web.Mvc;

namespace toDo.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PanelAuthorizeAttribute : AuthorizeAttribute
    {
        public string Panel { get; private set; }

        public PanelAuthorizeAttribute(string panel)
        {
            Panel = panel;
            Roles = panel;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            HttpContextBase http = filterContext.HttpContext;

            if (http.Request.IsAjaxRequest())
            {
                http.Response.SuppressFormsAuthenticationRedirect = true;

                filterContext.Result = new HttpStatusCodeResult(
                    http.User.Identity.IsAuthenticated
                        ? System.Net.HttpStatusCode.Forbidden
                        : System.Net.HttpStatusCode.Unauthorized);
                return;
            }

            if (http.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectResult("~/");
                return;
            }

            string loginUrl = Panel == "Admin" ? "~/Admin/Login" : "~/Account/Login";
            string returnUrl = http.Server.UrlEncode(http.Request.RawUrl);

            filterContext.Result = new RedirectResult(loginUrl + "?returnUrl=" + returnUrl);
        }
    }
}
