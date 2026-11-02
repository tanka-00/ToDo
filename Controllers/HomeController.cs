using System.Web.Mvc;

namespace toDo.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return User.IsInRole("Admin")
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "User");
        }
    }
}
