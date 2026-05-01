using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class MaintenanceService
    {
        public static DataTable GetPlans()
        {
            const string sql = @"
SELECT p.Id, e.InventoryNumber, e.Name AS EquipmentName, p.MaintenanceType, p.PeriodDays, p.NextDate, p.ResponsiblePerson, p.IsActive
FROM dbo.MaintenancePlans p
INNER JOIN dbo.Equipment e ON e.Id = p.EquipmentId
ORDER BY p.NextDate ASC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void AddPlan(int equipmentId, string maintenanceType, int periodDays, DateTime nextDate, string responsible)
        {
            const string sql = @"
INSERT INTO dbo.MaintenancePlans (EquipmentId, MaintenanceType, PeriodDays, NextDate, ResponsiblePerson, IsActive)
VALUES (@EquipmentId, @MaintenanceType, @PeriodDays, @NextDate, @ResponsiblePerson, 1);
SELECT SCOPE_IDENTITY();";
            var id = Convert.ToInt32(Db.ExecuteScalar(
                sql,
                new SqlParameter("@EquipmentId", equipmentId),
                new SqlParameter("@MaintenanceType", maintenanceType),
                new SqlParameter("@PeriodDays", periodDays),
                new SqlParameter("@NextDate", nextDate),
                new SqlParameter("@ResponsiblePerson", responsible)));

            AuditService.LogChange("MaintenancePlans", "INSERT", id.ToString(), null, $"{{\"MaintenanceType\":\"{maintenanceType}\"}}");
        }

        public static void MarkCompleted(int planId, string resultComment)
        {
            const string insertHistory = @"
INSERT INTO dbo.MaintenanceHistory (PlanId, PerformedAt, ResultComment, PerformedByUserId)
VALUES (@PlanId, SYSUTCDATETIME(), @ResultComment, @PerformedByUserId);";
            Db.ExecuteNonQuery(
                insertHistory,
                new SqlParameter("@PlanId", planId),
                new SqlParameter("@ResultComment", (object)resultComment ?? DBNull.Value),
                new SqlParameter("@PerformedByUserId", CurrentUserContext.UserId));

            const string updatePlan = @"
UPDATE dbo.MaintenancePlans
SET NextDate = DATEADD(DAY, PeriodDays, NextDate)
WHERE Id = @PlanId;";
            Db.ExecuteNonQuery(updatePlan, new SqlParameter("@PlanId", planId));

            AuditService.LogChange("MaintenancePlans", "UPDATE", planId.ToString(), null, "{\"Completed\":true}");
        }

        public static void UpdatePlan(int id, int equipmentId, string maintenanceType, int periodDays, DateTime nextDate, string responsiblePerson, bool isActive)
        {
            const string sql = @"
UPDATE dbo.MaintenancePlans
SET EquipmentId = @EquipmentId,
    MaintenanceType = @MaintenanceType,
    PeriodDays = @PeriodDays,
    NextDate = @NextDate,
    ResponsiblePerson = @ResponsiblePerson,
    IsActive = @IsActive
WHERE Id = @Id;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@EquipmentId", equipmentId),
                new SqlParameter("@MaintenanceType", maintenanceType),
                new SqlParameter("@PeriodDays", periodDays),
                new SqlParameter("@NextDate", nextDate),
                new SqlParameter("@ResponsiblePerson", (object)responsiblePerson ?? DBNull.Value),
                new SqlParameter("@IsActive", isActive),
                new SqlParameter("@Id", id));

            AuditService.LogChange("MaintenancePlans", "UPDATE", id.ToString(), null, $"{{\"MaintenanceType\":\"{maintenanceType}\",\"IsActive\":{isActive.ToString().ToLowerInvariant()}}}");
        }

        public static void DeletePlan(int id)
        {
            Db.ExecuteNonQuery("DELETE FROM dbo.MaintenanceHistory WHERE PlanId = @Id;", new SqlParameter("@Id", id));
            Db.ExecuteNonQuery("DELETE FROM dbo.MaintenancePlans WHERE Id = @Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("MaintenancePlans", "DELETE", id.ToString(), null, "{\"Deleted\":\"permanent\"}");
        }

        public static DataTable GetMaintenanceTypeLookup()
        {
            const string sql = @"
SELECT DISTINCT v AS Value
FROM (
    SELECT MaintenanceType AS v FROM dbo.MaintenancePlans WHERE ISNULL(MaintenanceType, N'') <> N''
    UNION
    SELECT Value AS v FROM dbo.LookupDictionary WHERE Category = N'MaintenanceType' AND ISNULL(Value, N'') <> N''
) x
ORDER BY Value;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetMaintenanceResponsibleLookup()
        {
            const string sql = @"
SELECT DISTINCT v AS Value
FROM (
    SELECT ResponsiblePerson AS v FROM dbo.MaintenancePlans WHERE ISNULL(ResponsiblePerson, N'') <> N''
    UNION
    SELECT Value AS v FROM dbo.LookupDictionary WHERE Category = N'MaintenanceResponsible' AND ISNULL(Value, N'') <> N''
) x
ORDER BY Value;";
            return Db.ExecuteDataTable(sql);
        }
    }
}
