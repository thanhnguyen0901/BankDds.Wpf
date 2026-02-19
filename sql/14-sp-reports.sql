/*=============================================================================
  14-sp-reports.sql — Stored Procedures for Reports
  Generated: 2026-02-18

  ROUTING (mirroring SqlReportRepository connection logic):
    SP_GetAccountStatement       → branch server  (GetConnectionStringForBranch)
    SP_GetAccountsOpenedInPeriod → branch server when branchCode set;
                                   Bank_Main  when branchCode is NULL
    SP_GetCustomersByBranch      → Bank_Main  (GetBankConnection) for reports
                                   (branch-specific version is in 10-sp-customers.sql)
    SP_GetTransactionSummary     → branch server when branchCode set;
                                   Bank_Main  when branchCode is NULL

  EXECUTION:
    Run SECTION A on SERVER1 (NGANHANG_BT) AND SERVER2 (NGANHANG_TD)
    Run SECTION B on SERVER3 (NGANHANG)
=============================================================================*/


/* =========================================================================
   SECTION A — Branch Databases  (run on NGANHANG_BT and NGANHANG_TD)
   ========================================================================= */

-- USE NGANHANG_BT;  -- ← uncomment for SERVER1
-- USE NGANHANG_TD;  -- ← uncomment for SERVER2
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountStatement
-- Called by: SqlReportRepository.GetAccountStatementAsync
--            GetConnectionStringForBranch(account's branch)
--
-- Returns TWO result sets (read in C# with NextResultAsync):
--   RS1 (1 row):  SOTK nChar(9), OpeningBalance money
--   RS2 (n rows): MAGD, NGAYGD, LOAIGD, SOTIEN, OpeningBal money,
--                 RunningBalance money, Description nvarchar, IsDebit bit
--                 ORDER BY NGAYGD ASC
--
-- Opening balance = balance at start of @TuNgay (i.e., the current balance
-- after reversing all transactions within the period).
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccountStatement', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccountStatement;
GO
CREATE PROCEDURE dbo.SP_GetAccountStatement
    @SOTK   nChar(9),
    @TuNgay  datetime,
    @DenNgay datetime
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Step 1: Derive opening balance ───────────────────────────────────────
    -- Start from the current balance and work backwards:
    --   add back   : withdrawals (RT) and outgoing transfers (CT) in the period
    --   subtract   : deposits  (GT) and incoming transfers              in the period
    DECLARE @OpeningBal money;

    SELECT @OpeningBal =
        ISNULL((SELECT SODU FROM dbo.TAIKHOAN WHERE SOTK = @SOTK), 0)
        -- reverse withdrawals in period
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'RT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- reverse outgoing transfers in period
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_CHUYEN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- reverse deposits in period
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'GT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- reverse incoming transfers in period
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_NHAN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0);

    -- ── RS1: account + opening balance ──────────────────────────────────────
    SELECT @SOTK AS SOTK, @OpeningBal AS OpeningBalance;

    -- ── RS2: transaction lines with running balance ──────────────────────────
    ;WITH AllTx AS (
        -- Deposits and withdrawals
        SELECT
            MAGD,
            NGAYGD,
            LOAIGD,
            SOTIEN,
            MANV,
            CAST(NULL AS nChar(9)) AS SOTK_NHAN,
            Status,
            ErrorMessage,
            CASE LOAIGD WHEN N'RT' THEN -SOTIEN ELSE SOTIEN END  AS SignedAmount,
            CAST(CASE LOAIGD WHEN N'RT' THEN 1 ELSE 0 END AS bit) AS IsDebit
        FROM dbo.GD_GOIRUT
        WHERE SOTK    = @SOTK
          AND NGAYGD BETWEEN @TuNgay AND @DenNgay
          AND Status  = N'Completed'

        UNION ALL

        -- Outgoing transfers
        SELECT
            MAGD, NGAYGD, LOAIGD, SOTIEN, MANV, SOTK_NHAN,
            Status, ErrorMessage,
            -SOTIEN AS SignedAmount,
            CAST(1 AS bit) AS IsDebit
        FROM dbo.GD_CHUYENTIEN
        WHERE SOTK_CHUYEN = @SOTK
          AND NGAYGD BETWEEN @TuNgay AND @DenNgay
          AND Status = N'Completed'

        UNION ALL

        -- Incoming transfers (recorded locally; cross-branch ones may be missing —
        -- see file header note about federated limitation)
        SELECT
            MAGD, NGAYGD, LOAIGD, SOTIEN, MANV, SOTK_NHAN,
            Status, ErrorMessage,
            SOTIEN AS SignedAmount,
            CAST(0 AS bit) AS IsDebit
        FROM dbo.GD_CHUYENTIEN
        WHERE SOTK_NHAN = @SOTK
          AND NGAYGD BETWEEN @TuNgay AND @DenNgay
          AND Status = N'Completed'
    )
    SELECT
        MAGD,
        NGAYGD,
        LOAIGD,
        SOTIEN,
        -- Opening balance before THIS row (running total of all preceding rows)
        ISNULL(
            @OpeningBal + SUM(SignedAmount) OVER (
                ORDER BY NGAYGD ASC, MAGD ASC
                ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
            ),
            @OpeningBal
        ) AS OpeningBal,
        -- Balance after THIS row
        @OpeningBal + SUM(SignedAmount) OVER (
            ORDER BY NGAYGD ASC, MAGD ASC
            ROWS UNBOUNDED PRECEDING
        ) AS RunningBalance,
        -- Human-readable description
        CAST(
            CASE LOAIGD
                WHEN N'GT' THEN N'Gửi tiền'
                WHEN N'RT' THEN N'Rút tiền'
                ELSE
                    CASE IsDebit
                        WHEN 1 THEN N'Chuyển tiền đi (' + RTRIM(ISNULL(SOTK_NHAN, N'')) + N')'
                        ELSE        N'Nhận chuyển khoản (' + RTRIM(MAGD) + N')'
                    END
            END
        AS nvarchar(200)) AS Description,
        IsDebit
    FROM AllTx
    ORDER BY NGAYGD ASC, MAGD ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsOpenedInPeriod  (branch version — local TAIKHOAN only)
-- Called by: SqlReportRepository.GetAccountsOpenedInPeriodAsync
--            when @BranchCode is not null → GetConnectionStringForBranch
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccountsOpenedInPeriod', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccountsOpenedInPeriod;
GO
CREATE PROCEDURE dbo.SP_GetAccountsOpenedInPeriod
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    WHERE  NGAYMOTK BETWEEN CAST(@FromDate AS date) AND CAST(@ToDate AS date)
      AND  (@BranchCode IS NULL OR MACN = @BranchCode)
    ORDER BY NGAYMOTK ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionSummary  (branch version — local tables only)
-- Called by: SqlReportRepository.GetTransactionSummaryAsync
--            when @BranchCode is not null → GetConnectionStringForBranch
--
-- Returns TWO result sets:
--   RS1 (1 row):  TotalCount, DepositCount, WithdrawalCount, TransferCount,
--                 TotalDepositAmount, TotalWithdrawalAmount, TotalTransferAmount
--   RS2 (n rows): MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
--                 SOTK_NHAN, Status, ErrorMessage
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetTransactionSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetTransactionSummary;
GO
CREATE PROCEDURE dbo.SP_GetTransactionSummary
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1: Aggregate totals ────────────────────────────────────────────────
    SELECT
        (
            SELECT COUNT(*) FROM dbo.GD_GOIRUT gr
            JOIN dbo.TAIKHOAN tk ON tk.SOTK = gr.SOTK
            WHERE gr.NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)
        ) +
        (
            SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN ct
            JOIN dbo.TAIKHOAN tk ON tk.SOTK = ct.SOTK
            WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)
        ) AS TotalCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT gr
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'GT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode))  AS DepositCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT gr
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'RT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode))  AS WithdrawalCount,

        (SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN ct
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = ct.SOTK
         WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode))  AS TransferCount,

        ISNULL((SELECT SUM(gr.SOTIEN) FROM dbo.GD_GOIRUT gr
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'GT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalDepositAmount,

        ISNULL((SELECT SUM(gr.SOTIEN) FROM dbo.GD_GOIRUT gr
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'RT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalWithdrawalAmount,

        ISNULL((SELECT SUM(ct.SOTIEN) FROM dbo.GD_CHUYENTIEN ct
         JOIN dbo.TAIKHOAN tk ON tk.SOTK = ct.SOTK
         WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalTransferAmount;

    -- ── RS2: Transaction detail rows ─────────────────────────────────────────
    SELECT gr.MAGD, gr.SOTK, gr.LOAIGD, gr.NGAYGD, gr.SOTIEN, gr.MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, gr.Status, gr.ErrorMessage
    FROM   dbo.GD_GOIRUT gr
    JOIN   dbo.TAIKHOAN  tk ON tk.SOTK = gr.SOTK
    WHERE  gr.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR tk.MACN = @BranchCode)

    UNION ALL

    SELECT ct.MAGD, ct.SOTK_CHUYEN AS SOTK, ct.LOAIGD, ct.NGAYGD, ct.SOTIEN, ct.MANV,
           ct.SOTK_NHAN, ct.Status, ct.ErrorMessage
    FROM   dbo.GD_CHUYENTIEN ct
    JOIN   dbo.TAIKHOAN      tk ON tk.SOTK = ct.SOTK_CHUYEN
    WHERE  ct.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR tk.MACN = @BranchCode)

    ORDER BY NGAYGD DESC;
END
GO


/* =========================================================================
   SECTION B — Bank_Main  (run on SERVER3 / NGANHANG)
   Requires TAIKHOAN_ALL, GD_GOIRUT_ALL, GD_CHUYENTIEN_ALL views from
   01-schema.sql SECTION D (linked servers must be configured first).
   ========================================================================= */

USE NGANHANG;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsOpenedInPeriod  (Bank_Main ALL-branches version)
-- Called by: SqlReportRepository.GetAccountsOpenedInPeriodAsync
--            when @BranchCode is null → GetBankConnection
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetAccountsOpenedInPeriod', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetAccountsOpenedInPeriod;
GO
CREATE PROCEDURE dbo.SP_GetAccountsOpenedInPeriod
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN_ALL
    WHERE  NGAYMOTK BETWEEN CAST(@FromDate AS date) AND CAST(@ToDate AS date)
      AND  (@BranchCode IS NULL OR MACN = @BranchCode)
    ORDER BY NGAYMOTK ASC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionSummary  (Bank_Main ALL-branches version)
-- Called by: SqlReportRepository.GetTransactionSummaryAsync
--            when @BranchCode is null → GetBankConnection
-- ─────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SP_GetTransactionSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_GetTransactionSummary;
GO
CREATE PROCEDURE dbo.SP_GetTransactionSummary
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1: Aggregate totals ────────────────────────────────────────────────
    SELECT
        (
            SELECT COUNT(*) FROM dbo.GD_GOIRUT_ALL gr
            JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = gr.SOTK
            WHERE gr.NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)
        ) +
        (
            SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN_ALL ct
            JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = ct.SOTK
            WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)
        ) AS TotalCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT_ALL gr
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'GT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)) AS DepositCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT_ALL gr
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'RT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)) AS WithdrawalCount,

        (SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN_ALL ct
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = ct.SOTK
         WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)) AS TransferCount,

        ISNULL((SELECT SUM(gr.SOTIEN) FROM dbo.GD_GOIRUT_ALL gr
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'GT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalDepositAmount,

        ISNULL((SELECT SUM(gr.SOTIEN) FROM dbo.GD_GOIRUT_ALL gr
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = gr.SOTK
         WHERE gr.LOAIGD = N'RT' AND gr.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalWithdrawalAmount,

        ISNULL((SELECT SUM(ct.SOTIEN) FROM dbo.GD_CHUYENTIEN_ALL ct
         JOIN dbo.TAIKHOAN_ALL tk ON tk.SOTK = ct.SOTK
         WHERE ct.NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR tk.MACN = @BranchCode)), 0) AS TotalTransferAmount;

    -- ── RS2: Transaction detail rows ─────────────────────────────────────────
    SELECT gr.MAGD, gr.SOTK, gr.LOAIGD, gr.NGAYGD, gr.SOTIEN, gr.MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, gr.Status, gr.ErrorMessage
    FROM   dbo.GD_GOIRUT_ALL gr
    JOIN   dbo.TAIKHOAN_ALL  tk ON tk.SOTK = gr.SOTK
    WHERE  gr.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR tk.MACN = @BranchCode)

    UNION ALL

    SELECT ct.MAGD, ct.SOTK, ct.LOAIGD, ct.NGAYGD, ct.SOTIEN, ct.MANV,
           ct.SOTK_NHAN, ct.Status, ct.ErrorMessage
    FROM   dbo.GD_CHUYENTIEN_ALL ct
    JOIN   dbo.TAIKHOAN_ALL      tk ON tk.SOTK = ct.SOTK
    WHERE  ct.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR tk.MACN = @BranchCode)

    ORDER BY NGAYGD DESC;
END
GO
