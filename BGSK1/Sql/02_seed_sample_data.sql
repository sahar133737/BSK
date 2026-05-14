/*
  Demo / test seed for BGSK1DiplomaDB.
  Run after 01_create_schema.sql.

  Targets (first run):
    ~100 rows: Equipment, SpareParts, RepairRequests, MaintenancePlans,
               MaintenanceHistory, RepairRequestParts, LookupDictionary,
               Statistics, LoginAttempts, DataRecords, AuditLog
    Smaller:  Users (up to 15 total), Backups (12), Reports (15),
               Sessions (25), ErrorLog (30), a few SystemSettings keys

  Idempotent: each block only inserts until the table reaches the target count.
*/

USE BGSK1DiplomaDB;
GO

SET NOCOUNT ON;
GO

/* ------------------------------------------------------------------ Users (up to 15) */
IF (SELECT COUNT(1) FROM dbo.Users) < 15
BEGIN
    DECLARE @uNext INT = (SELECT COUNT(1) FROM dbo.Users) + 1;
    DECLARE @uTarget INT = 15;
    DECLARE @demoHash NVARCHAR(400) =
        N'PBKDF2$120000$U2xvY2FsQWRtaW5TYWx0MDE=$0LJV3F1jR9cQjuE6e7UxvcpllxgwSVSL1bCBa/YAFzo=';
    DECLARE @roleCount INT = (SELECT COUNT(1) FROM dbo.Roles);
    DECLARE @roleIds TABLE (RowNum INT PRIMARY KEY, Id INT);
    INSERT INTO @roleIds (RowNum, Id)
    SELECT ROW_NUMBER() OVER (ORDER BY Id), Id FROM dbo.Roles;

    DECLARE @rid INT;

    WHILE @uNext <= @uTarget AND @roleCount > 0
    BEGIN
        SET @rid = (SELECT Id FROM @roleIds WHERE RowNum = 1 + ((@uNext - 1) % @roleCount));

        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'demo.user' + CAST(@uNext AS NVARCHAR(10)) + N'@bgsk.local')
        BEGIN
            INSERT INTO dbo.Users (Email, PasswordHash, FullName, RoleId, IsActive, MustChangePassword, IsDeleted)
            VALUES
            (
                N'demo.user' + CAST(@uNext AS NVARCHAR(10)) + N'@bgsk.local',
                @demoHash,
                N'Демо пользователь ' + CAST(@uNext AS NVARCHAR(10)),
                @rid,
                CASE WHEN @uNext % 9 = 0 THEN 0 ELSE 1 END,
                0,
                0
            );
        END;

        SET @uNext += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Equipment (~100) */
IF (SELECT COUNT(1) FROM dbo.Equipment) < 100
BEGIN
    DECLARE @eqCount INT = (SELECT COUNT(1) FROM dbo.Equipment);
    DECLARE @eqTarget INT = 100;
    DECLARE @eqIndex INT = @eqCount + 1;

    WHILE @eqIndex <= @eqTarget
    BEGIN
        INSERT INTO dbo.Equipment (InventoryNumber, Name, TypeName, LocationName, ResponsiblePerson, PurchaseDate, WarrantyUntil, StatusName, IsDeleted)
        VALUES
        (
            N'SEED-EQ-' + RIGHT(N'000' + CAST(@eqIndex AS NVARCHAR(10)), 3),
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
            DATEADD(DAY, -(@eqIndex * 11), CONVERT(date, GETDATE())),
            DATEADD(DAY, 400 - (@eqIndex % 200), CONVERT(date, GETDATE())),
            CASE
                WHEN @eqIndex % 17 = 0 THEN N'На диагностике'
                WHEN @eqIndex % 11 = 0 THEN N'Требует ремонта'
                ELSE N'В эксплуатации'
            END,
            0
        );

        SET @eqIndex += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ SpareParts (~100) */
