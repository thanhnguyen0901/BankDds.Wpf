USE NGANHANG;
GO
-- Tạo view ánh xạ chi nhánh và server phân mảnh.
CREATE OR ALTER VIEW dbo.view_DanhSachPhanManh
AS
    SELECT TOP 2
        cn.MACN,
        cn.TENCN,
        CASE cn.MACN
            WHEN N'BENTHANH' THEN CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) + N'\SQLSERVER2'
            WHEN N'TANDINH'  THEN CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) + N'\SQLSERVER3'
        END                          AS TENSERVER,
        N'NGANHANG'                  AS TENDB
    FROM  dbo.CHINHANH cn
    WHERE cn.MACN IN (N'BENTHANH', N'TANDINH')
    ORDER BY cn.MACN ASC;
GO
PRINT N'>>> Đã tạo view_DanhSachPhanManh.';
GO
-- Nhóm thủ tục khách hàng.
CREATE OR ALTER PROCEDURE dbo.SP_GetCustomersByBranch
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

CREATE OR ALTER PROCEDURE dbo.SP_GetCustomerByCMND
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG
    WHERE  CMND = @CMND;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_AddCustomer
    @CMND     nChar(10),
    @HO       nvarchar(50),
    @TEN      nvarchar(10),
    @NGAYSINH date          = NULL,
    @DIACHI   nvarchar(100) = NULL,
    @NGAYCAP  date          = NULL,
    @SODT     nvarchar(15)  = NULL,
    @PHAI     nChar(3),
    @MACN     nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;    
    IF EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = @CMND)
        RETURN; 
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (@CMND, @HO, @TEN, @NGAYSINH, @DIACHI, @NGAYCAP, @SODT, @PHAI, @MACN, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_UpdateCustomer
    @CMND     nChar(10),
    @HO       nvarchar(50),
    @TEN      nvarchar(10),
    @NGAYSINH date          = NULL,
    @DIACHI   nvarchar(100) = NULL,
    @NGAYCAP  date          = NULL,
    @SODT     nvarchar(15)  = NULL,
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

CREATE OR ALTER PROCEDURE dbo.SP_DeleteCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 1 WHERE CMND = @CMND AND TrangThaiXoa = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_RestoreCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 0 WHERE CMND = @CMND AND TrangThaiXoa = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetAllCustomers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
    FROM   dbo.KHACHHANG
    WHERE  TrangThaiXoa = 0
    ORDER BY HO ASC, TEN ASC;
END
GO
PRINT N'>>> Đã tạo nhóm thủ tục Khách hàng (7 SP).';
GO
-- Nhóm thủ tục nhân viên.
CREATE OR ALTER PROCEDURE dbo.SP_GetEmployeesByBranch
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

CREATE OR ALTER PROCEDURE dbo.SP_GetEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM   dbo.NHANVIEN
    WHERE  MANV = @MANV;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_AddEmployee
    @MANV   nChar(10),
    @HO     nvarchar(50),
    @TEN    nvarchar(10),
    @DIACHI nvarchar(100) = NULL,
    @CMND   nChar(10)     = NULL,
    @PHAI   nChar(3),
    @SODT   nvarchar(15)  = NULL,
    @MACN   nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = @MANV)
        RETURN;   
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (@MANV, @HO, @TEN, @DIACHI, @CMND, @PHAI, @SODT, @MACN, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_UpdateEmployee
    @MANV   nChar(10),
    @HO     nvarchar(50),
    @TEN    nvarchar(10),
    @DIACHI nvarchar(100) = NULL,
    @CMND   nChar(10)     = NULL,
    @PHAI   nChar(3),
    @SODT   nvarchar(15)  = NULL,
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

CREATE OR ALTER PROCEDURE dbo.SP_DeleteEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 1 WHERE MANV = @MANV AND TrangThaiXoa = 0;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_RestoreEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 0 WHERE MANV = @MANV AND TrangThaiXoa = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_TransferEmployee
    @MANV      nChar(10),
    @MACN_MOI  nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET MACN = @MACN_MOI WHERE MANV = @MANV;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_EmployeeExists
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) FROM dbo.NHANVIEN WHERE MANV = @MANV;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetAllEmployees
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM   dbo.NHANVIEN
    WHERE  TrangThaiXoa = 0
    ORDER BY MACN ASC, HO ASC, TEN ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetNextManv
AS
BEGIN
    SET NOCOUNT ON;
    SELECT N'NV' + RIGHT(N'00000000' + CAST(NEXT VALUE FOR dbo.SEQ_MANV AS nvarchar(8)), 8);
END
GO
PRINT N'>>> Đã tạo nhóm thủ tục Nhân viên (10 SP).';
GO

-- Nhóm thủ tục tài khoản.
CREATE OR ALTER PROCEDURE dbo.SP_GetAccountsByBranch
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

CREATE OR ALTER PROCEDURE dbo.SP_GetAccountsByCustomer
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

CREATE OR ALTER PROCEDURE dbo.SP_GetAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    WHERE  SOTK = @SOTK;
END
GO
 
CREATE OR ALTER PROCEDURE dbo.SP_AddAccount
    @SOTK     nChar(9),
    @CMND     nChar(10),
    @SODU     money,
    @MACN     nChar(10),
    @NGAYMOTK datetime
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = @SOTK)
        RETURN; 
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (@SOTK, @CMND, @SODU, @MACN, @NGAYMOTK, N'Active');
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_UpdateAccount
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

CREATE OR ALTER PROCEDURE dbo.SP_DeleteAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.TAIKHOAN
    WHERE  SOTK = @SOTK
      AND  SODU = 0; 
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CloseAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Closed'
    WHERE  SOTK = @SOTK AND Status = N'Active';
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_ReopenAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Active'
    WHERE  SOTK = @SOTK AND Status = N'Closed';
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_DeductFromAccount
    @SOTK   nChar(9),
    @Amount money
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN
    SET    SODU = SODU - @Amount
    WHERE  SOTK   = @SOTK
      AND  Status = N'Active'
      AND  SODU  >= @Amount;   
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_AddToAccount
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

CREATE OR ALTER PROCEDURE dbo.SP_GetAllAccounts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    ORDER BY MACN ASC, SOTK ASC;
END
GO
PRINT N'>>> Đã tạo nhóm thủ tục Tài khoản (11 SP).';
GO

-- Nhóm thủ tục giao dịch.
CREATE OR ALTER PROCEDURE dbo.SP_GetTransactionsByAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_GOIRUT
    WHERE  SOTK = @SOTK
    UNION ALL

    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_CHUYEN = @SOTK
    UNION ALL

    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_NHAN = @SOTK
    ORDER BY NGAYGD DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetTransactionsByBranch
    @MACN     nChar(10),
    @FromDate datetime,
    @ToDate   datetime
AS
BEGIN
    SET NOCOUNT ON;
    SELECT gr.MAGD, gr.SOTK, gr.LOAIGD, gr.NGAYGD, gr.SOTIEN, gr.MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, gr.Status, gr.ErrorMessage
    FROM   dbo.GD_GOIRUT gr
    WHERE  gr.MACN   = @MACN
      AND  gr.NGAYGD BETWEEN @FromDate AND @ToDate

    UNION ALL

    SELECT ct.MAGD, ct.SOTK_CHUYEN AS SOTK, ct.LOAIGD, ct.NGAYGD, ct.SOTIEN, ct.MANV,
           ct.SOTK_NHAN, ct.Status, ct.ErrorMessage
    FROM   dbo.GD_CHUYENTIEN ct
    WHERE  ct.MACN   = @MACN
      AND  ct.NGAYGD BETWEEN @FromDate AND @ToDate

    ORDER BY NGAYGD DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetDailyWithdrawalTotal
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

CREATE OR ALTER PROCEDURE dbo.SP_GetDailyTransferTotal
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

