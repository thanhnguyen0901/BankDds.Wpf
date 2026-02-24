/*=============================================================================
  05_replication_setup_merge.sql
  Vai trò: Máy chủ phát hành / Máy chủ phân phối (server gốc — DESKTOP-JBB41QU)
  Chạy trên: DESKTOP-JBB41QU  (default instance) — SSMS hoặc sqlcmd
  Mục đích: Cấu hình Sao chép hợp nhất Ngân hàng từ đầu đến cuối.

  ┌────────────────────────────────────────────────────────────────────────────┐
  │                      TÓM TẮT THỨ TỰ CHẠY                                │
  │                                                                           │
  │  Script này có 5 phần. Chạy theo THỨ TỰ trên Máy chủ phát hành:        │
  │                                                                           │
  │  Phần A — Cài đặt Máy chủ phân phối    (USE master)                      │
  │  Phần B — Bật phát hành hợp nhất        (USE master)                      │
  │  Phần C — Tạo 3 Ấn phẩm                (USE NGANHANG_PUB)               │
  │           + Thêm đối tượng phát hành (bảng, SP, view)                     │
  │           + Thêm bộ lọc hàng/kết hợp hợp nhất                            │
  │  Phần D — Tạo 3 Đăng ký nhận đẩy       (USE NGANHANG_PUB)               │
  │  Phần E — Khởi chạy Tác vụ snapshot     (USE NGANHANG_PUB)               │
  │                                                                           │
  │  ĐIỀU KIỆN TIÊN QUYẾT:                                                   │
  │   1. SQL Server Agent đang CHẠY trên máy chủ phát hành                   │
  │   2. Tính năng Sao chép SQL Server đã CÀI ĐẶT (kiểm tra SSMS)          │
  │   3. Các script 01–04 đã chạy (DB + schema + SP + bảo mật tồn tại)      │
  │   4. DB Thuê bao đã tạo qua 07_subscribers_create_db.sql                 │
  │      trên SQLSERVER2, SQLSERVER3, SQLSERVER4                              │
  │                                                                           │
  │  SQL SERVER AGENT — BẮT BUỘC:                                             │
  │   Sao chép hợp nhất phụ thuộc vào SQL Server Agent cho:                  │
  │     • Công việc Tác vụ snapshot (đồng bộ ban đầu)                        │
  │     • Công việc Tác nhân hợp nhất (đồng bộ liên tục)                     │
  │   Để xác minh:                                                            │
  │     PowerShell: Get-Service SQLSERVERAGENT | Select Status                │
  │     SSMS: Object Explorer → SQL Server Agent → (nhấp phải) → Start      │
  └────────────────────────────────────────────────────────────────────────────┘

  Các ấn phẩm:
    ┌─────────────────────┬────────────────────────┬───────────────────────────┐
    │ Ấn phẩm              │ Thuê bao               │ Bộ lọc hàng              │
    ├─────────────────────┼────────────────────────┼───────────────────────────┤
    │ PUB_NGANHANG_BT     │ SQLSERVER2/NGANHANG_BT │ MACN = N'BENTHANH'       │
    │ PUB_NGANHANG_TD     │ SQLSERVER3/NGANHANG_TD │ MACN = N'TANDINH'        │
    │ PUB_TRACUU          │ SQLSERVER4/TRACUU       │ Không lọc (KHACHHANG     │
    │                     │                        │  + chỉ CHINHANH)          │
    └─────────────────────┴────────────────────────┴───────────────────────────┘

  Đối tượng phát hành theo ấn phẩm:
    ┌─────────────────────┬──────────────┬──────────────────────────────────────┐
    │ Loại đối tượng       │ PUB_CN1/CN2  │ PUB_TRACUU                          │
    ├─────────────────────┼──────────────┼──────────────────────────────────────┤
    │ Bảng (dữ liệu)     │ CHINHANH     │ CHINHANH                            │
    │                     │ KHACHHANG    │ KHACHHANG                           │
    │                     │ NHANVIEN     │                                     │
    │                     │ TAIKHOAN     │                                     │
    │                     │ GD_GOIRUT    │                                     │
    │                     │ GD_CHUYENTIEN│                                     │
    ├─────────────────────┼──────────────┼──────────────────────────────────────┤
    │ SP (chỉ schema)     │ 50 SP        │ (không — DB tra cứu chỉ đọc)       │
    ├─────────────────────┼──────────────┼──────────────────────────────────────┤
    │ View (chỉ schema)   │ view_DanhSachPhanManh │ (không)                    │
    └─────────────────────┴──────────────┴──────────────────────────────────────┘

  Chiến lược bộ lọc hàng:
    • CHINHANH: không lọc (cần tất cả chi nhánh để giải quyết FK)
    • KHACHHANG, NHANVIEN, TAIKHOAN: bộ lọc trực tiếp  MACN = N'<branch>'
    • GD_GOIRUT: bộ lọc kết hợp trên TAIKHOAN.SOTK (kế thừa phân vùng MACN)
    • GD_CHUYENTIEN: bộ lọc kết hợp trên TAIKHOAN.SOTK_CHUYEN (tài khoản nguồn)
    • PUB_TRACUU: KHACHHANG không lọc (tất cả khách hàng), CHINHANH không lọc

  Bất biến lũy đẳng: CÓ — tất cả đối tượng được bảo vệ bằng IF NOT EXISTS trên catalog sao chép.
  THỨ TỰ THỰC THI: Bước 5/8 (chỉ Máy chủ phát hành, sau 04_publisher_security.sql).

  Nguồn: Tổng hợp các script đã lên kế hoạch:
    sql/17-replication-distributor.sql  → Phần A
    sql/18-replication-publications.sql → Phần B + C
    sql/19-replication-subscriptions.sql → Phần D
    sql/20-replication-snapshot.sql      → Phần E
=============================================================================*/


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN A — Cài đặt Máy chủ phân phối
   ═══════════════════════════════════════════════════════════════════════════════
   CHẠY TRÊN: Máy chủ phát hành (DESKTOP-JBB41QU) — USE master
   
   Máy chủ phát hành hoạt động như Máy chủ phân phối của chính nó (lab đơn máy).
   CSDL distribution lưu trữ metadata sao chép và được tạo
   tự động bởi sp_adddistributiondb.

   LƯU Ý: Các đường dẫn @data_folder / @log_folder bên dưới giả định cài đặt
   SQL Server 2022 mặc định. Điều chỉnh nếu thư mục dữ liệu MSSQL khác.
   Để tìm đường dẫn:
     SELECT SERVERPROPERTY('InstanceDefaultDataPath');
   ═══════════════════════════════════════════════════════════════════════════════ */

