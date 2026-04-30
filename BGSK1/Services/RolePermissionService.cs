using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class RolePermissionService
    {
        private static readonly HashSet<string> Permissions = new HashSet<string>();

        public static void LoadCurrentRolePermissions()
        {
            Permissions.Clear();
            const string sql = @"
SELECT PermissionKey
FROM dbo.RolePermissions
WHERE RoleId = @RoleId AND IsAllowed = 1;";
            var table = Db.ExecuteDataTable(sql, new SqlParameter("@RoleId", CurrentUserContext.RoleId));
            foreach (DataRow row in table.Rows)
            {
                Permissions.Add(row["PermissionKey"].ToString());
            }
        }

        public static bool HasPermission(string permissionKey)
        {
            return Permissions.Contains(permissionKey);
        }

        public static DataTable GetRoles()
        {
            return Db.ExecuteDataTable("SELECT Id, Name FROM dbo.Roles WHERE IsDeleted = 0 ORDER BY AccessLevel DESC;");
        }

        public static DataTable GetPermissionsByRole(int roleId)
        {
            const string sql = @"
SELECT PermissionKey, IsAllowed
FROM dbo.RolePermissions
WHERE RoleId = @RoleId
ORDER BY PermissionKey;";
            return Db.ExecuteDataTable(sql, new SqlParameter("@RoleId", roleId));
        }

        public static void SavePermission(int roleId, string permissionKey, bool isAllowed)
        {
            const string sql = @"
MERGE dbo.RolePermissions AS target
USING (SELECT @RoleId AS RoleId, @PermissionKey AS PermissionKey) AS source
ON target.RoleId = source.RoleId AND target.PermissionKey = source.PermissionKey
WHEN MATCHED THEN
    UPDATE SET IsAllowed = @IsAllowed
WHEN NOT MATCHED THEN
    INSERT (RoleId, PermissionKey, IsAllowed) VALUES (@RoleId, @PermissionKey, @IsAllowed);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@RoleId", roleId),
                new SqlParameter("@PermissionKey", permissionKey),
                new SqlParameter("@IsAllowed", isAllowed));
        }
    }
}
