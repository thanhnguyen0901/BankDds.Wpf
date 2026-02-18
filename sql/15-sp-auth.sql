/*=============================================================================
  15-sp-auth.sql — Stored Procedures for NGUOIDUNG (Users) and CHINHANH (Branches)
  Generated: 2026-02-18

  All procedures in this file run on SERVER3 / NGANHANG (Bank_Main).
  SqlUserRepository and SqlBranchRepository both use GetBankConnection().

  GAP-12: SP_SoftDeleteUser / SP_RestoreUser use TrangThaiXoa (0=active, 1=deleted)
=============================================================================*/

USE NGANHANG;
GO


/* =========================================================================
   USERS — NGUOIDUNG
   ========================================================================= */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetUser
-- Called by: SqlUserRepository.GetUserAsync
-- Returns: single row for the given Username (including soft-deleted users so
--          the auth layer can reject them explicitly rather than treating them
--          as not found)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetUser;
GO
CREATE PROCEDURE dbo.SP_GetUser
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllUsers
-- Called by: SqlUserRepository.GetAllUsersAsync
-- Returns all users including soft-deleted (C# filters on TrangThaiXoa if needed)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAllUsers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAllUsers;
GO
CREATE PROCEDURE dbo.SP_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Username, PasswordHash, UserGroup, DefaultBranch,
           CustomerCMND, EmployeeId, TrangThaiXoa
    FROM   dbo.NGUOIDUNG
    ORDER BY Username ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddUser
-- Called by: SqlUserRepository.AddUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddUser;
GO
CREATE PROCEDURE dbo.SP_AddUser
    @Username     nvarchar(50),
    @PasswordHash nvarchar(255),
    @UserGroup    int,
    @DefaultBranch nvarchar(20),
    @CustomerCMND nChar(10)    = NULL,
    @EmployeeId   nChar(10)    = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = @Username)
        RETURN;   -- duplicate → 0 rows affected; caller checks
    INSERT INTO dbo.NGUOIDUNG
        (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa)
    VALUES
        (@Username, @PasswordHash, @UserGroup, @DefaultBranch, @CustomerCMND, @EmployeeId, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateUser
-- Called by: SqlUserRepository.UpdateUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_UpdateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_UpdateUser;
GO
CREATE PROCEDURE dbo.SP_UpdateUser
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_SoftDeleteUser  (GAP-12 — sets TrangThaiXoa = 1; preserves record)
-- Called by: SqlUserRepository.SoftDeleteUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_SoftDeleteUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_SoftDeleteUser;
GO
CREATE PROCEDURE dbo.SP_SoftDeleteUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 1
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 0;   -- idempotent: only acts if currently active
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreUser  (GAP-12 — sets TrangThaiXoa = 0; re-activates account)
-- Called by: SqlUserRepository.RestoreUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_RestoreUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_RestoreUser;
GO
CREATE PROCEDURE dbo.SP_RestoreUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 0
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 1;   -- idempotent: only acts if currently deleted
END
GO


/* =========================================================================
   BRANCHES — CHINHANH
   ========================================================================= */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetBranches
-- Called by: SqlBranchRepository.GetBranchesAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetBranches', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetBranches;
GO
CREATE PROCEDURE dbo.SP_GetBranches
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MACN, TENCN, DIACHI, SODT
    FROM   dbo.CHINHANH
    ORDER BY MACN ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetBranch
-- Called by: SqlBranchRepository.GetBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetBranch;
GO
CREATE PROCEDURE dbo.SP_GetBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MACN, TENCN, DIACHI, SODT
    FROM   dbo.CHINHANH
    WHERE  MACN = @MACN;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddBranch
-- Called by: SqlBranchRepository.AddBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddBranch;
GO
CREATE PROCEDURE dbo.SP_AddBranch
    @MACN   nChar(10),
    @TENCN  nvarchar(50),
    @DIACHI nvarchar(100) = NULL,
    @SODT   varchar(15)   = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = @MACN)
        RETURN;   -- duplicate PK → 0 rows affected
    INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
    VALUES (@MACN, @TENCN, @DIACHI, @SODT);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateBranch
-- Called by: SqlBranchRepository.UpdateBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_UpdateBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_UpdateBranch;
GO
CREATE PROCEDURE dbo.SP_UpdateBranch
    @MACN   nChar(10),
    @TENCN  nvarchar(50),
    @DIACHI nvarchar(100) = NULL,
    @SODT   varchar(15)   = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.CHINHANH
    SET    TENCN  = @TENCN,
           DIACHI = @DIACHI,
           SODT   = @SODT
    WHERE  MACN = @MACN;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteBranch
-- Called by: SqlBranchRepository.DeleteBranchAsync
-- Hard-delete; caller must check for dependent employees/accounts first.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_DeleteBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DeleteBranch;
GO
CREATE PROCEDURE dbo.SP_DeleteBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.CHINHANH WHERE MACN = @MACN;
END
GO
