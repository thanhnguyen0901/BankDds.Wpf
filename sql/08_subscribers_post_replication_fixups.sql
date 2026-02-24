/*=============================================================================
  08_subscribers_post_replication_fixups.sql
  Vai trò: Các máy chủ đăng ký nhận (CN1, CN2, TraCuu) — chạy SAU KHI Snapshot được áp dụng
  Chạy trên: Từng máy chủ đăng ký nhận riêng biệt qua sqlcmd:

    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"
    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"
    sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"

  Mục đích: Các hiệu chỉnh sau snapshot mà Tác vụ snapshot KHÔNG mang theo:
    1. Tạo vai trò cơ sở dữ liệu (NGANHANG, CHINHANH, KHACHHANG) — sao chép
       KHÔNG sao chép vai trò hoặc quyền.
    2. DENY truy cập trực tiếp bảng theo vai trò.
    3. GRANT EXECUTE trên các SP đã sao chép theo vai trò (cùng ma trận như Máy chủ phát hành).
    4. Tạo các SP bảo mật (sp_DangNhap, sp_TaoTaiKhoan) cục bộ trên từng
       máy chủ đăng ký nhận — các SP này KHÔNG phải là bài viết "proc schema only" vì chúng sử dụng
       các thao tác cấp server (sp_addlogin) dành riêng cho từng instance.
    5. Ánh xạ các đăng nhập SQL hiện có vào cơ sở dữ liệu đăng ký nhận (đồng bộ đăng nhập).
    6. Xóa các view liên chi nhánh không có ý nghĩa trên máy chủ đăng ký nhận.
    7. Gia cố bảo mật chỉ đọc dành riêng cho TraCuu.

  Chiến lược:
    Sao chép hợp nhất sao chép lược đồ SP (CREATE PROCEDURE) nhưng KHÔNG:
      • Vai trò cơ sở dữ liệu
      • Các lệnh GRANT/DENY
      • Đăng nhập cấp server
      • Ánh xạ đăng nhập-đến-người dùng
    Do đó script này phải tạo lại lớp bảo mật trên mỗi máy chủ đăng ký nhận.

  QUAN TRỌNG: Script này tự động phát hiện cơ sở dữ liệu đăng ký nhận đang chạy trên
  (NGANHANG_BT, NGANHANG_TD, hoặc NGANHANG_TRACUU) bằng cách kiểm tra DB_NAME().
  Chạy trên TẤT CẢ các máy chủ đăng ký nhận — script tự động thích ứng.

  Bất biến lũy đẳng: CÓ — tất cả đối tượng được bảo vệ bởi IF NOT EXISTS / CREATE OR ALTER.
  THỨ TỰ THỰC THI: Bước 8/8 (cuối cùng; chạy sau khi Tác vụ snapshot hoàn tất).
=============================================================================*/


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 0 — Tự động phát hiện cơ sở dữ liệu đăng ký nhận
   Đặt ngữ cảnh cơ sở dữ liệu dựa trên cơ sở dữ liệu nào tồn tại trên instance này.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Thử NGANHANG_BT trước (CN1), sau đó NGANHANG_TD (CN2), rồi NGANHANG_TRACUU
IF DB_ID('NGANHANG_BT') IS NOT NULL
    USE NGANHANG_BT;
ELSE IF DB_ID('NGANHANG_TD') IS NOT NULL
    USE NGANHANG_TD;
ELSE IF DB_ID('NGANHANG_TRACUU') IS NOT NULL
    USE NGANHANG_TRACUU;
ELSE
    PRINT 'WARNING: No subscriber database found (NGANHANG_BT/TD/TRACUU). Wrong instance?';
GO

PRINT '══════════════════════════════════════════════════════';
PRINT ' Subscriber Post-Replication Fixups';
PRINT ' Database: ' + DB_NAME();
PRINT ' Server:   ' + @@SERVERNAME;
PRINT '══════════════════════════════════════════════════════';
PRINT '';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — Tạo vai trò cơ sở dữ liệu
   Sao chép chính xác các vai trò của Máy chủ phát hành để GRANT EXECUTE hoạt động.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NGANHANG' AND type = 'R')
