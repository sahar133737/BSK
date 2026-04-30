using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class UserService
    {
        public static DataTable GetUsers()
        {
            const string sql = @"
SELECT u.Id, u.Email, u.FullName, r.Name AS RoleName, u.IsActive, u.IsDeleted, u.RegistrationDate
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Id = u.RoleId
ORDER BY u.Id DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetRoles()
        {
            const string sql = @"SELECT Id, Name FROM dbo.Roles WHERE IsDeleted = 0 ORDER BY AccessLevel DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void CreateUser(string email, string fullName, int roleId, string password)
        {
            var hash = PasswordHasher.HashPassword(password);
            const string sql = @"
INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, IsDeleted, MustChangePassword)
VALUES (@Email, @PasswordHash, @FullName, @RoleId, 1, 0, 1);
SELECT SCOPE_IDENTITY();";

            var newId = Convert.ToInt32(Db.ExecuteScalar(
                sql,
                new SqlParameter("@Email", email),
                new SqlParameter("@PasswordHash", hash),
                new SqlParameter("@FullName", fullName),
                new SqlParameter("@RoleId", roleId)));

            AuditService.LogChange("Users", "INSERT", newId.ToString(), null, $"{{\"Email\":\"{email}\",\"FullName\":\"{fullName}\"}}");
        }

        public static void UpdateUser(int id, string email, string fullName, int roleId, bool isActive)
        {
            var previous = Db.ExecuteDataTable("SELECT TOP 1 Email, FullName, RoleId, IsActive FROM dbo.Users WHERE Id=@Id;",
                new SqlParameter("@Id", id));
            const string sql = @"
UPDATE dbo.Users
SET Email = @Email,
    FullName = @FullName,
    RoleId = @RoleId,
    IsActive = @IsActive
WHERE Id = @Id;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@Email", email),
                new SqlParameter("@FullName", fullName),
                new SqlParameter("@RoleId", roleId),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@Id", id));

            var oldJson = previous.Rows.Count == 0 ? null :
                $"{{\"Email\":\"{previous.Rows[0]["Email"]}\",\"FullName\":\"{previous.Rows[0]["FullName"]}\",\"IsActive\":{previous.Rows[0]["IsActive"].ToString().ToLowerInvariant()}}}";
            var newJson = $"{{\"Email\":\"{email}\",\"FullName\":\"{fullName}\",\"IsActive\":{isActive.ToString().ToLowerInvariant()}}}";
            AuditService.LogChange("Users", "UPDATE", id.ToString(), oldJson, newJson);
        }

        public static void SoftDeleteUser(int id)
        {
            Db.ExecuteNonQuery("UPDATE dbo.Users SET IsDeleted = 1, IsActive = 0 WHERE Id = @Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("Users", "DELETE", id.ToString(), "{\"IsDeleted\":false}", "{\"IsDeleted\":true}");
        }

        public static void ForceResetPassword(int id, string newPassword)
        {
            AuthService.ChangePassword(id, newPassword, true);
            AuditService.LogChange("Users", "UPDATE", id.ToString(), null, "{\"Password\":\"RESET_BY_ADMIN\"}");
        }
    }
}
