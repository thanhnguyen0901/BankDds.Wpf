/*=============================================================================
  13-sp-transactions.sql — Stored Procedures for GD_GOIRUT / GD_CHUYENTIEN
  Generated: 2026-02-18

  EXECUTION:
    Run SECTION A on SERVER1 (DESKTOP-JBB41QU\SQLSERVER2 / NGANHANG_BT)
    AND SERVER2 (DESKTOP-JBB41QU\SQLSERVER3 / NGANHANG_TD)

  DISTRIBUTED TRANSACTIONS:
    SP_CrossBranchTransfer uses BEGIN DISTRIBUTED TRANSACTION + Linked Server.
    MSDTC must be running and configured on both source and destination instances.
    Linked servers (SERVER1 / SERVER2) must be registered beforehand
    (see 16-linked-servers.sql).
    The SP version on each branch server only ever calls the OTHER branch via
    Linked Server — never itself. If you add more branches add a new IF block.
=============================================================================*/


/* =========================================================================
   SECTION A — Branch Databases  (run on NGANHANG_BT and NGANHANG_TD)
   ========================================================================= */

-- USE NGANHANG_BT;  -- ← uncomment when running on DESKTOP-JBB41QU\SQLSERVER2 (SERVER1)
-- USE NGANHANG_TD;  -- ← uncomment when running on DESKTOP-JBB41QU\SQLSERVER3 (SERVER2)
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionsByAccount
-- Called by: SqlTransactionRepository.GetTransactionsByAccountAsync
--            GetConnectionStringForBranch(session branch)
-- Returns: MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, SOTK_NHAN, Status, ErrorMessage
-- Includes: deposits/withdrawals (GD_GOIRUT) and transfers (GD_CHUYENTIEN)
--           where the account is either the source (SOTK) or destination (SOTK_NHAN).
-- LIMITATION (federated): incoming cross-branch transfers are recorded on the
--   source server's GD_CHUYENTIEN.  To show them here, query via Linked Server
--   or implement application-level merging.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetTransactionsByAccount', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetTransactionsByAccount;
GO
CREATE PROCEDURE dbo.SP_GetTransactionsByAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT ON;
    -- Deposits and withdrawals
    SELECT MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_GOIRUT
    WHERE  SOTK = @SOTK

    UNION ALL

    -- Outgoing transfers (this account is source)
    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_CHUYEN = @SOTK

    UNION ALL

    -- Incoming transfers (this account is destination, recorded locally)
    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_NHAN = @SOTK

    ORDER BY NGAYGD DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionsByBranch
-- Called by: SqlTransactionRepository.GetTransactionsByBranchAsync
--            GetConnectionStringForBranch(branchCode)
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetTransactionsByBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetTransactionsByBranch;
GO
CREATE PROCEDURE dbo.SP_GetTransactionsByBranch
    @MACN     nChar(10),
    @FromDate datetime,
    @ToDate   datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT gr.MAGD, gr.SOTK, gr.LOAIGD, gr.NGAYGD, gr.SOTIEN, gr.MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, gr.Status, gr.ErrorMessage
    FROM   dbo.GD_GOIRUT gr
    JOIN   dbo.TAIKHOAN  tk ON tk.SOTK = gr.SOTK
    WHERE  tk.MACN   = @MACN
      AND  gr.NGAYGD BETWEEN @FromDate AND @ToDate

    UNION ALL

    SELECT ct.MAGD, ct.SOTK_CHUYEN AS SOTK, ct.LOAIGD, ct.NGAYGD, ct.SOTIEN, ct.MANV,
           ct.SOTK_NHAN, ct.Status, ct.ErrorMessage
    FROM   dbo.GD_CHUYENTIEN ct
    JOIN   dbo.TAIKHOAN      tk ON tk.SOTK = ct.SOTK_CHUYEN
    WHERE  tk.MACN   = @MACN
      AND  ct.NGAYGD BETWEEN @FromDate AND @ToDate

    ORDER BY NGAYGD DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetDailyWithdrawalTotal
