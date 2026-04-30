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
    }
}