BEGIN
    CREATE ROLE NGANHANG;
    PRINT '>>> Role NGANHANG created on ' + DB_NAME();
END
ELSE
    PRINT '>>> Role NGANHANG already exists on ' + DB_NAME();
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'CHINHANH' AND type = 'R')
BEGIN
    CREATE ROLE CHINHANH;
    PRINT '>>> Role CHINHANH created on ' + DB_NAME();
END
ELSE
    PRINT '>>> Role CHINHANH already exists on ' + DB_NAME();
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KHACHHANG' AND type = 'R')
BEGIN
    CREATE ROLE KHACHHANG;
    PRINT '>>> Role KHACHHANG created on ' + DB_NAME();
END
ELSE
    PRINT '>>> Role KHACHHANG already exists on ' + DB_NAME();
GO

PRINT '>>> Section 1: Database roles created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — DENY truy cập trực tiếp bảng/view
   Cùng mẫu như Máy chủ phát hành: mọi truy cập dữ liệu chỉ qua SP.
   Chỉ deny các bảng tồn tại trên máy chủ đăng ký nhận này (bảo vệ bằng OBJECT_ID).
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bảng cơ sở (tất cả máy chủ đăng ký nhận có CHINHANH + KHACHHANG tối thiểu)
IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;

-- Máy chủ đăng ký nhận chi nhánh (CN1/CN2) có thêm các bảng này
IF OBJECT_ID('dbo.NHANVIEN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NHANVIEN TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.TAIKHOAN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.TAIKHOAN TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.GD_GOIRUT', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_GOIRUT TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.GD_CHUYENTIEN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_CHUYENTIEN TO NGANHANG, CHINHANH, KHACHHANG;
GO

PRINT '>>> Section 2: DENY direct table access applied.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — GRANT EXECUTE trên các SP đã sao chép
   Sao chép mang theo lược đồ SP nhưng KHÔNG mang theo các lệnh GRANT.
   Chúng ta phải áp dụng lại cùng ma trận quyền như Máy chủ phát hành.

   Vì không phải tất cả SP đều tồn tại trên mọi máy chủ đăng ký nhận (ví dụ: TraCuu có ít hơn),
   mỗi GRANT được bảo vệ bằng kiểm tra OBJECT_ID qua SQL động.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Trợ giúp: cấp EXECUTE an toàn trên SP nếu nó tồn tại trên máy chủ đăng ký nhận này
IF OBJECT_ID('dbo.sp_SafeGrantExec', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SafeGrantExec;
GO
CREATE PROCEDURE dbo.sp_SafeGrantExec
    @SPName   nvarchar(200),
    @RoleName nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    IF OBJECT_ID(@SPName, 'P') IS NOT NULL
    BEGIN
        DECLARE @sql nvarchar(500) = N'GRANT EXECUTE ON ' + @SPName + N' TO ' + QUOTENAME(@RoleName);
        EXEC sp_executesql @sql;
    END
END
GO

-- ── 3A. Cấp quyền NGANHANG (tất cả SP nghiệp vụ) ───────────────────────────────────
-- Khách hàng
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomersByBranch',      'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomerByCMND',         'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddCustomer',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateCustomer',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteCustomer',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreCustomer',           'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllCustomers',           'NGANHANG';
-- Nhân viên
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetEmployeesByBranch',      'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetEmployee',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddEmployee',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateEmployee',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteEmployee',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreEmployee',           'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_TransferEmployee',          'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_EmployeeExists',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllEmployees',           'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetNextManv',               'NGANHANG';
-- Tài khoản
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByBranch',       'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByCustomer',     'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccount',                'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddAccount',                'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateAccount',             'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteAccount',             'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CloseAccount',              'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_ReopenAccount',             'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeductFromAccount',         'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddToAccount',              'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllAccounts',            'NGANHANG';
-- Giao dịch
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByAccount',  'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByBranch',   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyWithdrawalTotal',   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyTransferTotal',     'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Deposit',                   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Withdraw',                  'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CreateTransferTransaction', 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CrossBranchTransfer',       'NGANHANG';
-- Báo cáo
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountStatement',       'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsOpenedInPeriod', 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionSummary',     'NGANHANG';
-- Xác thực + Chi nhánh
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetUser',                   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllUsers',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddUser',                   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateUser',                'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_SoftDeleteUser',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreUser',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddBranch',                 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateBranch',              'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteBranch',              'NGANHANG';
GO

-- ── 3B. Cấp quyền CHINHANH ────────────────────────────────────────────────────────
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomersByBranch',      'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomerByCMND',         'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddCustomer',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateCustomer',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteCustomer',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreCustomer',           'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetEmployeesByBranch',      'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetEmployee',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddEmployee',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateEmployee',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteEmployee',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreEmployee',           'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_EmployeeExists',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetNextManv',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByBranch',       'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByCustomer',     'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccount',                'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddAccount',                'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateAccount',             'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteAccount',             'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CloseAccount',              'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_ReopenAccount',             'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeductFromAccount',         'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddToAccount',              'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByAccount',  'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByBranch',   'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyWithdrawalTotal',   'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyTransferTotal',     'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Deposit',                   'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Withdraw',                  'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CreateTransferTransaction', 'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CrossBranchTransfer',       'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountStatement',       'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsOpenedInPeriod', 'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionSummary',     'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetUser',                   'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllUsers',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddUser',                   'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateUser',                'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_SoftDeleteUser',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'CHINHANH';
GO

-- ── 3C. Cấp quyền KHACHHANG (tối thiểu) ────────────────────────────────────────
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByCustomer',     'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccount',                'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByAccount',  'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountStatement',       'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'KHACHHANG';
GO

-- Xóa SP trợ giúp (chỉ cần cho các lệnh cấp quyền có điều kiện)
DROP PROCEDURE dbo.sp_SafeGrantExec;
GO

PRINT '>>> Section 3: GRANT EXECUTE on SPs applied.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — Tạo các SP bảo mật trên máy chủ đăng ký nhận
   sp_DangNhap — Bộ phân giải đăng nhập ngân hàng (giống Máy chủ phát hành)
   sp_TaoTaiKhoan — Bộ bọc tạo tài khoản (giống Máy chủ phát hành)
   sp_XoaTaiKhoan — Xóa tài khoản (giống Máy chủ phát hành)
   sp_DoiMatKhau  — Đổi mật khẩu (giống Máy chủ phát hành)

   Các SP này KHÔNG được sao chép qua "proc schema only" vì chúng sử dụng
   các thao tác cấp server (sp_addlogin, sp_droplogin, sp_password) dành riêng
   cho từng instance. Chúng phải tồn tại cục bộ trên mỗi máy chủ đăng ký nhận.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── sp_DangNhap ──────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.sp_DangNhap
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MANV     nvarchar(50)  = SYSTEM_USER;
    DECLARE @HOTEN    nvarchar(128) = USER_NAME();
    DECLARE @TENNHOM  nvarchar(128) = NULL;

    -- Ưu tiên: NGANHANG > CHINHANH > KHACHHANG
    SELECT TOP 1 @TENNHOM = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = USER_NAME()
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END ASC;

    IF @TENNHOM IS NULL
    BEGIN
        IF IS_MEMBER('db_owner') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1
            SET @TENNHOM = N'NGANHANG';
        ELSE
            SET @TENNHOM = N'KHACHHANG';
    END

    SELECT @MANV AS MANV, @HOTEN AS HOTEN, @TENNHOM AS TENNHOM;
END
GO

GRANT EXECUTE ON dbo.sp_DangNhap TO PUBLIC;
GO

-- ── sp_TaoTaiKhoan ──────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoan
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128),
    @TENNHOM  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    IF @TENNHOM NOT IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    BEGIN
        RAISERROR(N'Invalid role name: %s. Must be NGANHANG, CHINHANH, or KHACHHANG.', 16, 1, @TENNHOM);
        RETURN;
    END

    -- Phân quyền người gọi
    DECLARE @CallerRole nvarchar(128);
    SELECT TOP 1 @CallerRole = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = USER_NAME()
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY CASE r.name WHEN N'NGANHANG' THEN 1 WHEN N'CHINHANH' THEN 2 WHEN N'KHACHHANG' THEN 3 END ASC;

    IF @CallerRole IS NULL AND (IS_MEMBER('db_owner') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1)
        SET @CallerRole = N'NGANHANG';

    IF @CallerRole IS NULL
    BEGIN
        RAISERROR(N'Caller does not belong to any recognized role.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'CHINHANH' AND @TENNHOM = N'NGANHANG'
    BEGIN
        RAISERROR(N'CHINHANH cannot create NGANHANG logins.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'KHACHHANG cannot create logins.', 16, 1);
        RETURN;
    END

    -- Tạo đăng nhập
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        EXEC sp_addlogin @loginame = @LOGIN, @passwd = @PASS, @defdb = DB_NAME();

    -- Ánh xạ sang người dùng DB
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        EXEC sp_grantdbaccess @loginame = @LOGIN, @name_in_db = @LOGIN;

    -- Thêm vào vai trò
    IF NOT EXISTS (
        SELECT 1 FROM sys.database_role_members rm
        JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
        JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
        WHERE u.name = @LOGIN AND r.name = @TENNHOM
    )
        EXEC sp_addrolemember @rolename = @TENNHOM, @membername = @LOGIN;
END
GO

GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO NGANHANG;
GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO CHINHANH;
GO

-- ── sp_XoaTaiKhoan ──────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.sp_XoaTaiKhoan
    @LOGIN nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Only NGANHANG members can delete login accounts.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        EXEC sp_revokedbaccess @name_in_db = @LOGIN;

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        EXEC sp_droplogin @loginame = @LOGIN;
END
GO

GRANT EXECUTE ON dbo.sp_XoaTaiKhoan TO NGANHANG;
GO

-- ── sp_DoiMatKhau ────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.sp_DoiMatKhau
    @LOGIN    nvarchar(50),
    @PASSCU   nvarchar(128),
    @PASSMOI  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    IF @LOGIN = SYSTEM_USER
    BEGIN
        EXEC sp_password @old = @PASSCU, @new = @PASSMOI, @loginame = @LOGIN;
        RETURN;
    END

    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Only NGANHANG members can reset other users'' passwords.', 16, 1);
        RETURN;
    END

    EXEC sp_password @old = NULL, @new = @PASSMOI, @loginame = @LOGIN;
END
GO

GRANT EXECUTE ON dbo.sp_DoiMatKhau TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO CHINHANH;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO KHACHHANG;
GO

-- ── sp_DanhSachNhanVien ──────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.sp_DanhSachNhanVien
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.name          AS LOGINNAME,
        r.name          AS TENNHOM,
        u.create_date   AS NGAYTAO
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
      AND  u.type IN ('S', 'U')
    ORDER BY r.name ASC, u.name ASC;
END
GO

GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO CHINHANH;
GO

PRINT '>>> Section 4: Security SPs created on subscriber.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — Đồng bộ đăng nhập dữ liệu mẫu từ Máy chủ phát hành
   Các đăng nhập demo phải tồn tại trên mỗi máy chủ đăng ký nhận để người dùng có thể kết nối
   với cùng thông tin xác thực. Đăng nhập SQL Server ở cấp instance nên phải
   được tạo trên mọi instance.

   GHI CHÚ: Mật khẩu phải khớp với dữ liệu mẫu Máy chủ phát hành (04_publisher_security.sql).
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ADMIN_NH → NGANHANG
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ADMIN_NH')
    EXEC sp_addlogin @loginame = N'ADMIN_NH', @passwd = N'Admin@123', @defdb = DB_NAME();
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ADMIN_NH' AND type IN ('S', 'U'))
    EXEC sp_grantdbaccess @loginame = N'ADMIN_NH', @name_in_db = N'ADMIN_NH';
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = N'ADMIN_NH' AND r.name = N'NGANHANG'
)
    EXEC sp_addrolemember @rolename = N'NGANHANG', @membername = N'ADMIN_NH';
GO

-- NV_BT → CHINHANH
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NV_BT')
    EXEC sp_addlogin @loginame = N'NV_BT', @passwd = N'NhanVien@123', @defdb = DB_NAME();
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NV_BT' AND type IN ('S', 'U'))
    EXEC sp_grantdbaccess @loginame = N'NV_BT', @name_in_db = N'NV_BT';
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = N'NV_BT' AND r.name = N'CHINHANH'
)
    EXEC sp_addrolemember @rolename = N'CHINHANH', @membername = N'NV_BT';
GO

-- KH_DEMO → KHACHHANG
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'KH_DEMO')
    EXEC sp_addlogin @loginame = N'KH_DEMO', @passwd = N'KhachHang@123', @defdb = DB_NAME();
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KH_DEMO' AND type IN ('S', 'U'))
    EXEC sp_grantdbaccess @loginame = N'KH_DEMO', @name_in_db = N'KH_DEMO';
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = N'KH_DEMO' AND r.name = N'KHACHHANG'
)
    EXEC sp_addrolemember @rolename = N'KHACHHANG', @membername = N'KH_DEMO';