USE master;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part A: Install Distributor on ' + @@SERVERNAME;
PRINT '══════════════════════════════════════════════════════';

-- A1. Kiểm tra xem đã là Máy chủ phân phối chưa
IF NOT EXISTS (
    SELECT 1 FROM sys.servers
    WHERE is_distributor = 1 AND name = @@SERVERNAME
)
BEGIN
    -- A1a. Cấu hình máy chủ này làm Máy chủ phân phối của chính nó
    EXEC sp_adddistributor
        @distributor = @@SERVERNAME,
        @password    = N'Distrib@2026!';

    PRINT '>>> Distributor registered: ' + @@SERVERNAME;
END
ELSE
    PRINT '>>> Distributor already configured — skipped.';
GO

-- A2. Tạo CSDL distribution (nếu chưa tồn tại)
IF DB_ID(N'distribution') IS NULL
BEGIN
    DECLARE @DataPath nvarchar(500);
    SET @DataPath = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(500));

    EXEC sp_adddistributiondb
        @database          = N'distribution',
        @data_folder       = @DataPath,
        @log_folder        = @DataPath,
        @log_file_size     = 2,
        @min_distretention = 0,
        @max_distretention = 72,
        @history_retention = 48,
        @security_mode     = 1;   -- Xác thực Windows

    PRINT '>>> Distribution database created at: ' + @DataPath;
END
ELSE
    PRINT '>>> Distribution database already exists — skipped.';
GO

-- A3. Đăng ký máy chủ này làm máy chủ phát hành phân phối
IF NOT EXISTS (
    SELECT 1 FROM distribution.dbo.MSpublisher_databases
    WHERE publisher_db = N'NGANHANG_PUB'
)
BEGIN
    EXEC sp_adddistpublisher
        @publisher       = @@SERVERNAME,
        @distribution_db = N'distribution',
        @security_mode   = 1,
        @working_directory = N'\\' + REPLACE(@@SERVERNAME, '\', '') + N'\ReplData';
        -- ↑ Chia sẻ UNC cho file snapshot. Mặc định: C:\...\ReplData

    PRINT '>>> Distribution publisher registered.';
END
ELSE
    PRINT '>>> Distribution publisher already registered — skipped.';
GO

PRINT '>>> Part A complete: Distributor installed.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN B — Bật phát hành hợp nhất cho NGANHANG_PUB
   ═══════════════════════════════════════════════════════════════════════════════
   CHẠY TRÊN: Máy chủ phát hành (DESKTOP-JBB41QU) — USE master
   
   Đã thực hiện trong 01_publisher_create_db.sql, nhưng lặp lại ở đây cho an toàn.
   sp_replicationdboption là bất biến lũy đẳng — gọi khi đã bật
   sẽ không có tác dụng.
   ═══════════════════════════════════════════════════════════════════════════════ */

USE master;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part B: Enable merge publish on NGANHANG_PUB';
PRINT '══════════════════════════════════════════════════════';

EXEC sp_replicationdboption
    @dbname  = N'NGANHANG_PUB',
    @optname = N'merge publish',
    @value   = N'true';

PRINT '>>> Merge publish enabled on NGANHANG_PUB.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN C — Tạo Ấn phẩm + Đối tượng phát hành + Bộ lọc hàng/Bộ lọc kết hợp
   ═══════════════════════════════════════════════════════════════════════════════
   CHẠY TRÊN: Máy chủ phát hành (DESKTOP-JBB41QU) — USE NGANHANG_PUB

   Ba ấn phẩm:
     C1. PUB_NGANHANG_BT  — chi nhánh CN1 (BENTHANH)
     C2. PUB_NGANHANG_TD  — chi nhánh CN2 (TANDINH)
     C3. PUB_TRACUU       — tra cứu chỉ đọc cho TraCuu

   Mỗi ấn phẩm chi nhánh bao gồm:
     • 6 đối tượng phát hành bảng (CHINHANH + 5 bảng hoạt động chi nhánh)
     • 50 đối tượng phát hành SP (chỉ schema thủ tục)
     • 1 đối tượng phát hành view (chỉ schema view)
     • Bộ lọc hàng trên MACN cho các bảng lọc trực tiếp
     • Bộ lọc kết hợp cho các bảng giao dịch (GD_GOIRUT, GD_CHUYENTIEN)
   ═══════════════════════════════════════════════════════════════════════════════ */

USE NGANHANG_PUB;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part C: Create Publications + Articles';
PRINT '══════════════════════════════════════════════════════';
GO


/* ─────────────────────────────────────────────────────────────────────────────
   C1. PUB_NGANHANG_BT — Chi nhánh Bến Thành (MACN = 'BENTHANH')
   ───────────────────────────────────────────────────────────────────────────── */

-- C1a. Tạo ấn phẩm
IF NOT EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_NGANHANG_BT')
BEGIN
    EXEC sp_addmergepublication
        @publication                = N'PUB_NGANHANG_BT',
        @description                = N'Merge replication → CN1 BENTHANH (NGANHANG_BT)',
        @sync_mode                  = N'native',
        @retention                  = 14,
        @allow_push                 = N'true',
        @allow_pull                 = N'false',
        @allow_anonymous            = N'false',
        @enabled_for_internet       = N'false',
        @snapshot_in_defaultfolder  = N'true',
        @compress_snapshot          = N'false',
        @conflict_policy            = N'pub wins',
        @centralized_conflicts      = N'true',
        @publication_compatibility_level = N'100RTM';

    PRINT '>>> Publication PUB_NGANHANG_BT created.';
END
ELSE
    PRINT '>>> Publication PUB_NGANHANG_BT already exists — skipped.';
GO

-- C1b. Đặt lịch Tác vụ snapshot (chạy một lần để khởi tạo, sau đó theo yêu cầu)
--      Công việc Tác vụ snapshot được tạo tự động bởi sp_addmergepublication.
--      Cấu hình sử dụng Xác thực Windows (security_mode = 1).
IF EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_NGANHANG_BT')
AND NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE publication_name = N'PUB_NGANHANG_BT'
)
BEGIN
    EXEC sp_addpublication_snapshot
        @publication       = N'PUB_NGANHANG_BT',
        @frequency_type    = 1,    -- 1 = một lần (khởi chạy thủ công)
        @publisher_security_mode = 1;   -- Xác thực Windows

    PRINT '>>> Snapshot Agent configured for PUB_NGANHANG_BT.';
