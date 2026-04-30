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
        }
    }
}
