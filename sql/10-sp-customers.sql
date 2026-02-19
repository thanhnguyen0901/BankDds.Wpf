/*=============================================================================
  10-sp-customers.sql — Stored Procedures for KHACHHANG
  Generated: 2026-02-18

  EXECUTION:
    Run SECTION A on SERVER1 (NGANHANG_BT)
    Run SECTION A on SERVER2 (NGANHANG_TD)
    Run SECTION B on SERVER3 (NGANHANG)    ← Bank_Main cross-branch SP

  C# callers:
    SqlCustomerRepository  → GetConnectionStringForBranch(branch)
    SqlReportRepository    → GetBankConnection() when branchCode is NULL/ALL
=============================================================================*/


/* =========================================================================
   SECTION A — Branch Databases  (run on NGANHANG_BT and NGANHANG_TD)
   ========================================================================= */

-- USE NGANHANG_BT;  -- ← uncomment for SERVER1
-- USE NGANHANG_TD;  -- ← uncomment for SERVER2
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetCustomersByBranch
-- Called by:
--   SqlCustomerRepository.GetCustomersByBranchAsync  param: @MACN
--   SqlReportRepository.GetCustomersByBranchAsync    param: @BranchCode
-- Both parameter names are declared; COALESCE selects whichever is non-NULL.
-- GAP-06: result set must be ORDER BY HO ASC, TEN ASC.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetCustomersByBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetCustomersByBranch;
GO
CREATE PROCEDURE dbo.SP_GetCustomersByBranch
    @MACN       nChar(10) = NULL,
    @BranchCode nChar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Branch nChar(10) = COALESCE(@MACN, @BranchCode);
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG
    WHERE  (@Branch IS NULL OR MACN = @Branch)
    ORDER BY HO ASC, TEN ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetCustomerByCMND
-- Called by: SqlCustomerRepository.GetCustomerByCMNDAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetCustomerByCMND', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetCustomerByCMND;
GO
CREATE PROCEDURE dbo.SP_GetCustomerByCMND
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG
    WHERE  CMND = @CMND;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddCustomer
-- Called by: SqlCustomerRepository.AddCustomerAsync
-- Returns:   rows affected (1 = success, 0 = duplicate CMND)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddCustomer;
GO
CREATE PROCEDURE dbo.SP_AddCustomer
    @CMND     nChar(10),
    @HO       nvarchar(50),
    @TEN      nvarchar(10),
    @NGAYSINH date          = NULL,
    @DIACHI   nvarchar(100) = NULL,
    @NGAYCAP  date          = NULL,
    @SODT      nvarchar(15)  = NULL,
    @PHAI     nChar(3),
    @MACN     nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;   -- caller checks ExecuteNonQueryAsync() > 0
    IF EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = @CMND)
        RETURN;        -- returns 0 rows affected
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (@CMND, @HO, @TEN, @NGAYSINH, @DIACHI, @NGAYCAP, @SODT, @PHAI, @MACN, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateCustomer
-- Called by: SqlCustomerRepository.UpdateCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_UpdateCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_UpdateCustomer;
GO
CREATE PROCEDURE dbo.SP_UpdateCustomer
    @CMND     nChar(10),
    @HO       nvarchar(50),
    @TEN      nvarchar(10),
    @NGAYSINH date          = NULL,
    @DIACHI   nvarchar(100) = NULL,
    @NGAYCAP  date          = NULL,
    @SODT      nvarchar(15)  = NULL,
    @PHAI     nChar(3),
    @MACN     nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG
    SET    HO = @HO, TEN = @TEN, NGAYSINH = @NGAYSINH, DIACHI = @DIACHI,
           NGAYCAP = @NGAYCAP, SODT = @SODT, PHAI = @PHAI, MACN = @MACN
    WHERE  CMND = @CMND;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteCustomer  (soft-delete: sets TrangThaiXoa = 1)
-- Called by: SqlCustomerRepository.DeleteCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_DeleteCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DeleteCustomer;
GO
CREATE PROCEDURE dbo.SP_DeleteCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 1 WHERE CMND = @CMND AND TrangThaiXoa = 0;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreCustomer  (clears soft-delete: sets TrangThaiXoa = 0)
-- Called by: SqlCustomerRepository.RestoreCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_RestoreCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_RestoreCustomer;
GO
CREATE PROCEDURE dbo.SP_RestoreCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 0 WHERE CMND = @CMND AND TrangThaiXoa = 1;
END
GO


/* =========================================================================
   SECTION B — Bank_Main (run on SERVER3 / NGANHANG)
   Queries KHACHHANG_ALL view which unions both branch servers via Linked Servers.
   ========================================================================= */

USE NGANHANG;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllCustomers
-- Called by: SqlCustomerRepository.GetAllCustomersAsync  (uses GetBankConnection)
-- Returns all customers from all branches, sorted by HO ASC, TEN ASC.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAllCustomers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAllCustomers;
GO
CREATE PROCEDURE dbo.SP_GetAllCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG_ALL
    ORDER BY HO ASC, TEN ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetCustomersByBranch  (Bank_Main version — used by reports with @BranchCode NULL)
-- Called by: SqlReportRepository.GetCustomersByBranchAsync  (uses GetBankConnection when ALL)
-- GAP-06: ORDER BY HO ASC, TEN ASC mandatory.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetCustomersByBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetCustomersByBranch;
GO
CREATE PROCEDURE dbo.SP_GetCustomersByBranch
    @MACN       nChar(10) = NULL,
    @BranchCode nChar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Branch nChar(10) = COALESCE(@MACN, @BranchCode);
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG_ALL
    WHERE  (@Branch IS NULL OR MACN = @Branch)
    ORDER BY HO ASC, TEN ASC;
END
GO