-- Called by: SqlTransactionRepository.GetDailyWithdrawalTotalAsync
-- Returns: scalar money  — total withdrawn from @SOTK on @Date
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetDailyWithdrawalTotal', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetDailyWithdrawalTotal;
GO
CREATE PROCEDURE dbo.SP_GetDailyWithdrawalTotal
    @SOTK nChar(9),
    @Date datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(SUM(SOTIEN), 0)
    FROM   dbo.GD_GOIRUT
    WHERE  SOTK    = @SOTK
      AND  LOAIGD  = N'RT'
      AND  CAST(NGAYGD AS date) = CAST(@Date AS date)
      AND  Status  = N'Completed';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetDailyTransferTotal
-- Called by: SqlTransactionRepository.GetDailyTransferTotalAsync
-- Returns: scalar money  — total outgoing transfers from @SOTK on @Date
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetDailyTransferTotal', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetDailyTransferTotal;
GO
CREATE PROCEDURE dbo.SP_GetDailyTransferTotal
    @SOTK nChar(9),
    @Date datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(SUM(SOTIEN), 0)
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_CHUYEN = @SOTK
      AND  CAST(NGAYGD AS date) = CAST(@Date AS date)
      AND  Status = N'Completed';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_Deposit  (insert GT into GD_GOIRUT, credit TAIKHOAN.SODU)
-- Called by: SqlTransactionRepository.DepositAsync
-- The SP manages its own transaction; C# wraps the call in a simple ExecuteNonQuery.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_Deposit', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Deposit;
GO
CREATE PROCEDURE dbo.SP_Deposit
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Credit the account
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU + @Amount
        WHERE  SOTK = @SOTK AND Status = N'Active';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK;
            RAISERROR(N'Account not found or not active: %s', 16, 1, @SOTK);
            RETURN;
        END

        -- Record the transaction (MAGD is int IDENTITY -- auto-assigned by DB)
        INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
        VALUES (@SOTK, N'GT', GETDATE(), @Amount, @MANV, N'Completed');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_Withdraw  (insert RT into GD_GOIRUT, debit TAIKHOAN.SODU)
-- Called by: SqlTransactionRepository.WithdrawAsync
-- Fails (0 rows) when SODU < @Amount or account Closed.
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_Withdraw', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_Withdraw;
GO
CREATE PROCEDURE dbo.SP_Withdraw
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Debit the account (balance check in WHERE clause)
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU - @Amount
        WHERE  SOTK   = @SOTK
          AND  Status = N'Active'
          AND  SODU  >= @Amount;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK;
            RAISERROR(N'Insufficient funds or account not active: %s', 16, 1, @SOTK);
            RETURN;
        END

        -- Record the transaction (MAGD is int IDENTITY -- auto-assigned by DB)
        INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
        VALUES (@SOTK, N'RT', GETDATE(), @Amount, @MANV, N'Completed');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CreateTransferTransaction