END
GO

-- C1c. Đối tượng phát hành bảng — với bộ lọc hàng
-- CHINHANH: không có bộ lọc hàng (cần tất cả chi nhánh để giải quyết FK trên thuê bao)
IF NOT EXISTS (SELECT 1 FROM sysmergeschemaarticles WHERE name = N'art_BT_CHINHANH'
               UNION ALL
               SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_CHINHANH')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_CHINHANH',
        @source_object          = N'CHINHANH',
        @type                   = N'table',
        @description            = N'Branch reference table (all rows)',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;   -- 0 = cho phép tải lên

    PRINT '  + art_BT_CHINHANH (no filter)';
END
GO

-- KHACHHANG: bộ lọc hàng MACN = 'BENTHANH'
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_KHACHHANG')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_KHACHHANG',
        @source_object          = N'KHACHHANG',
        @type                   = N'table',
        @description            = N'Customers — filtered MACN=BENTHANH',
        @subset_filterclause    = N'MACN = N''BENTHANH''',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;

    PRINT '  + art_BT_KHACHHANG (filter: MACN=BENTHANH)';
END
GO

-- NHANVIEN: bộ lọc hàng MACN = 'BENTHANH'
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_NHANVIEN')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_NHANVIEN',
        @source_object          = N'NHANVIEN',
        @type                   = N'table',
        @description            = N'Employees — filtered MACN=BENTHANH',
        @subset_filterclause    = N'MACN = N''BENTHANH''',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;

    PRINT '  + art_BT_NHANVIEN (filter: MACN=BENTHANH)';
END
GO

-- TAIKHOAN: bộ lọc hàng MACN = 'BENTHANH'
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_TAIKHOAN')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_TAIKHOAN',
        @source_object          = N'TAIKHOAN',
        @type                   = N'table',
        @description            = N'Accounts — filtered MACN=BENTHANH',
        @subset_filterclause    = N'MACN = N''BENTHANH''',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;

    PRINT '  + art_BT_TAIKHOAN (filter: MACN=BENTHANH)';
END
GO

-- GD_GOIRUT: bộ lọc kết hợp qua TAIKHOAN (kế thừa phân vùng MACN)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_GD_GOIRUT')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_GD_GOIRUT',
        @source_object          = N'GD_GOIRUT',
        @type                   = N'table',
        @description            = N'Deposit/Withdrawal txns — join-filtered via TAIKHOAN',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;

    PRINT '  + art_BT_GD_GOIRUT (join filter pending)';
END
GO

-- GD_CHUYENTIEN: bộ lọc kết hợp qua TAIKHOAN (tài khoản nguồn)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_BT_GD_CHUYENTIEN')
BEGIN
    EXEC sp_addmergearticle
        @publication            = N'PUB_NGANHANG_BT',
        @article                = N'art_BT_GD_CHUYENTIEN',
        @source_object          = N'GD_CHUYENTIEN',
        @type                   = N'table',
        @description            = N'Transfer txns — join-filtered via TAIKHOAN(SOTK_CHUYEN)',
        @column_tracking        = N'true',
        @subscriber_upload_options = 0;

    PRINT '  + art_BT_GD_CHUYENTIEN (join filter pending)';
END
GO

-- C1d. Bộ lọc kết hợp cho các bảng giao dịch
-- GD_GOIRUT ← TAIKHOAN (qua SOTK)
IF NOT EXISTS (
    SELECT 1 FROM sysmergesubsetfilters
    WHERE filtername = N'JF_BT_GOIRUT_TAIKHOAN'
)
BEGIN
    EXEC sp_addmergefilter
        @publication        = N'PUB_NGANHANG_BT',
        @article            = N'art_BT_GD_GOIRUT',
        @filtername          = N'JF_BT_GOIRUT_TAIKHOAN',
        @join_articlename    = N'art_BT_TAIKHOAN',
        @join_filterclause   = N'GD_GOIRUT.SOTK = TAIKHOAN.SOTK',
        @join_unique_key     = 1,
        @filter_type         = 1;    -- 1 = bộ lọc kết hợp

    PRINT '  + Join filter: GD_GOIRUT → TAIKHOAN (SOTK)';
END
GO

-- GD_CHUYENTIEN ← TAIKHOAN (qua SOTK_CHUYEN)
IF NOT EXISTS (
    SELECT 1 FROM sysmergesubsetfilters
    WHERE filtername = N'JF_BT_CHUYENTIEN_TAIKHOAN'
)
BEGIN
    EXEC sp_addmergefilter
        @publication        = N'PUB_NGANHANG_BT',
        @article            = N'art_BT_GD_CHUYENTIEN',
        @filtername          = N'JF_BT_CHUYENTIEN_TAIKHOAN',
        @join_articlename    = N'art_BT_TAIKHOAN',
        @join_filterclause   = N'GD_CHUYENTIEN.SOTK_CHUYEN = TAIKHOAN.SOTK',
        @join_unique_key     = 1,
        @filter_type         = 1;

    PRINT '  + Join filter: GD_CHUYENTIEN → TAIKHOAN (SOTK_CHUYEN)';
END
GO

-- C1e. Đối tượng phát hành thủ tục lưu trữ (chỉ schema thủ tục)
--      Sao chép định nghĩa SP đến thuê bao để cùng các
--      SP tồn tại trên cả CSDL Máy chủ phát hành và Thuê bao.
-- SP Khách hàng
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetCustomersByBranch',      @source_object=N'SP_GetCustomersByBranch',      @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetCustomerByCMND',         @source_object=N'SP_GetCustomerByCMND',         @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddCustomer',               @source_object=N'SP_AddCustomer',               @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_UpdateCustomer',            @source_object=N'SP_UpdateCustomer',            @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_DeleteCustomer',            @source_object=N'SP_DeleteCustomer',            @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_RestoreCustomer',           @source_object=N'SP_RestoreCustomer',           @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAllCustomers',           @source_object=N'SP_GetAllCustomers',           @type=N'proc schema only', @description=N'SP: Customer';
GO
-- SP Nhân viên
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetEmployeesByBranch',      @source_object=N'SP_GetEmployeesByBranch',      @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetEmployee',               @source_object=N'SP_GetEmployee',               @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddEmployee',               @source_object=N'SP_AddEmployee',               @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_UpdateEmployee',            @source_object=N'SP_UpdateEmployee',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_DeleteEmployee',            @source_object=N'SP_DeleteEmployee',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_RestoreEmployee',           @source_object=N'SP_RestoreEmployee',           @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_TransferEmployee',          @source_object=N'SP_TransferEmployee',          @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_EmployeeExists',            @source_object=N'SP_EmployeeExists',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAllEmployees',           @source_object=N'SP_GetAllEmployees',           @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetNextManv',               @source_object=N'SP_GetNextManv',               @type=N'proc schema only', @description=N'SP: Employee';
GO
-- SP Tài khoản
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAccountsByBranch',       @source_object=N'SP_GetAccountsByBranch',       @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAccountsByCustomer',     @source_object=N'SP_GetAccountsByCustomer',     @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAccount',                @source_object=N'SP_GetAccount',                @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddAccount',                @source_object=N'SP_AddAccount',                @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_UpdateAccount',             @source_object=N'SP_UpdateAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_DeleteAccount',             @source_object=N'SP_DeleteAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_CloseAccount',              @source_object=N'SP_CloseAccount',              @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_ReopenAccount',             @source_object=N'SP_ReopenAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_DeductFromAccount',         @source_object=N'SP_DeductFromAccount',         @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddToAccount',              @source_object=N'SP_AddToAccount',              @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAllAccounts',            @source_object=N'SP_GetAllAccounts',            @type=N'proc schema only', @description=N'SP: Account';
GO
-- SP Giao dịch
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetTransactionsByAccount',  @source_object=N'SP_GetTransactionsByAccount',  @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetTransactionsByBranch',   @source_object=N'SP_GetTransactionsByBranch',   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetDailyWithdrawalTotal',   @source_object=N'SP_GetDailyWithdrawalTotal',   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetDailyTransferTotal',     @source_object=N'SP_GetDailyTransferTotal',     @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_Deposit',                   @source_object=N'SP_Deposit',                   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_Withdraw',                  @source_object=N'SP_Withdraw',                  @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_CreateTransferTransaction', @source_object=N'SP_CreateTransferTransaction', @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_CrossBranchTransfer',       @source_object=N'SP_CrossBranchTransfer',       @type=N'proc schema only', @description=N'SP: Transaction';
GO
-- SP Báo cáo
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAccountStatement',       @source_object=N'SP_GetAccountStatement',       @type=N'proc schema only', @description=N'SP: Report';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAccountsOpenedInPeriod', @source_object=N'SP_GetAccountsOpenedInPeriod', @type=N'proc schema only', @description=N'SP: Report';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetTransactionSummary',     @source_object=N'SP_GetTransactionSummary',     @type=N'proc schema only', @description=N'SP: Report';
GO
-- SP Xác thực + Chi nhánh
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetUser',                   @source_object=N'SP_GetUser',                   @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetAllUsers',               @source_object=N'SP_GetAllUsers',               @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddUser',                   @source_object=N'SP_AddUser',                   @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_UpdateUser',                @source_object=N'SP_UpdateUser',                @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_SoftDeleteUser',            @source_object=N'SP_SoftDeleteUser',            @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_RestoreUser',               @source_object=N'SP_RestoreUser',               @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetBranches',               @source_object=N'SP_GetBranches',               @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_GetBranch',                 @source_object=N'SP_GetBranch',                 @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_AddBranch',                 @source_object=N'SP_AddBranch',                 @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_UpdateBranch',              @source_object=N'SP_UpdateBranch',               @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_BT', @article=N'SP_DeleteBranch',              @source_object=N'SP_DeleteBranch',              @type=N'proc schema only', @description=N'SP: Branch';
GO

-- C1f. Đối tượng phát hành view (chỉ schema)
EXEC sp_addmergearticle
    @publication    = N'PUB_NGANHANG_BT',
    @article        = N'view_DanhSachPhanManh',
    @source_object  = N'view_DanhSachPhanManh',
    @type           = N'view schema only',
    @description    = N'View: branch partition list (Banking)';
GO

PRINT '>>> PUB_NGANHANG_BT: all articles + filters added.';
GO


/* ─────────────────────────────────────────────────────────────────────────────
   C2. PUB_NGANHANG_TD — Chi nhánh Tân Định (MACN = 'TANDINH')
       Bản sao của C1 với giá trị bộ lọc TANDINH.
   ───────────────────────────────────────────────────────────────────────────── */

-- C2a. Tạo ấn phẩm
IF NOT EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_NGANHANG_TD')
BEGIN
    EXEC sp_addmergepublication
        @publication                = N'PUB_NGANHANG_TD',
        @description                = N'Merge replication → CN2 TANDINH (NGANHANG_TD)',
        @sync_mode                  = N'native',
        @retention                  = 14,
        @allow_push                 = N'true',
        @allow_pull                 = N'false',
        @allow_anonymous            = N'false',
        @enabled_for_internet       = N'false',
        @snapshot_in_defaultfolder  = N'true',
        @compress_snapshot          = N'false',
        @conflict_policy            = N'pub wins',
        @centralized_conflicts      = N'true',
        @publication_compatibility_level = N'100RTM';

    PRINT '>>> Publication PUB_NGANHANG_TD created.';
END
ELSE
    PRINT '>>> Publication PUB_NGANHANG_TD already exists — skipped.';
GO

-- C2b. Tác vụ snapshot
IF EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_NGANHANG_TD')
AND NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE publication_name = N'PUB_NGANHANG_TD'
)
BEGIN
    EXEC sp_addpublication_snapshot
        @publication       = N'PUB_NGANHANG_TD',
        @frequency_type    = 1,
        @publisher_security_mode = 1;

    PRINT '>>> Snapshot Agent configured for PUB_NGANHANG_TD.';
