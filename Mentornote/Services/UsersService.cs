using Mentornote.Models;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Data;

namespace Mentornote.Services
{
    public class UsersService
    {
        private string? connectionString;
        public UsersService()
        {
            ConfigurationBuilder DatabaseUserBuilder = new ConfigurationBuilder();
            DatabaseUserBuilder.SetBasePath(Directory.GetCurrentDirectory());
            DatabaseUserBuilder.AddJsonFile("appsettings.json");
            IConfiguration DatabaseUserConfiguration = DatabaseUserBuilder.Build();
            connectionString = DatabaseUserConfiguration.GetConnectionString("DefaultConnection");
        }
        public User GetUserByEmail(string email)
        {
            User user = new User();
            SqlConnection mentornoteCoonnection = new SqlConnection();
            mentornoteCoonnection.ConnectionString = connectionString;
            mentornoteCoonnection.Open();

            SqlCommand GetUser = new()
            {
                CommandType = CommandType.StoredProcedure,
                Connection = mentornoteCoonnection,
                CommandText = "GetUserByEmail"
            };

            SqlParameter EmailParameter = new()
            {
                ParameterName = "@Email",
                SqlDbType = SqlDbType.VarChar,
                Direction = ParameterDirection.Input,
                SqlValue = email
            };

            GetUser.Parameters.Add(EmailParameter);
            SqlDataReader UserReader = GetUser.ExecuteReader();

            if (UserReader.HasRows)
            {
                while (UserReader.Read())
                {
                    user.FirstName = (string)UserReader["FirstName"];
                    user.LastName = (string)UserReader["LastName"];
                    user.Id = (int)UserReader["Id"];
                    user.Email = (string)UserReader["Email"];
                    user.PasswordSalt = (byte[])UserReader["PasswordSalt"];
                    user.PasswordHash = (byte[])UserReader["PasswordHash"];
                }
            }
            UserReader.Close();
            mentornoteCoonnection.Close();
            return user;
        }
    }
}