IF (SELECT COUNT(1) FROM dbo.SpareParts) < 100
BEGIN
    DECLARE @spCount INT = (SELECT COUNT(1) FROM dbo.SpareParts);
    DECLARE @spTarget INT = 100;
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
            END + N' (seed ' + CAST(@spIndex AS NVARCHAR(10)) + N')',
            N'SEED-PRT-' + RIGHT(N'000' + CAST(@spIndex AS NVARCHAR(10)), 3),
            5 + (@spIndex % 25),
            1 + (@spIndex % 8),
            N'шт',
            DATEADD(DAY, -(@spIndex % 20), SYSUTCDATETIME())
        );

        SET @spIndex += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ MaintenancePlans (~100) */
IF (SELECT COUNT(1) FROM dbo.MaintenancePlans) < 100
BEGIN
    DECLARE @mpCount2 INT = (SELECT COUNT(1) FROM dbo.MaintenancePlans);
    DECLARE @mpTarget2 INT = 100;
    DECLARE @mpIndex2 INT = @mpCount2 + 1;
    DECLARE @eqTotal2 INT = (SELECT COUNT(1) FROM dbo.Equipment);

    WHILE @mpIndex2 <= @mpTarget2 AND @eqTotal2 > 0
    BEGIN
        INSERT INTO dbo.MaintenancePlans (EquipmentId, MaintenanceType, PeriodDays, NextDate, ResponsiblePerson, IsActive)
        VALUES
        (
            1 + ((@mpIndex2 - 1) % @eqTotal2),
            CASE (@mpIndex2 % 5)
                WHEN 0 THEN N'Плановая чистка и диагностика'
                WHEN 1 THEN N'Проверка системы охлаждения'
                WHEN 2 THEN N'Обновление ПО и антивируса'
                WHEN 3 THEN N'Проверка сетевых интерфейсов'
                ELSE N'Комплексное профилактическое ТО'
            END,
            CASE (@mpIndex2 % 4)
                WHEN 0 THEN 30
                WHEN 1 THEN 60
                WHEN 2 THEN 90
                ELSE 120
            END,
            DATEADD(DAY, (@mpIndex2 % 45) - 25, CONVERT(date, GETDATE())),
            CASE (@mpIndex2 % 6)
                WHEN 0 THEN N'Служба ИТ'
                WHEN 1 THEN N'Иванов И.И.'
                WHEN 2 THEN N'Петрова А.А.'
                WHEN 3 THEN N'Смирнов Д.В.'
                WHEN 4 THEN N'Кузнецова Е.П.'
                ELSE N'Орлов Н.С.'
            END,
            CASE WHEN @mpIndex2 % 13 = 0 THEN 0 ELSE 1 END
        );

        SET @mpIndex2 += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ RepairRequests (~100) */