END
GO

-- C2c. Đối tượng phát hành bảng
-- CHINHANH (không lọc)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_CHINHANH')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_CHINHANH',
        @source_object=N'CHINHANH', @type=N'table',
        @description=N'Branch reference table (all rows)',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_CHINHANH (no filter)';
END
GO

-- KHACHHANG (bộ lọc: TANDINH)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_KHACHHANG')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_KHACHHANG',
        @source_object=N'KHACHHANG', @type=N'table',
        @description=N'Customers — filtered MACN=TANDINH',
        @subset_filterclause=N'MACN = N''TANDINH''',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_KHACHHANG (filter: MACN=TANDINH)';
END
GO

-- NHANVIEN (bộ lọc: TANDINH)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_NHANVIEN')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_NHANVIEN',
        @source_object=N'NHANVIEN', @type=N'table',
        @description=N'Employees — filtered MACN=TANDINH',
        @subset_filterclause=N'MACN = N''TANDINH''',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_NHANVIEN (filter: MACN=TANDINH)';
END
GO

-- TAIKHOAN (bộ lọc: TANDINH)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_TAIKHOAN')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_TAIKHOAN',
        @source_object=N'TAIKHOAN', @type=N'table',
        @description=N'Accounts — filtered MACN=TANDINH',
        @subset_filterclause=N'MACN = N''TANDINH''',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_TAIKHOAN (filter: MACN=TANDINH)';
