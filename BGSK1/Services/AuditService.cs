using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class AuditService
    {
        public static DataTable GetAuditLog(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT TOP 1000 a.Id, a.[Timestamp], u.Email, a.TableName, a.OperationType, a.RecordId, a.IPAddress
FROM dbo.AuditLog a
LEFT JOIN dbo.Users u ON u.Id = a.UserId
WHERE a.[Timestamp] BETWEEN @From AND @To
ORDER BY a.[Timestamp] DESC;";

            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }

        public static void LogChange(string tableName, string operationType, string recordId, string oldValueJson, string newValueJson)
        {
            const string sql = @"
INSERT INTO dbo.AuditLog (UserId, TableName, OperationType, RecordId, OldValue, NewValue, [Timestamp], IPAddress)
VALUES (@UserId, @TableName, @OperationType, @RecordId, @OldValue, @NewValue, SYSUTCDATETIME(), @IPAddress);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@UserId", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                new SqlParameter("@TableName", tableName),
                new SqlParameter("@OperationType", operationType),
                new SqlParameter("@RecordId", (object)recordId ?? DBNull.Value),
                new SqlParameter("@OldValue", (object)oldValueJson ?? DBNull.Value),
                new SqlParameter("@NewValue", (object)newValueJson ?? DBNull.Value),
                new SqlParameter("@IPAddress", (object)CurrentUserContext.IpAddress ?? DBNull.Value));
        }
    }
}
