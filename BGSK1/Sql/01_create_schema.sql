IF DB_ID(N'BGSK1DiplomaDB') IS NULL
BEGIN
    CREATE DATABASE BGSK1DiplomaDB1;
END
GO

USE BGSK1DiplomaDB;
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL UNIQUE,
        AccessLevel INT NOT NULL,
        IsDeleted BIT NOT NULL DEFAULT(0),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(400) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        RoleId INT NOT NULL,
        RegistrationDate DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        LastPasswordChange DATETIME2 NULL,
        MustChangePassword BIT NOT NULL DEFAULT(0),
        IsActive BIT NOT NULL DEFAULT(1),
        IsDeleted BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
    );
END
GO

IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        TableName NVARCHAR(128) NOT NULL,
        OperationType NVARCHAR(20) NOT NULL,
        RecordId NVARCHAR(100) NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        IPAddress NVARCHAR(50) NULL
    );
END
GO

IF OBJECT_ID('dbo.Backups', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Backups
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        SizeBytes BIGINT NULL,
        CreationDate DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CreatedByUserID INT NULL,
        Comment NVARCHAR(500) NULL,
        IsAuto BIT NOT NULL DEFAULT(0),
        IsEncrypted BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_Backups_Users FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.SystemSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ParamName NVARCHAR(120) NOT NULL UNIQUE,
        ParamValue NVARCHAR(1000) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        LastModified DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.[Statistics]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Statistics]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MetricName NVARCHAR(200) NOT NULL,
        MetricValue DECIMAL(18,2) NOT NULL,
        PeriodStart DATETIME2 NOT NULL,
        PeriodEnd DATETIME2 NOT NULL,
        CalculatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.ErrorLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErrorLog
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        ErrorMessage NVARCHAR(2000) NOT NULL,
        StackTrace NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        IsResolved BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_ErrorLog_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.Reports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reports
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ReportName NVARCHAR(255) NOT NULL,
        ReportType NVARCHAR(60) NOT NULL,
        ParametersJSON NVARCHAR(MAX) NULL,
        CreatedByUserID INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        FilePath NVARCHAR(500) NULL,
        CONSTRAINT FK_Reports_Users FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.Sessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sessions
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        Token UNIQUEIDENTIFIER NOT NULL DEFAULT(NEWID()),
        LoginTime DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        LastActivity DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        IPAddress NVARCHAR(50) NULL,
        IsRevoked BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_Sessions_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.LoginAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginAttempts
    (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL,
        AttemptTime DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        IsSuccess BIT NOT NULL,
        IPAddress NVARCHAR(50) NULL,
        [Comment] NVARCHAR(300) NULL
    );
END
GO

IF OBJECT_ID('dbo.DataRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataRecords
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Payload NVARCHAR(MAX) NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL,
        IsDeleted BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_DataRecords_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_DataRecords_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(Id)
    );
END
GO

MERGE dbo.Roles AS target
USING (VALUES
    (N'Администратор', 100),
    (N'Аналитик', 50),
    (N'Оператор', 20)
) AS source(Name, AccessLevel)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
INSERT (Name, AccessLevel) VALUES (source.Name, source.AccessLevel);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@bgsk.local')
BEGIN
    INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, MustChangePassword)
    SELECT N'admin@bgsk.local',
           N'PBKDF2$120000$U2xvY2FsQWRtaW5TYWx0MDE=$0LJV3F1jR9cQjuE6e7UxvcpllxgwSVSL1bCBa/YAFzo=',
           N'Системный Администратор',
           r.Id,
           1,
           1
    FROM dbo.Roles r
    WHERE r.Name = N'Администратор';
END
GO

