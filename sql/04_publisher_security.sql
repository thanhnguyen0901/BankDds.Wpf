USE NGANHANG;
GO
-- Tạo các role nghiệp vụ nếu chưa tồn tại.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NGANHANG' AND type = 'R')
BEGIN
    CREATE ROLE NGANHANG;
    PRINT N'>>> Đã tạo role NGANHANG.';
END
ELSE
    PRINT N'>>> Role NGANHANG đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'CHINHANH' AND type = 'R')
BEGIN
    CREATE ROLE CHINHANH;
    PRINT N'>>> Đã tạo role CHINHANH.';
END
ELSE
    PRINT N'>>> Role CHINHANH đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KHACHHANG' AND type = 'R')
BEGIN
    CREATE ROLE KHACHHANG;
    PRINT N'>>> Đã tạo role KHACHHANG.';
END
ELSE
    PRINT N'>>> Role KHACHHANG đã tồn tại, bỏ qua.';
GO
PRINT N'>>> Đã kiểm tra xong nhóm role trong database.';
GO
-- Chặn truy cập trực tiếp vào bảng, chỉ cho phép qua stored procedure.
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.CHINHANH       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NGUOIDUNG      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.KHACHHANG      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NHANVIEN       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.TAIKHOAN       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_GOIRUT      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_CHUYENTIEN  TO NGANHANG, CHINHANH, KHACHHANG;
GO
PRINT N'>>> Đã áp dụng DENY truy cập bảng trực tiếp.';
GO
-- Cấp quyền EXECUTE cho role NGANHANG.
GRANT EXECUTE ON dbo.SP_GetCustomersByBranch     TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetCustomerByCMND        TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddCustomer              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateCustomer           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteCustomer           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreCustomer          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllCustomers          TO NGANHANG;

GRANT EXECUTE ON dbo.SP_GetEmployeesByBranch     TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetEmployee              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddEmployee              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateEmployee           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteEmployee           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreEmployee          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_TransferEmployee         TO NGANHANG;
GRANT EXECUTE ON dbo.SP_EmployeeExists           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllEmployees          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetNextManv              TO NGANHANG;

GRANT EXECUTE ON dbo.SP_GetAccountsByBranch      TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccount               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddAccount               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CloseAccount             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_ReopenAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeductFromAccount        TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddToAccount             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllAccounts           TO NGANHANG;

GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetTransactionsByBranch  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetDailyWithdrawalTotal  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetDailyTransferTotal    TO NGANHANG;
GRANT EXECUTE ON dbo.SP_Deposit                  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_Withdraw                 TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CreateTransferTransaction TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CrossBranchTransfer      TO NGANHANG;

GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccountsOpenedInPeriod TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetTransactionSummary    TO NGANHANG;

GRANT EXECUTE ON dbo.SP_GetUser                  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllUsers              TO NGANHANG;
GRANT EXECUTE ON dbo.USP_AddUser                 TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateUser               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_SoftDeleteUser           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreUser              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetBranches              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetBranch                TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddBranch                TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateBranch             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteBranch             TO NGANHANG;
GO
-- Cấp quyền EXECUTE cho role CHINHANH.
GRANT EXECUTE ON dbo.SP_GetCustomersByBranch     TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetCustomerByCMND        TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddCustomer              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateCustomer           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteCustomer           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_RestoreCustomer          TO CHINHANH;

GRANT EXECUTE ON dbo.SP_GetEmployeesByBranch     TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetEmployee              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddEmployee              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateEmployee           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteEmployee           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_RestoreEmployee          TO CHINHANH;
GRANT EXECUTE ON dbo.SP_EmployeeExists           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetNextManv              TO CHINHANH;

GRANT EXECUTE ON dbo.SP_GetAccountsByBranch      TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccount               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddAccount               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CloseAccount             TO CHINHANH;
GRANT EXECUTE ON dbo.SP_ReopenAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeductFromAccount        TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddToAccount             TO CHINHANH;

GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetTransactionsByBranch  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetDailyWithdrawalTotal  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetDailyTransferTotal    TO CHINHANH;
GRANT EXECUTE ON dbo.SP_Deposit                  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_Withdraw                 TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CreateTransferTransaction TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CrossBranchTransfer      TO CHINHANH;

GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccountsOpenedInPeriod TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetTransactionSummary    TO CHINHANH;

GRANT EXECUTE ON dbo.SP_GetUser                  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAllUsers              TO CHINHANH;
GRANT EXECUTE ON dbo.USP_AddUser                 TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateUser               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_SoftDeleteUser           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_RestoreUser              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetBranches              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetBranch                TO CHINHANH;
GO
-- Cấp quyền EXECUTE tối thiểu cho role KHACHHANG.
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetAccount               TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetBranches              TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetBranch                TO KHACHHANG;
GO
PRINT N'>>> Đã cấp quyền EXECUTE theo từng role.';
GO
-- SP đăng nhập: trả về tài khoản, nhóm quyền và chi nhánh mặc định.
CREATE OR ALTER PROCEDURE dbo.sp_DangNhap
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LOGIN    nvarchar(128) = SYSTEM_USER;
    DECLARE @MANV     nvarchar(50)  = SYSTEM_USER;
    DECLARE @HOTEN    nvarchar(128) = SYSTEM_USER;
    DECLARE @TENNHOM  nvarchar(128) = NULL;
    DECLARE @MACN     nChar(10)     = NULL;
    DECLARE @CustomerCMND nChar(10) = NULL;
    DECLARE @EmployeeId   nChar(10) = NULL;

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

    IF OBJECT_ID(N'dbo.NGUOIDUNG', N'U') IS NOT NULL
    BEGIN
        SELECT TOP 1
            @MACN         = NULLIF(RTRIM(DefaultBranch), N''),
            @CustomerCMND = NULLIF(RTRIM(CustomerCMND), N''),
            @EmployeeId   = NULLIF(RTRIM(EmployeeId), N'')
        FROM dbo.NGUOIDUNG
        WHERE Username = @LOGIN
          AND TrangThaiXoa = 0;
    END

    IF @TENNHOM = N'CHINHANH'
    BEGIN
        IF @EmployeeId IS NULL
            SET @EmployeeId = @LOGIN;

        SELECT TOP 1
            @MACN  = COALESCE(@MACN, MACN),
            @HOTEN = RTRIM(HO) + N' ' + RTRIM(TEN)
        FROM dbo.NHANVIEN
        WHERE MANV = @EmployeeId
          AND TrangThaiXoa = 0;

        IF @MACN IS NULL
        BEGIN
            SELECT TOP 1
                @MACN  = MACN,
                @HOTEN = RTRIM(HO) + N' ' + RTRIM(TEN)
            FROM dbo.NHANVIEN
            WHERE MANV = @LOGIN
              AND TrangThaiXoa = 0;
        END

        SET @MANV = COALESCE(@EmployeeId, @MANV);
    END

    IF @TENNHOM = N'KHACHHANG'
    BEGIN
        IF @CustomerCMND IS NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = @LOGIN AND TrangThaiXoa = 0)
                SET @CustomerCMND = @LOGIN;
        END

        IF @CustomerCMND IS NOT NULL
        BEGIN
            SELECT TOP 1
                @MACN  = COALESCE(@MACN, MACN),
                @HOTEN = RTRIM(HO) + N' ' + RTRIM(TEN)
            FROM dbo.KHACHHANG
            WHERE CMND = @CustomerCMND
              AND TrangThaiXoa = 0;
        END
    END

    SELECT
        @MANV                    AS MANV,
        @HOTEN                   AS HOTEN,
        @TENNHOM                 AS TENNHOM,
        @MACN                    AS MACN,
        @CustomerCMND            AS CustomerCMND,
        @EmployeeId              AS EmployeeId;
END
GO

