USE master;
GO

-- Tạo database subscriber theo đúng instance đang chạy.
SET NOCOUNT ON;
PRINT N'--- Khởi tạo database subscriber ---';
PRINT N'Server: ' + @@SERVERNAME;
PRINT N'';
GO

-- Xác định tên database mục tiêu theo instance.
DECLARE @instance nvarchar(256) = @@SERVERNAME;
DECLARE @dbName   nvarchar(128) = NULL;
DECLARE @label    nvarchar(50)  = NULL;

IF @instance LIKE N'%\SQLSERVER2'
BEGIN
    SET @dbName = N'NGANHANG_BT';
    SET @label  = N'CN1 - Bến Thành';
END
ELSE IF @instance LIKE N'%\SQLSERVER3'
BEGIN
    SET @dbName = N'NGANHANG_TD';
    SET @label  = N'CN2 - Tân Định';
END
ELSE IF @instance LIKE N'%\SQLSERVER4'
BEGIN
    SET @dbName = N'NGANHANG_TRACUU';
    SET @label  = N'TraCuu - Chỉ đọc';
END
ELSE
BEGIN
    PRINT N'Cảnh báo: Instance [' + @instance + N'] không hợp lệ.';
    PRINT N'          Yêu cầu: SQLSERVER2, SQLSERVER3 hoặc SQLSERVER4.';
    PRINT N'          Script này chỉ dùng cho subscriber.';
    PRINT N'          Bỏ qua toàn bộ thao tác.';
END

IF @dbName IS NOT NULL
BEGIN
    PRINT N'>>> Instance: ' + @instance;
    PRINT N'>>> Database mục tiêu: ' + @dbName + N' (' + @label + N')';
END
GO

-- Tạo database shell cho CN1 nếu chưa tồn tại.
IF @@SERVERNAME LIKE N'%\SQLSERVER2'
BEGIN
    IF DB_ID(N'NGANHANG_BT') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_BT;
        PRINT N'>>> Đã tạo database NGANHANG_BT.';
    END
    ELSE
        PRINT N'>>> Database NGANHANG_BT đã tồn tại, bỏ qua.';
END
GO

-- Tạo database shell cho CN2 nếu chưa tồn tại.
IF @@SERVERNAME LIKE N'%\SQLSERVER3'
BEGIN
    IF DB_ID(N'NGANHANG_TD') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_TD;
        PRINT N'>>> Đã tạo database NGANHANG_TD.';
    END
    ELSE
        PRINT N'>>> Database NGANHANG_TD đã tồn tại, bỏ qua.';
END
GO

-- Tạo database shell cho TraCuu nếu chưa tồn tại.
IF @@SERVERNAME LIKE N'%\SQLSERVER4'
BEGIN
    IF DB_ID(N'NGANHANG_TRACUU') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_TRACUU;
        PRINT N'>>> Đã tạo database NGANHANG_TRACUU.';
    END
    ELSE
        PRINT N'>>> Database NGANHANG_TRACUU đã tồn tại, bỏ qua.';
END
GO

-- Thiết lập recovery model và snapshot isolation cho CN1.
IF @@SERVERNAME LIKE N'%\SQLSERVER2' AND DB_ID(N'NGANHANG_BT') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_BT SET RECOVERY SIMPLE;
    PRINT N'>>> NGANHANG_BT: đã đặt recovery model = SIMPLE.';

    ALTER DATABASE NGANHANG_BT SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT N'>>> NGANHANG_BT: đã bật snapshot isolation.';
END
GO

-- Thiết lập recovery model và snapshot isolation cho CN2.
IF @@SERVERNAME LIKE N'%\SQLSERVER3' AND DB_ID(N'NGANHANG_TD') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_TD SET RECOVERY SIMPLE;
    PRINT N'>>> NGANHANG_TD: đã đặt recovery model = SIMPLE.';

    ALTER DATABASE NGANHANG_TD SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT N'>>> NGANHANG_TD: đã bật snapshot isolation.';
END
GO

-- Thiết lập recovery model và snapshot isolation cho TraCuu.
IF @@SERVERNAME LIKE N'%\SQLSERVER4' AND DB_ID(N'NGANHANG_TRACUU') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_TRACUU SET RECOVERY SIMPLE;
    PRINT N'>>> NGANHANG_TRACUU: đã đặt recovery model = SIMPLE.';

    ALTER DATABASE NGANHANG_TRACUU SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT N'>>> NGANHANG_TRACUU: đã bật snapshot isolation.';
END
GO

-- Kiểm tra nhanh các database subscriber trên instance hiện tại.
PRINT N'';
PRINT N'--- Danh sách database subscriber trên instance hiện tại ---';
SELECT
    name            AS DatabaseName,
    state_desc      AS [State],
    recovery_model_desc AS RecoveryModel,
    snapshot_isolation_state_desc AS SnapshotIsolation,
    create_date     AS Created
FROM sys.databases
WHERE name IN (N'NGANHANG_BT', N'NGANHANG_TD', N'NGANHANG_TRACUU')
ORDER BY name;
GO

PRINT N'';
PRINT N'--- Hoàn tất 07_subscribers_create_db.sql ---';
PRINT N'Server: ' + @@SERVERNAME;
PRINT N'';
PRINT N'Bước tiếp theo:';
PRINT N'  1. Quay lại Publisher để kiểm tra subscription đã tạo theo SSMS UI runbook.';
PRINT N'  2. Chờ Snapshot Agent hoàn tất (~1-5 phút).';
PRINT N'  3. Kiểm tra bảng đã được đẩy xuống subscriber:';
PRINT N'       USE <subscriber_db> ;';
PRINT N'       SELECT name FROM sys.tables ORDER BY name;';
PRINT N'  4. (Legacy optional) nếu cần tham chiếu fixup cũ: sql/archive/08_subscribers_post_replication_fixups.sql';
GO
