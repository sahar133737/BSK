using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class RepairRequestPartsService
    {
        public static DataTable GetRequestParts(int requestId)
        {
            const string sql = @"
SELECT rp.Id, sp.PartName, sp.PartNumber, rp.QuantityUsed
     , rp.SparePartId
FROM dbo.RepairRequestParts rp
INNER JOIN dbo.SpareParts sp ON sp.Id = rp.SparePartId
WHERE rp.RequestId = @RequestId
ORDER BY sp.PartName, rp.Id;";
            return Db.ExecuteDataTable(sql, new SqlParameter("@RequestId", requestId));
        }

        /// <summary>Сливает несколько строк одной и той же запчасти (один SparePartId) в одну — на случай старых дублей в БД.</summary>
        public static void MergeDuplicateLinesForRequest(int requestId)
        {
            const string findDupes = @"
SELECT SparePartId, SUM(QuantityUsed) AS TotalQty, MIN(Id) AS KeepId
FROM dbo.RepairRequestParts
WHERE RequestId = @RequestId
GROUP BY SparePartId
HAVING COUNT(*) > 1;";
            var dupes = Db.ExecuteDataTable(findDupes, new SqlParameter("@RequestId", requestId));
            foreach (DataRow row in dupes.Rows)
            {
                var keepId = Convert.ToInt32(row["KeepId"]);
                var totalQty = Convert.ToInt32(row["TotalQty"]);
                var sparePartId = Convert.ToInt32(row["SparePartId"]);
                Db.ExecuteNonQuery(
                    "UPDATE dbo.RepairRequestParts SET QuantityUsed = @QuantityUsed WHERE Id = @Id;",
                    new SqlParameter("@QuantityUsed", totalQty),
                    new SqlParameter("@Id", keepId));
                Db.ExecuteNonQuery(
                    @"DELETE FROM dbo.RepairRequestParts
WHERE RequestId = @RequestId AND SparePartId = @SparePartId AND Id <> @KeepId;",
                    new SqlParameter("@RequestId", requestId),
                    new SqlParameter("@SparePartId", sparePartId),
                    new SqlParameter("@KeepId", keepId));
            }
        }

        public static void AddPartToRequest(int requestId, int sparePartId, int quantity)
        {
            const string findSql = @"
SELECT TOP 1 Id, QuantityUsed
FROM dbo.RepairRequestParts
WHERE RequestId = @RequestId AND SparePartId = @SparePartId;";
            var existing = Db.ExecuteDataTable(findSql,
                new SqlParameter("@RequestId", requestId),
                new SqlParameter("@SparePartId", sparePartId));

            if (existing.Rows.Count > 0)
            {
                var lineId = Convert.ToInt32(existing.Rows[0]["Id"]);
                var oldQty = Convert.ToInt32(existing.Rows[0]["QuantityUsed"]);
                var newQty = oldQty + quantity;
                Db.ExecuteNonQuery(
                    "UPDATE dbo.RepairRequestParts SET QuantityUsed = @QuantityUsed WHERE Id = @Id;",
                    new SqlParameter("@QuantityUsed", newQty),
                    new SqlParameter("@Id", lineId));
                SparePartService.WriteOffPart(sparePartId, quantity);
                AuditService.LogChange("RepairRequestParts", "UPDATE", lineId.ToString(),
                    $"{{\"RequestId\":{requestId},\"SparePartId\":{sparePartId},\"QuantityUsed\":{oldQty}}}",
                    $"{{\"RequestId\":{requestId},\"SparePartId\":{sparePartId},\"QuantityUsed\":{newQty}}}");
                return;
            }

            const string insertSql = @"
INSERT INTO dbo.RepairRequestParts (RequestId, SparePartId, QuantityUsed)
VALUES (@RequestId, @SparePartId, @QuantityUsed);";
            Db.ExecuteNonQuery(insertSql,
                new SqlParameter("@RequestId", requestId),
                new SqlParameter("@SparePartId", sparePartId),
                new SqlParameter("@QuantityUsed", quantity));

            SparePartService.WriteOffPart(sparePartId, quantity);
            AuditService.LogChange("RepairRequestParts", "INSERT", null, null, $"{{\"RequestId\":{requestId},\"SparePartId\":{sparePartId},\"QuantityUsed\":{quantity}}}");
        }

        public static void RemovePartFromRequest(int requestPartId)
        {
            const string selectSql = @"
SELECT TOP 1 RequestId, SparePartId, QuantityUsed
FROM dbo.RepairRequestParts
WHERE Id = @Id;";
            var table = Db.ExecuteDataTable(selectSql, new SqlParameter("@Id", requestPartId));
            if (table.Rows.Count == 0)
            {
                return;
            }

            var row = table.Rows[0];
            var requestId = Convert.ToInt32(row["RequestId"]);
            var sparePartId = Convert.ToInt32(row["SparePartId"]);
            var qty = Convert.ToInt32(row["QuantityUsed"]);

            Db.ExecuteNonQuery("DELETE FROM dbo.RepairRequestParts WHERE Id = @Id;", new SqlParameter("@Id", requestPartId));
            SparePartService.ReturnPartToStock(sparePartId, qty);
            AuditService.LogChange("RepairRequestParts", "DELETE", requestPartId.ToString(), $"{{\"RequestId\":{requestId},\"SparePartId\":{sparePartId},\"QuantityUsed\":{qty}}}", null);
        }

        public static void RemoveAllByRequest(int requestId)
        {
            const string sql = @"
SELECT Id
FROM dbo.RepairRequestParts
WHERE RequestId = @RequestId;";
            var table = Db.ExecuteDataTable(sql, new SqlParameter("@RequestId", requestId));
            foreach (DataRow row in table.Rows)
            {
                RemovePartFromRequest(Convert.ToInt32(row["Id"]));
            }
        }
    }
}
