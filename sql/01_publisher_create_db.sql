/*=============================================================================
  01_publisher_create_db.sql
  Vai trò   : Máy chủ phát hành / Điều phối (server gốc)
  Chạy trên : DESKTOP-JBB41QU  (phiên bản SQL Server mặc định)
  Mục đích: Tạo cơ sở dữ liệu Máy chủ phát hành NGANHANG_PUB nếu chưa tồn tại,
           đặt mô hình phục hồi thành FULL (bắt buộc bởi Sao chép hợp nhất (Merge Replication)), và
           bật khả năng phát hành hợp nhất cho cơ sở dữ liệu.

  Bất biến lũy đẳng: CÓ — tất cả thao tác được bảo vệ bởi kiểm tra IF.

  THỨ TỰ THỰC THI: Bước 1/8 (chạy ĐẦU TIÊN trên Máy chủ phát hành).
=============================================================================*/

USE master;
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- PHẦN 1. TẠO CƠ SỞ DỮ LIỆU
-- ═══════════════════════════════════════════════════════════════════════════════
IF DB_ID(N'NGANHANG_PUB') IS NULL
BEGIN
    CREATE DATABASE NGANHANG_PUB;
    PRINT '>>> Database NGANHANG_PUB created.';
END
ELSE
    PRINT '>>> Database NGANHANG_PUB already exists — skipped.';
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- PHẦN 2. MÔ HÌNH PHỤC HỒI = FULL
--    Sao chép hợp nhất (Merge Replication) yêu cầu cơ sở dữ liệu ấn phẩm sử dụng phục hồi FULL
--    để Log Reader Agent có thể theo dõi thay đổi một cách đáng tin cậy.
-- ═══════════════════════════════════════════════════════════════════════════════
IF EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = N'NGANHANG_PUB' AND recovery_model_desc <> N'FULL'
)
BEGIN
    ALTER DATABASE NGANHANG_PUB SET RECOVERY FULL;
    PRINT '>>> Recovery model set to FULL.';
END
ELSE
    PRINT '>>> Recovery model is already FULL — skipped.';
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- PHẦN 3. BẬT PHÁT HÀNH HỢP NHẤT (NẾU DISTRIBUTOR ĐÃ SẴN SÀNG)
--    Đánh dấu NGANHANG_PUB để sp_addmergepublication có thể được gọi sau
--    (trong 05_replication_setup_merge.sql).
--
--    Lưu ý: Nếu Distributor chưa được cài (thường đến Phần A của script 05),
--    bước này sẽ được bỏ qua thay vì ném lỗi Msg 20028.
-- ═══════════════════════════════════════════════════════════════════════════════
USE master;
GO

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

        PRINT '>>> Merge publish option enabled on NGANHANG_PUB.';
    END TRY
    BEGIN CATCH
        PRINT '>>> WARNING: Could not enable merge publish in step 1. Will retry in step 5.';
        PRINT '>>> SQL Error: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE
BEGIN
    PRINT '>>> Distributor is not configured yet — skip enabling merge publish in step 1.';
    PRINT '>>> Run 05_replication_setup_merge.sql (Part A/B) to install Distributor and enable publication.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════════
-- PHẦN 4. XÁC MINH
-- ═══════════════════════════════════════════════════════════════════════════════
USE master;
GO

SELECT
    name                    AS DatabaseName,
    state_desc              AS [State],
    recovery_model_desc     AS RecoveryModel,
    is_merge_published      AS MergePublished,
    create_date             AS CreatedAt
FROM sys.databases
WHERE name = N'NGANHANG_PUB';
GO

PRINT '=== 01_publisher_create_db.sql completed successfully ===';
GO
