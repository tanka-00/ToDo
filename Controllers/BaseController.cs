using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using toDo.Models;

namespace toDo.Controllers
{
    public abstract class BaseController : Controller
    {
        protected int CurrentUserId
        {
            get
            {
                var identity = User.Identity as FormsIdentity;

                if (identity == null)
                {
                    return 0;
                }

                string[] parts = identity.Ticket.UserData.Split('|');
                int userId;

                return parts.Length > 0 && int.TryParse(parts[0], out userId) ? userId : 0;
            }
        }

        protected void SignIn(User user, bool rememberMe)
        {
            string userData = user.UserId + "|" + user.Role;
            int timeoutMinutes = rememberMe ? 60 * 24 * 7 : 120;

            var ticket = new FormsAuthenticationTicket(
                1,
                user.Email,
                DateTime.Now,
                DateTime.Now.AddMinutes(timeoutMinutes),
                rememberMe,
                userData,
                FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(
                FormsAuthentication.FormsCookieName,
                FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Path = FormsAuthentication.FormsCookiePath,
                Secure = Request.IsSecureConnection
            };

            if (rememberMe)
            {
                cookie.Expires = ticket.Expiration;
            }

            Response.Cookies.Add(cookie);
        }

        protected void SignOutUser()
        {
            FormsAuthentication.SignOut();
        }

        protected ActionResult RedirectToPanel()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return User.IsInRole("Admin")
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "User");
        }

        protected static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            string[] parts = name
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToArray();

            return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }

        protected static string GetFirstName(string name)
        {
            string[] parts = (name ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        protected static string GetGreeting()
        {
            int hour = DateTime.Now.Hour;

            if (hour < 12)
            {
                return "Good morning";
            }

            if (hour < 18)
            {
                return "Good afternoon";
            }

            return "Good evening";
        }
    }
}