GRANT EXECUTE ON dbo.sp_DangNhap TO PUBLIC;
GO
PRINT N'>>> Đã tạo sp_DangNhap.';
GO
-- SP tạo tài khoản đăng nhập và gán role theo phân quyền người gọi.
CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoan
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128),
    @TENNHOM  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

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
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END ASC;

    IF @CallerRole IS NULL AND (IS_MEMBER('db_owner') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1)
        SET @CallerRole = N'NGANHANG';

    IF @CallerRole IS NULL
    BEGIN
        RAISERROR(N'Người gọi không thuộc role hợp lệ, không thể tạo tài khoản.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'NGANHANG' AND @TENNHOM <> N'NGANHANG'
    BEGIN
        RAISERROR(N'Tài khoản NGANHANG chỉ được tạo login thuộc role NGANHANG.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'CHINHANH' AND @TENNHOM NOT IN (N'CHINHANH', N'KHACHHANG')
    BEGIN
        RAISERROR(N'Tài khoản CHINHANH chỉ được tạo login thuộc role CHINHANH hoặc KHACHHANG.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'Tài khoản KHACHHANG không được phép tạo login.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
    BEGIN
        EXEC sp_addlogin @loginame = @LOGIN, @passwd = @PASS, @defdb = N'NGANHANG';
        PRINT N'Đã tạo login: ' + @LOGIN;
    END
    ELSE
        PRINT N'Login đã tồn tại: ' + @LOGIN;

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
    BEGIN
        EXEC sp_grantdbaccess @loginame = @LOGIN, @name_in_db = @LOGIN;
        PRINT N'Đã tạo DB user: ' + @LOGIN;
    END
    ELSE
        PRINT N'DB user đã tồn tại: ' + @LOGIN;

    IF NOT EXISTS (
        SELECT 1
        FROM   sys.database_role_members rm
        JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
        JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
        WHERE  u.name = @LOGIN AND r.name = @TENNHOM
    )
    BEGIN
        EXEC sp_addrolemember @rolename = @TENNHOM, @membername = @LOGIN;
        PRINT N'Đã thêm user ' + @LOGIN + N' vào role ' + @TENNHOM;
    END
    ELSE
        PRINT N'User ' + @LOGIN + N' đã thuộc role ' + @TENNHOM;
END
GO

GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO NGANHANG;
GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO CHINHANH;
GO
PRINT N'>>> Đã tạo sp_TaoTaiKhoan.';
GO
-- SP xóa tài khoản login (chỉ NGANHANG hoặc quản trị hệ thống).
CREATE OR ALTER PROCEDURE dbo.sp_XoaTaiKhoan
    @LOGIN nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Chỉ role NGANHANG mới được xóa tài khoản login.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
    BEGIN
        EXEC sp_revokedbaccess @name_in_db = @LOGIN;
        PRINT N'Đã xóa DB user: ' + @LOGIN;
    END

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
    BEGIN
        EXEC sp_droplogin @loginame = @LOGIN;
        PRINT N'Đã xóa login: ' + @LOGIN;
    END
END
GO

GRANT EXECUTE ON dbo.sp_XoaTaiKhoan TO NGANHANG;
GO
PRINT N'>>> Đã tạo sp_XoaTaiKhoan.';
GO
-- SP đổi mật khẩu: cho phép tự đổi hoặc NGANHANG reset cho tài khoản khác.
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
        PRINT N'Đã đổi mật khẩu cho: ' + @LOGIN;
        RETURN;
    END

    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Chỉ role NGANHANG mới được đặt lại mật khẩu cho tài khoản khác.', 16, 1);
        RETURN;
    END

    EXEC sp_password @old = NULL, @new = @PASSMOI, @loginame = @LOGIN;
    PRINT N'Đã đặt lại mật khẩu cho: ' + @LOGIN;
END
GO

GRANT EXECUTE ON dbo.sp_DoiMatKhau TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO CHINHANH;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO KHACHHANG;
GO
PRINT N'>>> Đã tạo sp_DoiMatKhau.';
GO
-- SP liệt kê user theo nhóm quyền trong database.
CREATE OR ALTER PROCEDURE dbo.sp_DanhSachNhanVien
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.name                      AS LOGINNAME,
        r.name                      AS TENNHOM,
        u.create_date               AS NGAYTAO
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
PRINT N'>>> Đã tạo sp_DanhSachNhanVien.';
GO
-- Tạo sẵn login demo và gán role tương ứng.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ADMIN_NH')
BEGIN
    EXEC sp_addlogin @loginame = N'ADMIN_NH', @passwd = N'Admin@123', @defdb = N'NGANHANG';
    PRINT N'>>> Đã tạo login mẫu ADMIN_NH.';
END
ELSE
    PRINT N'>>> Login mẫu ADMIN_NH đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ADMIN_NH' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'ADMIN_NH', @name_in_db = N'ADMIN_NH';
    PRINT N'>>> Đã ánh xạ DB user ADMIN_NH.';
END
ELSE
    PRINT N'>>> DB user ADMIN_NH đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'ADMIN_NH' AND r.name = N'NGANHANG'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'NGANHANG', @membername = N'ADMIN_NH';
    PRINT N'>>> Đã thêm ADMIN_NH vào role NGANHANG.';
END
ELSE
    PRINT N'>>> ADMIN_NH đã thuộc role NGANHANG, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NV_BT')
BEGIN
    EXEC sp_addlogin @loginame = N'NV_BT', @passwd = N'NhanVien@123', @defdb = N'NGANHANG';
    PRINT N'>>> Đã tạo login mẫu NV_BT.';
END
ELSE
    PRINT N'>>> Login mẫu NV_BT đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NV_BT' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'NV_BT', @name_in_db = N'NV_BT';
    PRINT N'>>> Đã ánh xạ DB user NV_BT.';
END
ELSE
    PRINT N'>>> DB user NV_BT đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'NV_BT' AND r.name = N'CHINHANH'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'CHINHANH', @membername = N'NV_BT';
    PRINT N'>>> Đã thêm NV_BT vào role CHINHANH.';
END
ELSE
    PRINT N'>>> NV_BT đã thuộc role CHINHANH, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'KH_DEMO')
BEGIN
    EXEC sp_addlogin @loginame = N'KH_DEMO', @passwd = N'KhachHang@123', @defdb = N'NGANHANG';
    PRINT N'>>> Đã tạo login mẫu KH_DEMO.';
END
ELSE
    PRINT N'>>> Login mẫu KH_DEMO đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KH_DEMO' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'KH_DEMO', @name_in_db = N'KH_DEMO';
    PRINT N'>>> Đã ánh xạ DB user KH_DEMO.';
END
ELSE
    PRINT N'>>> DB user KH_DEMO đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'KH_DEMO' AND r.name = N'KHACHHANG'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'KHACHHANG', @membername = N'KH_DEMO';
    PRINT N'>>> Đã thêm KH_DEMO vào role KHACHHANG.';
END
ELSE
    PRINT N'>>> KH_DEMO đã thuộc role KHACHHANG, bỏ qua.';
GO
PRINT N'>>> Đã xử lý xong nhóm login mẫu (ADMIN_NH, NV_BT, KH_DEMO).';
GO
PRINT N'';
PRINT N'--- Danh sách thành viên role ---';
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
    dp.name                     AS RoleName,
    COUNT(*)                    AS GrantedSPCount
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
PRINT N'--- Danh sách stored procedure bảo mật ---';
SELECT name, type_desc, create_date
FROM   sys.objects
WHERE  type = 'P'
  AND  name IN ('sp_DangNhap', 'sp_TaoTaiKhoan', 'sp_XoaTaiKhoan',
                'sp_DoiMatKhau', 'sp_DanhSachNhanVien')
ORDER BY name;
GO
PRINT N'';
PRINT N'=== Hoàn tất 04_publisher_security.sql ===';
PRINT N'    Role: NGANHANG, CHINHANH, KHACHHANG';
PRINT N'    SP bảo mật: sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan,';
PRINT N'               sp_DoiMatKhau, sp_DanhSachNhanVien';
PRINT N'    Login mẫu: ADMIN_NH (NGANHANG), NV_BT (CHINHANH), KH_DEMO (KHACHHANG)';
PRINT N'    Bước tiếp theo: setup hạ tầng theo SSMS UI runbook (không dùng script 05/06/08 làm flow chính).';
GO

