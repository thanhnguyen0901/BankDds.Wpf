DECLARE @CurrentDb sysname;
SET @CurrentDb = DB_NAME();

IF @CurrentDb NOT IN (N'NGANHANG_BT', N'NGANHANG_TD', N'NGANHANG_TRACUU')
BEGIN
    RAISERROR(N'Hãy chạy script với -d NGANHANG_BT | NGANHANG_TD | NGANHANG_TRACUU. DB hiện tại=%s', 16, 1, @CurrentDb);
    RETURN;
END
GO

-- Kiểm tra DB đích và bắt đầu hậu xử lý replication.
PRINT N'--- Hậu xử lý sau replication trên Subscriber ---';
PRINT N'DB: ' + DB_NAME();
PRINT N'Server: ' + @@SERVERNAME;
PRINT '';
GO

-- Phần 1: tạo role nghiệp vụ nếu chưa tồn tại.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NGANHANG' AND type = 'R')
BEGIN
    CREATE ROLE NGANHANG;
    PRINT N'>>> Đã tạo role NGANHANG trên ' + DB_NAME();
END
ELSE
    PRINT N'>>> Role NGANHANG đã tồn tại trên ' + DB_NAME();
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'CHINHANH' AND type = 'R')
BEGIN
    CREATE ROLE CHINHANH;
    PRINT N'>>> Đã tạo role CHINHANH trên ' + DB_NAME();
END
ELSE
    PRINT N'>>> Role CHINHANH đã tồn tại trên ' + DB_NAME();
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KHACHHANG' AND type = 'R')
BEGIN
    CREATE ROLE KHACHHANG;
    PRINT N'>>> Đã tạo role KHACHHANG trên ' + DB_NAME();
END
ELSE
    PRINT N'>>> Role KHACHHANG đã tồn tại trên ' + DB_NAME();
GO

PRINT N'>>> Phần 1: Đã tạo/xác nhận role database.';
GO

-- Phần 2: chặn truy cập trực tiếp vào bảng.
IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;