IF OBJECT_ID('dbo.trg_DataRecords_Audit', 'TR') IS NULL
EXEC('
CREATE TRIGGER dbo.trg_DataRecords_Audit
ON dbo.DataRecords
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog (UserId, TableName, OperationType, RecordId, OldValue, NewValue, [Timestamp], IPAddress)
    SELECT
        TRY_CONVERT(INT, SESSION_CONTEXT(N''AppUserId'')),
        N''DataRecords'',
        CASE
            WHEN i.Id IS NOT NULL AND d.Id IS NULL THEN N''INSERT''
            WHEN i.Id IS NOT NULL AND d.Id IS NOT NULL THEN N''UPDATE''
            ELSE N''DELETE''
        END,
        CONVERT(NVARCHAR(100), COALESCE(i.Id, d.Id)),
        CASE WHEN d.Id IS NULL THEN NULL ELSE (SELECT d.Id, d.Title, d.Payload, d.IsDeleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        CASE WHEN i.Id IS NULL THEN NULL ELSE (SELECT i.Id, i.Title, i.Payload, i.IsDeleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
        SYSUTCDATETIME(),
        TRY_CONVERT(NVARCHAR(50), SESSION_CONTEXT(N''AppIp''))
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.Id = d.Id;
END
');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE ParamName = N'BackupInterval')
BEGIN
    INSERT INTO dbo.SystemSettings (ParamName, ParamValue, [Description])
    VALUES (N'BackupInterval', N'Daily', N'Интервал автосоздания бэкапов (Daily/Weekly/Monthly)');
END
GO

IF OBJECT_ID('dbo.Equipment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipment
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InventoryNumber NVARCHAR(80) NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        TypeName NVARCHAR(120) NOT NULL,
        LocationName NVARCHAR(160) NOT NULL,
        ResponsiblePerson NVARCHAR(200) NULL,
        PurchaseDate DATE NULL,
        WarrantyUntil DATE NULL,
        StatusName NVARCHAR(60) NOT NULL DEFAULT(N'В эксплуатации'),
        IsDeleted BIT NOT NULL DEFAULT(0)
    );
END
GO

IF OBJECT_ID('dbo.RepairRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairRequests
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RequestNumber NVARCHAR(60) NOT NULL UNIQUE,
        EquipmentId INT NOT NULL,
        ProblemDescription NVARCHAR(MAX) NOT NULL,
        PriorityName NVARCHAR(40) NOT NULL DEFAULT(N'Средний'),
        StatusName NVARCHAR(40) NOT NULL DEFAULT(N'Новая'),
        CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        AssignedTo NVARCHAR(200) NULL,
        CompletedAt DATETIME2 NULL,
        CONSTRAINT FK_RepairRequests_Equipment FOREIGN KEY (EquipmentId) REFERENCES dbo.Equipment(Id),
        CONSTRAINT FK_RepairRequests_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.MaintenancePlans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenancePlans
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EquipmentId INT NOT NULL,
        MaintenanceType NVARCHAR(120) NOT NULL,
        PeriodDays INT NOT NULL,
        NextDate DATE NOT NULL,
        ResponsiblePerson NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CONSTRAINT FK_MaintenancePlans_Equipment FOREIGN KEY (EquipmentId) REFERENCES dbo.Equipment(Id)
    );
END
GO

IF OBJECT_ID('dbo.MaintenanceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceHistory
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PlanId INT NOT NULL,
        PerformedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
        ResultComment NVARCHAR(1000) NULL,
        PerformedByUserId INT NULL,
        CONSTRAINT FK_MaintenanceHistory_Plans FOREIGN KEY (PlanId) REFERENCES dbo.MaintenancePlans(Id),
        CONSTRAINT FK_MaintenanceHistory_Users FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.SpareParts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SpareParts
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PartName NVARCHAR(200) NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        QuantityInStock INT NOT NULL DEFAULT(0),
        MinQuantity INT NOT NULL DEFAULT(0),
        UnitName NVARCHAR(30) NOT NULL DEFAULT(N'шт'),
        LastUpdated DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.RepairRequestParts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairRequestParts
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RequestId INT NOT NULL,
        SparePartId INT NOT NULL,
        QuantityUsed INT NOT NULL,
        CONSTRAINT FK_RepairRequestParts_Requests FOREIGN KEY (RequestId) REFERENCES dbo.RepairRequests(Id),
        CONSTRAINT FK_RepairRequestParts_Parts FOREIGN KEY (SparePartId) REFERENCES dbo.SpareParts(Id)
    );
END
GO

IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,
        PermissionKey NVARCHAR(120) NOT NULL,
        IsAllowed BIT NOT NULL DEFAULT(1),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
        CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, PermissionKey)
    );
END
GO

MERGE dbo.RolePermissions AS target
USING (
    SELECT r.Id AS RoleId, p.PermissionKey
    FROM dbo.Roles r
    CROSS APPLY (VALUES
        (N'module.equipment'),
        (N'module.requests'),
        (N'module.maintenance'),
        (N'module.parts'),
        (N'module.users'),
        (N'module.reports'),
        (N'module.backups'),
        (N'module.admin')
    ) p(PermissionKey)
    WHERE r.Name = N'Администратор'
) AS source
ON target.RoleId = source.RoleId AND target.PermissionKey = source.PermissionKey
WHEN NOT MATCHED THEN
INSERT (RoleId, PermissionKey, IsAllowed) VALUES (source.RoleId, source.PermissionKey, 1);
GO

MERGE dbo.RolePermissions AS target
USING (
    SELECT r.Id AS RoleId, p.PermissionKey
    FROM dbo.Roles r
    CROSS APPLY (VALUES
        (N'module.equipment'),
        (N'module.requests'),
        (N'module.maintenance'),
        (N'module.parts'),
        (N'module.users'),
        (N'module.reports')
    ) p(PermissionKey)
    WHERE r.Name = N'Аналитик'
) AS source
ON target.RoleId = source.RoleId AND target.PermissionKey = source.PermissionKey
WHEN NOT MATCHED THEN
INSERT (RoleId, PermissionKey, IsAllowed) VALUES (source.RoleId, source.PermissionKey, 1);
GO

MERGE dbo.RolePermissions AS target
USING (
    SELECT r.Id AS RoleId, p.PermissionKey
    FROM dbo.Roles r
    CROSS APPLY (VALUES
        (N'module.equipment'),
        (N'module.requests'),
        (N'module.maintenance'),
        (N'module.parts'),
        (N'module.users')
    ) p(PermissionKey)
    WHERE r.Name = N'Оператор'
) AS source
ON target.RoleId = source.RoleId AND target.PermissionKey = source.PermissionKey
WHEN NOT MATCHED THEN
INSERT (RoleId, PermissionKey, IsAllowed) VALUES (source.RoleId, source.PermissionKey, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Equipment)
BEGIN
    INSERT INTO dbo.Equipment (InventoryNumber, Name, TypeName, LocationName, ResponsiblePerson, StatusName, IsDeleted)
    VALUES
    (N'ПК-001', N'Компьютер учебный 1', N'Системный блок', N'Кабинет 204', N'Иванов И.И.', N'В эксплуатации', 0),
    (N'ПК-002', N'Компьютер учебный 2', N'Системный блок', N'Кабинет 204', N'Иванов И.И.', N'В эксплуатации', 0),
    (N'НБ-001', N'Ноутбук преподавателя', N'Ноутбук', N'Методический кабинет', N'Петрова А.А.', N'В эксплуатации', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SpareParts)
BEGIN
    INSERT INTO dbo.SpareParts (PartName, PartNumber, QuantityInStock, MinQuantity, UnitName)
    VALUES
    (N'SSD 512 GB', N'SSD-512-01', 6, 2, N'шт'),
    (N'Память DDR4 8 GB', N'RAM-8-01', 10, 4, N'шт'),
    (N'Блок питания 500W', N'PSU-500-01', 3, 2, N'шт');
END
GO

IF (SELECT COUNT(1) FROM dbo.Equipment) < 50
BEGIN
    DECLARE @eqCount INT = (SELECT COUNT(1) FROM dbo.Equipment);
    DECLARE @eqTarget INT = 50;
    DECLARE @eqIndex INT = @eqCount + 1;

    WHILE @eqIndex <= @eqTarget
    BEGIN
        INSERT INTO dbo.Equipment (InventoryNumber, Name, TypeName, LocationName, ResponsiblePerson, PurchaseDate, WarrantyUntil, StatusName, IsDeleted)
        VALUES
        (
            N'EQ-' + RIGHT(N'000' + CAST(@eqIndex AS NVARCHAR(10)), 3),
            CASE (@eqIndex % 6)
                WHEN 0 THEN N'Системный блок Dell OptiPlex'
                WHEN 1 THEN N'Ноутбук Lenovo ThinkPad'
                WHEN 2 THEN N'Принтер HP LaserJet'
                WHEN 3 THEN N'МФУ Canon i-SENSYS'
                WHEN 4 THEN N'Коммутатор Cisco Catalyst'
                ELSE N'Интерактивная панель SMART'
            END + N' №' + CAST(@eqIndex AS NVARCHAR(10)),
            CASE (@eqIndex % 6)
                WHEN 0 THEN N'Системный блок'
                WHEN 1 THEN N'Ноутбук'
                WHEN 2 THEN N'Принтер'
                WHEN 3 THEN N'МФУ'
                WHEN 4 THEN N'Сетевое оборудование'
                ELSE N'Дисплей'
            END,
            CASE (@eqIndex % 8)
                WHEN 0 THEN N'Кабинет 101'
                WHEN 1 THEN N'Кабинет 102'
                WHEN 2 THEN N'Кабинет 201'
                WHEN 3 THEN N'Кабинет 204'
                WHEN 4 THEN N'Лаборатория ИТ-1'
                WHEN 5 THEN N'Лаборатория ИТ-2'
                WHEN 6 THEN N'Методический кабинет'
                ELSE N'Библиотека'
            END,
            CASE (@eqIndex % 10)
                WHEN 0 THEN N'Иванов И.И.'
                WHEN 1 THEN N'Петрова А.А.'
                WHEN 2 THEN N'Смирнов Д.В.'
                WHEN 3 THEN N'Кузнецова Е.П.'
                WHEN 4 THEN N'Орлов Н.С.'
                WHEN 5 THEN N'Макарова Т.В.'
                WHEN 6 THEN N'Федоров Р.А.'
                WHEN 7 THEN N'Белова М.С.'
                WHEN 8 THEN N'Никитин П.О.'
                ELSE N'Егорова Л.И.'
            END,
            DATEADD(DAY, -(@eqIndex * 40), CONVERT(date, GETDATE())),
            DATEADD(DAY, 365 * 2 - (@eqIndex * 7), CONVERT(date, GETDATE())),
            CASE
                WHEN @eqIndex % 17 = 0 THEN N'На диагностике'
                WHEN @eqIndex % 11 = 0 THEN N'Требует ремонта'
                ELSE N'В эксплуатации'
            END,
            0
        );

        SET @eqIndex = @eqIndex + 1;
    END
END
GO

IF (SELECT COUNT(1) FROM dbo.SpareParts) < 50
BEGIN
    DECLARE @spCount INT = (SELECT COUNT(1) FROM dbo.SpareParts);
    DECLARE @spTarget INT = 50;
    DECLARE @spIndex INT = @spCount + 1;

    WHILE @spIndex <= @spTarget
    BEGIN
        INSERT INTO dbo.SpareParts (PartName, PartNumber, QuantityInStock, MinQuantity, UnitName, LastUpdated)
        VALUES
        (
            CASE (@spIndex % 10)
                WHEN 0 THEN N'SSD 1TB NVMe'
                WHEN 1 THEN N'Оперативная память DDR4 16GB'
                WHEN 2 THEN N'Блок питания 600W'
                WHEN 3 THEN N'Картридж лазерный CF283A'
                WHEN 4 THEN N'Термопаста Arctic MX-4'
                WHEN 5 THEN N'Вентилятор 120 мм'
                WHEN 6 THEN N'Кабель HDMI 2м'
                WHEN 7 THEN N'Клавиатура USB'
                WHEN 8 THEN N'Мышь оптическая USB'
                ELSE N'Аккумулятор ноутбука'
            END + N' (партия ' + CAST(@spIndex AS NVARCHAR(10)) + N')',
            N'PRT-' + RIGHT(N'000' + CAST(@spIndex AS NVARCHAR(10)), 3),
            5 + (@spIndex % 20),
            2 + (@spIndex % 7),
            N'шт',
            DATEADD(DAY, -(@spIndex % 15), SYSUTCDATETIME())
        );

        SET @spIndex = @spIndex + 1;
    END
END
GO

IF (SELECT COUNT(1) FROM dbo.MaintenancePlans) < 50
BEGIN
    DECLARE @mpCount INT = (SELECT COUNT(1) FROM dbo.MaintenancePlans);
    DECLARE @mpTarget INT = 50;
    DECLARE @mpIndex INT = @mpCount + 1;
    DECLARE @eqTotal INT = (SELECT COUNT(1) FROM dbo.Equipment);

    WHILE @mpIndex <= @mpTarget
    BEGIN
        INSERT INTO dbo.MaintenancePlans (EquipmentId, MaintenanceType, PeriodDays, NextDate, ResponsiblePerson, IsActive)
        VALUES
        (
            1 + ((@mpIndex - 1) % @eqTotal),
            CASE (@mpIndex % 5)
                WHEN 0 THEN N'Плановая чистка и диагностика'
                WHEN 1 THEN N'Проверка системы охлаждения'
                WHEN 2 THEN N'Обновление ПО и антивируса'
                WHEN 3 THEN N'Проверка сетевых интерфейсов'
                ELSE N'Комплексное профилактическое ТО'
            END,
            CASE (@mpIndex % 4)
                WHEN 0 THEN 30
                WHEN 1 THEN 60
                WHEN 2 THEN 90
                ELSE 120
            END,
            DATEADD(DAY, (@mpIndex % 40) - 20, CONVERT(date, GETDATE())),
            CASE (@mpIndex % 6)
                WHEN 0 THEN N'Служба ИТ'
                WHEN 1 THEN N'Иванов И.И.'
                WHEN 2 THEN N'Петрова А.А.'
                WHEN 3 THEN N'Смирнов Д.В.'
                WHEN 4 THEN N'Кузнецова Е.П.'
                ELSE N'Орлов Н.С.'
            END,
            CASE WHEN @mpIndex % 13 = 0 THEN 0 ELSE 1 END
        );

        SET @mpIndex = @mpIndex + 1;
    END
END
GO

IF (SELECT COUNT(1) FROM dbo.RepairRequests) < 50
BEGIN
    DECLARE @rrCount INT = (SELECT COUNT(1) FROM dbo.RepairRequests);
    DECLARE @rrTarget INT = 50;
    DECLARE @rrIndex INT = @rrCount + 1;
    DECLARE @eqForReq INT = (SELECT COUNT(1) FROM dbo.Equipment);
    DECLARE @defaultUserId INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @rrIndex <= @rrTarget
    BEGIN
        DECLARE @createdAt DATETIME2 = DATEADD(DAY, -(@rrIndex % 35), SYSUTCDATETIME());
        DECLARE @status NVARCHAR(40) =
            CASE
                WHEN @rrIndex % 5 = 0 THEN N'Завершена'
                WHEN @rrIndex % 4 = 0 THEN N'Ожидание'
                WHEN @rrIndex % 3 = 0 THEN N'В работе'
                ELSE N'Новая'
            END;

        INSERT INTO dbo.RepairRequests (RequestNumber, EquipmentId, ProblemDescription, PriorityName, StatusName, CreatedAt, CreatedByUserId, AssignedTo, CompletedAt)
        VALUES
        (
            N'RR-' + RIGHT(N'0000' + CAST(@rrIndex AS NVARCHAR(10)), 4),
            1 + ((@rrIndex - 1) % @eqForReq),
            CASE (@rrIndex % 10)
                WHEN 0 THEN N'Не включается после скачка напряжения.'
                WHEN 1 THEN N'Перегрев и шум системы охлаждения.'
                WHEN 2 THEN N'Сбои при печати, замятие бумаги.'
                WHEN 3 THEN N'Не определяется сетевой адаптер.'
                WHEN 4 THEN N'Ошибка загрузки ОС после обновления.'
                WHEN 5 THEN N'Мерцание экрана и артефакты изображения.'
                WHEN 6 THEN N'Неисправность клавиатуры, часть клавиш не работает.'
                WHEN 7 THEN N'Падение производительности и зависания.'
                WHEN 8 THEN N'Проблема с USB-портами на передней панели.'
                ELSE N'Требуется замена расходных материалов и диагностика.'
            END,
            CASE (@rrIndex % 3)
                WHEN 0 THEN N'Высокий'
                WHEN 1 THEN N'Средний'
                ELSE N'Низкий'
            END,
            @status,
            @createdAt,
            @defaultUserId,
            CASE (@rrIndex % 6)
                WHEN 0 THEN N'Иванов И.И.'
                WHEN 1 THEN N'Петрова А.А.'
                WHEN 2 THEN N'Смирнов Д.В.'
                WHEN 3 THEN N'Кузнецова Е.П.'
                WHEN 4 THEN N'Орлов Н.С.'
                ELSE N'Дежурный инженер'
            END,
            CASE WHEN @status = N'Завершена' THEN DATEADD(HOUR, (@rrIndex % 72) + 4, @createdAt) ELSE NULL END
        );

        SET @rrIndex = @rrIndex + 1;
    END
END
GO

IF (SELECT COUNT(1) FROM dbo.MaintenanceHistory) < 50
BEGIN
    DECLARE @mhCount INT = (SELECT COUNT(1) FROM dbo.MaintenanceHistory);
    DECLARE @mhTarget INT = 50;
    DECLARE @mhIndex INT = @mhCount + 1;
    DECLARE @plansCount INT = (SELECT COUNT(1) FROM dbo.MaintenancePlans);
    DECLARE @historyUserId INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @mhIndex <= @mhTarget
    BEGIN
        INSERT INTO dbo.MaintenanceHistory (PlanId, PerformedAt, ResultComment, PerformedByUserId)
        VALUES
        (
            1 + ((@mhIndex - 1) % @plansCount),
            DATEADD(DAY, -(@mhIndex % 60), SYSUTCDATETIME()),
            CASE (@mhIndex % 5)
                WHEN 0 THEN N'ТО выполнено в полном объеме, замечаний нет.'
                WHEN 1 THEN N'Выполнена чистка, рекомендована замена вентилятора в следующем цикле.'
                WHEN 2 THEN N'Обновлено ПО, устранены ошибки журналов событий.'
                WHEN 3 THEN N'Проведена диагностика питания, заменен кабель.'
                ELSE N'Профилактика завершена, оборудование работает стабильно.'
            END,
            @historyUserId
        );

        SET @mhIndex = @mhIndex + 1;
    END
END
GO

IF (SELECT COUNT(1) FROM dbo.RepairRequestParts) < 50
BEGIN
    DECLARE @rpCount INT = (SELECT COUNT(1) FROM dbo.RepairRequestParts);
    DECLARE @rpTarget INT = 50;
    DECLARE @rpIndex INT = @rpCount + 1;
    DECLARE @reqCount INT = (SELECT COUNT(1) FROM dbo.RepairRequests);
    DECLARE @partsCount INT = (SELECT COUNT(1) FROM dbo.SpareParts);

    WHILE @rpIndex <= @rpTarget
    BEGIN
        INSERT INTO dbo.RepairRequestParts (RequestId, SparePartId, QuantityUsed)
        VALUES
        (
            1 + ((@rpIndex - 1) % @reqCount),
            1 + ((@rpIndex * 3 - 1) % @partsCount),
            1 + (@rpIndex % 4)
        );

        SET @rpIndex = @rpIndex + 1;
    END
END
GO