END
GO

-- GD_GOIRUT (bộ lọc kết hợp)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_GD_GOIRUT')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_GD_GOIRUT',
        @source_object=N'GD_GOIRUT', @type=N'table',
        @description=N'Deposit/Withdrawal txns — join-filtered via TAIKHOAN',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_GD_GOIRUT (join filter pending)';
END
GO

-- GD_CHUYENTIEN (bộ lọc kết hợp)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TD_GD_CHUYENTIEN')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_GD_CHUYENTIEN',
        @source_object=N'GD_CHUYENTIEN', @type=N'table',
        @description=N'Transfer txns — join-filtered via TAIKHOAN(SOTK_CHUYEN)',
        @column_tracking=N'true', @subscriber_upload_options=0;
    PRINT '  + art_TD_GD_CHUYENTIEN (join filter pending)';
END
GO

-- C2d. Bộ lọc kết hợp (tương tự C1d)
IF NOT EXISTS (SELECT 1 FROM sysmergesubsetfilters WHERE filtername = N'JF_TD_GOIRUT_TAIKHOAN')
BEGIN
    EXEC sp_addmergefilter
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_GD_GOIRUT',
        @filtername=N'JF_TD_GOIRUT_TAIKHOAN', @join_articlename=N'art_TD_TAIKHOAN',
        @join_filterclause=N'GD_GOIRUT.SOTK = TAIKHOAN.SOTK',
        @join_unique_key=1, @filter_type=1;
    PRINT '  + Join filter: GD_GOIRUT → TAIKHOAN (SOTK)';
END
GO

IF NOT EXISTS (SELECT 1 FROM sysmergesubsetfilters WHERE filtername = N'JF_TD_CHUYENTIEN_TAIKHOAN')
BEGIN
    EXEC sp_addmergefilter
        @publication=N'PUB_NGANHANG_TD', @article=N'art_TD_GD_CHUYENTIEN',
        @filtername=N'JF_TD_CHUYENTIEN_TAIKHOAN', @join_articlename=N'art_TD_TAIKHOAN',
        @join_filterclause=N'GD_CHUYENTIEN.SOTK_CHUYEN = TAIKHOAN.SOTK',
        @join_unique_key=1, @filter_type=1;
    PRINT '  + Join filter: GD_CHUYENTIEN → TAIKHOAN (SOTK_CHUYEN)';
END
GO