IF OBJECT_ID('dbo.NHANVIEN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NHANVIEN TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.TAIKHOAN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.TAIKHOAN TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.GD_GOIRUT', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_GOIRUT TO NGANHANG, CHINHANH, KHACHHANG;
IF OBJECT_ID('dbo.GD_CHUYENTIEN', 'U') IS NOT NULL
    DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_CHUYENTIEN TO NGANHANG, CHINHANH, KHACHHANG;
GO

PRINT N'>>> Phần 2: Đã áp dụng DENY truy cập bảng trực tiếp.';
GO

-- Phần 3: cấp quyền EXECUTE theo role nếu SP tồn tại.
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

EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomersByBranch',      'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetCustomerByCMND',         'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddCustomer',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateCustomer',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteCustomer',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreCustomer',           'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllCustomers',           'NGANHANG';

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

EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByAccount',  'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByBranch',   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyWithdrawalTotal',   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetDailyTransferTotal',     'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Deposit',                   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_Withdraw',                  'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CreateTransferTransaction', 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_CrossBranchTransfer',       'NGANHANG';

EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountStatement',       'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsOpenedInPeriod', 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionSummary',     'NGANHANG';

EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetUser',                   'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAllUsers',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.USP_AddUser',                  'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateUser',                'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_SoftDeleteUser',            'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_RestoreUser',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_AddBranch',                 'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateBranch',              'NGANHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_DeleteBranch',              'NGANHANG';
GO

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
EXEC dbo.sp_SafeGrantExec 'dbo.USP_AddUser',                  'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_UpdateUser',                'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_SoftDeleteUser',            'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'CHINHANH';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'CHINHANH';
GO

EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountsByCustomer',     'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccount',                'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetTransactionsByAccount',  'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetAccountStatement',       'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranches',               'KHACHHANG';
EXEC dbo.sp_SafeGrantExec 'dbo.SP_GetBranch',                 'KHACHHANG';
GO

DROP PROCEDURE dbo.sp_SafeGrantExec;
GO

PRINT N'>>> Phần 3: Đã cấp GRANT EXECUTE cho stored procedure.';
GO

-- Phần 4: tạo lại nhóm SP bảo mật cục bộ trên subscriber.
IF OBJECT_ID(N'dbo.sp_DangNhap', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DangNhap;
GO
CREATE PROCEDURE dbo.sp_DangNhap
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MANV     nvarchar(50)  = SYSTEM_USER;
    DECLARE @HOTEN    nvarchar(128) = USER_NAME();
    DECLARE @TENNHOM  nvarchar(128) = NULL;
    DECLARE @MACN     nChar(10)     = NULL;

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
    
    IF @TENNHOM IN (N'CHINHANH', N'KHACHHANG')
    BEGIN
        SET @MACN = CASE DB_NAME()
            WHEN N'NGANHANG_BT' THEN N'BENTHANH'
            WHEN N'NGANHANG_TD' THEN N'TANDINH'
            ELSE NULL       
        END;
    END

    SELECT @MANV AS MANV, @HOTEN AS HOTEN, @TENNHOM AS TENNHOM, @MACN AS MACN;
END
GO

-- Phần 4: tạo lại nhóm SP bảo mật cục bộ trên subscriber.
IF OBJECT_ID(N'dbo.sp_DangNhap', N'P') IS NOT NULL
    GRANT EXECUTE ON dbo.sp_DangNhap TO PUBLIC;
GO

IF OBJECT_ID(N'dbo.sp_TaoTaiKhoan', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TaoTaiKhoan;
GO
CREATE PROCEDURE dbo.sp_TaoTaiKhoan
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128),
    @TENNHOM  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DefaultDb sysname = DB_NAME();

    IF @TENNHOM NOT IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    BEGIN
        RAISERROR(N'Tên role không hợp lệ: %s. Chỉ chấp nhận NGANHANG, CHINHANH hoặc KHACHHANG.', 16, 1, @TENNHOM);
        RETURN;
    END
    
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
        RAISERROR(N'Người gọi không thuộc role hợp lệ.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'CHINHANH' AND @TENNHOM = N'NGANHANG'
    BEGIN
        RAISERROR(N'CHINHANH không được tạo login thuộc role NGANHANG.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'KHACHHANG không được tạo login.', 16, 1);
        RETURN;
    END

    
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        EXEC sp_addlogin @loginame = @LOGIN, @passwd = @PASS, @defdb = @DefaultDb;

    
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        EXEC sp_grantdbaccess @loginame = @LOGIN, @name_in_db = @LOGIN;

    
    IF NOT EXISTS (
        SELECT 1 FROM sys.database_role_members rm
        JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
        JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
        WHERE u.name = @LOGIN AND r.name = @TENNHOM
    )
        EXEC sp_addrolemember @rolename = @TENNHOM, @membername = @LOGIN;
END
GO

IF OBJECT_ID(N'dbo.sp_TaoTaiKhoan', N'P') IS NOT NULL
BEGIN
    GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO NGANHANG;
    GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO CHINHANH;
END
GO

IF OBJECT_ID(N'dbo.sp_XoaTaiKhoan', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_XoaTaiKhoan;
GO
CREATE PROCEDURE dbo.sp_XoaTaiKhoan
    @LOGIN nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Chỉ NGANHANG mới được xóa tài khoản login.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        EXEC sp_revokedbaccess @name_in_db = @LOGIN;

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        EXEC sp_droplogin @loginame = @LOGIN;
END
GO

IF OBJECT_ID(N'dbo.sp_XoaTaiKhoan', N'P') IS NOT NULL
    GRANT EXECUTE ON dbo.sp_XoaTaiKhoan TO NGANHANG;
GO

IF OBJECT_ID(N'dbo.sp_DoiMatKhau', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DoiMatKhau;
GO
CREATE PROCEDURE dbo.sp_DoiMatKhau
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
        RAISERROR(N'Chỉ NGANHANG mới được đặt lại mật khẩu cho tài khoản khác.', 16, 1);
        RETURN;
    END

    EXEC sp_password @old = NULL, @new = @PASSMOI, @loginame = @LOGIN;
END
GO

IF OBJECT_ID(N'dbo.sp_DoiMatKhau', N'P') IS NOT NULL
BEGIN
    GRANT EXECUTE ON dbo.sp_DoiMatKhau TO NGANHANG;
    GRANT EXECUTE ON dbo.sp_DoiMatKhau TO CHINHANH;
    GRANT EXECUTE ON dbo.sp_DoiMatKhau TO KHACHHANG;
END
GO

IF OBJECT_ID(N'dbo.sp_DanhSachNhanVien', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DanhSachNhanVien;
GO
CREATE PROCEDURE dbo.sp_DanhSachNhanVien
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

IF OBJECT_ID(N'dbo.sp_DanhSachNhanVien', N'P') IS NOT NULL
BEGIN
    GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO NGANHANG;
    GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO CHINHANH;
END
GO

PRINT N'>>> Phần 4: Đã tạo nhóm SP bảo mật trên subscriber.';
GO

-- Phần 5: đồng bộ login mẫu và gán role.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ADMIN_NH')
BEGIN
    DECLARE @SeedDefaultDb_Admin sysname;
    SET @SeedDefaultDb_Admin = DB_NAME();
    EXEC sp_addlogin @loginame = N'ADMIN_NH', @passwd = N'Admin@123', @defdb = @SeedDefaultDb_Admin;
END
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

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NV_BT')
BEGIN
    DECLARE @SeedDefaultDb_NV sysname;
    SET @SeedDefaultDb_NV = DB_NAME();
    EXEC sp_addlogin @loginame = N'NV_BT', @passwd = N'NhanVien@123', @defdb = @SeedDefaultDb_NV;
END
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

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'KH_DEMO')
BEGIN
    DECLARE @SeedDefaultDb_KH sysname;
    SET @SeedDefaultDb_KH = DB_NAME();
    EXEC sp_addlogin @loginame = N'KH_DEMO', @passwd = N'KhachHang@123', @defdb = @SeedDefaultDb_KH;
END
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

PRINT N'>>> Phần 5: Đã đồng bộ login mẫu vào subscriber.';
GO

-- Phần 6: dọn các view thừa chỉ dùng ở Publisher.
BEGIN TRY
    IF OBJECT_ID('dbo.KHACHHANG_ALL',         'V') IS NOT NULL DROP VIEW dbo.KHACHHANG_ALL;
    IF OBJECT_ID('dbo.NHANVIEN_ALL',          'V') IS NOT NULL DROP VIEW dbo.NHANVIEN_ALL;
    IF OBJECT_ID('dbo.TAIKHOAN_ALL',          'V') IS NOT NULL DROP VIEW dbo.TAIKHOAN_ALL;
    IF OBJECT_ID('dbo.GD_GOIRUT_ALL',         'V') IS NOT NULL DROP VIEW dbo.GD_GOIRUT_ALL;
    IF OBJECT_ID('dbo.GD_CHUYENTIEN_ALL',     'V') IS NOT NULL DROP VIEW dbo.GD_CHUYENTIEN_ALL;
    IF OBJECT_ID('dbo.view_DanhSachPhanManh', 'V') IS NOT NULL DROP VIEW dbo.view_DanhSachPhanManh;
END TRY
BEGIN CATCH
    PRINT N'>>> Cảnh báo Phần 6: một số view replicate không thể xóa trên subscriber. Lỗi=' + ERROR_MESSAGE();
END CATCH
GO

PRINT N'>>> Phần 6: Đã xóa view dư chỉ dùng ở Publisher.';
GO

-- Phần 7: tăng cường chính sách chỉ đọc cho TraCuu.
IF DB_NAME() = N'NGANHANG_TRACUU'
BEGIN
    PRINT N'>>> Phát hiện TraCuu - áp dụng chế độ tăng cường chỉ đọc.';

    -- Chặn thao tác ghi trực tiếp trên các bảng tra cứu.
    IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
        DENY INSERT, UPDATE, DELETE ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;
    IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
        DENY INSERT, UPDATE, DELETE ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;

    -- Dọn view cũ nếu còn tồn tại.
    IF OBJECT_ID('dbo.V_KHACHHANG_ALL', 'V') IS NOT NULL
    BEGIN
        DROP VIEW dbo.V_KHACHHANG_ALL;
        PRINT N'>>> Đã xóa view cũ V_KHACHHANG_ALL.';
    END

    -- Chỉ cho phép đọc dữ liệu tra cứu.
    IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NOT NULL
        GRANT SELECT ON dbo.KHACHHANG TO NGANHANG, CHINHANH, KHACHHANG;
    IF OBJECT_ID('dbo.CHINHANH', 'U') IS NOT NULL
        GRANT SELECT ON dbo.CHINHANH TO NGANHANG, CHINHANH, KHACHHANG;

    PRINT N'>>> Đã áp dụng tăng cường chỉ đọc cho TraCuu.';
END
ELSE
    PRINT N'>>> Không phải TraCuu - bỏ qua tăng cường chỉ đọc.';
GO

PRINT N'';
-- Phần 8: in báo cáo kiểm tra sau khi cấu hình.
PRINT N'--- Thành viên role trên ' + DB_NAME() + N' ---';
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

PRINT N'';
PRINT N'--- Số quyền EXECUTE theo role ---';
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

PRINT N'';
PRINT N'=== Hoàn tất 08_subscribers_post_replication_fixups.sql ===';
PRINT N'    Database:  ' + DB_NAME();
PRINT N'    Server:    ' + @@SERVERNAME;
PRINT N'    Role:      NGANHANG, CHINHANH, KHACHHANG';
PRINT N'    SP:        sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan,';
PRINT N'               sp_DoiMatKhau, sp_DanhSachNhanVien';
PRINT N'    Seed:      ADMIN_NH, NV_BT, KH_DEMO';
GO


