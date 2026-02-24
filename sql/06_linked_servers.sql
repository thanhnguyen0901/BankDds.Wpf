/*=============================================================================
  06_linked_servers.sql
  Vai trò: Cấu hình Linked Server (đặt tên tuân thủ quy tắc ngân hàng)
  Mục đích: Tạo linked server liên thể hiện cần thiết cho:
             • SP chuyển khoản liên chi nhánh trên CN1/CN2
             • Truy vấn báo cáo tổng hợp/tra cứu trên Máy chủ phát hành
             • Truy cập tra cứu TraCuu từ chi nhánh (LINK0)

  QUY ƯỚC ĐẶT TÊN
  ─────────────────────
  Quy tắc ngân hàng yêu cầu **tên linked server phải giống nhau** trên
  cả hai subscriber chi nhánh để cùng một mã SP chạy không cần sửa đổi.

    ┌──────────────────────────────┬──────────────────────┬──────────────────────────────┐
    │ Chạy trên                    │ Tên Linked Server    │ Trỏ đến                      │
    ├──────────────────────────────┼──────────────────────┼──────────────────────────────┤
    │ CN1 (SQLSERVER2)             │ LINK1                │ CN2 — SQLSERVER3 (TANDINH)   │
    │                              │ LINK0                │ TraCuu — SQLSERVER4          │
    ├──────────────────────────────┼──────────────────────┼──────────────────────────────┤
    │ CN2 (SQLSERVER3)             │ LINK1                │ CN1 — SQLSERVER2 (BENTHANH)  │
    │                              │ LINK0                │ TraCuu — SQLSERVER4          │
    ├──────────────────────────────┼──────────────────────┼──────────────────────────────┤
    │ Máy chủ phát hành (mặc định) │ LINK1                │ CN1 — SQLSERVER2 (BENTHANH)  │
    │                              │ LINK2                │ CN2 — SQLSERVER3 (TANDINH)   │
    │                              │ LINK0                │ TraCuu — SQLSERVER4          │
    └──────────────────────────────┴──────────────────────┴──────────────────────────────┘

    Thuộc tính chính: Trên CN1, LINK1 → "chi nhánh kia" (CN2).
                      Trên CN2, LINK1 → "chi nhánh kia" (CN1).
                      Cùng tên, đích đối xứng = tính di động mã.

  KIẾN TRÚC MẠNG
  ────────
    Máy chủ phát hành (thể hiện mặc định)  : DESKTOP-JBB41QU            → NGANHANG_PUB
    CN1  (LINK1 từ Máy chủ phát hành)    : DESKTOP-JBB41QU\SQLSERVER2 → NGANHANG_BT
    CN2  (LINK2 từ Máy chủ phát hành)    : DESKTOP-JBB41QU\SQLSERVER3 → NGANHANG_TD
    TraCuu (LINK0 ở mọi nơi)             : DESKTOP-JBB41QU\SQLSERVER4 → NGANHANG_TRACUU

  THỰC THI
  ─────────
    Script này có 3 phần. Mỗi phần phải được chạy trên thể hiện tương ứng:

      Phần A → sqlcmd -S "DESKTOP-JBB41QU"            -E -i "06_linked_servers.sql"
      Phần B → sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "06_linked_servers.sql"
      Phần C → sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "06_linked_servers.sql"

    Mỗi phần tự phát hiện thể hiện đang chạy (thông qua @@SERVERNAME)
    và chỉ tạo các liên kết phù hợp cho thể hiện đó.

  BẢO MẬT
  ────────
    Thông tin đăng nhập hiện tại: sa / Password!123  (mặc định phòng thí nghiệm).
    ┌─────────────────────────────────────────────────────────────────────────┐
    │ MÔI TRƯỜNG THỰC TẾ: Thay sa/Password!123 bằng tài khoản SQL chuyên     │
    │ dụng quyền tối thiểu (vd: svc_linkedserver) chỉ có:                    │
    │   • db_datareader trên cơ sở dữ liệu từ xa                              │
    │   • EXECUTE trên các SP cần thiết                                        │
    │ Tạo tài khoản trên tất cả thể hiện trước, sau đó tham chiếu dưới.      │
    └─────────────────────────────────────────────────────────────────────────┘

  TÙY CHỌN KÍCH HOẠT MỖI LIÊN KẾT
  ────────────────────────
    • rpc        = true   (cho phép EXEC trên SP từ xa)
    • rpc out    = true   (cho phép đầu ra thủ tục từ xa)
    • data access = true  (cho phép truy vấn tên 4 phần: [LINK1].[DB].[dbo].T)

  Bất biến lũy đẳng: CÓ — xóa linked server hiện có trước khi tạo lại.
  THỨ TỰ THỰC THI: Bước 6/8.
  Nguồn: Thay thế sql/16-linked-servers.sql (đặt tên cũ SERVER1/SERVER2/SERVER3).
=============================================================================*/

