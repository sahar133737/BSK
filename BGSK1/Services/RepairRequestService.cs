using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class RepairRequestService
    {
        public static DataTable GetRequestsLookup()
        {
            const string sql = "SELECT Id, RequestNumber + N' | ' + StatusName AS DisplayName FROM dbo.RepairRequests ORDER BY Id DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static DataTable GetRequests()
        {
            const string sql = @"
SELECT r.Id, r.EquipmentId, r.RequestNumber, r.CreatedAt, e.InventoryNumber, e.Name AS EquipmentName,
       r.ProblemDescription, r.PriorityName, r.StatusName, r.AssignedTo, r.CompletedAt
FROM dbo.RepairRequests r
INNER JOIN dbo.Equipment e ON e.Id = r.EquipmentId
ORDER BY r.Id DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void CreateRequest(int equipmentId, string problem, string priority, string assignedTo)
        {
            const string sql = @"
INSERT INTO dbo.RepairRequests (RequestNumber, EquipmentId, ProblemDescription, PriorityName, StatusName, CreatedByUserId, AssignedTo)
VALUES (@RequestNumber, @EquipmentId, @ProblemDescription, @PriorityName, N'Новая', @CreatedByUserId, @AssignedTo);
SELECT SCOPE_IDENTITY();";

            var requestNumber = "REQ-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var id = Convert.ToInt32(Db.ExecuteScalar(
                sql,
                new SqlParameter("@RequestNumber", requestNumber),
                new SqlParameter("@EquipmentId", equipmentId),
                new SqlParameter("@ProblemDescription", problem),
                new SqlParameter("@PriorityName", priority),
                new SqlParameter("@CreatedByUserId", CurrentUserContext.UserId),
                new SqlParameter("@AssignedTo", (object)assignedTo ?? DBNull.Value)));

            AuditService.LogChange("RepairRequests", "INSERT", id.ToString(), null, $"{{\"RequestNumber\":\"{requestNumber}\"}}");
        }

        public static void UpdateRequestStatus(int id, string statusName, string assignedTo)
        {
            const string sql = @"
UPDATE dbo.RepairRequests
SET StatusName = @StatusName,
    AssignedTo = @AssignedTo,
    CompletedAt = CASE WHEN @StatusName = N'Завершена' THEN SYSUTCDATETIME() ELSE NULL END
WHERE Id = @Id;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@StatusName", statusName),
                new SqlParameter("@AssignedTo", (object)assignedTo ?? DBNull.Value),
                new SqlParameter("@Id", id));

            AuditService.LogChange("RepairRequests", "UPDATE", id.ToString(), null, $"{{\"StatusName\":\"{statusName}\"}}");
        }

        public static void UpdateRequest(int id, int equipmentId, string problemDescription, string priorityName, string statusName, string assignedTo)
        {
            const string sql = @"
UPDATE dbo.RepairRequests
SET EquipmentId = @EquipmentId,
    ProblemDescription = @ProblemDescription,
    PriorityName = @PriorityName,
    StatusName = @StatusName,
    AssignedTo = @AssignedTo,
    CompletedAt = CASE WHEN @StatusName = N'Завершена' THEN ISNULL(CompletedAt, SYSUTCDATETIME()) ELSE NULL END
WHERE Id = @Id;";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@EquipmentId", equipmentId),
                new SqlParameter("@ProblemDescription", problemDescription),
                new SqlParameter("@PriorityName", priorityName),
                new SqlParameter("@StatusName", statusName),
                new SqlParameter("@AssignedTo", (object)assignedTo ?? DBNull.Value),
                new SqlParameter("@Id", id));

            AuditService.LogChange("RepairRequests", "UPDATE", id.ToString(), null, $"{{\"PriorityName\":\"{priorityName}\",\"StatusName\":\"{statusName}\"}}");
        }

        public static void DeleteRequest(int id)
        {
            RepairRequestPartsService.RemoveAllByRequest(id);
            Db.ExecuteNonQuery("DELETE FROM dbo.RepairRequests WHERE Id=@Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("RepairRequests", "DELETE", id.ToString(), null, "{\"Deleted\":\"permanent\"}");
        }
    }
}
