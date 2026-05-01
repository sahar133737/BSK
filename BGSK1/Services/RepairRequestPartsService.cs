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
ORDER BY rp.Id DESC;";
            return Db.ExecuteDataTable(sql, new SqlParameter("@RequestId", requestId));
        }

        public static void AddPartToRequest(int requestId, int sparePartId, int quantity)
        {
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
