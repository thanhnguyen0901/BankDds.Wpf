USE NGANHANG_PUB;
GO
-- Transitional only (to be removed in Phase D).
-- Current app Admin module still calls these procedures.
-- Planned removal after switching app to sp_TaoTaiKhoan/sp_XoaTaiKhoan/sp_DoiMatKhau.
GO
-- Nhóm thủ tục người dùng và chi nhánh.
CREATE OR ALTER PROCEDURE dbo.SP_GetUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Username, PasswordHash, UserGroup, DefaultBranch,
           CustomerCMND, EmployeeId, TrangThaiXoa
    FROM   dbo.NGUOIDUNG
    WHERE  Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Username, PasswordHash, UserGroup, DefaultBranch,
           CustomerCMND, EmployeeId, TrangThaiXoa
    FROM   dbo.NGUOIDUNG
    ORDER BY Username ASC;
END
GO
IF OBJECT_ID('dbo.SP_AddUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddUser;
GO

CREATE OR ALTER PROCEDURE dbo.USP_AddUser
    @Username      nvarchar(50),
    @PasswordHash  nvarchar(255),
    @UserGroup     int,
    @DefaultBranch nvarchar(20),
    @CustomerCMND  nChar(10)    = NULL,
    @EmployeeId    nChar(10)    = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = @Username)
        RETURN; 
    INSERT INTO dbo.NGUOIDUNG
        (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa)
    VALUES
        (@Username, @PasswordHash, @UserGroup, @DefaultBranch, @CustomerCMND, @EmployeeId, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_UpdateUser
    @Username      nvarchar(50),
    @PasswordHash  nvarchar(255),
    @UserGroup     int,
    @DefaultBranch nvarchar(20),
    @CustomerCMND  nChar(10)    = NULL,
    @EmployeeId    nChar(10)    = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    PasswordHash  = @PasswordHash,
           UserGroup     = @UserGroup,
           DefaultBranch = @DefaultBranch,
           CustomerCMND  = @CustomerCMND,
           EmployeeId    = @EmployeeId
    WHERE  Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_SoftDeleteUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 1
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 0; 
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_RestoreUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 0
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 1;  
END
GO