CREATE OR ALTER PROCEDURE dbo.SP_Deposit
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU + @Amount
        WHERE  SOTK = @SOTK AND Status = N'Active';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK;
            RAISERROR(N'Account not found or not active: %s', 16, 1, @SOTK);
            RETURN;
        END

        INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
        VALUES (@SOTK, N'GT', GETDATE(), @Amount, @MANV,
                (SELECT MACN FROM dbo.TAIKHOAN WHERE SOTK = @SOTK), N'Completed');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_Withdraw
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

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

        INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
        VALUES (@SOTK, N'RT', GETDATE(), @Amount, @MANV,
                (SELECT MACN FROM dbo.TAIKHOAN WHERE SOTK = @SOTK), N'Completed');

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_CreateTransferTransaction
    @SOTK_FROM nChar(9),
    @SOTK_TO   nChar(9),
    @Amount    money,
    @MANV      nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.GD_CHUYENTIEN
        (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (
        @SOTK_FROM, @SOTK_TO, N'CT', GETDATE(), @Amount, @MANV,
        (SELECT MACN FROM dbo.TAIKHOAN WHERE SOTK = @SOTK_FROM), N'Completed'
    );

    SELECT CAST(SCOPE_IDENTITY() AS int); 
END
GO
-- Chuyển khoản liên chi nhánh, có xử lý local và remote qua linked server.
CREATE OR ALTER PROCEDURE dbo.SP_CrossBranchTransfer
    @SOTK_CHUYEN nChar(9),
    @SOTK_NHAN   nChar(9),
    @SOTIEN      money,
    @MANV        nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SOTIEN <= 0
    BEGIN
        RAISERROR(N'RC-6: Transfer amount must be greater than 0.', 16, 1);
        RETURN -6;
    END

    IF RTRIM(@SOTK_CHUYEN) = RTRIM(@SOTK_NHAN)
    BEGIN
        RAISERROR(N'RC-5: Cannot transfer to the same account.', 16, 1);
        RETURN -5;
    END

    DECLARE @srcSODU money, @srcStatus nvarchar(10), @srcMACN nChar(10);
    SELECT @srcSODU = SODU, @srcStatus = Status, @srcMACN = MACN
    FROM   dbo.TAIKHOAN
    WHERE  SOTK = @SOTK_CHUYEN;

    IF @srcSODU IS NULL
    BEGIN
        RAISERROR(N'RC-1: Source account %s not found.', 16, 1, @SOTK_CHUYEN);
        RETURN -1;
    END

    IF @srcStatus <> N'Active'
    BEGIN
        RAISERROR(N'RC-1: Source account %s is not active (status: %s).', 16, 1,
                  @SOTK_CHUYEN, @srcStatus);
        RETURN -1;
    END

    IF @srcSODU < @SOTIEN
    BEGIN
        DECLARE @sAvail nvarchar(30) = CONVERT(nvarchar(30), @srcSODU, 1);
        DECLARE @sReq   nvarchar(30) = CONVERT(nvarchar(30), @SOTIEN,  1);
        RAISERROR(N'RC-2: Insufficient balance in %s. Available: %s, requested: %s.',
                  16, 1, @SOTK_CHUYEN, @sAvail, @sReq);
        RETURN -2;
    END

    DECLARE @dstStatus nvarchar(10) = NULL;
    SELECT @dstStatus = Status
    FROM   dbo.TAIKHOAN
    WHERE  SOTK = @SOTK_NHAN;

    IF @dstStatus IS NOT NULL
    BEGIN
        IF @dstStatus <> N'Active'
        BEGIN
            RAISERROR(N'RC-4: Destination account %s is not active.', 16, 1, @SOTK_NHAN);
            RETURN -4;
        END

        BEGIN TRY
            BEGIN TRANSACTION;

            UPDATE dbo.TAIKHOAN
            SET    SODU = SODU - @SOTIEN
            WHERE  SOTK   = @SOTK_CHUYEN
              AND  SODU  >= @SOTIEN
              AND  Status = N'Active';

            IF @@ROWCOUNT = 0
                THROW 50002, N'RC-2: Debit failed — balance changed concurrently.', 1;

            UPDATE dbo.TAIKHOAN
            SET    SODU = SODU + @SOTIEN
            WHERE  SOTK   = @SOTK_NHAN
              AND  Status = N'Active';

            IF @@ROWCOUNT = 0
                THROW 50004, N'RC-4: Credit to destination failed.', 1;

            INSERT INTO dbo.GD_CHUYENTIEN
                (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
            VALUES
                (@SOTK_CHUYEN, @SOTK_NHAN, N'CT', GETDATE(), @SOTIEN, @MANV,
                 @srcMACN, N'Completed');

            COMMIT TRANSACTION;
            RETURN 0;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    DECLARE @remoteDB nvarchar(128) = N'NGANHANG';

    IF DB_NAME() <> N'NGANHANG'
    BEGIN
        DECLARE @curDB nvarchar(128) = DB_NAME();
        RAISERROR(N'RC-7: Cross-branch transfer not supported from database [%s]. Expected NGANHANG.',
              16, 1, @curDB);
        RETURN -7;
    END

    DECLARE @remoteDstStatus nvarchar(10) = NULL;
    DECLARE @checkSql nvarchar(500) =
        N'SELECT @st = Status FROM [LINK1].' + QUOTENAME(@remoteDB)
        + N'.dbo.TAIKHOAN WHERE SOTK = @tk';

    EXEC sp_executesql @checkSql,
        N'@tk nChar(9), @st nvarchar(10) OUTPUT',
        @SOTK_NHAN, @remoteDstStatus OUTPUT;

    IF @remoteDstStatus IS NULL
    BEGIN
        RAISERROR(N'RC-3: Destination account %s not found on local or remote branch.',
                  16, 1, @SOTK_NHAN);
        RETURN -3;
    END

    IF @remoteDstStatus <> N'Active'
    BEGIN
        RAISERROR(N'RC-4: Destination account %s on remote branch is not active.',
                  16, 1, @SOTK_NHAN);
        RETURN -4;
    END

    DECLARE @creditSql nvarchar(500) =
        N'UPDATE [LINK1].' + QUOTENAME(@remoteDB)
        + N'.dbo.TAIKHOAN SET SODU = SODU + @amt '
        + N'WHERE SOTK = @tk AND Status = N''Active''; '
        + N'SET @rc = @@ROWCOUNT;';

    DECLARE @creditRows int = 0;

    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN DISTRIBUTED TRANSACTION;

        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU - @SOTIEN
        WHERE  SOTK   = @SOTK_CHUYEN
          AND  SODU  >= @SOTIEN
          AND  Status = N'Active';

        IF @@ROWCOUNT = 0
            THROW 50002, N'RC-2: Debit failed — balance changed concurrently.', 1;

        EXEC sp_executesql @creditSql,
            N'@amt money, @tk nChar(9), @rc int OUTPUT',
            @SOTIEN, @SOTK_NHAN, @creditRows OUTPUT;

        IF @creditRows = 0
            THROW 50004, N'RC-4: Remote credit failed — destination may have been modified.', 1;

        INSERT INTO dbo.GD_CHUYENTIEN
            (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
        VALUES
            (@SOTK_CHUYEN, @SOTK_NHAN, N'CT', GETDATE(), @SOTIEN, @MANV,
             @srcMACN, N'Completed');

        COMMIT TRANSACTION;
        SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        THROW;
    END CATCH
END
GO
IF OBJECT_ID(N'dbo.SP_CrossBranchTransfer', N'P') IS NOT NULL
    PRINT N'>>> Đã tạo nhóm thủ tục Giao dịch (8 SP).';
ELSE
    PRINT N'>>> Cảnh báo: Nhóm Giao dịch có thể chưa đầy đủ (chưa tạo được SP_CrossBranchTransfer).';
GO
-- Nhóm thủ tục báo cáo.
CREATE OR ALTER PROCEDURE dbo.SP_GetAccountStatement
    @SOTK    nChar(9),
    @TuNgay  datetime,
    @DenNgay datetime
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OpeningBal money;

    SELECT @OpeningBal =
        ISNULL((SELECT SODU FROM dbo.TAIKHOAN WHERE SOTK = @SOTK), 0)
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'RT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_CHUYEN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'GT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_NHAN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0);

    SELECT @SOTK AS SOTK, @OpeningBal AS OpeningBalance;

    ;WITH AllTx AS (
        SELECT
            MAGD, NGAYGD, LOAIGD, SOTIEN, MANV,
            CAST(NULL AS nChar(9)) AS SOTK_NHAN,
            Status, ErrorMessage,
            CASE LOAIGD WHEN N'RT' THEN -SOTIEN ELSE SOTIEN END  AS SignedAmount,
            CAST(CASE LOAIGD WHEN N'RT' THEN 1 ELSE 0 END AS bit) AS IsDebit
        FROM dbo.GD_GOIRUT
        WHERE SOTK    = @SOTK
          AND NGAYGD BETWEEN @TuNgay AND @DenNgay
          AND Status  = N'Completed'

        UNION ALL

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
        ISNULL(
            @OpeningBal + SUM(SignedAmount) OVER (
                ORDER BY NGAYGD ASC, MAGD ASC
                ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
            ),
            @OpeningBal
        ) AS OpeningBal,
        @OpeningBal + SUM(SignedAmount) OVER (
            ORDER BY NGAYGD ASC, MAGD ASC
            ROWS UNBOUNDED PRECEDING
        ) AS RunningBalance,
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

CREATE OR ALTER PROCEDURE dbo.SP_GetAccountsOpenedInPeriod
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

CREATE OR ALTER PROCEDURE dbo.SP_GetTransactionSummary
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (
            SELECT COUNT(*) FROM dbo.GD_GOIRUT
            WHERE NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR MACN = @BranchCode)
        ) +
        (
            SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN
            WHERE NGAYGD BETWEEN @FromDate AND @ToDate
              AND (@BranchCode IS NULL OR MACN = @BranchCode)
        ) AS TotalCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT
         WHERE LOAIGD = N'GT' AND NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)) AS DepositCount,

        (SELECT COUNT(*) FROM dbo.GD_GOIRUT
         WHERE LOAIGD = N'RT' AND NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)) AS WithdrawalCount,

        (SELECT COUNT(*) FROM dbo.GD_CHUYENTIEN
         WHERE NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)) AS TransferCount,

        ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
         WHERE LOAIGD = N'GT' AND NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)), 0) AS TotalDepositAmount,

        ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
         WHERE LOAIGD = N'RT' AND NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)), 0) AS TotalWithdrawalAmount,

        ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
         WHERE NGAYGD BETWEEN @FromDate AND @ToDate
           AND (@BranchCode IS NULL OR MACN = @BranchCode)), 0) AS TotalTransferAmount;

    SELECT gr.MAGD, gr.SOTK, gr.LOAIGD, gr.NGAYGD, gr.SOTIEN, gr.MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, gr.Status, gr.ErrorMessage
    FROM   dbo.GD_GOIRUT gr
    WHERE  gr.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR gr.MACN = @BranchCode)

    UNION ALL

    SELECT ct.MAGD, ct.SOTK_CHUYEN AS SOTK, ct.LOAIGD, ct.NGAYGD, ct.SOTIEN, ct.MANV,
           ct.SOTK_NHAN, ct.Status, ct.ErrorMessage
    FROM   dbo.GD_CHUYENTIEN ct
    WHERE  ct.NGAYGD BETWEEN @FromDate AND @ToDate
      AND  (@BranchCode IS NULL OR ct.MACN = @BranchCode)

    ORDER BY NGAYGD DESC;
