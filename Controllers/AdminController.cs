using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using toDo.Filters;
using toDo.Models;

namespace toDo.Controllers
{
    public class AdminController : BaseController
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

            if (user == null || user.Role != "Admin")
            {
                ModelState.AddModelError(string.Empty, "Admin email or password is incorrect.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This admin account is inactive.");
                return View(model);
            }

            if (!PasswordHasher.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Admin email or password is incorrect.");
                return View(model);
            }

            SignIn(user, model.RememberMe);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl) &&
                returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            SignOutUser();
            return RedirectToAction("Login", "Admin");
        }

        [PanelAuthorize("Admin")]
        public ActionResult Index()
        {
            User admin = GetUserById(CurrentUserId);

            if (admin == null || admin.Role != "Admin")
            {
                SignOutUser();
                return RedirectToAction("Login", "Admin");
            }

            ViewBag.DisplayName = admin.Name;
            ViewBag.Initials = GetInitials(admin.Name);
            ViewBag.FirstName = admin.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? admin.Name;

            return View(admin);
        }

        [HttpGet]
        [PanelAuthorize("Admin")]
        public JsonResult GetUsers()
        {
            List<UserSummary> users = GetUserSummaries();
            var payload = users.Select(ToUserJson).ToList();

            return Json(new { success = true, users = payload }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [PanelAuthorize("Admin")]
        public JsonResult GetUserTasks(int userId)
        {
            User user = GetUserById(userId);

            if (user == null || user.Role != "User")
            {
                return Json(new { success = false, message = "User not found." }, JsonRequestBehavior.AllowGet);
            }

            List<TaskItem> tasks = GetTasksForUser(userId);
            var payload = tasks.Select(ToTaskJson).ToList();

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.UserId,
                    name = user.Name,
                    email = user.Email,
                    initials = GetInitials(user.Name),
                    isActive = user.IsActive
                },
                tasks = payload
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PanelAuthorize("Admin")]
        public JsonResult ToggleUser(int userId, bool isActive)
        {
            if (userId <= 0)
            {
                return Json(new { success = false, message = "User not found." });
            }

            int affected = ToggleUserStatus(userId, isActive);

            return affected == 0
                ? Json(new { success = false, message = "User not found." })
                : Json(new
                {
                    success = true,
                    message = isActive ? "User activated." : "User deactivated."
                });
        }

        private static object ToUserJson(UserSummary user)
        {
            return new
            {
                id = user.UserId,
                name = user.Name,
                email = user.Email,
                initials = GetInitials(user.Name),
                isActive = user.IsActive,
                createdLabel = user.CreatedAt.ToString("MMM d, yyyy"),
                totalTasks = user.TotalTasks,
                completedTasks = user.CompletedTasks
            };
        }

        private static object ToTaskJson(TaskItem task)
        {
            return new
            {
                id = task.TaskId,
                title = task.Title,
                description = task.Description ?? string.Empty,
                isCompleted = task.IsCompleted,
                createdLabel = task.CreatedAt.ToString("MMM d, yyyy")
            };
        }

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

        private User GetUserById(int userId)
        {
            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.GetUserById", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", userId);
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
                        Role = Convert.ToString(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    };
                }
            }
        }

        private List<UserSummary> GetUserSummaries()
        {
            var users = new List<UserSummary>();

            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.GetUsers", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserSummary
                        {
                            UserId = Convert.ToInt32(reader["UserId"]),
                            Name = Convert.ToString(reader["Name"]),
                            Email = Convert.ToString(reader["Email"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            TotalTasks = Convert.ToInt32(reader["TotalTasks"]),
                            CompletedTasks = Convert.ToInt32(reader["CompletedTasks"])
                        });
                    }
                }
            }

            return users;
        }

        private List<TaskItem> GetTasksForUser(int userId)
        {
            var tasks = new List<TaskItem>();

            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.GetTasks", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", userId);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new TaskItem
                        {
                            TaskId = Convert.ToInt32(reader["TaskId"]),
                            UserId = Convert.ToInt32(reader["UserId"]),
                            Title = Convert.ToString(reader["Title"]),
                            Description = reader["Description"] == DBNull.Value ? null : Convert.ToString(reader["Description"]),
                            IsCompleted = Convert.ToBoolean(reader["IsCompleted"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["UpdatedAt"])
                        });
                    }
                }
            }

            return tasks;
        }

        private int ToggleUserStatus(int userId, bool isActive)
        {
            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.ToggleUserStatus", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@IsActive", isActive);
                connection.Open();

                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private class UserSummary
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
        }
    }
}