-- Called by: SqlTransactionRepository.ExecuteSameBranchTransferAsync
--            within a C# SqlTransaction (same branch only)
-- Returns: scalar MAGD  (ExecuteScalarAsync in C#)
-- Inserts one row into GD_CHUYENTIEN; does NOT update SODU
-- (SP_DeductFromAccount / SP_AddToAccount handle balance updates separately).
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_CreateTransferTransaction', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_CreateTransferTransaction;
GO
CREATE PROCEDURE dbo.SP_CreateTransferTransaction
    @SOTK_FROM nChar(9),
    @SOTK_TO   nChar(9),
    @Amount    money,
    @MANV      nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- MAGD is int IDENTITY -- auto-assigned by DB
    INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
    VALUES (@SOTK_FROM, @SOTK_TO, N'CT', GETDATE(), @Amount, @MANV, N'Completed');

    SELECT CAST(SCOPE_IDENTITY() AS int);   -- returns new MAGD int to caller   -- scalar return consumed by ExecuteScalarAsync
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CrossBranchTransfer
-- Called by: SqlTransactionRepository.ExecuteCrossBranchTransferAsync
--            Runs on SOURCE branch server with:
--              @SOTK_CHUYEN  — source account (on this server)
--              @SOTK_NHAN    — destination account (on @DEST_BRANCH server)
--              @SOTIEN       — amount
--              @MANV         — employee performing the transfer
--              @DEST_BRANCH  — target branch code ('BENTHANH' or 'TANDINH')
--
-- Requires: MSDTC running + Linked Servers configured (16-linked-servers.sql)
-- Uses BEGIN DISTRIBUTED TRANSACTION so both sides commit or roll back together.
--
-- SERVER1 version targets SERVER2, SERVER2 version targets SERVER1.
-- Both are written below; deploy only the relevant block to each server.
-- ─────────────────────────────────────────────────────────────────────────────

-- ── Version for SERVER1 / NGANHANG_BT  (replace USE if needed) ───────────────
IF OBJECT_ID('dbo.SP_CrossBranchTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_CrossBranchTransfer;
GO
CREATE PROCEDURE dbo.SP_CrossBranchTransfer
    @SOTK_CHUYEN nChar(9),
    @SOTK_NHAN   nChar(9),
    @SOTIEN      money,
    @MANV        nChar(10),
    @DEST_BRANCH nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;   -- auto-rollback on error in distributed tx
    BEGIN DISTRIBUTED TRANSACTION;
    BEGIN TRY
        -- Debit source account (this server)
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU - @SOTIEN
        WHERE  SOTK   = @SOTK_CHUYEN
          AND  Status = N'Active'
          AND  SODU  >= @SOTIEN;

        IF @@ROWCOUNT = 0
            THROW 50001, N'Insufficient funds or account not active at source.', 1;

        -- Credit destination account (remote server via Linked Server)
        IF @DEST_BRANCH = N'TANDINH'          -- SERVER1 → SERVER2
        BEGIN
            UPDATE [SERVER2].[NGANHANG_TD].[dbo].TAIKHOAN
            SET    SODU = SODU + @SOTIEN
            WHERE  SOTK   = @SOTK_NHAN
              AND  Status = N'Active';
        END
        -- Add more IF blocks here for additional branch servers
        ELSE
            THROW 50002, N'Unknown destination branch.', 1;

        IF @@ROWCOUNT = 0
            THROW 50003, N'Destination account not found or not active.', 1;

        -- Record outgoing transfer on this (source) server (MAGD is int IDENTITY)
        INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
        VALUES (@SOTK_CHUYEN, @SOTK_NHAN, N'CT', GETDATE(), @SOTIEN, @MANV, N'Completed');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- == Version for SERVER2 / NGANHANG_TD (deploy to DESKTOP-JBB41QU\SQLSERVER3 ONLY; do NOT run on SERVER1) ==
-- USE NGANHANG_TD;  -- <- uncomment when running on DESKTOP-JBB41QU\SQLSERVER3 (SERVER2)
-- GO
IF OBJECT_ID('dbo.SP_CrossBranchTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_CrossBranchTransfer;
GO
CREATE PROCEDURE dbo.SP_CrossBranchTransfer
    @SOTK_CHUYEN nChar(9),
    @SOTK_NHAN   nChar(9),
    @SOTIEN      money,
    @MANV        nChar(10),
    @DEST_BRANCH nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN DISTRIBUTED TRANSACTION;
    BEGIN TRY
        -- Debit source account (SERVER2)
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU - @SOTIEN
        WHERE  SOTK   = @SOTK_CHUYEN
          AND  Status = N'Active'
          AND  SODU  >= @SOTIEN;

        IF @@ROWCOUNT = 0
            THROW 50001, N'Insufficient funds or account not active at source.', 1;

        -- Credit destination account (SERVER1 via Linked Server)
        IF @DEST_BRANCH = N'BENTHANH'
        BEGIN
            UPDATE [SERVER1].[NGANHANG_BT].[dbo].TAIKHOAN
            SET    SODU = SODU + @SOTIEN
            WHERE  SOTK   = @SOTK_NHAN
              AND  Status = N'Active';
        END
        ELSE
            THROW 50002, N'Unknown destination branch.', 1;

        IF @@ROWCOUNT = 0
            THROW 50003, N'Destination account not found or not active.', 1;

        -- Record outgoing transfer on this (source) server (MAGD is int IDENTITY)
        INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
        VALUES (@SOTK_CHUYEN, @SOTK_NHAN, N'CT', GETDATE(), @SOTIEN, @MANV, N'Completed');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO