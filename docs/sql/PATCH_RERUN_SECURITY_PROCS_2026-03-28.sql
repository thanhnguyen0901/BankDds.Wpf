USE [NGANHANG];
GO

/*
Run this file on Publisher DESKTOP-JBB41QU using sa/sysadmin.

Prerequisites:
1. Linked servers LINK0/LINK1/LINK2 have been recreated correctly.
2. Database NGANHANG has owner = sa and TRUSTWORTHY ON.

This patch reruns the 4 security procedures after the root-cause fix:
- sp_SyncSecurityToSubscribers
- sp_TaoTaiKhoan
- sp_XoaTaiKhoan
- sp_DoiMatKhau

It also reruns:
- USP_AddUser
*/

CREATE OR ALTER PROCEDURE dbo.sp_SyncSecurityToSubscribers
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128) = NULL,
    @TENNHOM  nvarchar(128) = NULL,
    @MODE     nvarchar(20)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    IF (SELECT COUNT(*) FROM sys.servers WHERE is_linked = 1 AND name IN (N'LINK0', N'LINK1', N'LINK2')) < 3
        RETURN;

    DECLARE @LinkedServer sysname;
    DECLARE @RemoteSql    nvarchar(max);
    DECLARE @ExecSql      nvarchar(max);
    DECLARE @RoleName     nvarchar(128) = UPPER(LTRIM(RTRIM(ISNULL(@TENNHOM, N''))));

    DECLARE link_cursor CURSOR FAST_FORWARD FOR
    SELECT name
    FROM sys.servers
    WHERE is_linked = 1
      AND name IN (N'LINK0', N'LINK1', N'LINK2')
    ORDER BY name;

    OPEN link_cursor;
    FETCH NEXT FROM link_cursor INTO @LinkedServer;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            IF @MODE = N'UPSERT'
            BEGIN
                SET @RemoteSql = N'
IF SUSER_ID(N''' + REPLACE(@LOGIN, '''', '''''') + N''') IS NULL
    CREATE LOGIN ' + QUOTENAME(@LOGIN) + N' WITH PASSWORD = N''' + REPLACE(ISNULL(@PASS, N''), '''', '''''') + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [NGANHANG];
ELSE
    ALTER LOGIN ' + QUOTENAME(@LOGIN) + N' WITH PASSWORD = N''' + REPLACE(ISNULL(@PASS, N''), '''', '''''') + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;

USE [NGANHANG];
IF DATABASE_PRINCIPAL_ID(N''' + REPLACE(@LOGIN, '''', '''''') + N''') IS NULL
    CREATE USER ' + QUOTENAME(@LOGIN) + N' FOR LOGIN ' + QUOTENAME(@LOGIN) + N';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N''' + REPLACE(@RoleName, '''', '''''') + N''' AND type = ''R'')
    CREATE ROLE ' + QUOTENAME(@RoleName) + N';

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = N''' + REPLACE(@LOGIN, '''', '''''') + N'''
      AND r.name = N''' + REPLACE(@RoleName, '''', '''''') + N'''
)
    ALTER ROLE ' + QUOTENAME(@RoleName) + N' ADD MEMBER ' + QUOTENAME(@LOGIN) + N';';
            END
            ELSE IF @MODE = N'PASSWORD'
            BEGIN
                SET @RemoteSql = N'
IF SUSER_ID(N''' + REPLACE(@LOGIN, '''', '''''') + N''') IS NOT NULL
    ALTER LOGIN ' + QUOTENAME(@LOGIN) + N' WITH PASSWORD = N''' + REPLACE(ISNULL(@PASS, N''), '''', '''''') + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
            END
            ELSE IF @MODE = N'DROP'
            BEGIN
                SET @RemoteSql = N'
USE [NGANHANG];
IF DATABASE_PRINCIPAL_ID(N''' + REPLACE(@LOGIN, '''', '''''') + N''') IS NOT NULL
    DROP USER ' + QUOTENAME(@LOGIN) + N';

