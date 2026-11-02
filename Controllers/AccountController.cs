using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using toDo.Models;

namespace toDo.Controllers
{
    public class AccountController : BaseController
    {
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPanel();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPanel();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            User user = GetUserByEmail(email);

            if (user == null || user.Role != "User")
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact an administrator.");
                return View(model);
            }

            if (!PasswordHasher.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }

            SignIn(user, model.RememberMe);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl) &&
                returnUrl.StartsWith("/User", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "User");
        }

        [HttpGet]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPanel();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPanel();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string name = (model.Name ?? string.Empty).Trim();
            string email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            // Check again after trimming spaces.
            if (name.Length < 2)
            {
                ModelState.AddModelError("Name", "Enter your name.");
                return View(model);
            }

            string hash;
            string salt;
            PasswordHasher.CreateHash(model.Password, out hash, out salt);

            try
            {
                int userId = RegisterUser(name, email, hash, salt);
                var user = new User
                {
                    UserId = userId,
                    Name = name,
                    Email = email,
                    Role = "User",
                    IsActive = true
                };

                SignIn(user, false);
                return RedirectToAction("Index", "User");
            }
            catch (SqlException ex)
            {
                // SQL Server errors 2601 and 2627 mean the email is already used.
                if (ex.Number != 2601 && ex.Number != 2627)
                {
                    throw;
                }

                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            SignOutUser();
            return RedirectToAction("Login", "Account");
        }

        // Reads one user row with the GetUserByEmail stored procedure.
        private User GetUserByEmail(string email)
        {
            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.GetUserByEmail", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Email", email);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new User
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Name = Convert.ToString(reader["Name"]),
                        Email = Convert.ToString(reader["Email"]),
                        PasswordHash = Convert.ToString(reader["PasswordHash"]),
                        PasswordSalt = Convert.ToString(reader["PasswordSalt"]),
                        Role = Convert.ToString(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
            }
        }

        // Inserts a new user with the RegisterUser stored procedure.
        private int RegisterUser(string name, string email, string hash, string salt)
        {
            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.RegisterUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", hash);
                command.Parameters.AddWithValue("@PasswordSalt", salt);
                connection.Open();

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}
