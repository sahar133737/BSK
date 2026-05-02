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

        /// <summary>На минимальном или критически низком остатке (остаток ≤ минимально допустимого).</summary>
        public static int GetLowStockCount()
        {
            var value = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.SpareParts WHERE QuantityInStock <= MinQuantity;");
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        /// <summary>
        /// Ещё выше минимума, но близко к нему («скоро закончатся»): остаток в пределах зоны опережения.
        /// Не включает позиции из <see cref="GetLowStockCount"/>.
        /// </summary>
        public static int GetLowStockSoonCount()
        {
            const string sql = @"
SELECT COUNT(*)
FROM dbo.SpareParts
WHERE QuantityInStock > MinQuantity
  AND QuantityInStock <= MinQuantity +
        CASE
            WHEN MinQuantity <= 0 THEN 2
            WHEN MinQuantity <= 5 THEN MinQuantity + 2
            ELSE (MinQuantity / 6) + 3
        END;";
            var value = Db.ExecuteScalar(sql);
            return value == null ? 0 : System.Convert.ToInt32(value);
        }

        public static DataTable GetRecentRequests()
        {
            const string sql = @"
SELECT TOP 12
    Id,
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
