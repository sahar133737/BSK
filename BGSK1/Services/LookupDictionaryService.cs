using System;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    /// <summary>Справочные значения для комбобоксов (типы техники, кабинеты, виды ТО и т.д.).</summary>
    internal static class LookupDictionaryService
    {
        public const string EquipmentType = "EquipmentType";
        public const string Location = "Location";
        public const string EquipmentResponsible = "EquipmentResponsible";
        public const string MaintenanceType = "MaintenanceType";
        public const string MaintenanceResponsible = "MaintenanceResponsible";

        public static void AddValue(string category, string value)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Категория справочника не задана.", nameof(category));
            }

            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(v))
            {
                throw new InvalidOperationException("Значение не может быть пустым.");
            }

            if (v.Length > 200)
            {
                throw new InvalidOperationException("Значение не длиннее 200 символов.");
            }

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.LookupDictionary WHERE Category = @Category AND Value = @Value)
    INSERT INTO dbo.LookupDictionary (Category, Value) VALUES (@Category, @Value);";

            Db.ExecuteNonQuery(
                sql,
                new SqlParameter("@Category", category),
                new SqlParameter("@Value", v));
        }
    }
}