USE master;
GO

/* ═══════════════════════════════════════════════════════════════════════════════
   BIẾN — Thông tin đăng nhập từ xa (thay đổi ở đây, áp dụng cho tất cả các phần)
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Thông tin đăng nhập mặc định dành cho phòng thí nghiệm. Thay thế cho môi trường thực tế.
DECLARE @RemoteUser nvarchar(128) = N'sa';
DECLARE @RemotePass nvarchar(128) = N'Password!123';
-- ↑ TODO-LS-SEC: Thay thế bằng tài khoản svc_linkedserver chuyên dụng.

-- Không thể sử dụng biến qua các đợt GO, nên thông tin đăng nhập được lặp lại
-- bên dưới. Cập nhật TẤT CẢ các lần xuất hiện nếu bạn thay đổi chúng.
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN A — Chạy trên Máy chủ phát hành (DESKTOP-JBB41QU, thể hiện mặc định)
   Tạo: LINK1 → CN1, LINK2 → CN2, LINK0 → TraCuu
   Mục đích: Báo cáo tổng hợp, truy vấn chẩn đoán, giám sát snapshot.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bảo vệ: chỉ chạy phần này trên Máy chủ phát hành (thể hiện mặc định)
IF @@SERVERNAME = N'DESKTOP-JBB41QU'
BEGIN
    PRINT '';
    PRINT '══════════════════════════════════════════════════════';
    PRINT ' Section A: Publisher linked servers';
    PRINT ' Instance: ' + @@SERVERNAME;
    PRINT '══════════════════════════════════════════════════════';

    -- ── LINK1 → CN1 (BENTHANH / SQLSERVER2) ─────────────────────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK1.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',              -- TODO-LS-SEC: svc_linkedserver
        @rmtpassword = N'Password!123';    -- TODO-LS-SEC: thông tin xác thực bảo mật

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT '  LINK1 → SQLSERVER2 (NGANHANG_BT)  OK';

    -- ── LINK2 → CN2 (TANDINH / SQLSERVER3) ──────────────────────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK2')
    BEGIN
        EXEC sp_dropserver @server = N'LINK2', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK2.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK2',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK2',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK2', 'rpc',         'true';
    EXEC sp_serveroption N'LINK2', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK2', 'data access', 'true';
    PRINT '  LINK2 → SQLSERVER3 (NGANHANG_TD)  OK';

    -- ── LINK0 → TraCuu (SQLSERVER4) ─────────────────────────────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK0.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT '  LINK0 → SQLSERVER4 (NGANHANG_TRACUU)  OK';

    PRINT '>>> Section A complete: Publisher linked servers ready.';
END
ELSE
    PRINT 'Section A: Skipped (not Publisher instance).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN B — Chạy trên CN1 (DESKTOP-JBB41QU\SQLSERVER2)
   Tạo: LINK1 → CN2 (chi nhánh kia), LINK0 → TraCuu
   Mục đích: SP chuyển khoản liên chi nhánh, tra cứu TraCuu nếu cần.

   Quy tắc ngân hàng: Tên LINK1 giống nhau trên cả CN1 và CN2.
              Trên CN1, LINK1 trỏ đến CN2.
              Trên CN2, LINK1 trỏ đến CN1.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bảo vệ: chỉ chạy phần này trên CN1
IF @@SERVERNAME = N'DESKTOP-JBB41QU\SQLSERVER2'
BEGIN
    PRINT '';
    PRINT '══════════════════════════════════════════════════════';
    PRINT ' Section B: CN1 (BENTHANH) linked servers';
    PRINT ' Instance: ' + @@SERVERNAME;
    PRINT '══════════════════════════════════════════════════════';

    -- ── LINK1 → CN2 (TANDINH / SQLSERVER3) — chi nhánh kia ──────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK1.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT '  LINK1 → SQLSERVER3 (NGANHANG_TD — other branch)  OK';

    -- ── LINK0 → TraCuu (SQLSERVER4) ─────────────────────────────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK0.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT '  LINK0 → SQLSERVER4 (NGANHANG_TRACUU)  OK';

    PRINT '>>> Section B complete: CN1 linked servers ready.';
END
ELSE
    PRINT 'Section B: Skipped (not CN1 instance).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN C — Chạy trên CN2 (DESKTOP-JBB41QU\SQLSERVER3)
   Tạo: LINK1 → CN1 (chi nhánh kia), LINK0 → TraCuu
   Mục đích: SP chuyển khoản liên chi nhánh, tra cứu TraCuu nếu cần.

   Quy tắc ngân hàng: Bản sao đối xứng của Phần B.
              LINK1 = cùng tên, trỏ đến chi nhánh KHÁC.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bảo vệ: chỉ chạy phần này trên CN2
IF @@SERVERNAME = N'DESKTOP-JBB41QU\SQLSERVER3'
BEGIN
    PRINT '';
    PRINT '══════════════════════════════════════════════════════';
    PRINT ' Section C: CN2 (TANDINH) linked servers';
    PRINT ' Instance: ' + @@SERVERNAME;
    PRINT '══════════════════════════════════════════════════════';

    -- ── LINK1 → CN1 (BENTHANH / SQLSERVER2) — chi nhánh kia ─────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK1.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT '  LINK1 → SQLSERVER2 (NGANHANG_BT — other branch)  OK';

    -- ── LINK0 → TraCuu (SQLSERVER4) ─────────────────────────────────────────
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT '  Dropped existing LINK0.';
    END

    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'Password!123';

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT '  LINK0 → SQLSERVER4 (NGANHANG_TRACUU)  OK';

    PRINT '>>> Section C complete: CN2 linked servers ready.';
END
ELSE
    PRINT 'Section C: Skipped (not CN2 instance).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN D — Xác minh (chạy trên thể hiện bạn vừa cấu hình)
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '─── Linked Servers on ' + @@SERVERNAME + ' ──────────────';
SELECT
    s.name          AS LinkedServerName,
    s.data_source   AS DataSource,
    s.provider      AS Provider,
    s.is_remote_login_enabled  AS RpcEnabled,
    s.is_data_access_enabled   AS DataAccessEnabled
FROM sys.servers s
WHERE s.name IN (N'LINK0', N'LINK1', N'LINK2')
ORDER BY s.name;
GO

PRINT '';
PRINT '=== 06_linked_servers.sql completed on ' + @@SERVERNAME + ' ===';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   XÁC MINH SAU KHI CHẠY — Kiểm tra kết nối
   ═══════════════════════════════════════════════════════════════════════════════

   ── Từ Máy chủ phát hành (DESKTOP-JBB41QU) ─────────────────────────────────────────
   SELECT TOP 1 * FROM [LINK1].[NGANHANG_BT].[dbo].KHACHHANG;     -- → CN1
   SELECT TOP 1 * FROM [LINK2].[NGANHANG_TD].[dbo].KHACHHANG;     -- → CN2
   SELECT TOP 1 * FROM [LINK0].[NGANHANG_TRACUU].[dbo].KHACHHANG; -- → TraCuu

   ── Từ CN1 (SQLSERVER2) ───────────────────────────────────────────────────
   SELECT TOP 1 * FROM [LINK1].[NGANHANG_TD].[dbo].NHANVIEN;      -- → CN2
   SELECT TOP 1 * FROM [LINK0].[NGANHANG_TRACUU].[dbo].KHACHHANG; -- → TraCuu

   ── Từ CN2 (SQLSERVER3) ───────────────────────────────────────────────────
   SELECT TOP 1 * FROM [LINK1].[NGANHANG_BT].[dbo].NHANVIEN;      -- → CN1
   SELECT TOP 1 * FROM [LINK0].[NGANHANG_TRACUU].[dbo].KHACHHANG; -- → TraCuu

   ── Yêu cầu MSDTC ───────────────────────────────────────────────────────
   Nếu SP_CrossBranchTransfer sử dụng giao dịch phân tán qua LINK1:
     Component Services → My Computer → DTC → Local DTC → Security:
       ✓ Truy cập DTC mạng
       ✓ Cho phép máy khách từ xa
       ✓ Cho phép kết nối đến / Cho phép kết nối đi
       ✓ Không yêu cầu xác thực (phòng thí nghiệm)
     Lặp lại trên TẤT CẢ thể hiện. Khởi động lại dịch vụ DTC sau khi thay đổi.

   ── Lộ trình nâng cấp bảo mật ───────────────────────────────────────────────────
   1. Tạo tài khoản svc_linkedserver trên tất cả 4 thể hiện:
        CREATE LOGIN svc_linkedserver WITH PASSWORD = '<strong>';
   2. Cấp quyền tối thiểu trên mỗi DB subscriber:
        USE NGANHANG_BT; -- (hoặc _TD, _TRACUU)
        CREATE USER svc_linkedserver FOR LOGIN svc_linkedserver;
        ALTER ROLE db_datareader ADD MEMBER svc_linkedserver;
        -- + GRANT EXECUTE trên các SP liên chi nhánh cụ thể
   3. Cập nhật @rmtuser / @rmtpassword ở trên và chạy lại script này.
=============================================================================*/
