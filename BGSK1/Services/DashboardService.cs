using System.Data;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class DashboardService
    {
        public static int GetEquipmentCount()
        {
            var value = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.Equipment WHERE IsDeleted = 0;");
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        public static int GetOpenRequestsCount()
        {
            var value = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.RepairRequests WHERE StatusName <> N'Завершена';");
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        public static int GetOverdueMaintenanceCount()
        {
            var value = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.MaintenancePlans WHERE IsActive = 1 AND NextDate < CAST(SYSUTCDATETIME() AS DATE);");
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        public static int GetLowStockCount()
        {
            var value = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.SpareParts WHERE QuantityInStock <= MinQuantity;");
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        public static DataTable GetRecentRequests()
        {
            const string sql = @"
SELECT TOP 12
    RequestNumber,
    CreatedAt,
    StatusName,
    PriorityName,
    AssignedTo
FROM dbo.RepairRequests
ORDER BY CreatedAt DESC;";
            return Db.ExecuteDataTable(sql);
        }
    }
}