GO

PRINT '>>> Section 5: Seed logins synced to subscriber.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 6 — Xóa các view chỉ dành cho Máy chủ phát hành trên máy chủ đăng ký nhận
   Các view _ALL đã KHÔNG DÙNG NỮA ở cấp Máy chủ phát hành và không còn
   tồn tại (xem 02_publisher_schema.sql Phần 5). Tuy nhiên, nếu bất kỳ
   bản sao còn sót lại đến qua snapshot cũ hơn, xóa chúng ở đây.
   Cũng xóa view_DanhSachPhanManh — chỉ dành cho Máy chủ phát hành.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF OBJECT_ID('dbo.KHACHHANG_ALL',         'V') IS NOT NULL DROP VIEW dbo.KHACHHANG_ALL;
IF OBJECT_ID('dbo.NHANVIEN_ALL',          'V') IS NOT NULL DROP VIEW dbo.NHANVIEN_ALL;
IF OBJECT_ID('dbo.TAIKHOAN_ALL',          'V') IS NOT NULL DROP VIEW dbo.TAIKHOAN_ALL;
IF OBJECT_ID('dbo.GD_GOIRUT_ALL',         'V') IS NOT NULL DROP VIEW dbo.GD_GOIRUT_ALL;
IF OBJECT_ID('dbo.GD_CHUYENTIEN_ALL',     'V') IS NOT NULL DROP VIEW dbo.GD_CHUYENTIEN_ALL;
IF OBJECT_ID('dbo.view_DanhSachPhanManh',  'V') IS NOT NULL DROP VIEW dbo.view_DanhSachPhanManh;
GO

