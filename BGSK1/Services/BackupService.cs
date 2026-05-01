using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using BGSK1.Infrastructure;
using BGSK1.Security;

namespace BGSK1.Services
{
    internal static class BackupService
    {
        /// <summary>
        /// Каталог, куда SQL Server обычно может писать (не папка пользователя в bin\Debug).
        /// </summary>
        public static string GetRecommendedBackupDirectory()
        {
            try
            {
                var t = Db.ExecuteDataTable(
                    "SELECT CONVERT(NVARCHAR(500), SERVERPROPERTY('InstanceDefaultBackupPath')) AS p;");
                if (t.Rows.Count > 0)
                {
                    var p = t.Rows[0]["p"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        return p.TrimEnd('\\');
                    }
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                using (var conn = new SqlConnection(Db.MasterConnectionString))
                using (var cmd = new SqlCommand(
                    "EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'BackupDirectory';",
                    conn))
                {
                    conn.Open();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var table = new DataTable();
                        adapter.Fill(table);
                        if (table.Rows.Count > 0)
                        {
                            var row = table.Rows[0];
                            if (table.Columns.Contains("Data") && row["Data"] != DBNull.Value)
                            {
                                var data = row["Data"].ToString()?.Trim();
                                if (!string.IsNullOrWhiteSpace(data))
                                {
                                    return data.TrimEnd('\\');
                                }
                            }

                            var last = row[table.Columns.Count - 1]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(last))
                            {
                                return last.TrimEnd('\\');
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "BGSK1",
                "Backups");
        }

        public static string GetDefaultBackupFilePath(string fileName = "BGSK1_manual.bak")
        {
            return Path.Combine(GetRecommendedBackupDirectory(), fileName);
        }

        public static void CreateBackup(string backupDirectory, string comment, bool isAuto)
        {
            Directory.CreateDirectory(backupDirectory);
            var fileName = $"BGSK1DiplomaDB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var fullPath = Path.Combine(backupDirectory, fileName);

            const string backupSql = "BACKUP DATABASE [BGSK1DiplomaDB] TO DISK = @Path WITH INIT, COMPRESSION;";
            try
            {
                Db.ExecuteNonQuery(backupSql, new SqlParameter("@Path", fullPath));
            }
            catch (SqlException ex)
            {
                if (ex.Message.IndexOf("Operating system error 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Message.IndexOf("Ошибка операционной системы 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Number == 3201)
                {
                    throw new InvalidOperationException(
                        "Служба SQL Server не имеет прав на запись в выбранную папку (ошибка доступа ОС 5). "
                        + "Укажите каталог, доступный службе SQL Server, например: "
                        + GetRecommendedBackupDirectory(),
                        ex);
                }

                throw;
            }

            var fileInfo = new FileInfo(fullPath);
            const string insertBackup = @"
INSERT INTO dbo.Backups (FileName, FilePath, SizeBytes, CreatedByUserID, Comment, IsAuto)
VALUES (@FileName, @FilePath, @SizeBytes, @CreatedByUserID, @Comment, @IsAuto);";

            Db.ExecuteNonQuery(
                insertBackup,
                new SqlParameter("@FileName", fileName),
                new SqlParameter("@FilePath", fullPath),
                new SqlParameter("@SizeBytes", fileInfo.Exists ? fileInfo.Length : 0),
                new SqlParameter("@CreatedByUserID", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value),
                new SqlParameter("@IsAuto", isAuto));
        }

        public static void CreateBackupToFile(string backupFilePath, string comment, bool isAuto)
        {
            var directory = Path.GetDirectoryName(backupFilePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Не удалось определить каталог для файла резервной копии.");
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Не удалось создать каталог для резервной копии. Укажите путь, доступный для записи службе SQL Server (например стандартную папку резервного копирования экземпляра или C:\\ProgramData\\BGSK1\\Backups).",
                    ex);
            }

            const string backupSql = "BACKUP DATABASE [BGSK1DiplomaDB] TO DISK = @Path WITH INIT, COMPRESSION;";
            try
            {
                Db.ExecuteNonQuery(backupSql, new SqlParameter("@Path", backupFilePath));
            }
            catch (SqlException ex)
            {
                if (ex.Message.IndexOf("Operating system error 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Message.IndexOf("Ошибка операционной системы 5", StringComparison.OrdinalIgnoreCase) >= 0
                    || ex.Number == 3201)
                {
                    throw new InvalidOperationException(
                        "Служба SQL Server не имеет прав на запись в выбранную папку (ошибка доступа ОС 5). "
                        + "Не используйте каталог внутри профиля пользователя и bin\\Debug. "
                        + "Нажмите «Выбрать файл» и сохраните .bak в рекомендуемый каталог: "
                        + GetRecommendedBackupDirectory(),
                        ex);
                }

                throw;
            }

            var fileInfo = new FileInfo(backupFilePath);
            Db.ExecuteNonQuery(
                @"INSERT INTO dbo.Backups (FileName, FilePath, SizeBytes, CreatedByUserID, Comment, IsAuto)
                  VALUES (@FileName, @FilePath, @SizeBytes, @CreatedByUserID, @Comment, @IsAuto);",
                new SqlParameter("@FileName", Path.GetFileName(backupFilePath)),
                new SqlParameter("@FilePath", backupFilePath),
                new SqlParameter("@SizeBytes", fileInfo.Exists ? fileInfo.Length : 0),
                new SqlParameter("@CreatedByUserID", CurrentUserContext.UserId == 0 ? (object)DBNull.Value : CurrentUserContext.UserId),
                new SqlParameter("@Comment", (object)comment ?? DBNull.Value),
                new SqlParameter("@IsAuto", isAuto));
        }

        public static DataTable GetBackups()
        {
            const string sql = @"
SELECT TOP 500 Id, FileName, FilePath, SizeBytes, CreationDate, Comment, IsAuto
FROM dbo.Backups
ORDER BY CreationDate DESC;";
            return Db.ExecuteDataTable(sql);
        }

        public static void RestoreBackup(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Файл резервной копии не найден.", backupFilePath);
            }

            using (var connection = new SqlConnection(Db.MasterConnectionString))
            {
                connection.Open();
                var sql = @"
ALTER DATABASE [BGSK1DiplomaDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [BGSK1DiplomaDB] FROM DISK = @Path WITH REPLACE;
ALTER DATABASE [BGSK1DiplomaDB] SET MULTI_USER;";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 600;
                    command.Parameters.AddWithValue("@Path", backupFilePath);
                    command.ExecuteNonQuery();
                }
            }

            AuditService.LogChange("Backups", "RESTORE", null, null, $"{{\"FilePath\":\"{backupFilePath}\"}}");
        }
    }
}
