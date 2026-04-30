using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class MetricsService
    {
        public static DataTable BuildUserActivityMetrics(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT
    u.Email,
    SUM(CASE WHEN a.OperationType = 'INSERT' THEN 1 ELSE 0 END) AS InsertsCount,
    SUM(CASE WHEN a.OperationType = 'UPDATE' THEN 1 ELSE 0 END) AS UpdatesCount,
    SUM(CASE WHEN a.OperationType = 'DELETE' THEN 1 ELSE 0 END) AS DeletesCount,
    COUNT(*) AS TotalOperations
FROM dbo.AuditLog a
LEFT JOIN dbo.Users u ON u.Id = a.UserId
WHERE a.[Timestamp] BETWEEN @From AND @To
GROUP BY u.Email
ORDER BY TotalOperations DESC;";

            return Db.ExecuteDataTable(sql, new SqlParameter("@From", from), new SqlParameter("@To", to));
        }

        public static DataTable GetLoginFrequencyByDay(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT CAST(AttemptTime AS DATE) AS [Day], COUNT(*) AS Attempts
FROM dbo.LoginAttempts
WHERE AttemptTime BETWEEN @From AND @To
GROUP BY CAST(AttemptTime AS DATE)
ORDER BY [Day] ASC;";
            return Db.ExecuteDataTable(sql, new SqlParameter("@From", from), new SqlParameter("@To", to));
        }
    }
}