PRINT '>>> Section 6: Leftover Publisher-only views dropped.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 7 — Gia cố bảo mật dành riêng cho TraCuu
   NGANHANG_TRACUU là máy chủ đăng ký nhận chỉ đọc. Ngay cả đăng nhập nhân viên cũng không
   được phép INSERT/UPDATE/DELETE trên TraCuu.

   Theo mô hình sao chép ngân hàng, NGANHANG_TRACUU nhận các bảng KHACHHANG +
   CHINHANH trực tiếp qua Sao chép hợp nhất. View kế thừa
   V_KHACHHANG_ALL (sử dụng tên bốn phần Linked Server) đã
   không dùng nữa và không còn được tạo. Truy vấn chỉ đọc nên sử dụng
   bảng KHACHHANG đã sao chép trực tiếp (qua SP hoặc GRANT SELECT).

   Phần này chỉ chạy khi DB hiện tại là NGANHANG_TRACUU.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF DB_NAME() = N'NGANHANG_TRACUU'
BEGIN
    PRINT '>>> TraCuu detected — applying read-only hardening.';

    -- DENY mọi thao tác ghi trên tất cả bảng cho tất cả vai trò
    -- (SELECT qua SP vẫn được phép thông qua chuỗi quyền sở hữu)
    IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
        DENY INSERT, UPDATE, DELETE ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;
    IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
        DENY INSERT, UPDATE, DELETE ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;

    -- Xóa V_KHACHHANG_ALL kế thừa nếu nó còn sót lại từ triển khai cũ hơn.
    -- Thay thế bằng đọc trực tiếp từ bảng KHACHHANG đã sao chép.
    IF OBJECT_ID('dbo.V_KHACHHANG_ALL', 'V') IS NOT NULL
    BEGIN
        DROP VIEW dbo.V_KHACHHANG_ALL;
        PRINT '>>> Dropped legacy V_KHACHHANG_ALL (deprecated).';
    END

    -- GRANT SELECT trên KHACHHANG + CHINHANH để các vai trò TraCuu có thể đọc
    -- dữ liệu đã sao chép trực tiếp (ghi đè DENY SELECT trong Phần 2
    -- chỉ cho hai bảng này trên TraCuu).
    IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
        GRANT SELECT ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;
    IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
        GRANT SELECT ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;

    PRINT '>>> TraCuu read-only hardening applied (direct table SELECT granted).';
