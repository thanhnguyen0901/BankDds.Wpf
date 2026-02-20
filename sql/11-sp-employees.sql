/*=============================================================================
  11-sp-employees.sql — Stored Procedures for NHANVIEN
  Generated: 2026-02-18

  EXECUTION:
    Run SECTION A on SERVER1 (DESKTOP-JBB41QU\SQLSERVER2 / NGANHANG_BT)
    Run SECTION A on SERVER2 (DESKTOP-JBB41QU\SQLSERVER3 / NGANHANG_TD)
    Run SECTION B on Coordinator (DESKTOP-JBB41QU / NGANHANG)  ← Bank_Main SPs + MANV sequence SP

  C# callers:
    SqlEmployeeRepository  → branch and bank connections (see per-SP comments)
=============================================================================*/


/* =========================================================================
   SECTION A — Branch Databases  (run on NGANHANG_BT and NGANHANG_TD)
   ========================================================================= */

-- USE NGANHANG_BT;  -- ← uncomment when running on DESKTOP-JBB41QU\SQLSERVER2 (SERVER1)
-- USE NGANHANG_TD;  -- ← uncomment when running on DESKTOP-JBB41QU\SQLSERVER3 (SERVER2)
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetEmployeesByBranch
-- Called by: SqlEmployeeRepository.GetEmployeesByBranchAsync
--            GetConnectionStringForBranch(branchCode)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetEmployeesByBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetEmployeesByBranch;
GO
CREATE PROCEDURE dbo.SP_GetEmployeesByBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM   dbo.NHANVIEN
    WHERE  MACN = @MACN
    ORDER BY HO ASC, TEN ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetEmployee
-- Called by: SqlEmployeeRepository.GetEmployeeAsync
--            GetConnectionStringForBranch(session branch)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetEmployee;
GO
CREATE PROCEDURE dbo.SP_GetEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM   dbo.NHANVIEN
    WHERE  MANV = @MANV;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddEmployee
-- Called by: SqlEmployeeRepository.AddEmployeeAsync
--            GetConnectionStringForBranch(employee.MACN)
-- Returns: rows affected (1 = success)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddEmployee;
GO
CREATE PROCEDURE dbo.SP_AddEmployee
    @MANV   nChar(10),
    @HO     nvarchar(50),
    @TEN    nvarchar(10),
    @DIACHI nvarchar(100) = NULL,
    @CMND   nChar(10)     = NULL,
    @PHAI   nChar(3),
    @SODT    nvarchar(15)  = NULL,
    @MACN   nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = @MANV)
        RETURN;   -- duplicate MANV → returns 0 rows affected
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (@MANV, @HO, @TEN, @DIACHI, @CMND, @PHAI, @SODT, @MACN, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateEmployee
-- Called by: SqlEmployeeRepository.UpdateEmployeeAsync
--            GetConnectionStringForBranch(session branch)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_UpdateEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_UpdateEmployee;
GO
CREATE PROCEDURE dbo.SP_UpdateEmployee
    @MANV   nChar(10),
    @HO     nvarchar(50),
    @TEN    nvarchar(10),
    @DIACHI nvarchar(100) = NULL,
    @CMND   nChar(10)     = NULL,
    @PHAI   nChar(3),
    @SODT    nvarchar(15)  = NULL,
    @MACN   nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN
    SET    HO = @HO, TEN = @TEN, DIACHI = @DIACHI, CMND = @CMND,
           PHAI = @PHAI, SODT = @SODT, MACN = @MACN
    WHERE  MANV = @MANV;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteEmployee  (soft-delete: TrangThaiXoa = 1)
-- Called by: SqlEmployeeRepository.DeleteEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_DeleteEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DeleteEmployee;
GO
CREATE PROCEDURE dbo.SP_DeleteEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 1 WHERE MANV = @MANV AND TrangThaiXoa = 0;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreEmployee  (restore soft-deleted: TrangThaiXoa = 0)
-- Called by: SqlEmployeeRepository.RestoreEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_RestoreEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_RestoreEmployee;
GO
CREATE PROCEDURE dbo.SP_RestoreEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 0 WHERE MANV = @MANV AND TrangThaiXoa = 1;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_TransferEmployee  (move employee to a different branch)
-- Called by: SqlEmployeeRepository.TransferEmployeeAsync
--            GetBankConnection()  — cross-branch, runs on Bank_Main
--            (In practice this SP is on Bank_Main; included here for completeness.
--             If only same-server transfers occur, run this on each branch server.)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_TransferEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_TransferEmployee;
GO
CREATE PROCEDURE dbo.SP_TransferEmployee
    @MANV      nChar(10),
    @MACN_MOI  nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET MACN = @MACN_MOI WHERE MANV = @MANV;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_EmployeeExists
-- Called by: SqlEmployeeRepository.EmployeeExistsAsync  (GAP-13 uniqueness check)
--            GetConnectionStringForBranch(session branch)
-- Returns a scalar COUNT(1); C# casts result to int and checks > 0.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_EmployeeExists', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_EmployeeExists;
GO
CREATE PROCEDURE dbo.SP_EmployeeExists
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) FROM dbo.NHANVIEN WHERE MANV = @MANV;
END
GO


