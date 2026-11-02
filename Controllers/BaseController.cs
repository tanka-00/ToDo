using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using toDo.Models;

namespace toDo.Controllers
{
    /// <summary>
    /// Shared login and session helpers for the Account, User, and Admin controllers.
    /// The Forms Authentication ticket stores "userId|role" in its UserData.
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Reads the signed-in user's ID from the authentication ticket.
        /// Returns 0 when nobody is signed in.
        /// </summary>
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

        /// <summary>
        /// Creates the authentication cookie after a successful login.
        /// Remember me lasts 7 days; a normal login lasts 120 minutes.
        /// </summary>
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

        /// <summary>
        /// Sends a signed-in user to their own panel.
        /// </summary>
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

        /// <summary>
        /// Builds avatar initials from the first two words of a name.
        /// </summary>
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

        /// <summary>
        /// Returns the first word of a name for the dashboard greeting.
        /// </summary>
        protected static string GetFirstName(string name)
        {
            string[] parts = (name ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        /// <summary>
        /// Returns a time-of-day greeting for the user dashboard.
        /// </summary>
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