-- C2e. Đối tượng phát hành SP (chỉ lược đồ thủ tục) — danh sách giống PUB_NGANHANG_BT
-- SP Khách hàng
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetCustomersByBranch',      @source_object=N'SP_GetCustomersByBranch',      @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetCustomerByCMND',         @source_object=N'SP_GetCustomerByCMND',         @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddCustomer',               @source_object=N'SP_AddCustomer',               @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_UpdateCustomer',            @source_object=N'SP_UpdateCustomer',            @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_DeleteCustomer',            @source_object=N'SP_DeleteCustomer',            @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_RestoreCustomer',           @source_object=N'SP_RestoreCustomer',           @type=N'proc schema only', @description=N'SP: Customer';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAllCustomers',           @source_object=N'SP_GetAllCustomers',           @type=N'proc schema only', @description=N'SP: Customer';
GO
-- SP Nhân viên
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetEmployeesByBranch',      @source_object=N'SP_GetEmployeesByBranch',      @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetEmployee',               @source_object=N'SP_GetEmployee',               @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddEmployee',               @source_object=N'SP_AddEmployee',               @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_UpdateEmployee',            @source_object=N'SP_UpdateEmployee',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_DeleteEmployee',            @source_object=N'SP_DeleteEmployee',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_RestoreEmployee',           @source_object=N'SP_RestoreEmployee',           @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_TransferEmployee',          @source_object=N'SP_TransferEmployee',          @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_EmployeeExists',            @source_object=N'SP_EmployeeExists',            @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAllEmployees',           @source_object=N'SP_GetAllEmployees',           @type=N'proc schema only', @description=N'SP: Employee';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetNextManv',               @source_object=N'SP_GetNextManv',               @type=N'proc schema only', @description=N'SP: Employee';
GO
-- SP Tài khoản
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAccountsByBranch',       @source_object=N'SP_GetAccountsByBranch',       @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAccountsByCustomer',     @source_object=N'SP_GetAccountsByCustomer',     @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAccount',                @source_object=N'SP_GetAccount',                @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddAccount',                @source_object=N'SP_AddAccount',                @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_UpdateAccount',             @source_object=N'SP_UpdateAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_DeleteAccount',             @source_object=N'SP_DeleteAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_CloseAccount',              @source_object=N'SP_CloseAccount',              @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_ReopenAccount',             @source_object=N'SP_ReopenAccount',             @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_DeductFromAccount',         @source_object=N'SP_DeductFromAccount',         @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddToAccount',              @source_object=N'SP_AddToAccount',              @type=N'proc schema only', @description=N'SP: Account';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAllAccounts',            @source_object=N'SP_GetAllAccounts',            @type=N'proc schema only', @description=N'SP: Account';
GO
-- SP Giao dịch
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetTransactionsByAccount',  @source_object=N'SP_GetTransactionsByAccount',  @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetTransactionsByBranch',   @source_object=N'SP_GetTransactionsByBranch',   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetDailyWithdrawalTotal',   @source_object=N'SP_GetDailyWithdrawalTotal',   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetDailyTransferTotal',     @source_object=N'SP_GetDailyTransferTotal',     @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_Deposit',                   @source_object=N'SP_Deposit',                   @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_Withdraw',                  @source_object=N'SP_Withdraw',                  @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_CreateTransferTransaction', @source_object=N'SP_CreateTransferTransaction', @type=N'proc schema only', @description=N'SP: Transaction';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_CrossBranchTransfer',       @source_object=N'SP_CrossBranchTransfer',       @type=N'proc schema only', @description=N'SP: Transaction';
GO
-- SP Báo cáo
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAccountStatement',       @source_object=N'SP_GetAccountStatement',       @type=N'proc schema only', @description=N'SP: Report';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAccountsOpenedInPeriod', @source_object=N'SP_GetAccountsOpenedInPeriod', @type=N'proc schema only', @description=N'SP: Report';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetTransactionSummary',     @source_object=N'SP_GetTransactionSummary',     @type=N'proc schema only', @description=N'SP: Report';
GO
-- SP Xác thực + Chi nhánh
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetUser',                   @source_object=N'SP_GetUser',                   @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetAllUsers',               @source_object=N'SP_GetAllUsers',               @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddUser',                   @source_object=N'SP_AddUser',                   @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_UpdateUser',                @source_object=N'SP_UpdateUser',                @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_SoftDeleteUser',            @source_object=N'SP_SoftDeleteUser',            @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_RestoreUser',               @source_object=N'SP_RestoreUser',               @type=N'proc schema only', @description=N'SP: Auth';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetBranches',               @source_object=N'SP_GetBranches',               @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_GetBranch',                 @source_object=N'SP_GetBranch',                 @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_AddBranch',                 @source_object=N'SP_AddBranch',                 @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_UpdateBranch',              @source_object=N'SP_UpdateBranch',               @type=N'proc schema only', @description=N'SP: Branch';
EXEC sp_addmergearticle @publication=N'PUB_NGANHANG_TD', @article=N'SP_DeleteBranch',              @source_object=N'SP_DeleteBranch',              @type=N'proc schema only', @description=N'SP: Branch';
GO

-- C2f. Đối tượng phát hành View
EXEC sp_addmergearticle
    @publication=N'PUB_NGANHANG_TD', @article=N'view_DanhSachPhanManh',
    @source_object=N'view_DanhSachPhanManh', @type=N'view schema only',
    @description=N'View: branch partition list (Banking)';
GO

PRINT '>>> PUB_NGANHANG_TD: all articles + filters added.';
GO


/* ─────────────────────────────────────────────────────────────────────────────
   C3. PUB_TRACUU — Tra cứu chỉ đọc (chỉ KHACHHANG + CHINHANH)
       Không có SP, không có view — TraCuu là CSDL tra cứu nhẹ.
       Không có bộ lọc hàng trên KHACHHANG — TraCuu nhận TẤT CẢ khách hàng để tra cứu.
   ───────────────────────────────────────────────────────────────────────────── */

-- C3a. Tạo ấn phẩm
IF NOT EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_TRACUU')
BEGIN
    EXEC sp_addmergepublication
        @publication                = N'PUB_TRACUU',
        @description                = N'Merge replication → TraCuu (read-only lookup)',
        @sync_mode                  = N'native',
        @retention                  = 14,
        @allow_push                 = N'true',
        @allow_pull                 = N'false',
        @allow_anonymous            = N'false',
        @enabled_for_internet       = N'false',
        @snapshot_in_defaultfolder  = N'true',
        @compress_snapshot          = N'false',
        @conflict_policy            = N'pub wins',
        @centralized_conflicts      = N'true',
        @publication_compatibility_level = N'100RTM';

    PRINT '>>> Publication PUB_TRACUU created.';
END
ELSE
    PRINT '>>> Publication PUB_TRACUU already exists — skipped.';
GO

-- C3b. Tác vụ Snapshot
IF EXISTS (SELECT 1 FROM sysmergepublications WHERE name = N'PUB_TRACUU')
AND NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE publication_name = N'PUB_TRACUU'
)
BEGIN
    EXEC sp_addpublication_snapshot
        @publication       = N'PUB_TRACUU',
        @frequency_type    = 1,
        @publisher_security_mode = 1;

    PRINT '>>> Snapshot Agent configured for PUB_TRACUU.';
END
GO

-- C3c. Đối tượng phát hành bảng
-- CHINHANH: tất cả hàng (cho FK/hiển thị)
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TC_CHINHANH')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_TRACUU', @article=N'art_TC_CHINHANH',
        @source_object=N'CHINHANH', @type=N'table',
        @description=N'Branch reference table (all rows)',
        @column_tracking=N'true',
        @subscriber_upload_options=2;   -- 2 = chỉ tải xuống (không cho phép thay đổi từ máy đăng ký nhận)

    PRINT '  + art_TC_CHINHANH (download-only, no filter)';
END
GO

