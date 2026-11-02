using System.Configuration;
using System.Data.SqlClient;

namespace toDo.Models
{
    public static class DbHelper
    {
        public static SqlConnection CreateConnection()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["ToDoDB"];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException("The ToDoDB connection string is missing from Web.config.");
            }

            return new SqlConnection(settings.ConnectionString);
        }
    }
}
