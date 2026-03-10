/*=============================================================================
  07_subscribers_create_db.sql
  Vai trò   : Các máy chủ đăng ký nhận (CN1, CN2, TraCuu)
  Chạy trên : Từng máy chủ đăng ký nhận riêng lẻ qua sqlcmd:

    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"

  Mục đích: Tạo các cơ sở dữ liệu rỗng trên máy chủ đăng ký nhận và các điều kiện
            tiên quyết tối thiểu cho Sao chép hợp nhất.

           *** KHÔNG tạo bảng, SP, hoặc view ở đây. ***
           Tác vụ snapshot đẩy lược đồ + dữ liệu ban đầu từ
           Máy chủ phát hành.  Script này chỉ tạo:
             1. Vùng chứa cơ sở dữ liệu (rỗng)
             2. Thiết lập mô hình phục hồi
             3. Tùy chọn cơ sở dữ liệu tương thích sao chép hợp nhất

           Sau script này:
             - Quay lại Máy chủ phát hành → 05_replication_setup_merge.sql
               (đăng ký nhận + Tác vụ snapshot nếu chưa thực hiện)
             - Chờ Tác vụ snapshot hoàn tất (~1-5 phút dữ liệu dev)
             - Sau đó chạy 08_subscribers_post_replication_fixups.sql

  Chiến lược:
    Script tự nhận diện máy chủ đang chạy qua
    @@SERVERNAME và chỉ tạo cơ sở dữ liệu phù hợp cho máy chủ
    đó.  Điều này cho phép CÙNG MỘT script được sử dụng trên cả 3
    máy chủ đăng ký nhận mà không cần chỉnh sửa thủ công.

  Kiến trúc mạng:
    SQLSERVER2 → NGANHANG_BT     (Chi nhánh Bến Thành)
    SQLSERVER3 → NGANHANG_TD     (Chi nhánh Tân Định)
    SQLSERVER4 → NGANHANG_TRACUU (Tra cứu — chỉ đọc)

  Bất biến lũy đẳng: CÓ — được bảo vệ bởi IF DB_ID(…) IS NULL + IF NOT EXISTS.
  THỨ TỰ THỰC THI: Bước 7/8.
    Chạy trên từng máy chủ đăng ký nhận TRƯỚC KHI Tác vụ snapshot áp dụng lược đồ.
=============================================================================*/

USE master;
GO

PRINT '══════════════════════════════════════════════════════';
PRINT ' Subscriber DB Shell Creator';
PRINT ' Server: ' + @@SERVERNAME;
PRINT '══════════════════════════════════════════════════════';
PRINT '';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — Tự nhận diện máy chủ và tạo cơ sở dữ liệu phù hợp
   Chỉ một cơ sở dữ liệu được tạo cho mỗi máy chủ, dựa trên @@SERVERNAME.
   ═══════════════════════════════════════════════════════════════════════════════ */

DECLARE @instance nvarchar(256) = @@SERVERNAME;
DECLARE @dbName   nvarchar(128) = NULL;
DECLARE @label    nvarchar(50)  = NULL;

IF @instance LIKE N'%\SQLSERVER2'
BEGIN
    SET @dbName = N'NGANHANG_BT';
    SET @label  = N'CN1 — Bến Thành';
END
ELSE IF @instance LIKE N'%\SQLSERVER3'
BEGIN
    SET @dbName = N'NGANHANG_TD';
    SET @label  = N'CN2 — Tân Định';
END
ELSE IF @instance LIKE N'%\SQLSERVER4'
BEGIN
    SET @dbName = N'NGANHANG_TRACUU';
    SET @label  = N'TraCuu — Read-only';
END
ELSE
BEGIN
    PRINT 'WARNING: Unrecognized instance [' + @instance + '].';
    PRINT '         Expected: SQLSERVER2, SQLSERVER3, or SQLSERVER4.';
    PRINT '         This script is for subscriber instances only.';
    PRINT '         Skipping all operations.';
END

IF @dbName IS NOT NULL
BEGIN
    PRINT '>>> Instance: ' + @instance;
    PRINT '>>> Target DB: ' + @dbName + ' (' + @label + ')';
END
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — Tạo cơ sở dữ liệu (nếu chưa tồn tại)
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── NGANHANG_BT (SQLSERVER2) ─────────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER2'
BEGIN
    IF DB_ID(N'NGANHANG_BT') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_BT;
        PRINT '>>> Database NGANHANG_BT created.';
    END
    ELSE
        PRINT '>>> Database NGANHANG_BT already exists — skipped.';
