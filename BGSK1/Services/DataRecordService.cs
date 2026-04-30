using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class DataRecordService
    {
        public static DataTable GetRecords()
        {
            const string sql = @"
SELECT d.Id, d.Title, d.Payload, d.CreatedAt, d.UpdatedAt, d.IsDeleted, u.Email AS UpdatedBy
FROM dbo.DataRecords d
LEFT JOIN dbo.Users u ON u.Id = d.UpdatedByUserId
ORDER BY d.Id DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void AddRecord(string title, string payload)
        {
            const string sql = @"
INSERT INTO dbo.DataRecords (Title, Payload, CreatedByUserId, UpdatedByUserId, UpdatedAt, IsDeleted)
VALUES (@Title, @Payload, @UserId, @UserId, SYSUTCDATETIME(), 0);
SELECT SCOPE_IDENTITY();";

            var id = Convert.ToInt32(Db.ExecuteScalar(
                sql,
                new SqlParameter("@Title", title),
                new SqlParameter("@Payload", (object)payload ?? DBNull.Value),
                new SqlParameter("@UserId", CurrentUserContext.UserId)));

            AuditService.LogChange("DataRecords", "INSERT", id.ToString(), null, $"{{\"Title\":\"{title}\"}}");
        }

        public static void UpdateRecord(int id, string title, string payload)
        {
            var old = Db.ExecuteDataTable("SELECT TOP 1 Title, Payload FROM dbo.DataRecords WHERE Id = @Id;", new SqlParameter("@Id", id));
            Db.ExecuteNonQuery(
                @"UPDATE dbo.DataRecords SET Title=@Title, Payload=@Payload, UpdatedByUserId=@UserId, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id;",
                new SqlParameter("@Title", title),
                new SqlParameter("@Payload", (object)payload ?? DBNull.Value),
                new SqlParameter("@UserId", CurrentUserContext.UserId),
                new SqlParameter("@Id", id));

            var oldJson = old.Rows.Count == 0 ? null : $"{{\"Title\":\"{old.Rows[0]["Title"]}\"}}";
            var newJson = $"{{\"Title\":\"{title}\"}}";
            AuditService.LogChange("DataRecords", "UPDATE", id.ToString(), oldJson, newJson);
        }

        public static void SoftDeleteRecord(int id)
        {
            Db.ExecuteNonQuery(
                @"UPDATE dbo.DataRecords SET IsDeleted = 1, UpdatedByUserId=@UserId, UpdatedAt=SYSUTCDATETIME() WHERE Id = @Id;",
                new SqlParameter("@UserId", CurrentUserContext.UserId),
                new SqlParameter("@Id", id));
            AuditService.LogChange("DataRecords", "DELETE", id.ToString(), "{\"IsDeleted\":false}", "{\"IsDeleted\":true}");
        }
    }
}