-- KHACHHANG: TẤT CẢ khách hàng (không có bộ lọc hàng) — TraCuu dùng để tra cứu liên chi nhánh
-- Bộ lọc cột: loại trừ các hàng TrangThaiXoa = 1 qua subset_filterclause
IF NOT EXISTS (SELECT 1 FROM sysmergearticles WHERE name = N'art_TC_KHACHHANG')
BEGIN
    EXEC sp_addmergearticle
        @publication=N'PUB_TRACUU', @article=N'art_TC_KHACHHANG',
        @source_object=N'KHACHHANG', @type=N'table',
        @description=N'All customers for lookup (active only)',
        @subset_filterclause=N'TrangThaiXoa = 0',
        @column_tracking=N'true',
        @subscriber_upload_options=2;   -- chỉ tải xuống

    PRINT '  + art_TC_KHACHHANG (filter: TrangThaiXoa=0, download-only)';
END
GO

PRINT '>>> PUB_TRACUU: all articles added.';
GO

PRINT '>>> Part C complete: 3 publications with all articles and filters.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN D — Tạo đăng ký nhận đẩy (Push Subscriptions)
   ═══════════════════════════════════════════════════════════════════════════════
   CHẠY TRÊN: Máy chủ phát hành (DESKTOP-JBB41QU) — USE NGANHANG_PUB

   ĐIỀU KIỆN TIÊN QUYẾT:
     • CSDL đăng ký nhận phải đã tồn tại (07_subscribers_create_db.sql)
     • SQL Server Agent phải đang chạy trên Máy chủ phát hành

   Mỗi đăng ký nhận là PUSH — Máy chủ phát hành điều khiển Tác vụ hợp nhất (Merge Agent).
   ═══════════════════════════════════════════════════════════════════════════════ */

USE NGANHANG_PUB;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part D: Create Push Subscriptions';
PRINT '══════════════════════════════════════════════════════';
GO

-- D1. PUB_NGANHANG_BT → CN1 (SQLSERVER2 / NGANHANG_BT)
IF NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE subscriber_server = N'DESKTOP-JBB41QU\SQLSERVER2'
      AND db_name = N'NGANHANG_BT'
)
BEGIN
    EXEC sp_addmergesubscription
        @publication        = N'PUB_NGANHANG_BT',
        @subscriber         = N'DESKTOP-JBB41QU\SQLSERVER2',
        @subscriber_db      = N'NGANHANG_BT',
        @subscription_type  = N'push',
        @sync_type          = N'automatic',
        @subscriber_type    = N'local';

    -- Cấu hình bảo mật Tác vụ hợp nhất (Merge Agent) (Xác thực Windows)
    EXEC sp_addmergepushsubscription_agent
        @publication                = N'PUB_NGANHANG_BT',
        @subscriber                 = N'DESKTOP-JBB41QU\SQLSERVER2',
        @subscriber_db              = N'NGANHANG_BT',
        @subscriber_security_mode   = 1,   -- Xác thực Windows
        @publisher_security_mode    = 1,
        @frequency_type             = 64,  -- 64 = tự khởi động (liên tục)
        @frequency_interval         = 0;

    PRINT '>>> Subscription: PUB_NGANHANG_BT → SQLSERVER2/NGANHANG_BT (push)';
END
ELSE
    PRINT '>>> Subscription to NGANHANG_BT already exists — skipped.';
GO

-- D2. PUB_NGANHANG_TD → CN2 (SQLSERVER3 / NGANHANG_TD)
IF NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE subscriber_server = N'DESKTOP-JBB41QU\SQLSERVER3'
      AND db_name = N'NGANHANG_TD'
)
BEGIN
    EXEC sp_addmergesubscription
        @publication        = N'PUB_NGANHANG_TD',
        @subscriber         = N'DESKTOP-JBB41QU\SQLSERVER3',
        @subscriber_db      = N'NGANHANG_TD',
        @subscription_type  = N'push',
        @sync_type          = N'automatic',
        @subscriber_type    = N'local';

    EXEC sp_addmergepushsubscription_agent
        @publication                = N'PUB_NGANHANG_TD',
        @subscriber                 = N'DESKTOP-JBB41QU\SQLSERVER3',
        @subscriber_db              = N'NGANHANG_TD',
        @subscriber_security_mode   = 1,
        @publisher_security_mode    = 1,
        @frequency_type             = 64,
        @frequency_interval         = 0;

    PRINT '>>> Subscription: PUB_NGANHANG_TD → SQLSERVER3/NGANHANG_TD (push)';
END
ELSE
    PRINT '>>> Subscription to NGANHANG_TD already exists — skipped.';
GO

-- D3. PUB_TRACUU → TraCuu (SQLSERVER4 / NGANHANG_TRACUU)
IF NOT EXISTS (
    SELECT 1 FROM dbo.sysmergesubscriptions
    WHERE subscriber_server = N'DESKTOP-JBB41QU\SQLSERVER4'
      AND db_name = N'NGANHANG_TRACUU'
)
BEGIN
    EXEC sp_addmergesubscription
        @publication        = N'PUB_TRACUU',
        @subscriber         = N'DESKTOP-JBB41QU\SQLSERVER4',
        @subscriber_db      = N'NGANHANG_TRACUU',
        @subscription_type  = N'push',
        @sync_type          = N'automatic',
        @subscriber_type    = N'local';

    EXEC sp_addmergepushsubscription_agent
        @publication                = N'PUB_TRACUU',
        @subscriber                 = N'DESKTOP-JBB41QU\SQLSERVER4',
        @subscriber_db              = N'NGANHANG_TRACUU',
        @subscriber_security_mode   = 1,
        @publisher_security_mode    = 1,
        @frequency_type             = 64,
        @frequency_interval         = 0;

    PRINT '>>> Subscription: PUB_TRACUU → SQLSERVER4/NGANHANG_TRACUU (push)';
END
ELSE
    PRINT '>>> Subscription to NGANHANG_TRACUU already exists — skipped.';
GO

