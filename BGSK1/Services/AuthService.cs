using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class AuthService
    {
        public static bool TryLogin(string email, string password, string ipAddress, out string error)
        {
            const string sql = @"
SELECT u.Id, u.RoleId, u.PasswordHash, u.FullName, u.IsActive, r.Name AS RoleName
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.Email = @Email AND u.IsDeleted = 0;";

            var table = Db.ExecuteDataTable(sql, new SqlParameter("@Email", email));
            if (table.Rows.Count == 0)
            {
                error = "Пользователь не найден.";
                RegisterLoginAttempt(email, false, ipAddress, error);
                return false;
            }

            var row = table.Rows[0];
            if (!(bool)row["IsActive"])
            {
                error = "Учетная запись отключена.";
                RegisterLoginAttempt(email, false, ipAddress, error);
                return false;
            }

            var hash = row["PasswordHash"].ToString();
            if (!PasswordHasher.Verify(password, hash))
            {
                error = "Неверный пароль.";
                RegisterLoginAttempt(email, false, ipAddress, error);
                return false;
            }

            CurrentUserContext.UserId = Convert.ToInt32(row["Id"]);
            CurrentUserContext.RoleId = Convert.ToInt32(row["RoleId"]);
            CurrentUserContext.Email = email;
            CurrentUserContext.FullName = row["FullName"].ToString();
            CurrentUserContext.RoleName = row["RoleName"].ToString();
            CurrentUserContext.IpAddress = ipAddress;

            RegisterLoginAttempt(email, true, ipAddress, "Успешная авторизация");
            CreateSession();
            error = null;
            return true;
        }

        public static void ChangePassword(int userId, string newPassword, bool forceChangeNextLogin)
        {
            var passwordHash = PasswordHasher.HashPassword(newPassword);
            const string sql = @"
UPDATE dbo.Users
SET PasswordHash = @PasswordHash,
    LastPasswordChange = SYSUTCDATETIME(),
    MustChangePassword = @MustChangePassword
WHERE Id = @UserId;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@MustChangePassword", forceChangeNextLogin),
                new SqlParameter("@UserId", userId));
        }

        private static void RegisterLoginAttempt(string email, bool isSuccess, string ipAddress, string comment)
        {
            const string sql = @"
INSERT INTO dbo.LoginAttempts (Email, IsSuccess, IPAddress, [Comment])
VALUES (@Email, @IsSuccess, @IPAddress, @Comment);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@Email", email),
                new SqlParameter("@IsSuccess", isSuccess),
                new SqlParameter("@IPAddress", (object)ipAddress ?? DBNull.Value),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value));
        }

        private static void CreateSession()
        {
            const string sql = @"
INSERT INTO dbo.Sessions (UserID, IPAddress)
VALUES (@UserId, @IPAddress);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@UserId", CurrentUserContext.UserId),
                new SqlParameter("@IPAddress", (object)CurrentUserContext.IpAddress ?? DBNull.Value));
        }
    }
}