IF (SELECT COUNT(1) FROM dbo.RepairRequests) < 100
BEGIN
    DECLARE @rrCount2 INT = (SELECT COUNT(1) FROM dbo.RepairRequests);
    DECLARE @rrTarget2 INT = 100;
    DECLARE @rrIndex2 INT = @rrCount2 + 1;
    DECLARE @eqForReq2 INT = (SELECT COUNT(1) FROM dbo.Equipment);

    DECLARE @userIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @userIds (Id)
    SELECT Id FROM dbo.Users WHERE IsDeleted = 0;

    DECLARE @userCnt INT = (SELECT COUNT(1) FROM @userIds);
    DECLARE @uid2 INT;
    DECLARE @createdAt2 DATETIME2;
    DECLARE @status2 NVARCHAR(40);

    WHILE @rrIndex2 <= @rrTarget2 AND @eqForReq2 > 0
    BEGIN
        SET @uid2 =
            (SELECT Id FROM (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM @userIds
            ) x WHERE rn = CASE WHEN @userCnt > 0 THEN 1 + ((@rrIndex2 - 1) % @userCnt) ELSE 1 END);

        IF @uid2 IS NULL
            SET @uid2 = (SELECT TOP 1 Id FROM dbo.Users ORDER BY Id);

        SET @createdAt2 = DATEADD(DAY, -(@rrIndex2 % 60), SYSUTCDATETIME());
        SET @status2 =
            CASE
                WHEN @rrIndex2 % 5 = 0 THEN N'Завершена'
                WHEN @rrIndex2 % 4 = 0 THEN N'Ожидание'
                WHEN @rrIndex2 % 3 = 0 THEN N'В работе'
                ELSE N'Новая'
            END;

        INSERT INTO dbo.RepairRequests (RequestNumber, EquipmentId, ProblemDescription, PriorityName, StatusName, CreatedAt, CreatedByUserId, AssignedTo, CompletedAt)
        VALUES
        (
            N'SEED-RR-' + RIGHT(N'0000' + CAST(@rrIndex2 AS NVARCHAR(10)), 4),
            1 + ((@rrIndex2 - 1) % @eqForReq2),
            CASE (@rrIndex2 % 10)
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
            CASE (@rrIndex2 % 3)
                WHEN 0 THEN N'Высокий'
                WHEN 1 THEN N'Средний'
                ELSE N'Низкий'
            END,
            @status2,
            @createdAt2,
            @uid2,
            CASE (@rrIndex2 % 6)
                WHEN 0 THEN N'Иванов И.И.'
                WHEN 1 THEN N'Петрова А.А.'
                WHEN 2 THEN N'Смирнов Д.В.'
                WHEN 3 THEN N'Кузнецова Е.П.'
                WHEN 4 THEN N'Орлов Н.С.'
                ELSE N'Дежурный инженер'
            END,
            CASE WHEN @status2 = N'Завершена' THEN DATEADD(HOUR, (@rrIndex2 % 72) + 4, @createdAt2) ELSE NULL END
        );

        SET @rrIndex2 += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ MaintenanceHistory (~100) */
IF (SELECT COUNT(1) FROM dbo.MaintenanceHistory) < 100
BEGIN
    DECLARE @mhCount2 INT = (SELECT COUNT(1) FROM dbo.MaintenanceHistory);
    DECLARE @mhTarget2 INT = 100;
    DECLARE @mhIndex2 INT = @mhCount2 + 1;
    DECLARE @plansCount2 INT = (SELECT COUNT(1) FROM dbo.MaintenancePlans);

    DECLARE @histUsers TABLE (Id INT PRIMARY KEY);
    INSERT INTO @histUsers (Id)
    SELECT Id FROM dbo.Users WHERE IsDeleted = 0;

    DECLARE @histUserCnt INT = (SELECT COUNT(1) FROM @histUsers);
    DECLARE @huid INT;

    WHILE @mhIndex2 <= @mhTarget2 AND @plansCount2 > 0
    BEGIN
        SET @huid =
            (SELECT Id FROM (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM @histUsers
            ) y WHERE rn = CASE WHEN @histUserCnt > 0 THEN 1 + ((@mhIndex2 - 1) % @histUserCnt) ELSE 1 END);

        INSERT INTO dbo.MaintenanceHistory (PlanId, PerformedAt, ResultComment, PerformedByUserId)
        VALUES
        (
            1 + ((@mhIndex2 - 1) % @plansCount2),
            DATEADD(DAY, -(@mhIndex2 % 90), SYSUTCDATETIME()),
            CASE (@mhIndex2 % 5)
                WHEN 0 THEN N'ТО выполнено в полном объеме, замечаний нет.'
                WHEN 1 THEN N'Выполнена чистка, рекомендована замена вентилятора в следующем цикле.'
                WHEN 2 THEN N'Обновлено ПО, устранены ошибки журналов событий.'
                WHEN 3 THEN N'Проведена диагностика питания, заменен кабель.'
                ELSE N'Профилактика завершена, оборудование работает стабильно.'
            END,
            @huid
        );

        SET @mhIndex2 += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ RepairRequestParts (~100) */
