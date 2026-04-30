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
        public static void CreateBackup(string backupDirectory, string comment, bool isAuto)
        {
            Directory.CreateDirectory(backupDirectory);
            var fileName = $"BGSK1DiplomaDB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var fullPath = Path.Combine(backupDirectory, fileName);

            const string backupSql = "BACKUP DATABASE [BGSK1DiplomaDB] TO DISK = @Path WITH INIT, COMPRESSION;";
            Db.ExecuteNonQuery(backupSql, new SqlParameter("@Path", fullPath));

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
