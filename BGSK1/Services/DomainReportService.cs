using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class DomainReportService
    {
        // Legacy wrappers for deprecated MainForm.
        public static DataTable GetOpenRepairRequests()
        {
            return GetRepairSlaAnalytics(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        }

        public static DataTable GetOpenRepairRequests(DateTime from, DateTime to, string statusName, string assignedTo)
        {
            return GetRepairSlaAnalytics(from, to);
        }

        public static DataTable GetOverdueMaintenance()
        {
            return GetMaintenanceComplianceReport(DateTime.UtcNow.AddDays(-365), DateTime.UtcNow);
        }

        public static DataTable GetLowStockReport()
        {
            return GetPartsProcurementForecast(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        }

        public static DataTable GetEngineerWorkload(DateTime from, DateTime to)
        {
            return GetRepairSlaAnalytics(from, to);
        }

        public static DataTable GetEquipmentTechnicalPassport(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT
    e.InventoryNumber,
    e.Name AS EquipmentName,
    e.TypeName,
    e.LocationName,
    e.ResponsiblePerson,
    e.StatusName,
    COUNT(DISTINCT rr.Id) AS RequestsTotal,
    SUM(CASE WHEN rr.StatusName <> N'Завершена' THEN 1 ELSE 0 END) AS RequestsOpen,
    MAX(rr.CreatedAt) AS LastRequestDate,
    MAX(mh.PerformedAt) AS LastMaintenanceDate,
    MIN(CASE WHEN mp.IsActive = 1 THEN mp.NextDate END) AS NextMaintenanceDate,
    SUM(CASE WHEN mp.IsActive = 1 AND mp.NextDate < CAST(SYSUTCDATETIME() AS DATE) THEN 1 ELSE 0 END) AS OverduePlansCount
FROM dbo.Equipment e
LEFT JOIN dbo.RepairRequests rr ON rr.EquipmentId = e.Id AND rr.CreatedAt BETWEEN @From AND @To
LEFT JOIN dbo.MaintenancePlans mp ON mp.EquipmentId = e.Id
LEFT JOIN dbo.MaintenanceHistory mh ON mh.PlanId = mp.Id
WHERE e.IsDeleted = 0
GROUP BY
    e.InventoryNumber, e.Name, e.TypeName, e.LocationName, e.ResponsiblePerson, e.StatusName
ORDER BY e.InventoryNumber;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }

        public static DataTable GetRepairSlaAnalytics(DateTime from, DateTime to)
        {
            const string sql = @"
WITH request_base AS (
    SELECT
        rr.Id,
        e.TypeName,
        rr.PriorityName,
        ISNULL(NULLIF(rr.AssignedTo, N''), N'Не назначен') AS AssignedTo,
        rr.StatusName,
        rr.CreatedAt,
        rr.CompletedAt,
        CASE WHEN rr.CompletedAt IS NOT NULL THEN DATEDIFF(MINUTE, rr.CreatedAt, rr.CompletedAt) END AS ResolveMinutes
    FROM dbo.RepairRequests rr
    INNER JOIN dbo.Equipment e ON e.Id = rr.EquipmentId
    WHERE rr.CreatedAt BETWEEN @From AND @To
)
SELECT
    TypeName,
    PriorityName,
    AssignedTo,
    COUNT(*) AS RequestsTotal,
    SUM(CASE WHEN StatusName = N'Завершена' THEN 1 ELSE 0 END) AS RequestsClosed,
    SUM(CASE WHEN StatusName <> N'Завершена' THEN 1 ELSE 0 END) AS RequestsOpen,
    CAST(AVG(CASE WHEN ResolveMinutes IS NOT NULL THEN ResolveMinutes / 60.0 END) AS DECIMAL(10,2)) AS AvgResolveHours,
    CAST(MAX(CASE WHEN ResolveMinutes IS NOT NULL THEN ResolveMinutes / 60.0 END) AS DECIMAL(10,2)) AS MaxResolveHours
FROM request_base
GROUP BY TypeName, PriorityName, AssignedTo
ORDER BY RequestsTotal DESC, TypeName, PriorityName;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }

        public static DataTable GetMaintenanceComplianceReport(DateTime from, DateTime to)
        {
            const string sql = @"
SELECT
    e.InventoryNumber,
    e.Name AS EquipmentName,
    mp.MaintenanceType,
    mp.PeriodDays,
    mp.NextDate,
    mp.ResponsiblePerson,
    MAX(mh.PerformedAt) AS LastPerformedAt,
    CASE
        WHEN mp.NextDate < CAST(SYSUTCDATETIME() AS DATE) THEN DATEDIFF(DAY, mp.NextDate, CAST(SYSUTCDATETIME() AS DATE))
        ELSE 0
    END AS OverdueDays,
    CASE
        WHEN mp.NextDate < CAST(SYSUTCDATETIME() AS DATE) THEN N'Просрочено'
        WHEN mp.NextDate <= DATEADD(DAY, 7, CAST(SYSUTCDATETIME() AS DATE)) THEN N'Скоро срок'
        ELSE N'В норме'
    END AS ComplianceStatus
FROM dbo.MaintenancePlans mp
INNER JOIN dbo.Equipment e ON e.Id = mp.EquipmentId
LEFT JOIN dbo.MaintenanceHistory mh ON mh.PlanId = mp.Id AND mh.PerformedAt BETWEEN @From AND @To
WHERE mp.IsActive = 1 AND e.IsDeleted = 0
GROUP BY
    e.InventoryNumber, e.Name, mp.MaintenanceType, mp.PeriodDays, mp.NextDate, mp.ResponsiblePerson
ORDER BY OverdueDays DESC, mp.NextDate;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }

        public static DataTable GetPartsProcurementForecast(DateTime from, DateTime to)
        {
            const string sql = @"
WITH usage_data AS (
    SELECT
        rrp.SparePartId,
        SUM(rrp.QuantityUsed) AS UsedQty
    FROM dbo.RepairRequestParts rrp
    INNER JOIN dbo.RepairRequests rr ON rr.Id = rrp.RequestId
    WHERE rr.CreatedAt BETWEEN @From AND @To
    GROUP BY rrp.SparePartId
)
SELECT
    sp.PartName,
    sp.PartNumber,
    sp.UnitName,
    sp.QuantityInStock,
    sp.MinQuantity,
    ISNULL(u.UsedQty, 0) AS UsedInPeriod,
    CASE
        WHEN ISNULL(u.UsedQty, 0) = 0 THEN 0
        ELSE CAST(CEILING(ISNULL(u.UsedQty, 0) * 1.3) AS INT)
    END AS ForecastNeed,
    CASE
        WHEN (sp.QuantityInStock - sp.MinQuantity) >= ISNULL(u.UsedQty, 0) THEN 0
        ELSE CAST(CEILING((ISNULL(u.UsedQty, 0) * 1.3) - (sp.QuantityInStock - sp.MinQuantity)) AS INT)
    END AS RecommendedOrderQty,
    CASE
        WHEN sp.QuantityInStock <= sp.MinQuantity THEN N'Критично'
        WHEN sp.QuantityInStock <= sp.MinQuantity + 2 THEN N'Низкий запас'
        ELSE N'Достаточно'
    END AS StockRisk
FROM dbo.SpareParts sp
LEFT JOIN usage_data u ON u.SparePartId = sp.Id
ORDER BY RecommendedOrderQty DESC, UsedInPeriod DESC, sp.PartName;";
            return Db.ExecuteDataTable(
                sql,
                new SqlParameter("@From", from),
                new SqlParameter("@To", to));
        }
    }
}