PRINT '>>> Part D complete: 3 push subscriptions created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN E — Khởi động tác vụ Snapshot
   ═══════════════════════════════════════════════════════════════════════════════
   CHẠY TRÊN: Máy chủ phát hành (DESKTOP-JBB41QU) — USE NGANHANG_PUB

   Lệnh này kích hoạt Tác vụ Snapshot để tạo bản chụp dữ liệu ban đầu
   cho mỗi ấn phẩm. Sau đó Tác vụ hợp nhất (Merge Agent) áp dụng nó lên mỗi máy đăng ký nhận.

   QUAN TRỌNG: SQL Server Agent PHẢI đang chạy để các lệnh này thực thi!
   Kiểm tra: Get-Service SQLSERVERAGENT | Select Status

   Quá trình snapshot thường mất 1–5 phút trên bộ dữ liệu thực hành.
   Theo dõi tiến trình qua SSMS: Replication → Replication Monitor → tab Agents.
   ═══════════════════════════════════════════════════════════════════════════════ */

USE NGANHANG_PUB;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part E: Start Snapshot Agents';
PRINT '══════════════════════════════════════════════════════';

-- Khởi động snapshot cho PUB_NGANHANG_BT
EXEC sp_startpublication_snapshot @publication = N'PUB_NGANHANG_BT';
PRINT '>>> Snapshot Agent started for PUB_NGANHANG_BT';
GO

-- Khởi động snapshot cho PUB_NGANHANG_TD
EXEC sp_startpublication_snapshot @publication = N'PUB_NGANHANG_TD';
PRINT '>>> Snapshot Agent started for PUB_NGANHANG_TD';
GO

-- Khởi động snapshot cho PUB_TRACUU
EXEC sp_startpublication_snapshot @publication = N'PUB_TRACUU';
PRINT '>>> Snapshot Agent started for PUB_TRACUU';
GO

PRINT '>>> Part E complete: All Snapshot Agents started.';
PRINT '    Monitor progress in SSMS: Replication → Replication Monitor';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN F — Truy vấn xác minh (chạy sau khi snapshot hoàn tất)
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '══════════════════════════════════════════════════════';
PRINT ' Part F: Verification (run after snapshots complete)';
PRINT '══════════════════════════════════════════════════════';
PRINT '';

-- F1. Liệt kê tất cả ấn phẩm sao chép hợp nhất
PRINT '─── Merge Publications ─────────────────────────────────';
SELECT
    name            AS PublicationName,
    description     AS Description,
    retention       AS RetentionDays,
    status          AS Status
FROM sysmergepublications
ORDER BY name;
GO

-- F2. Liệt kê tất cả đối tượng phát hành sao chép hợp nhất
PRINT '';
PRINT '─── Merge Articles ─────────────────────────────────────';
SELECT
    p.name                  AS Publication,
    a.name                  AS ArticleName,
    a.source_object_name    AS SourceObject,
    a.type                  AS ArticleType,
    ISNULL(a.subset_filterclause, '(none)') AS RowFilter
FROM sysmergearticles a
JOIN sysmergepublications p ON a.pubid = p.pubid
ORDER BY p.name, a.name;
GO

-- F3. Liệt kê tất cả đăng ký nhận sao chép hợp nhất
PRINT '';
PRINT '─── Merge Subscriptions ────────────────────────────────';
SELECT
    subscriber_server   AS SubscriberServer,
    db_name             AS SubscriberDB,
    subscription_type   AS SubType,
    status              AS Status,
    sync_type           AS SyncType
FROM dbo.sysmergesubscriptions
WHERE subscriber_server <> @@SERVERNAME
ORDER BY subscriber_server;
GO

-- F4. Trạng thái tác vụ snapshot (đợi 1–5 phút, sau đó chạy lệnh này)
/*
USE distribution;
SELECT
    a.name           AS AgentName,
    h.start_time     AS StartTime,
    h.runstatus      AS Status,
    h.comments       AS Message
FROM dbo.MSsnapshot_agents a
JOIN dbo.MSsnapshot_history h ON a.id = h.agent_id
ORDER BY h.start_time DESC;
GO
*/

PRINT '';
PRINT '=== 05_replication_setup_merge.sql completed ===';
PRINT '    Publications: PUB_NGANHANG_BT, PUB_NGANHANG_TD, PUB_TRACUU';
PRINT '    Subscriptions: 3 push subscriptions';
PRINT '    Snapshots: Started (monitor in Replication Monitor)';
PRINT '';
PRINT '    NEXT STEPS:';
PRINT '    1. Wait for snapshots to complete (1–5 min)';
PRINT '    2. Run 08_subscribers_post_replication_fixups.sql on each subscriber';
PRINT '    3. Run 06_linked_servers.sql on each instance (optional)';
PRINT '    4. Verify data: SELECT COUNT(*) FROM KHACHHANG on each subscriber';
GO

/*=============================================================================
  XỬ LÝ SỰ CỐ
  ─────────────────────────────────────────────────────────────────────────────
  Lỗi: "SQL Server Agent is not running"
    → Khởi động Agent: SSMS → SQL Server Agent → nhấp chuột phải → Start
    → PowerShell: Start-Service SQLSERVERAGENT

  Lỗi: "Cannot add articles to publication after snapshot has been generated"
    → Chạy: EXEC sp_reinitmergesubscription @publication = N'PUB_NGANHANG_BT';
    → Sau đó: EXEC sp_startpublication_snapshot @publication = N'PUB_NGANHANG_BT';

  Lỗi: "Subscriber database does not exist"
    → Chạy 07_subscribers_create_db.sql trên instance đăng ký nhận trước.

  Lỗi: "The process could not connect to Subscriber"
    → Kiểm tra instance đăng ký nhận đang chạy và có thể kết nối:
      Test-NetConnection DESKTOP-JBB41QU -Port 1433
    → Kiểm tra dịch vụ SQL Server Browser đang chạy (cho các instance có tên)

  Lỗi: "The distribution agent failed" / xung đột hợp nhất
    → Kiểm tra Replication Monitor → nhấp chuột phải agent → View Details
    → Chính sách xung đột là 'pub wins' — Máy chủ phát hành luôn thắng xung đột

  Để XÓA toàn bộ sao chép và bắt đầu lại:
    -- Trên Máy chủ phát hành:
    EXEC sp_removedbreplication @dbname = N'NGANHANG_PUB';
    -- Sau đó xóa phân phối:
    EXEC sp_dropdistpublisher @@SERVERNAME;
    EXEC sp_dropdistributiondb N'distribution';
    EXEC sp_dropdistributor;
    -- Chạy lại script này từ Phần A.
=============================================================================*/