IF (SELECT COUNT(1) FROM dbo.RepairRequestParts) < 100
BEGIN
    DECLARE @rpCount2 INT = (SELECT COUNT(1) FROM dbo.RepairRequestParts);
    DECLARE @rpTarget2 INT = 100;
    DECLARE @rpIndex2 INT = @rpCount2 + 1;
    DECLARE @reqCount2 INT = (SELECT COUNT(1) FROM dbo.RepairRequests);
    DECLARE @partsCount2 INT = (SELECT COUNT(1) FROM dbo.SpareParts);

    WHILE @rpIndex2 <= @rpTarget2 AND @reqCount2 > 0 AND @partsCount2 > 0
    BEGIN
        INSERT INTO dbo.RepairRequestParts (RequestId, SparePartId, QuantityUsed)
        VALUES
        (
            1 + ((@rpIndex2 - 1) % @reqCount2),
            1 + ((@rpIndex2 * 5 + 2) % @partsCount2),
            1 + (@rpIndex2 % 5)
        );

        SET @rpIndex2 += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ LookupDictionary (~100) */
IF (SELECT COUNT(1) FROM dbo.LookupDictionary) < 100
BEGIN
    DECLARE @lk INT = (SELECT COUNT(1) FROM dbo.LookupDictionary) + 1;
    DECLARE @lkTarget INT = 100;

    WHILE @lk <= @lkTarget
    BEGIN
        INSERT INTO dbo.LookupDictionary (Category, Value)
        VALUES
        (
            N'SeedCategory' + CAST((@lk % 5) AS NVARCHAR(10)),
            N'SeedValue-' + RIGHT(N'000' + CAST(@lk AS NVARCHAR(10)), 3)
        );
        SET @lk += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Statistics (~100) */
IF (SELECT COUNT(1) FROM dbo.[Statistics]) < 100
BEGIN
    DECLARE @st INT = (SELECT COUNT(1) FROM dbo.[Statistics]) + 1;
    DECLARE @stTarget INT = 100;
    DECLARE @p0 DATETIME2 = DATEADD(DAY, -120, SYSUTCDATETIME());

    WHILE @st <= @stTarget
    BEGIN
        INSERT INTO dbo.[Statistics] (MetricName, MetricValue, PeriodStart, PeriodEnd, CalculatedAt)
        VALUES
        (
            N'SeedMetric.' + CAST((@st % 7) AS NVARCHAR(10)),
            CAST(100 + (@st % 5000) AS DECIMAL(18, 2)) / 10.0,
            DATEADD(DAY, @st % 30, @p0),
            DATEADD(DAY, @st % 30 + 7, @p0),
            DATEADD(HOUR, -(@st % 200), SYSUTCDATETIME())
        );
        SET @st += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ LoginAttempts (~100) */
IF (SELECT COUNT(1) FROM dbo.LoginAttempts) < 100
BEGIN
    DECLARE @la INT = (SELECT COUNT(1) FROM dbo.LoginAttempts) + 1;
    DECLARE @laTarget INT = 100;

    WHILE @la <= @laTarget
    BEGIN
        INSERT INTO dbo.LoginAttempts (Email, AttemptTime, IsSuccess, IPAddress, [Comment])
        VALUES
        (
            N'attempt' + CAST(@la AS NVARCHAR(10)) + N'@bgsk.local',
            DATEADD(MINUTE, -(@la * 13), SYSUTCDATETIME()),
            CASE WHEN @la % 4 = 0 THEN 1 ELSE 0 END,
            N'192.168.1.' + CAST(1 + (@la % 200) AS NVARCHAR(10)),
            CASE WHEN @la % 4 = 0 THEN N'OK' ELSE N'Invalid password' END
        );
        SET @la += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ DataRecords (~100) */
IF (SELECT COUNT(1) FROM dbo.DataRecords) < 100
BEGIN
    DECLARE @dr INT = (SELECT COUNT(1) FROM dbo.DataRecords) + 1;
    DECLARE @drTarget INT = 100;
    DECLARE @drUser INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @dr <= @drTarget
    BEGIN
        INSERT INTO dbo.DataRecords (Title, Payload, CreatedByUserId, UpdatedByUserId, CreatedAt, UpdatedAt, IsDeleted)
        VALUES
        (
            N'Seed record ' + CAST(@dr AS NVARCHAR(10)),
            N'{"seed":true,"idx":' + CAST(@dr AS NVARCHAR(10)) + N'}',
            @drUser,
            NULL,
            DATEADD(DAY, -(@dr % 40), SYSUTCDATETIME()),
            NULL,
            CASE WHEN @dr % 23 = 0 THEN 1 ELSE 0 END
        );
        SET @dr += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ AuditLog (~100, no trigger dependency) */
IF (SELECT COUNT(1) FROM dbo.AuditLog) < 100
BEGIN
    DECLARE @al INT = (SELECT COUNT(1) FROM dbo.AuditLog) + 1;
    DECLARE @alTarget INT = 100;
    DECLARE @alUser INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @al <= @alTarget
    BEGIN
        INSERT INTO dbo.AuditLog (UserId, TableName, OperationType, RecordId, OldValue, NewValue, [Timestamp], IPAddress)
        VALUES
        (
            @alUser,
            CASE (@al % 4) WHEN 0 THEN N'Users' WHEN 1 THEN N'Equipment' WHEN 2 THEN N'RepairRequests' ELSE N'SpareParts' END,
            CASE (@al % 3) WHEN 0 THEN N'INSERT' WHEN 1 THEN N'UPDATE' ELSE N'DELETE' END,
            CAST(@al AS NVARCHAR(20)),
            NULL,
            N'{"seed":' + CAST(@al AS NVARCHAR(10)) + N'}',
            DATEADD(MINUTE, -(@al * 7), SYSUTCDATETIME()),
            N'10.0.0.' + CAST(1 + (@al % 50) AS NVARCHAR(10))
        );
        SET @al += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Backups (12) */
IF (SELECT COUNT(1) FROM dbo.Backups) < 12
BEGIN
    DECLARE @bk INT = (SELECT COUNT(1) FROM dbo.Backups) + 1;
    DECLARE @bkTarget INT = 12;
    DECLARE @bkUser INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @bk <= @bkTarget
    BEGIN
        INSERT INTO dbo.Backups (FileName, FilePath, SizeBytes, CreationDate, CreatedByUserID, Comment, IsAuto, IsEncrypted)
        VALUES
        (
            N'BGSK1_seed_' + RIGHT(N'00' + CAST(@bk AS NVARCHAR(10)), 2) + N'.bak',
            N'\\backup\share\seed\BGSK1_seed_' + RIGHT(N'00' + CAST(@bk AS NVARCHAR(10)), 2) + N'.bak',
            CAST(1048576 AS BIGINT) * (@bk % 50 + 1),
            DATEADD(DAY, -@bk, SYSUTCDATETIME()),
            @bkUser,
            N'Seed backup',
            CASE WHEN @bk % 3 = 0 THEN 1 ELSE 0 END,
            0
        );
        SET @bk += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Reports (15) */
IF (SELECT COUNT(1) FROM dbo.Reports) < 15
BEGIN
    DECLARE @rp INT = (SELECT COUNT(1) FROM dbo.Reports) + 1;
    DECLARE @rpT INT = 15;
    DECLARE @rpUser INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @rp <= @rpT
    BEGIN
        INSERT INTO dbo.Reports (ReportName, ReportType, ParametersJSON, CreatedByUserID, CreatedAt, FilePath)
        VALUES
        (
            N'Отчёт (seed) ' + CAST(@rp AS NVARCHAR(10)),
            CASE (@rp % 3) WHEN 0 THEN N'PDF' WHEN 1 THEN N'Excel' ELSE N'HTML' END,
            N'{"from":"seed","id":' + CAST(@rp AS NVARCHAR(10)) + N'}',
            @rpUser,
            DATEADD(DAY, -@rp, SYSUTCDATETIME()),
            NULL
        );
        SET @rp += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Sessions (25) */