/* =========================================================================
   SECTION B — Bank_Main (run on Coordinator — DESKTOP-JBB41QU / NGANHANG)
   ========================================================================= */

USE NGANHANG;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllEmployees
-- Called by: SqlEmployeeRepository.GetAllEmployeesAsync  (GetBankConnection)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAllEmployees', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAllEmployees;
GO
CREATE PROCEDURE dbo.SP_GetAllEmployees
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM   dbo.NHANVIEN_ALL
    ORDER BY MACN ASC, HO ASC, TEN ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetNextManv  (GAP-13 collision-free MANV generation)
-- Called by: SqlEmployeeRepository.GenerateEmployeeIdAsync  (GetBankConnection)
-- Returns a scalar nvarchar(10): 'NV' + 8 zero-padded digits from SEQ_MANV.
-- SEQ_MANV is defined in 01-schema.sql (start=5, increment=1, no cycle).
-- The sequence is atomic — concurrent calls always get distinct values.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetNextManv', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetNextManv;
GO
CREATE PROCEDURE dbo.SP_GetNextManv
AS
BEGIN
    SET NOCOUNT ON;
    SELECT N'NV' + RIGHT(N'00000000' + CAST(NEXT VALUE FOR dbo.SEQ_MANV AS nvarchar(8)), 8);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_TransferEmployee  (Bank_Main version — for cross-branch transfers)
-- Uses NHANVIEN_ALL view to update via Linked Server.
-- Note: Linked Server pass-through UPDATE requires the view to be updatable
-- or use OPENQUERY(). For multi-server setup, use the branch-local SP instead
-- and call it directly with the correct branch connection string.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_TransferEmployee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_TransferEmployee;
GO
CREATE PROCEDURE dbo.SP_TransferEmployee
    @MANV      nChar(10),
    @MACN_MOI  nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    -- For cross-server update, determine current MACN and use the branch connection.
    -- This placeholder works if Linked Server view is updatable; otherwise
    -- the application layer must call the branch SP directly.
    DECLARE @MACN_CU nChar(10) = (SELECT MACN FROM dbo.NHANVIEN_ALL WHERE MANV = @MANV);
    IF @MACN_CU = N'BENTHANH'
        UPDATE [SERVER1].[NGANHANG_BT].[dbo].NHANVIEN SET MACN = @MACN_MOI WHERE MANV = @MANV;
    ELSE IF @MACN_CU = N'TANDINH'
        UPDATE [SERVER2].[NGANHANG_TD].[dbo].NHANVIEN SET MACN = @MACN_MOI WHERE MANV = @MANV;
END
GO