END
ELSE
    PRINT '>>> Not TraCuu — skipping read-only hardening.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 8 — Xác minh
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '─── Role Membership on ' + DB_NAME() + ' ───────────────';
SELECT
    r.name   AS RoleName,
    u.name   AS MemberName,
    u.type_desc AS MemberType
FROM   sys.database_role_members rm
JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
WHERE  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
ORDER BY r.name, u.name;
GO

PRINT '';
PRINT '─── SP Permission Counts per Role ──────────────────────';
SELECT
    dp.name    AS RoleName,
    COUNT(*)   AS GrantedSPCount
FROM   sys.database_permissions p
JOIN   sys.database_principals  dp ON dp.principal_id = p.grantee_principal_id
JOIN   sys.objects              o  ON o.object_id     = p.major_id
WHERE  p.permission_name = 'EXECUTE'
  AND  o.type = 'P'
  AND  dp.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG', N'public')
GROUP BY dp.name
ORDER BY dp.name;
GO

PRINT '';
PRINT '=== 08_subscribers_post_replication_fixups.sql completed ===';
PRINT '    Database:  ' + DB_NAME();
PRINT '    Server:    ' + @@SERVERNAME;
PRINT '    Roles:     NGANHANG, CHINHANH, KHACHHANG';
PRINT '    SPs:       sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan,';
PRINT '               sp_DoiMatKhau, sp_DanhSachNhanVien';
PRINT '    Seeds:     ADMIN_NH, NV_BT, KH_DEMO';
GO

/*=============================================================================
  Xác minh sau khi chạy (trên mỗi máy chủ đăng ký nhận):
  ----------------------------------------------------------------------------
  -- Kiểm tra thành viên vai trò:
  EXEC sp_DangNhap;

  -- Kiểm tra quyền SP cho NV_BT (vai trò CHINHANH):
  EXECUTE AS USER = 'NV_BT';
  EXEC SP_GetCustomersByBranch @MACN = 'BENTHANH';    -- sẽ hoạt động
  -- EXEC SP_GetAllCustomers;                          -- sẽ thất bại (không có GRANT)
  REVERT;

  -- Kiểm tra KH_DEMO (vai trò KHACHHANG):
  EXECUTE AS USER = 'KH_DEMO';
  -- SELECT * FROM dbo.KHACHHANG;                      -- sẽ thất bại (DENY)
  EXEC SP_GetAccountsByCustomer @CMND = '1234567890';  -- sẽ hoạt động
  REVERT;
=============================================================================*/
