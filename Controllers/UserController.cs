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
    [PanelAuthorize("User")]
    public class UserController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            User user = CurrentUserId == 0 ? null : GetUserById(CurrentUserId);

            // Sign out if the account was deleted, deactivated, or is no longer a user.
            if (user == null || user.Role != "User" || !user.IsActive)
            {
                SignOutUser();

                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.SuppressFormsAuthenticationRedirect = true;
                    filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
                }
                else
                {
                    filterContext.Result = RedirectToAction("Login", "Account");
                }

                return;
            }

            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            User user = GetUserById(CurrentUserId);

            if (user == null)
            {
                SignOutUser();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.DisplayName = user.Name;
            ViewBag.Initials = GetInitials(user.Name);
            ViewBag.FirstName = GetFirstName(user.Name);
            ViewBag.Greeting = GetGreeting();
            ViewBag.DateLine = DateTime.Now.ToString("dddd, MMMM d");

            return View();
        }

        [HttpGet]
        public JsonResult GetTasks()
        {
            List<TaskItem> tasks = GetTasksForUser(CurrentUserId);
            var payload = tasks.Select(ToTaskJson).ToList();

            return Json(new { success = true, tasks = payload }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveTask(TaskFormModel model)
        {
            if (!ModelState.IsValid)
            {
                string message = ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault();

                return Json(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(message) ? "Check the task details." : message
                });
            }

            string title = (model.Title ?? string.Empty).Trim();
            string description = (model.Description ?? string.Empty).Trim();

            // Store an empty description as NULL.
            if (description.Length == 0)
            {
                description = null;
            }

            bool isUpdate = model.TaskId > 0;
            int affected = ManageTask(
                isUpdate ? "UPDATE" : "INSERT",
                CurrentUserId,
                isUpdate ? model.TaskId : (int?)null,
                title,
                description,
                null);

            if (affected == 0)
            {
                return Json(new
                {
                    success = false,
                    message = isUpdate ? "That task no longer exists." : "The task could not be saved."
                });
            }

            return Json(new
            {
                success = true,
                message = isUpdate ? "Task updated." : "Task added."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteTask(int taskId)
        {
            if (taskId <= 0)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            int affected = ManageTask("DELETE", CurrentUserId, taskId, null, null, null);

            return affected == 0
                ? Json(new { success = false, message = "That task no longer exists." })
                : Json(new { success = true, message = "Task deleted." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ToggleTask(int taskId, bool isCompleted)
        {
            if (taskId <= 0)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            int affected = ManageTask("COMPLETE", CurrentUserId, taskId, null, null, isCompleted);

            return affected == 0
                ? Json(new { success = false, message = "That task no longer exists." })
                : Json(new
                {
                    success = true,
                    message = isCompleted ? "Task completed." : "Task reopened."
                });
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

        // Reads one user row with the GetUserById stored procedure.
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

        // Reads the signed-in user's tasks with the GetTasks stored procedure.
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

        // Calls the ManageTask stored procedure for insert, update, delete, or complete.
        private int ManageTask(string action, int userId, int? taskId, string title, string description, bool? isCompleted)
        {
            using (SqlConnection connection = DbHelper.CreateConnection())
            using (var command = new SqlCommand("dbo.ManageTask", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@TaskId", (object)taskId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Title", (object)title ?? DBNull.Value);
                command.Parameters.AddWithValue("@Description", (object)description ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsCompleted", isCompleted.HasValue ? (object)isCompleted.Value : DBNull.Value);
                connection.Open();

                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
    }
}
