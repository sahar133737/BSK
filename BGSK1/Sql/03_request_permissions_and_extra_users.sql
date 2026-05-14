/*
  Доп. пользователи (по 3 на роль) и права на заявки:
    requests.assign_executor — назначать исполнителя (Администратор, Аналитик: да; Оператор: нет)
    requests.delete            — удалять заявки (то же)

  Выполнить на BGSK1DiplomaDB после 01_create_schema.sql.
  После выполнения пользователям нужно перелогиниться, чтобы подтянулись новые права в приложении.
*/

USE BGSK1DiplomaDB;
GO

SET NOCOUNT ON;

MERGE dbo.RolePermissions AS target
USING (
    SELECT r.Id AS RoleId, p.PermissionKey, p.IsAllowed
    FROM dbo.Roles r
    INNER JOIN (VALUES
        (N'Администратор', N'requests.assign_executor', CAST(1 AS BIT)),
        (N'Администратор', N'requests.delete', CAST(1 AS BIT)),
        (N'Аналитик', N'requests.assign_executor', CAST(1 AS BIT)),
        (N'Аналитик', N'requests.delete', CAST(1 AS BIT)),
        (N'Оператор', N'requests.assign_executor', CAST(0 AS BIT)),
        (N'Оператор', N'requests.delete', CAST(0 AS BIT))
    ) AS p(RoleName, PermissionKey, IsAllowed)
        ON r.Name = p.RoleName AND r.IsDeleted = 0
) AS source
ON target.RoleId = source.RoleId AND target.PermissionKey = source.PermissionKey
WHEN MATCHED THEN
    UPDATE SET IsAllowed = source.IsAllowed
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RoleId, PermissionKey, IsAllowed)
    VALUES (source.RoleId, source.PermissionKey, source.IsAllowed);
GO

/* По 3 пользователя на каждую роль (уникальные Email).
   Хеш объявляем снова: после GO переменные из предыдущего батча недоступны. */
DECLARE @SeedPwdHash NVARCHAR(400) =
    N'PBKDF2$120000$U2xvY2FsQWRtaW5TYWx0MDE=$0LJV3F1jR9cQjuE6e7UxvcpllxgwSVSL1bCBa/YAFzo=';
DECLARE @n TINYINT = 1;
DECLARE @adminRoleId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'Администратор' AND IsDeleted = 0);
DECLARE @analystRoleId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'Аналитик' AND IsDeleted = 0);
DECLARE @operatorRoleId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE Name = N'Оператор' AND IsDeleted = 0);

WHILE @n <= 3
BEGIN
    IF @adminRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local')
        INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, MustChangePassword, IsDeleted)
        VALUES (
            N'admin.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local',
            @SeedPwdHash,
            N'Доп. администратор ' + CAST(@n AS NVARCHAR(2)),
            @adminRoleId,
            1, 0, 0);

    IF @analystRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'analyst.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local')
        INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, MustChangePassword, IsDeleted)
        VALUES (
            N'analyst.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local',
            @SeedPwdHash,
            N'Доп. аналитик ' + CAST(@n AS NVARCHAR(2)),
            @analystRoleId,
            1, 0, 0);

    IF @operatorRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'operator.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local')
        INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, MustChangePassword, IsDeleted)
        VALUES (
            N'operator.extra' + CAST(@n AS NVARCHAR(2)) + N'@bgsk.local',
            @SeedPwdHash,
            N'Доп. оператор ' + CAST(@n AS NVARCHAR(2)),
            @operatorRoleId,
            1, 0, 0);

    SET @n += 1;
END;
GO

PRINT N'03_request_permissions_and_extra_users.sql: готово (перелогиньтесь в приложении).';
GO
