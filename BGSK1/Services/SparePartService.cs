using System;
using System.Data;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class SparePartService
    {
        public static DataTable GetParts()
        {
            const string sql = @"
SELECT Id, PartName, PartNumber, QuantityInStock, MinQuantity, LastUpdated
FROM dbo.SpareParts
ORDER BY PartName;";
            return Db.ExecuteDataTable(sql);
        }

        public static void AddPart(string partName, string partNumber, int quantity, int minQuantity, string unitName)
        {
            const string sql = @"
INSERT INTO dbo.SpareParts (PartName, PartNumber, QuantityInStock, MinQuantity, UnitName)
VALUES (@PartName, @PartNumber, @Quantity, @MinQuantity, @UnitName);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@PartName", partName),
                new SqlParameter("@PartNumber", partNumber),
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@MinQuantity", minQuantity),
                new SqlParameter("@UnitName", unitName));

            AuditService.LogChange("SpareParts", "INSERT", null, null, $"{{\"PartName\":\"{partName}\",\"PartNumber\":\"{partNumber}\"}}");
        }

        public static void WriteOffPart(int partId, int quantity)
        {
            const string sql = @"
UPDATE dbo.SpareParts
SET QuantityInStock = CASE WHEN QuantityInStock >= @Quantity THEN QuantityInStock - @Quantity ELSE 0 END,
    LastUpdated = SYSUTCDATETIME()
WHERE Id = @PartId;";
            Db.ExecuteNonQuery(sql, new SqlParameter("@PartId", partId), new SqlParameter("@Quantity", quantity));
            AuditService.LogChange("SpareParts", "UPDATE", partId.ToString(), null, $"{{\"WriteOffQuantity\":{quantity}}}");
        }

        public static void ReturnPartToStock(int partId, int quantity)
        {
            const string sql = @"
UPDATE dbo.SpareParts
SET QuantityInStock = QuantityInStock + @Quantity,
    LastUpdated = SYSUTCDATETIME()
WHERE Id = @PartId;";
            Db.ExecuteNonQuery(sql, new SqlParameter("@PartId", partId), new SqlParameter("@Quantity", quantity));
            AuditService.LogChange("SpareParts", "UPDATE", partId.ToString(), null, $"{{\"ReturnQuantity\":{quantity}}}");
        }

        public static DataTable GetLowStock()
        {
            const string sql = "SELECT PartName, PartNumber, QuantityInStock, MinQuantity FROM dbo.SpareParts WHERE QuantityInStock <= MinQuantity ORDER BY PartName;";
            return Db.ExecuteDataTable(sql);
        }

        /// <summary>Позиции с низким запасом: ещё не на минимуме, но близко (для фильтра и отчётов).</summary>
        public static DataTable GetLowStockSoon()
        {
            const string sql = @"
SELECT PartName, PartNumber, QuantityInStock, MinQuantity
FROM dbo.SpareParts
WHERE QuantityInStock > MinQuantity
  AND QuantityInStock <= MinQuantity +
        CASE
            WHEN MinQuantity <= 0 THEN 2
            WHEN MinQuantity <= 5 THEN MinQuantity + 2
            ELSE (MinQuantity / 6) + 3
        END
ORDER BY QuantityInStock - MinQuantity;";
            return Db.ExecuteDataTable(sql);
        }

        public static void UpdatePart(int id, string partName, string partNumber, int quantityInStock, int minQuantity, string unitName)
        {
            const string sql = @"
UPDATE dbo.SpareParts
SET PartName = @PartName,
    PartNumber = @PartNumber,
    QuantityInStock = @QuantityInStock,
    MinQuantity = @MinQuantity,
    UnitName = @UnitName,
    LastUpdated = SYSUTCDATETIME()
WHERE Id = @Id;";
            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@PartName", partName),
                new SqlParameter("@PartNumber", partNumber),
                new SqlParameter("@QuantityInStock", quantityInStock),
                new SqlParameter("@MinQuantity", minQuantity),
                new SqlParameter("@UnitName", unitName),
                new SqlParameter("@Id", id));
            AuditService.LogChange("SpareParts", "UPDATE", id.ToString(), null, $"{{\"PartName\":\"{partName}\",\"QuantityInStock\":{quantityInStock}}}");
        }

        public static void DeletePart(int id)
        {
            var deps = Db.ExecuteScalar("SELECT COUNT(*) FROM dbo.RepairRequestParts WHERE SparePartId=@Id;", new SqlParameter("@Id", id));
            if (deps != null && Convert.ToInt32(deps) > 0)
            {
                throw new InvalidOperationException("Нельзя удалить запчасть: она используется в заявках.");
            }

            Db.ExecuteNonQuery("DELETE FROM dbo.SpareParts WHERE Id=@Id;", new SqlParameter("@Id", id));
            AuditService.LogChange("SpareParts", "DELETE", id.ToString(), null, "{\"Deleted\":\"permanent\"}");
        }
    }
}
