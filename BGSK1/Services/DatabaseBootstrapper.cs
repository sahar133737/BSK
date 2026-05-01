using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using BGSK1.Infrastructure;

namespace BGSK1.Services
{
    internal static class DatabaseBootstrapper
    {
        public static void EnsureDatabase()
        {
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sql", "01_create_schema.sql");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Не найден SQL-скрипт инициализации БД.", scriptPath);
            }

            var scriptText = File.ReadAllText(scriptPath);
            var batches = Regex.Split(scriptText, @"^\s*GO\s*$", RegexOptions.Multiline);

            using (var connection = new SqlConnection(Db.MasterConnectionString))
            {
                connection.Open();
                foreach (var rawBatch in batches)
                {
                    var batch = rawBatch.Trim();
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    using (var command = new SqlCommand(batch, connection))
                    {
                        command.CommandTimeout = 120;
                        command.ExecuteNonQuery();
                    }
                }
            }

            EnsureLookupDictionaryTable();
        }

        /// <summary>
        /// Создаёт dbo.LookupDictionary в базе приложения, если таблицы ещё нет (обновление со старых версий схемы).
        /// </summary>
        public static void EnsureLookupDictionaryTable()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.LookupDictionary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LookupDictionary
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Category NVARCHAR(50) NOT NULL,
        Value NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_LookupDictionary_Category_Value UNIQUE (Category, Value)
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LookupDictionary_Category' AND object_id = OBJECT_ID(N'dbo.LookupDictionary'))
        CREATE INDEX IX_LookupDictionary_Category ON dbo.LookupDictionary(Category);
END";

            using (var connection = new SqlConnection(Db.AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.CommandTimeout = 60;
                command.ExecuteNonQuery();
            }
        }
    }
}