IF (SELECT COUNT(1) FROM dbo.Sessions) < 25
BEGIN
    DECLARE @ss INT = (SELECT COUNT(1) FROM dbo.Sessions) + 1;
    DECLARE @ssT INT = 25;
    DECLARE @ssUsers TABLE (Id INT PRIMARY KEY, rn INT);
    INSERT INTO @ssUsers (Id, rn)
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Users WHERE IsDeleted = 0;

    DECLARE @ssUC INT = (SELECT COUNT(1) FROM @ssUsers);
    DECLARE @sid INT;

    WHILE @ss <= @ssT
    BEGIN
        SET @sid = (SELECT Id FROM @ssUsers WHERE rn = 1 + ((@ss - 1) % CASE WHEN @ssUC > 0 THEN @ssUC ELSE 1 END));

        INSERT INTO dbo.Sessions (UserID, Token, LoginTime, LastActivity, IPAddress, IsRevoked)
        VALUES
        (
            @sid,
            NEWID(),
            DATEADD(HOUR, -(@ss * 3), SYSUTCDATETIME()),
            DATEADD(HOUR, -(@ss), SYSUTCDATETIME()),
            N'172.16.0.' + CAST(1 + (@ss % 180) AS NVARCHAR(10)),
            CASE WHEN @ss % 7 = 0 THEN 1 ELSE 0 END
        );
        SET @ss += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ ErrorLog (30) */
IF (SELECT COUNT(1) FROM dbo.ErrorLog) < 30
BEGIN
    DECLARE @el INT = (SELECT COUNT(1) FROM dbo.ErrorLog) + 1;
    DECLARE @elT INT = 30;
    DECLARE @elUser INT = (SELECT TOP 1 Id FROM dbo.Users WHERE IsDeleted = 0 ORDER BY Id);

    WHILE @el <= @elT
    BEGIN
        INSERT INTO dbo.ErrorLog (UserId, ErrorMessage, StackTrace, [Timestamp], IsResolved)
        VALUES
        (
            CASE WHEN @el % 2 = 0 THEN @elUser ELSE NULL END,
            N'Seed error #' + CAST(@el AS NVARCHAR(10)) + N': simulated failure',
            N'at SeedScript line ' + CAST(@el AS NVARCHAR(10)),
            DATEADD(MINUTE, -(@el * 41), SYSUTCDATETIME()),
            CASE WHEN @el % 5 = 0 THEN 1 ELSE 0 END
        );
        SET @el += 1;
    END;
END;
GO

/* ------------------------------------------------------------------ Extra SystemSettings (small set, idempotent) */
MERGE dbo.SystemSettings AS t
USING (VALUES
    (N'Seed.MaxUploadMb', N'32', N'Seed: max upload MB'),
    (N'Seed.SessionTimeoutMin', N'60', N'Seed: session timeout'),
    (N'Seed.DefaultLocale', N'ru-RU', N'Seed: UI locale'),
    (N'Seed.EnableDemoBanner', N'1', N'Seed: demo banner'),
    (N'Seed.MaintenanceNotifyDays', N'7', N'Seed: notify before maintenance')
) AS s(ParamName, ParamValue, [Description])
ON t.ParamName = s.ParamName
WHEN NOT MATCHED THEN
    INSERT (ParamName, ParamValue, [Description])
    VALUES (s.ParamName, s.ParamValue, s.[Description]);
GO

PRINT N'Seed 02_seed_sample_data.sql finished. Review row counts with SELECT COUNT(*) FROM <table>.';
GO
