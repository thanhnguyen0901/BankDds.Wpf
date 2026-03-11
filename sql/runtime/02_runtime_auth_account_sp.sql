USE NGANHANG_PUB;
GO
-- Runtime-only auth/account SP package (UI-first).
-- Includes: sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau, sp_DanhSachNhanVien.
-- Role/Login/User creation and mapping must be done in SSMS UI (Phase B4).
GO
-- SP đăng nhập: trả về tài khoản, nhóm quyền và chi nhánh mặc định.
CREATE OR ALTER PROCEDURE dbo.sp_DangNhap
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
        IF OBJECT_ID(N'dbo.NGUOIDUNG', N'U') IS NOT NULL
            SELECT @MACN = NULLIF(RTRIM(DefaultBranch), N'')
            FROM   dbo.NGUOIDUNG
            WHERE  Username = SYSTEM_USER AND TrangThaiXoa = 0;
        IF @MACN IS NULL
            SELECT @MACN = MACN FROM dbo.NHANVIEN
            WHERE  MANV = SYSTEM_USER AND TrangThaiXoa = 0;

        IF @MACN IS NULL
            SELECT TOP 1 @MACN = MACN FROM dbo.KHACHHANG
            WHERE  CMND = SYSTEM_USER AND TrangThaiXoa = 0;
    END

    SELECT @MANV AS MANV, @HOTEN AS HOTEN, @TENNHOM AS TENNHOM, @MACN AS MACN;
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

    IF @CallerRole = N'CHINHANH' AND @TENNHOM = N'NGANHANG'
    BEGIN
        RAISERROR(N'Tài khoản CHINHANH không được tạo login thuộc role NGANHANG.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'Tài khoản KHACHHANG không được phép tạo login.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
    BEGIN
        EXEC sp_addlogin @loginame = @LOGIN, @passwd = @PASS, @defdb = N'NGANHANG_PUB';
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
