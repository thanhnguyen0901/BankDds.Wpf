/*=============================================================================
  12-sp-accounts.sql — Stored Procedures for TAIKHOAN
  Generated: 2026-02-18

  EXECUTION:
    Run SECTION A on SERVER1 (NGANHANG_BT)
    Run SECTION A on SERVER2 (NGANHANG_TD)
    Run SECTION B on SERVER3 (NGANHANG)    ← Bank_Main SP_GetAllAccounts

  NOTE: SP_DeductFromAccount and SP_AddToAccount are called WITHOUT their own
  transaction by SqlTransactionRepository (the C# layer manages the SqlTransaction
  for same-branch transfers). They should NOT call BEGIN/COMMIT TRANSACTION.
=============================================================================*/


/* =========================================================================
   SECTION A — Branch Databases  (run on NGANHANG_BT and NGANHANG_TD)
   ========================================================================= */

-- USE NGANHANG_BT;  -- ← uncomment for SERVER1
-- USE NGANHANG_TD;  -- ← uncomment for SERVER2
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsByBranch
-- Called by: SqlAccountRepository.GetAccountsByBranchAsync
--            GetConnectionStringForBranch(branchCode)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccountsByBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccountsByBranch;
GO
CREATE PROCEDURE dbo.SP_GetAccountsByBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    WHERE  MACN = @MACN
    ORDER BY SOTK ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsByCustomer
-- Called by: SqlAccountRepository.GetAccountsByCustomerAsync
--            GetConnectionStringForBranch(session branch)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccountsByCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccountsByCustomer;
GO
CREATE PROCEDURE dbo.SP_GetAccountsByCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    WHERE  CMND = @CMND
    ORDER BY NGAYMOTK DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccount
-- Called by: SqlAccountRepository.GetAccountAsync
--            GetConnectionStringForBranch(session branch)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccount;
GO
CREATE PROCEDURE dbo.SP_GetAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    WHERE  SOTK = @SOTK;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddAccount
-- Called by: SqlAccountRepository.AddAccountAsync
--            GetConnectionStringForBranch(session branch)
-- Returns: rows affected (1 = success, 0 = duplicate SOTK)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddAccount;
GO
CREATE PROCEDURE dbo.SP_AddAccount
    @SOTK     nChar(9),
    @CMND     nChar(10),
    @SODU     money,
    @MACN     nChar(10),
    @NGAYMOTK datetime
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = @SOTK)
        RETURN;   -- duplicate → 0 rows affected
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (@SOTK, @CMND, @SODU, @MACN, @NGAYMOTK, N'Active');
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateAccount
-- Called by: SqlAccountRepository.UpdateAccountAsync
-- Parameters: @SOTK, @SODU, @Status
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_UpdateAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_UpdateAccount;
GO
CREATE PROCEDURE dbo.SP_UpdateAccount
    @SOTK   nChar(9),
    @SODU   money,
    @Status nvarchar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN
    SET    SODU = @SODU, Status = @Status
    WHERE  SOTK = @SOTK;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteAccount  (hard-delete; only succeeds when SODU = 0)
-- Called by: SqlAccountRepository.DeleteAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_DeleteAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DeleteAccount;
GO
CREATE PROCEDURE dbo.SP_DeleteAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.TAIKHOAN
    WHERE  SOTK = @SOTK
      AND  SODU = 0;       -- cannot delete an account that still holds funds
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CloseAccount  (marks Status = 'Closed')
-- Called by: SqlAccountRepository.CloseAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_CloseAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_CloseAccount;
GO
CREATE PROCEDURE dbo.SP_CloseAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Closed'
    WHERE  SOTK = @SOTK AND Status = N'Active';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_ReopenAccount  (restores Status = 'Active')
-- Called by: SqlAccountRepository.ReopenAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_ReopenAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_ReopenAccount;
GO
CREATE PROCEDURE dbo.SP_ReopenAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Active'
    WHERE  SOTK = @SOTK AND Status = N'Closed';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeductFromAccount  (balance debit — NO own transaction)
-- Called by:
--   SqlTransactionRepository.ExecuteSameBranchTransferAsync  (C# SqlTransaction)
--   SqlAccountRepository.AtomicTransferAsync                 (C# SqlTransaction)
-- C# checks ExecuteNonQueryAsync() > 0; returns 0 if balance < Amount or
-- account does not exist / is Closed.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_DeductFromAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DeductFromAccount;
GO
CREATE PROCEDURE dbo.SP_DeductFromAccount
    @SOTK   nChar(9),
    @Amount money
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN
    SET    SODU = SODU - @Amount
    WHERE  SOTK   = @SOTK
      AND  Status = N'Active'
      AND  SODU  >= @Amount;   -- prevents negative balance
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddToAccount  (balance credit — NO own transaction)
-- Called by: same C# transaction wrappers as SP_DeductFromAccount above.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_AddToAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_AddToAccount;
GO
CREATE PROCEDURE dbo.SP_AddToAccount
    @SOTK   nChar(9),
    @Amount money
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN
    SET    SODU = SODU + @Amount
    WHERE  SOTK   = @SOTK
      AND  Status = N'Active';
END
GO


/* =========================================================================
   SECTION B — Bank_Main (run on SERVER3 / NGANHANG)
   ========================================================================= */

USE NGANHANG;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllAccounts
-- Called by: SqlAccountRepository.GetAllAccountsAsync  (GetBankConnection — GAP-07)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAllAccounts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAllAccounts;
GO
CREATE PROCEDURE dbo.SP_GetAllAccounts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN_ALL
    ORDER BY MACN ASC, SOTK ASC;
END
GO