IF SUSER_ID(N''' + REPLACE(@LOGIN, '''', '''''') + N''') IS NOT NULL
    DROP LOGIN ' + QUOTENAME(@LOGIN) + N';';
            END
            ELSE
            BEGIN
                RAISERROR(N'Unsupported sync mode: %s', 16, 1, @MODE);
                RETURN;
            END

            SET @ExecSql =
                N'EXEC ' + QUOTENAME(@LinkedServer) + N'.master.dbo.sp_executesql N'''
                + REPLACE(@RemoteSql, '''', '''''') + N''';';

            EXEC (@ExecSql);
        END TRY
        BEGIN CATCH
            DECLARE @Err nvarchar(4000) = ERROR_MESSAGE();
            RAISERROR(N'Sync security to %s failed: %s', 16, 1, @LinkedServer, @Err);
            CLOSE link_cursor;
            DEALLOCATE link_cursor;
            RETURN;
        END CATCH

        FETCH NEXT FROM link_cursor INTO @LinkedServer;
    END

    CLOSE link_cursor;
    DEALLOCATE link_cursor;
END
GO

GRANT EXECUTE ON dbo.sp_SyncSecurityToSubscribers TO NGANHANG;
GRANT EXECUTE ON dbo.sp_SyncSecurityToSubscribers TO CHINHANH;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoan
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128),
    @TENNHOM  nvarchar(128)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OriginalLogin nvarchar(128) = ORIGINAL_LOGIN();
    DECLARE @CallerDbUser  nvarchar(128) = NULL;

    IF @TENNHOM NOT IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    BEGIN
        RAISERROR(N'Tên role không hợp lệ: %s. Chỉ chấp nhận NGANHANG, CHINHANH hoặc KHACHHANG.', 16, 1, @TENNHOM);
        RETURN;
    END

    DECLARE @CallerRole nvarchar(128) = NULL;
    DECLARE @CreateLoginSql nvarchar(max);
    DECLARE @CreateUserSql  nvarchar(max);
    DECLARE @AddRoleSql     nvarchar(max);
    DECLARE @DropUserSql    nvarchar(max);
    DECLARE @DropLoginSql   nvarchar(max);
    DECLARE @CreatedLogin   bit = 0;
    DECLARE @CreatedDbUser  bit = 0;

    SELECT TOP 1
        @CallerDbUser = dp.name
    FROM sys.database_principals dp
    WHERE dp.sid = SUSER_SID(@OriginalLogin)
      AND dp.type IN ('S', 'U')
    ORDER BY
        CASE dp.type
            WHEN 'S' THEN 1
            ELSE 2
        END;

    IF @CallerDbUser IS NULL
        SET @CallerDbUser = @OriginalLogin;

    SELECT TOP 1 @CallerRole = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = @CallerDbUser
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END ASC;

    IF @CallerRole IS NULL AND IS_SRVROLEMEMBER('sysadmin', @OriginalLogin) = 1
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

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        BEGIN
            RAISERROR(N'Login %s da ton tai. Khong duoc phep tao de ghi de account hien co.', 16, 1, @LOGIN);
            RETURN;
        END

        SET @CreateLoginSql =
            N'CREATE LOGIN ' + QUOTENAME(@LOGIN) +
            N' WITH PASSWORD = N''' + REPLACE(@PASS, '''', '''''') +
            N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [NGANHANG];';
        EXEC (@CreateLoginSql);
        SET @CreatedLogin = 1;
        PRINT N'Đã tạo login: ' + @LOGIN;

        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        BEGIN
            SET @CreateUserSql = N'CREATE USER ' + QUOTENAME(@LOGIN) + N' FOR LOGIN ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@CreateUserSql);
            SET @CreatedDbUser = 1;
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
            SET @AddRoleSql = N'ALTER ROLE ' + QUOTENAME(@TENNHOM) + N' ADD MEMBER ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@AddRoleSql);
            PRINT N'Đã thêm user ' + @LOGIN + N' vào role ' + @TENNHOM;
        END
        ELSE
            PRINT N'User ' + @LOGIN + N' đã thuộc role ' + @TENNHOM;

        EXEC dbo.sp_SyncSecurityToSubscribers
            @LOGIN   = @LOGIN,
            @PASS    = @PASS,
            @TENNHOM = @TENNHOM,
            @MODE    = N'UPSERT';
    END TRY
    BEGIN CATCH
        DECLARE @CreateErr nvarchar(4000) = ERROR_MESSAGE();

        IF @CreatedDbUser = 1 AND DATABASE_PRINCIPAL_ID(@LOGIN) IS NOT NULL
        BEGIN TRY
            SET @DropUserSql = N'DROP USER ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@DropUserSql);
        END TRY
        BEGIN CATCH
        END CATCH;

        IF @CreatedLogin = 1 AND SUSER_ID(@LOGIN) IS NOT NULL
        BEGIN TRY
            SET @DropLoginSql = N'DROP LOGIN ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@DropLoginSql);
        END TRY
        BEGIN CATCH
        END CATCH;

        RAISERROR(N'Lỗi khi tạo tài khoản %s: %s', 16, 1, @LOGIN, @CreateErr);
        RETURN;
    END CATCH
END
GO

GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO NGANHANG;
GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO CHINHANH;
GO

CREATE OR ALTER PROCEDURE dbo.sp_XoaTaiKhoan
    @LOGIN nvarchar(50)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OriginalLogin nvarchar(128) = ORIGINAL_LOGIN();
    DECLARE @CallerDbUser  nvarchar(128) = NULL;
    DECLARE @CallerRole    nvarchar(128) = NULL;
    DECLARE @DropUserSql   nvarchar(max);
    DECLARE @DropLoginSql  nvarchar(max);

    SELECT TOP 1
        @CallerDbUser = dp.name
    FROM sys.database_principals dp
    WHERE dp.sid = SUSER_SID(@OriginalLogin)
      AND dp.type IN ('S', 'U')
    ORDER BY
        CASE dp.type
            WHEN 'S' THEN 1
            ELSE 2
        END;

    IF @CallerDbUser IS NULL
        SET @CallerDbUser = @OriginalLogin;

    SELECT TOP 1
        @CallerRole = r.name
    FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = @CallerDbUser
      AND r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END;

    IF @CallerRole <> N'NGANHANG'
       AND IS_SRVROLEMEMBER('sysadmin', @OriginalLogin) <> 1
    BEGIN
        RAISERROR(N'Chỉ role NGANHANG mới được xóa tài khoản login.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
        BEGIN
            SET @DropUserSql = N'DROP USER ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@DropUserSql);
            PRINT N'Đã xóa DB user: ' + @LOGIN;
        END

        IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
        BEGIN
            SET @DropLoginSql = N'DROP LOGIN ' + QUOTENAME(@LOGIN) + N';';
            EXEC (@DropLoginSql);
            PRINT N'Đã xóa login: ' + @LOGIN;
        END

        EXEC dbo.sp_SyncSecurityToSubscribers
            @LOGIN   = @LOGIN,
            @PASS    = NULL,
            @TENNHOM = NULL,
            @MODE    = N'DROP';
    END TRY
    BEGIN CATCH
        DECLARE @DeleteErr nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(N'Lỗi khi xóa tài khoản %s: %s', 16, 1, @LOGIN, @DeleteErr);
        RETURN;
    END CATCH
END
GO

GRANT EXECUTE ON dbo.sp_XoaTaiKhoan TO NGANHANG;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DoiMatKhau
    @LOGIN    nvarchar(50),
    @PASSCU   nvarchar(128),
    @PASSMOI  nvarchar(128)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OriginalLogin nvarchar(128) = ORIGINAL_LOGIN();
    DECLARE @CallerDbUser  nvarchar(128) = NULL;
    DECLARE @CallerRole    nvarchar(128) = NULL;
    DECLARE @AlterLoginSql nvarchar(max);

    SELECT TOP 1
        @CallerDbUser = dp.name
    FROM sys.database_principals dp
    WHERE dp.sid = SUSER_SID(@OriginalLogin)
      AND dp.type IN ('S', 'U')
    ORDER BY
        CASE dp.type
            WHEN 'S' THEN 1
            ELSE 2
        END;

    IF @CallerDbUser IS NULL
        SET @CallerDbUser = @OriginalLogin;

    SELECT TOP 1
        @CallerRole = r.name
    FROM sys.database_role_members rm
    JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    WHERE u.name = @CallerDbUser
      AND r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END;

    IF @LOGIN = @OriginalLogin
    BEGIN
        BEGIN TRY
            EXEC sp_password @old = @PASSCU, @new = @PASSMOI, @loginame = @LOGIN;
            PRINT N'Đã đổi mật khẩu cho: ' + @LOGIN;

            EXEC dbo.sp_SyncSecurityToSubscribers
                @LOGIN   = @LOGIN,
                @PASS    = @PASSMOI,
                @TENNHOM = NULL,
                @MODE    = N'PASSWORD';
        END TRY
        BEGIN CATCH
            DECLARE @SelfPasswordErr nvarchar(4000) = ERROR_MESSAGE();
            RAISERROR(N'Lỗi khi đổi mật khẩu cho %s: %s', 16, 1, @LOGIN, @SelfPasswordErr);
            RETURN;
        END CATCH;
        RETURN;
    END

    IF @CallerRole <> N'NGANHANG'
       AND IS_SRVROLEMEMBER('sysadmin', @OriginalLogin) <> 1
    BEGIN
        RAISERROR(N'Chỉ role NGANHANG mới được đặt lại mật khẩu cho tài khoản khác.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        SET @AlterLoginSql =
            N'ALTER LOGIN ' + QUOTENAME(@LOGIN) +
            N' WITH PASSWORD = N''' + REPLACE(@PASSMOI, '''', '''''') +
            N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
        EXEC (@AlterLoginSql);
        PRINT N'Đã đặt lại mật khẩu cho: ' + @LOGIN;

        EXEC dbo.sp_SyncSecurityToSubscribers
            @LOGIN   = @LOGIN,
            @PASS    = @PASSMOI,
            @TENNHOM = NULL,
            @MODE    = N'PASSWORD';
    END TRY
    BEGIN CATCH
        DECLARE @ResetPasswordErr nvarchar(4000) = ERROR_MESSAGE();
        RAISERROR(N'Lỗi khi đặt lại mật khẩu cho %s: %s', 16, 1, @LOGIN, @ResetPasswordErr);
        RETURN;
    END CATCH
END
GO

GRANT EXECUTE ON dbo.sp_DoiMatKhau TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO CHINHANH;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO KHACHHANG;
GO

CREATE OR ALTER PROCEDURE dbo.USP_AddUser
    @Username      nvarchar(50),
    @PasswordHash  nvarchar(255),
    @UserGroup     int,
    @DefaultBranch nvarchar(20),
    @CustomerCMND  nChar(10)    = NULL,
    @EmployeeId    nChar(10)    = NULL
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OriginalLogin nvarchar(128) = ORIGINAL_LOGIN();
    DECLARE @CallerDbUser  nvarchar(128) = NULL;

    SET @Username      = LTRIM(RTRIM(@Username));
    SET @PasswordHash  = LTRIM(RTRIM(@PasswordHash));
    SET @DefaultBranch = UPPER(LTRIM(RTRIM(@DefaultBranch)));
    SET @CustomerCMND  = NULLIF(LTRIM(RTRIM(@CustomerCMND)), N'');
    SET @EmployeeId    = NULLIF(UPPER(LTRIM(RTRIM(@EmployeeId))), N'');

    IF @UserGroup NOT IN (0, 1, 2)
    BEGIN
        RAISERROR(N'UserGroup khong hop le. Chi chap nhan 0 (NganHang), 1 (ChiNhanh), 2 (KhachHang).', 16, 1);
        RETURN;
    END

    IF @Username = N'' OR @DefaultBranch = N''
    BEGIN
        RAISERROR(N'Username va DefaultBranch khong duoc de trong.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = @DefaultBranch)
    BEGIN
        RAISERROR(N'DefaultBranch %s khong ton tai trong CHINHANH.', 16, 1, @DefaultBranch);
        RETURN;
    END

    DECLARE @CallerRole nvarchar(128) = NULL;
    DECLARE @CallerEmployeeId nChar(10) = NULL;

    SELECT TOP 1
        @CallerDbUser = dp.name
    FROM sys.database_principals dp
    WHERE dp.sid = SUSER_SID(@OriginalLogin)
      AND dp.type IN ('S', 'U')
    ORDER BY
        CASE dp.type
            WHEN 'S' THEN 1
            ELSE 2
        END;

    IF @CallerDbUser IS NULL
        SET @CallerDbUser = @OriginalLogin;

    SELECT TOP 1 @CallerRole = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = @CallerDbUser
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY CASE r.name
                WHEN N'NGANHANG'  THEN 1
                WHEN N'CHINHANH'  THEN 2
                WHEN N'KHACHHANG' THEN 3
             END;

    IF @CallerRole IS NULL AND IS_SRVROLEMEMBER('sysadmin', @OriginalLogin) = 1
        SET @CallerRole = N'NGANHANG';

    IF @CallerRole IS NULL
    BEGIN
        RAISERROR(N'Nguoi goi khong thuoc role hop le, khong the cap nhat NGUOIDUNG.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'NGANHANG' AND @UserGroup <> 0
    BEGIN
        RAISERROR(N'Role NGANHANG chi duoc tao account nhom NganHang.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'CHINHANH' AND @UserGroup NOT IN (1, 2)
    BEGIN
        RAISERROR(N'Role CHINHANH chi duoc tao account nhom ChiNhanh hoac KhachHang.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'Role KHACHHANG khong duoc phep tao account.', 16, 1);
        RETURN;
    END

    IF @CallerRole = N'CHINHANH'
    BEGIN
        DECLARE @CallerBranch nChar(10) = NULL;
        SELECT TOP 1
            @CallerBranch = DefaultBranch,
            @CallerEmployeeId = EmployeeId
        FROM dbo.NGUOIDUNG
        WHERE Username IN (@OriginalLogin, @CallerDbUser)
          AND TrangThaiXoa = 0;

        IF @CallerBranch IS NULL
        BEGIN
            SELECT TOP 1 @CallerBranch = MACN
            FROM dbo.NHANVIEN
            WHERE MANV IN (@OriginalLogin, @CallerEmployeeId)
              AND TrangThaiXoa = 0;
        END

        IF @CallerBranch IS NULL
        BEGIN
            RAISERROR(N'Khong xac dinh duoc chi nhanh cua user CHINHANH dang tao account.', 16, 1);
            RETURN;
        END

        IF RTRIM(@CallerBranch) <> @DefaultBranch
        BEGIN
            RAISERROR(N'User CHINHANH chi duoc tao account trong chi nhanh cua minh.', 16, 1);
            RETURN;
        END
    END

    IF @UserGroup = 1
    BEGIN
        IF @EmployeeId IS NULL
        BEGIN
            RAISERROR(N'EmployeeId bat buoc voi nhom ChiNhanh.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.NHANVIEN
            WHERE MANV = @EmployeeId
              AND TrangThaiXoa = 0
        )
        BEGIN
            RAISERROR(N'EmployeeId %s khong ton tai trong NHANVIEN.', 16, 1, @EmployeeId);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.NHANVIEN
            WHERE MANV = @EmployeeId
              AND TrangThaiXoa = 0
              AND RTRIM(MACN) = @DefaultBranch
        )
        BEGIN
            RAISERROR(N'EmployeeId %s khong thuoc chi nhanh %s.', 16, 1, @EmployeeId, @DefaultBranch);
            RETURN;
        END

        SET @CustomerCMND = NULL;
    END
    ELSE IF @UserGroup = 2
    BEGIN
        IF @CustomerCMND IS NULL
        BEGIN
            RAISERROR(N'CustomerCMND bat buoc voi nhom KhachHang.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.KHACHHANG
            WHERE CMND = @CustomerCMND
              AND TrangThaiXoa = 0
        )
        BEGIN
            RAISERROR(N'CustomerCMND %s khong ton tai trong KHACHHANG.', 16, 1, @CustomerCMND);
            RETURN;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.KHACHHANG
            WHERE CMND = @CustomerCMND
              AND TrangThaiXoa = 0
              AND RTRIM(MACN) = @DefaultBranch
        )
        BEGIN
            RAISERROR(N'CustomerCMND %s khong thuoc chi nhanh %s.', 16, 1, @CustomerCMND, @DefaultBranch);
            RETURN;
        END

        SET @EmployeeId = NULL;
    END
    ELSE
    BEGIN
        SET @EmployeeId = NULL;
        SET @CustomerCMND = NULL;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.database_principals
        WHERE name = @Username
          AND type IN ('S', 'U')
    )
    BEGIN
        RAISERROR(N'Username %s chua duoc tao SQL login/user trong database.', 16, 1, @Username);
        RETURN;
    END

    DECLARE @ExpectedRole nvarchar(128) =
        CASE @UserGroup
            WHEN 0 THEN N'NGANHANG'
            WHEN 1 THEN N'CHINHANH'
            WHEN 2 THEN N'KHACHHANG'
        END;

    IF NOT EXISTS (
        SELECT 1
        FROM   sys.database_role_members rm
        JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
        JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
        WHERE  u.name = @Username AND r.name = @ExpectedRole
    )
    BEGIN
        RAISERROR(N'Username %s chua thuoc role SQL tuong ung (%s).', 16, 1, @Username, @ExpectedRole);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = @Username)
    BEGIN
        RAISERROR(N'Username %s da ton tai trong NGUOIDUNG. Vui long dung chuc nang cap nhat phu hop.', 16, 1, @Username);
        RETURN;
    END

    INSERT INTO dbo.NGUOIDUNG
        (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa)
    VALUES
        (@Username, @PasswordHash, @UserGroup, @DefaultBranch, @CustomerCMND, @EmployeeId, 0);
END
GO