END
GO
PRINT N'>>> Đã tạo nhóm thủ tục Báo cáo (3 SP).';
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
BEGIN
    BEGIN TRY
        DROP PROCEDURE dbo.SP_AddUser;
    END TRY
    BEGIN CATCH
        PRINT N'>>> Cảnh báo: không thể DROP dbo.SP_AddUser (có thể do quyền hoặc object đã đổi trạng thái). Bỏ qua.';
    END CATCH
END
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

CREATE OR ALTER PROCEDURE dbo.SP_GetBranches
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MACN, TENCN, DIACHI, SODT
    FROM   dbo.CHINHANH
    ORDER BY MACN ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_GetBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MACN, TENCN, DIACHI, SODT
    FROM   dbo.CHINHANH
    WHERE  MACN = @MACN;
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_AddBranch
    @MACN   nChar(10),
    @TENCN  nvarchar(50),
    @DIACHI nvarchar(100) = NULL,
    @SODT   varchar(15)   = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = @MACN)
        RETURN; 
    INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
    VALUES (@MACN, @TENCN, @DIACHI, @SODT);
END
GO

CREATE OR ALTER PROCEDURE dbo.SP_UpdateBranch
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

CREATE OR ALTER PROCEDURE dbo.SP_DeleteBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.CHINHANH WHERE MACN = @MACN;
END
GO
PRINT N'>>> Đã tạo nhóm thủ tục Người dùng và Chi nhánh (11 SP).';
GO
-- Kiểm tra nhanh danh sách view và stored procedure đã tạo.
SELECT
    o.type_desc                                AS ObjectType,
    SCHEMA_NAME(o.schema_id) + '.' + o.name    AS ObjectName,
    o.create_date                               AS Created,
    o.modify_date                               AS LastModified
FROM sys.objects o
WHERE o.is_ms_shipped = 0
  AND o.type IN ('P', 'V')                      
  AND o.schema_id = SCHEMA_ID('dbo')
  AND (
      o.name LIKE 'SP[_]%'                      
      OR o.name = 'view_DanhSachPhanManh'       
  )
ORDER BY o.type_desc DESC, o.name ASC;
GO
PRINT N'';
PRINT N'=== Hoàn tất 03_publisher_sp_views.sql ===';
PRINT N'    Đối tượng đã tạo: 1 view + 50 stored procedure = 51';
PRINT N'    Cơ sở dữ liệu: NGANHANG';
PRINT N'    Bước tiếp theo: 04_publisher_security.sql (bước 4/8)';
GO
