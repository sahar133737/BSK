using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class UserService
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return false;
            }

            var hasUpper = false;
            var hasLower = false;
            var hasDigit = false;
            foreach (var c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                if (char.IsLower(c)) hasLower = true;
                if (char.IsDigit(c)) hasDigit = true;
            }

            return hasUpper && hasLower && hasDigit;
        }

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
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Некорректный email.");
            }
            if (!IsStrongPassword(password))
            {
                throw new ArgumentException("Пароль должен содержать минимум 8 символов, включая цифру, строчную и заглавную букву.");
            }

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
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Некорректный email.");
            }
            if (id == CurrentUserContext.UserId && !isActive)
            {
                throw new InvalidOperationException("Нельзя отключить текущую учетную запись.");
            }
            if (id == CurrentUserContext.UserId && roleId != CurrentUserContext.RoleId)
            {
                throw new InvalidOperationException("Нельзя изменить роль текущего пользователя.");
            }
            EnsureAdminSafety(id, roleId, isActive, isDeleteOperation: false);

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
            if (id == CurrentUserContext.UserId)
            {
                throw new InvalidOperationException("Нельзя удалить текущую учетную запись.");
            }
            EnsureAdminSafety(id, null, false, isDeleteOperation: true);
            Db.ExecuteNonQuery("UPDATE dbo.Users SET IsDeleted = 1, IsActive = 0 WHERE Id = @Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("Users", "DELETE", id.ToString(), "{\"IsDeleted\":false}", "{\"IsDeleted\":true}");
        }

        public static void ForceResetPassword(int id, string newPassword)
        {
            if (!RolePermissionService.HasPermission("module.admin"))
            {
                throw new InvalidOperationException("Сброс пароля доступен только администратору прав.");
            }
            if (id == CurrentUserContext.UserId)
            {
                throw new InvalidOperationException("Сброс собственного пароля выполните через смену пароля пользователя.");
            }
            AuthService.ChangePassword(id, newPassword, true);
            AuditService.LogChange("Users", "UPDATE", id.ToString(), null, "{\"Password\":\"RESET_BY_ADMIN\"}");
        }

        public static DataTable GetActiveUsersLookup()
        {
            return Db.ExecuteDataTable("SELECT Id, FullName FROM dbo.Users WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY FullName;");
        }

        private static void EnsureAdminSafety(int userId, int? newRoleId, bool newIsActive, bool isDeleteOperation)
        {
            const string adminIdSql = "SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'Администратор' AND IsDeleted = 0;";
            var adminRoleObj = Db.ExecuteScalar(adminIdSql);
            if (adminRoleObj == null)
            {
                return;
            }
            var adminRoleId = Convert.ToInt32(adminRoleObj);
            var targetRoleObj = Db.ExecuteScalar("SELECT RoleId FROM dbo.Users WHERE Id = @Id;", new SqlParameter("@Id", userId));
            if (targetRoleObj == null)
            {
                return;
            }
            var currentRoleId = Convert.ToInt32(targetRoleObj);
            var willBeAdmin = (newRoleId ?? currentRoleId) == adminRoleId;

            if (currentRoleId == adminRoleId && (isDeleteOperation || !newIsActive || !willBeAdmin))
            {
                var activeAdminsObj = Db.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.Users WHERE IsDeleted = 0 AND IsActive = 1 AND RoleId = @AdminRoleId;",
                    new SqlParameter("@AdminRoleId", adminRoleId));
                var activeAdmins = activeAdminsObj == null ? 0 : Convert.ToInt32(activeAdminsObj);
                if (activeAdmins <= 1)
                {
                    throw new InvalidOperationException("В системе должен оставаться хотя бы один активный администратор.");
                }
            }
        }
    }
}