END
GO

-- ── NGANHANG_TD (SQLSERVER3) ─────────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER3'
BEGIN
    IF DB_ID(N'NGANHANG_TD') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_TD;
        PRINT '>>> Database NGANHANG_TD created.';
    END
    ELSE
        PRINT '>>> Database NGANHANG_TD already exists — skipped.';
END
GO

-- ── NGANHANG_TRACUU (SQLSERVER4) ─────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER4'
BEGIN
    IF DB_ID(N'NGANHANG_TRACUU') IS NULL
    BEGIN
        CREATE DATABASE NGANHANG_TRACUU;
        PRINT '>>> Database NGANHANG_TRACUU created.';
    END
    ELSE
        PRINT '>>> Database NGANHANG_TRACUU already exists — skipped.';
END
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — Tùy chọn cơ sở dữ liệu
   Thiết lập mô hình phục hồi và các tùy chọn tương thích sao chép hợp nhất.

   Mô hình phục hồi SIMPLE là chấp nhận được cho máy chủ đăng ký nhận trong đồ án.
   Tác vụ hợp nhất sử dụng xác thực Windows (subscriber_security_mode = 1 trong
   05_replication_setup_merge.sql) nên không cần đăng nhập đặc biệt cho
   tác vụ — tài khoản dịch vụ SQL Server Agent đã có quyền truy cập.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── Tùy chọn NGANHANG_BT ────────────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER2' AND DB_ID(N'NGANHANG_BT') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_BT SET RECOVERY SIMPLE;
    PRINT '>>> NGANHANG_BT: Recovery model set to SIMPLE.';

    -- Cho phép cách ly snapshot để tương thích phát hiện xung đột sao chép hợp nhất
    ALTER DATABASE NGANHANG_BT SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT '>>> NGANHANG_BT: Snapshot isolation enabled.';
END
GO

-- ── Tùy chọn NGANHANG_TD ────────────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER3' AND DB_ID(N'NGANHANG_TD') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_TD SET RECOVERY SIMPLE;
    PRINT '>>> NGANHANG_TD: Recovery model set to SIMPLE.';

    ALTER DATABASE NGANHANG_TD SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT '>>> NGANHANG_TD: Snapshot isolation enabled.';
END
GO

-- ── Tùy chọn NGANHANG_TRACUU ────────────────────────────────────────────────
IF @@SERVERNAME LIKE N'%\SQLSERVER4' AND DB_ID(N'NGANHANG_TRACUU') IS NOT NULL
BEGIN
    ALTER DATABASE NGANHANG_TRACUU SET RECOVERY SIMPLE;
    PRINT '>>> NGANHANG_TRACUU: Recovery model set to SIMPLE.';

    ALTER DATABASE NGANHANG_TRACUU SET ALLOW_SNAPSHOT_ISOLATION ON;
    PRINT '>>> NGANHANG_TRACUU: Snapshot isolation enabled.';

    -- TraCuu là chỉ đọc ở tầng ứng dụng, nhưng Tác vụ hợp nhất vẫn cần
    -- quyền ghi để áp dụng snapshot + duy trì các bảng siêu dữ liệu hợp nhất.
    -- KHÔNG đặt READ_ONLY ở đây — điều đó sẽ chặn Tác vụ snapshot.
    -- Việc hạn chế chỉ đọc được thực hiện ở cấp VAI TRÒ trong script 08.
END
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — Xác minh
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '─── Databases on this instance ─────────────────────────';

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

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' 07_subscribers_create_db.sql completed';
PRINT ' Server: ' + @@SERVERNAME;
PRINT '';
PRINT ' NEXT STEPS:';
PRINT '   1. Return to Publisher and ensure subscriptions are set up';
PRINT '      (Part D of 05_replication_setup_merge.sql)';
PRINT '   2. Start/wait for Snapshot Agent to complete (~1-5 min)';
PRINT '   3. Verify tables exist:';
PRINT '        USE <subscriber_db>;';
PRINT '        SELECT name FROM sys.tables ORDER BY name;';
PRINT '   4. Run 08_subscribers_post_replication_fixups.sql on this';
PRINT '      instance for roles, permissions, and security SPs.';
PRINT '══════════════════════════════════════════════════════';
GO
