USE master;
GO
-- Tạo database Publisher nếu chưa tồn tại.
IF DB_ID(N'NGANHANG_PUB') IS NULL
BEGIN
    CREATE DATABASE NGANHANG_PUB;
    PRINT N'>>> Đã tạo database NGANHANG_PUB.';
END
ELSE
    PRINT N'>>> Database NGANHANG_PUB đã tồn tại, bỏ qua.';
GO
-- Đảm bảo recovery model là FULL để phục vụ replication.
IF EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = N'NGANHANG_PUB' AND recovery_model_desc <> N'FULL'
)
BEGIN
    ALTER DATABASE NGANHANG_PUB SET RECOVERY FULL;
    PRINT N'>>> Đã chuyển recovery model sang FULL.';
END
ELSE
    PRINT N'>>> Recovery model đã là FULL, bỏ qua.';
GO
USE master;
GO
-- Chỉ bật merge publish khi Distributor đã được cấu hình.
IF EXISTS (
    SELECT 1
    FROM sys.servers
    WHERE is_distributor = 1
)
AND DB_ID(N'distribution') IS NOT NULL
BEGIN
    BEGIN TRY
        EXEC sp_replicationdboption
            @dbname  = N'NGANHANG_PUB',
            @optname = N'merge publish',
            @value   = N'true';
        PRINT N'>>> Đã bật tùy chọn merge publish cho NGANHANG_PUB.';
    END TRY
    BEGIN CATCH
        PRINT N'>>> Cảnh báo: Không bật được merge publish ở bước 1, sẽ thử lại ở bước 5.';
        PRINT N'>>> Lỗi SQL: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE
BEGIN
    PRINT N'>>> Distributor chưa được cấu hình, bỏ qua bật merge publish ở bước 1.';
    PRINT N'>>> Tiếp theo: cấu hình Replication/Publication bằng SSMS UI theo docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md.';
END
GO
USE master;
GO
-- Kiểm tra nhanh trạng thái database sau khi chạy script.
SELECT
    name                    AS DatabaseName,
    state_desc              AS [State],
    recovery_model_desc     AS RecoveryModel,
    is_merge_published      AS MergePublished,
    create_date             AS CreatedAt
FROM sys.databases
WHERE name = N'NGANHANG_PUB';
GO
PRINT N'=== Hoàn tất 01_publisher_create_db.sql ===';
GO
