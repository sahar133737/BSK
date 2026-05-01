using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class EquipmentService
    {
        public static DataTable GetEquipment()
        {
            const string sql = @"
SELECT e.Id, e.InventoryNumber, e.Name, e.TypeName, e.LocationName, e.ResponsiblePerson,
       e.PurchaseDate, e.WarrantyUntil, e.StatusName, e.IsDeleted
FROM dbo.Equipment e
WHERE e.IsDeleted = 0
ORDER BY e.Id DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void AddEquipment(string inventoryNumber, string name, string typeName, string locationName, string responsiblePerson)
        {
            const string sql = @"
INSERT INTO dbo.Equipment (InventoryNumber, Name, TypeName, LocationName, ResponsiblePerson, StatusName, IsDeleted)
VALUES (@InventoryNumber, @Name, @TypeName, @LocationName, @ResponsiblePerson, N'В эксплуатации', 0);
SELECT SCOPE_IDENTITY();";

            var id = Convert.ToInt32(Db.ExecuteScalar(
                sql,
                new SqlParameter("@InventoryNumber", inventoryNumber),
                new SqlParameter("@Name", name),
                new SqlParameter("@TypeName", typeName),
                new SqlParameter("@LocationName", locationName),
                new SqlParameter("@ResponsiblePerson", responsiblePerson)));

            AuditService.LogChange("Equipment", "INSERT", id.ToString(), null, $"{{\"InventoryNumber\":\"{inventoryNumber}\",\"Name\":\"{name}\"}}");
        }

        public static void UpdateEquipment(int id, string inventoryNumber, string name, string typeName, string locationName, string responsiblePerson, string statusName)
        {
            const string sql = @"
UPDATE dbo.Equipment
SET InventoryNumber = @InventoryNumber,
    Name = @Name,
    TypeName = @TypeName,
    LocationName = @LocationName,
    ResponsiblePerson = @ResponsiblePerson,
    StatusName = @StatusName
WHERE Id = @Id;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@InventoryNumber", inventoryNumber),
                new SqlParameter("@Name", name),
                new SqlParameter("@TypeName", typeName),
                new SqlParameter("@LocationName", locationName),
                new SqlParameter("@ResponsiblePerson", responsiblePerson),
                new SqlParameter("@StatusName", statusName),
                new SqlParameter("@Id", id));

            AuditService.LogChange("Equipment", "UPDATE", id.ToString(), null, $"{{\"StatusName\":\"{statusName}\"}}");
        }

        public static DataTable GetEquipmentLookup()
        {
            const string sql = "SELECT Id, InventoryNumber + N' - ' + Name AS DisplayName FROM dbo.Equipment WHERE IsDeleted = 0 ORDER BY InventoryNumber;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetTypeLookup()
        {
            const string sql = @"
SELECT DISTINCT v AS Value
FROM (
    SELECT TypeName AS v FROM dbo.Equipment WHERE IsDeleted = 0 AND ISNULL(TypeName, N'') <> N''
    UNION
    SELECT Value AS v FROM dbo.LookupDictionary WHERE Category = N'EquipmentType' AND ISNULL(Value, N'') <> N''
) x
ORDER BY Value;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetLocationLookup()
        {
            const string sql = @"
SELECT DISTINCT v AS Value
FROM (
    SELECT LocationName AS v FROM dbo.Equipment WHERE IsDeleted = 0 AND ISNULL(LocationName, N'') <> N''
    UNION
    SELECT Value AS v FROM dbo.LookupDictionary WHERE Category = N'Location' AND ISNULL(Value, N'') <> N''
) x
ORDER BY Value;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetResponsibleLookup()
        {
            const string sql = @"
SELECT DISTINCT v AS Value
FROM (
    SELECT ResponsiblePerson AS v FROM dbo.Equipment WHERE IsDeleted = 0 AND ISNULL(ResponsiblePerson, N'') <> N''
    UNION
    SELECT Value AS v FROM dbo.LookupDictionary WHERE Category = N'EquipmentResponsible' AND ISNULL(Value, N'') <> N''
) x
ORDER BY Value;";
            return Db.ExecuteDataTable(sql);
        }

        public static void DeleteEquipmentPermanently(int id)
        {
            const string hasDepsSql = @"
SELECT
    (SELECT COUNT(*) FROM dbo.RepairRequests WHERE EquipmentId = @Id) AS RequestsCount,
    (SELECT COUNT(*) FROM dbo.MaintenancePlans WHERE EquipmentId = @Id) AS PlansCount;";
            var deps = Db.ExecuteDataTable(hasDepsSql, new SqlParameter("@Id", id));
            var requestsCount = Convert.ToInt32(deps.Rows[0]["RequestsCount"]);
            var plansCount = Convert.ToInt32(deps.Rows[0]["PlansCount"]);

            if (requestsCount > 0 || plansCount > 0)
            {
                throw new InvalidOperationException("Нельзя удалить технику: есть связанные заявки или планы ТО. Сначала удалите связанные записи.");
            }

            Db.ExecuteNonQuery("DELETE FROM dbo.Equipment WHERE Id = @Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("Equipment", "DELETE", id.ToString(), null, "{\"Deleted\":\"permanent\"}");
        }
    }
}
