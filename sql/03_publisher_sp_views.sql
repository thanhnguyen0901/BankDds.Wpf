/*=============================================================================
  03_publisher_sp_views.sql
  Vai trò   : Máy chủ phát hành / Điều phối (server gốc)
  Chạy trên : DESKTOP-JBB41QU / NGANHANG_PUB
  Mục đích: Tạo TẤT CẢ stored procedure + view vận hành trên Máy chủ phát hành.
           Với Sao chép hợp nhất, các SP được sao chép đến CN1 và CN2 thông qua
           đối tượng phát hành "stored procedure schema only" — KHÔNG triển khai script này
           lên các máy chủ đăng ký nhận thủ công.

  Cấu hình mạng (Sao chép hợp nhất Ngân hàng — DE3):
    Máy chủ phát hành    : DESKTOP-JBB41QU          → NGANHANG_PUB  (tất cả các hàng)
    CN1 / LINK1  : DESKTOP-JBB41QU\SQLSERVER2 → NGANHANG_BT  (MACN='BENTHANH')
    CN2 / LINK2  : DESKTOP-JBB41QU\SQLSERVER3 → NGANHANG_TD  (MACN='TANDINH')
    TraCuu/LINK0 : DESKTOP-JBB41QU\SQLSERVER4 → NGANHANG_TRACUU (chỉ đọc)

  Sau khi sao chép, Máy chủ phát hành sở hữu TẤT CẢ các hàng cục bộ, nên:
    • Không cần tên bốn phần Linked Server trong bất kỳ SP nào.
    • Các biến thể SP Chi nhánh + Bank_Main được GỘP thành một phiên bản.
    • SP_CrossBranchTransfer được đơn giản hóa thành giao dịch cục bộ (không MSDTC).
    • Các view tiện ích _ALL đã được XÓA BỎ (ngưng sử dụng). Các SP truy vấn
      trực tiếp bảng gốc. Xem docs/migration/00_migration_plan.md § Ngưng sử dụng.

  Các phần:
    0. view_DanhSachPhanManh   (Chỉ máy chủ phát hành, dropdown chi nhánh TOP 2)
    A. SP Khách hàng            (7 SP — từ sql/10-sp-customers.sql)
    B. SP Nhân viên            (10 SP — từ sql/11-sp-employees.sql)
    C. SP Tài khoản             (11 SP — từ sql/12-sp-accounts.sql)
    D. SP Giao dịch         (8 SP — từ sql/13-sp-transactions.sql)
    E. SP Báo cáo              (3 SP — từ sql/14-sp-reports.sql)
    F. SP Xác thực + Chi nhánh       (11 SP — từ sql/15-sp-auth.sql)
    G. Xác minh

  Tổng cộng: 1 view + 50 stored procedure = 51 đối tượng.

  Bất biến lũy đẳng: CÓ — sử dụng CREATE OR ALTER (SQL Server 2016 SP1+).
  THỨ TỰ THỰC THI: Bước 3/8  (Chỉ máy chủ phát hành, sau 02_publisher_schema.sql).
=============================================================================*/

USE NGANHANG_PUB;
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 0 — view_DanhSachPhanManh
   View ngân hàng liệt kê các chi nhánh tham gia phân mảnh.
   Sử dụng bởi dropdown chi nhánh WPF (quy tắc TOP 2).
   Chỉ máy chủ phát hành — KHÔNG sao chép đến máy chủ đăng ký nhận.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF OBJECT_ID('dbo.view_DanhSachPhanManh', 'V') IS NOT NULL
    DROP VIEW dbo.view_DanhSachPhanManh;
GO
CREATE VIEW dbo.view_DanhSachPhanManh
AS
    SELECT TOP 2
        cn.MACN,
        cn.TENCN,
        CASE cn.MACN
            WHEN N'BENTHANH' THEN N'DESKTOP-JBB41QU\SQLSERVER2'
            WHEN N'TANDINH'  THEN N'DESKTOP-JBB41QU\SQLSERVER3'
        END                          AS TENSERVER,
        CASE cn.MACN
            WHEN N'BENTHANH' THEN N'NGANHANG_BT'
            WHEN N'TANDINH'  THEN N'NGANHANG_TD'
        END                          AS TENDB
    FROM  dbo.CHINHANH cn
    WHERE cn.MACN IN (N'BENTHANH', N'TANDINH')
    ORDER BY cn.MACN ASC;
GO

PRINT '>>> Section 0: view_DanhSachPhanManh created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN A — Stored procedure Khách hàng
   Nguồn: sql/10-sp-customers.sql  (Chi nhánh + Bank_Main đã gộp)
   Phía gọi C#: SqlCustomerRepository, SqlReportRepository
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetCustomersByBranch
-- Gọi bởi: SqlCustomerRepository.GetCustomersByBranchAsync  (@MACN)
--            SqlReportRepository.GetCustomersByBranchAsync    (@BranchCode)
-- Cả hai tên tham số đều được khai báo; COALESCE chọn giá trị non-NULL.
-- Khi cả hai là NULL → trả về TẤT CẢ khách hàng (lệnh gọi "tất cả chi nhánh" Bank_Main).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetCustomerByCMND
-- Gọi bởi: SqlCustomerRepository.GetCustomerByCMNDAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddCustomer
-- Gọi bởi: SqlCustomerRepository.AddCustomerAsync
-- Trả về: số hàng bị ảnh hưởng (1 = thành công, 0 = CMND trùng lặp)
-- ─────────────────────────────────────────────────────────────────────────────
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
    SET NOCOUNT OFF;   -- phía gọi kiểm tra ExecuteNonQueryAsync() > 0
    IF EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = @CMND)
        RETURN;        -- trả về 0 hàng bị ảnh hưởng
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (@CMND, @HO, @TEN, @NGAYSINH, @DIACHI, @NGAYCAP, @SODT, @PHAI, @MACN, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateCustomer
-- Gọi bởi: SqlCustomerRepository.UpdateCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteCustomer  (xóa mềm: TrangThaiXoa = 1)
-- Gọi bởi: SqlCustomerRepository.DeleteCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_DeleteCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 1 WHERE CMND = @CMND AND TrangThaiXoa = 0;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreCustomer  (xóa cờ xóa mềm: TrangThaiXoa = 0)
-- Gọi bởi: SqlCustomerRepository.RestoreCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_RestoreCustomer
    @CMND nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.KHACHHANG SET TrangThaiXoa = 0 WHERE CMND = @CMND AND TrangThaiXoa = 1;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllCustomers
-- Gọi bởi: SqlCustomerRepository.GetAllCustomersAsync  (kết nối Bank_Main)
-- Trả về tất cả khách hàng chưa xóa trên tất cả chi nhánh.
-- Truy vấn trực tiếp bảng gốc (Máy chủ phát hành giữ tất cả hàng qua Sao chép hợp nhất).
-- ─────────────────────────────────────────────────────────────────────────────
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

PRINT '>>> Section A: 7 Customer SPs created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN B — Stored procedure Nhân viên
   Nguồn: sql/11-sp-employees.sql  (Chi nhánh + Bank_Main đã gộp)
   Phía gọi C#: SqlEmployeeRepository
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetEmployeesByBranch
-- Gọi bởi: SqlEmployeeRepository.GetEmployeesByBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetEmployee
-- Gọi bởi: SqlEmployeeRepository.GetEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddEmployee
-- Gọi bởi: SqlEmployeeRepository.AddEmployeeAsync
-- Trả về: số hàng bị ảnh hưởng (1 = thành công, 0 = MANV trùng lặp)
-- ─────────────────────────────────────────────────────────────────────────────
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
        RETURN;   -- MANV trùng lặp → trả về 0 hàng bị ảnh hưởng
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (@MANV, @HO, @TEN, @DIACHI, @CMND, @PHAI, @SODT, @MACN, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateEmployee
-- Gọi bởi: SqlEmployeeRepository.UpdateEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteEmployee  (xóa mềm: TrangThaiXoa = 1)
-- Gọi bởi: SqlEmployeeRepository.DeleteEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_DeleteEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 1 WHERE MANV = @MANV AND TrangThaiXoa = 0;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreEmployee  (phục hồi bản ghi đã xóa mềm: TrangThaiXoa = 0)
-- Gọi bởi: SqlEmployeeRepository.RestoreEmployeeAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_RestoreEmployee
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NHANVIEN SET TrangThaiXoa = 0 WHERE MANV = @MANV AND TrangThaiXoa = 1;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_TransferEmployee  (chuyển nhân viên sang chi nhánh khác)
-- Gọi bởi: SqlEmployeeRepository.TransferEmployeeAsync
-- ĐÃ GỘP: Phiên bản Bank_Main cũ sử dụng Linked Server [SERVER1]/[SERVER2].
-- Sau sao chép, tất cả dữ liệu là cục bộ — chỉ cần UPDATE đơn giản.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_TransferEmployee
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
-- Gọi bởi: SqlEmployeeRepository.EmployeeExistsAsync  (kiểm tra tính duy nhất)
-- Trả về giá trị vô hướng COUNT(1); C# ép kiểu kết quả sang int và kiểm tra > 0.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_EmployeeExists
    @MANV nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) FROM dbo.NHANVIEN WHERE MANV = @MANV;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllEmployees
-- Gọi bởi: SqlEmployeeRepository.GetAllEmployeesAsync  (kết nối Bank_Main)
-- Truy vấn trực tiếp bảng gốc (Máy chủ phát hành giữ tất cả hàng qua Sao chép hợp nhất).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetNextManv  (tạo MANV không trùng lặp)
-- Gọi bởi: SqlEmployeeRepository.GenerateEmployeeIdAsync  (kết nối Bank_Main)
-- Trả về giá trị vô hướng nvarchar(10): 'NV' + 8 chữ số đệm 0 từ SEQ_MANV.
-- Sequence đảm bảo ID nguyên tử, không bị gián đoạn giữa các lệnh gọi đồng thời.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetNextManv
AS
BEGIN
    SET NOCOUNT ON;
    SELECT N'NV' + RIGHT(N'00000000' + CAST(NEXT VALUE FOR dbo.SEQ_MANV AS nvarchar(8)), 8);
END
GO

PRINT '>>> Section B: 10 Employee SPs created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN C — Stored procedure Tài khoản
   Nguồn: sql/12-sp-accounts.sql  (Chi nhánh + Bank_Main đã gộp)
   Phía gọi C#: SqlAccountRepository
   LƯU Ý: SP_DeductFromAccount và SP_AddToAccount được gọi KHÔNG có transaction riêng;
         tầng C# quản lý SqlTransaction.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsByBranch
-- Gọi bởi: SqlAccountRepository.GetAccountsByBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsByCustomer
-- Gọi bởi: SqlAccountRepository.GetAccountsByCustomerAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccount
-- Gọi bởi: SqlAccountRepository.GetAccountAsync
-- Sau sao chép, Máy chủ phát hành giữ TẤT CẢ tài khoản — tra cứu liên chi nhánh hoạt động.
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddAccount
-- Gọi bởi: SqlAccountRepository.AddAccountAsync
-- Trả về: số hàng bị ảnh hưởng (1 = thành công, 0 = SOTK trùng lặp)
-- ─────────────────────────────────────────────────────────────────────────────
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
        RETURN;   -- trùng lặp → 0 hàng bị ảnh hưởng
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (@SOTK, @CMND, @SODU, @MACN, @NGAYMOTK, N'Active');
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateAccount
-- Gọi bởi: SqlAccountRepository.UpdateAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteAccount  (xóa cứng; chỉ thành công khi SODU = 0)
-- Gọi bởi: SqlAccountRepository.DeleteAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_DeleteAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.TAIKHOAN
    WHERE  SOTK = @SOTK
      AND  SODU = 0;       -- không thể xóa tài khoản vẫn còn tiền
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CloseAccount  (đánh dấu Status = 'Closed')
-- Gọi bởi: SqlAccountRepository.CloseAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_CloseAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Closed'
    WHERE  SOTK = @SOTK AND Status = N'Active';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_ReopenAccount  (phục hồi Status = 'Active')
-- Gọi bởi: SqlAccountRepository.ReopenAccountAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_ReopenAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.TAIKHOAN SET Status = N'Active'
    WHERE  SOTK = @SOTK AND Status = N'Closed';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeductFromAccount  (ghi nợ số dư — KHÔNG có transaction riêng)
-- Gọi bởi: SqlTransactionRepository (trình bao bọc C# SqlTransaction)
-- C# kiểm tra ExecuteNonQueryAsync() > 0; trả về 0 nếu số dư < Amount.
-- ─────────────────────────────────────────────────────────────────────────────
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
      AND  SODU  >= @Amount;   -- ngăn số dư âm
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddToAccount  (ghi có số dư — KHÔNG có transaction riêng)
-- Gọi bởi: cùng trình bao bọc transaction C# như SP_DeductFromAccount ở trên.
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllAccounts
-- Gọi bởi: SqlAccountRepository.GetAllAccountsAsync  (kết nối Bank_Main)
-- Truy vấn trực tiếp bảng gốc (Máy chủ phát hành giữ tất cả hàng qua Sao chép hợp nhất).
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetAllAccounts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SOTK, CMND, SODU, MACN, NGAYMOTK, Status
    FROM   dbo.TAIKHOAN
    ORDER BY MACN ASC, SOTK ASC;
END
GO

PRINT '>>> Section C: 11 Account SPs created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN D — Stored procedure Giao dịch
   Nguồn: sql/13-sp-transactions.sql  (Chi nhánh + Bank_Main đã gộp)
   Phía gọi C#: SqlTransactionRepository

   THAY ĐỔI CHÍNH so với script liên kết cũ:
   • SP_Deposit / SP_Withdraw giờ điền GD_GOIRUT.MACN khi INSERT.
   • SP_CreateTransferTransaction giờ điền GD_CHUYENTIEN.MACN khi INSERT.
   • SP_CrossBranchTransfer được ĐƠN GIẢN HÓA — không MSDTC / DISTRIBUTED TRANSACTION.
     Tất cả tài khoản là cục bộ trên Máy chủ phát hành; Sao chép hợp nhất truyền
     cập nhật số dư đến máy chủ đăng ký nhận tương ứng.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionsByAccount
-- Gọi bởi: SqlTransactionRepository.GetTransactionsByAccountAsync
-- Trả về: MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, SOTK_NHAN,
--          Status, ErrorMessage
-- Bao gồm gửi tiền/rút tiền (GD_GOIRUT) và chuyển khoản (GD_CHUYENTIEN)
-- trong đó tài khoản là nguồn HOẶC đích.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetTransactionsByAccount
    @SOTK nChar(9)
AS
BEGIN
    SET NOCOUNT ON;
    -- Gửi tiền và rút tiền
    SELECT MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           CAST(NULL AS nChar(9)) AS SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_GOIRUT
    WHERE  SOTK = @SOTK

    UNION ALL

    -- Chuyển khoản đi (tài khoản này là nguồn)
    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_CHUYEN = @SOTK

    UNION ALL

    -- Chuyển khoản đến (tài khoản này là đích)
    SELECT MAGD, SOTK_CHUYEN AS SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
           SOTK_NHAN, Status, ErrorMessage
    FROM   dbo.GD_CHUYENTIEN
    WHERE  SOTK_NHAN = @SOTK

    ORDER BY NGAYGD DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionsByBranch
-- Gọi bởi: SqlTransactionRepository.GetTransactionsByBranchAsync
-- Sử dụng trực tiếp GD_GOIRUT.MACN và GD_CHUYENTIEN.MACN (cả hai bảng giờ
-- có MACN, nên không cần JOIN đến TAIKHOAN để lọc theo chi nhánh).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetDailyWithdrawalTotal
-- Gọi bởi: SqlTransactionRepository.GetDailyWithdrawalTotalAsync
-- Trả về: giá trị vô hướng money — tổng rút tiền từ @SOTK trong @Date
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetDailyTransferTotal
-- Gọi bởi: SqlTransactionRepository.GetDailyTransferTotalAsync
-- Trả về: giá trị vô hướng money — tổng chuyển khoản đi từ @SOTK trong @Date
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_Deposit  (chèn GT vào GD_GOIRUT, ghi có TAIKHOAN.SODU)
-- Gọi bởi: SqlTransactionRepository.DepositAsync
-- SP tự quản lý transaction riêng.
-- LƯU Ý: GD_GOIRUT.MACN được lấy từ chi nhánh của tài khoản.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_Deposit
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Ghi có vào tài khoản
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU + @Amount
        WHERE  SOTK = @SOTK AND Status = N'Active';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK;
            RAISERROR(N'Account not found or not active: %s', 16, 1, @SOTK);
            RETURN;
        END

        -- Ghi nhận giao dịch (MAGD là IDENTITY, MACN từ tài khoản)
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_Withdraw  (chèn RT vào GD_GOIRUT, ghi nợ TAIKHOAN.SODU)
-- Gọi bởi: SqlTransactionRepository.WithdrawAsync
-- Lỗi khi SODU < @Amount hoặc tài khoản đã đóng.
-- LƯU Ý: GD_GOIRUT.MACN được lấy từ chi nhánh của tài khoản.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_Withdraw
    @SOTK   nChar(9),
    @Amount money,
    @MANV   nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Trừ tiền tài khoản (kiểm tra số dư trong mệnh đề WHERE)
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

        -- Ghi nhận giao dịch (MAGD là IDENTITY, MACN từ tài khoản)
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CreateTransferTransaction
-- Gọi bởi: SqlTransactionRepository.ExecuteSameBranchTransferAsync
--            trong một C# SqlTransaction (chỉ cùng chi nhánh)
-- Trả về: scalar MAGD  (ExecuteScalarAsync trong C#)
-- Chèn một dòng vào GD_CHUYENTIEN; KHÔNG cập nhật SODU
-- (SP_DeductFromAccount / SP_AddToAccount xử lý cập nhật số dư riêng).
-- GHI CHÚ: GD_CHUYENTIEN.MACN được lấy từ chi nhánh của tài khoản nguồn.
-- ─────────────────────────────────────────────────────────────────────────────
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

    SELECT CAST(SCOPE_IDENTITY() AS int);   -- trả về MAGD mới cho phía gọi
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_CrossBranchTransfer   (Mẫu chuẩn ngân hàng)
-- Gọi bởi: SqlTransactionRepository.TransferAsync  (một lệnh gọi SP duy nhất)
--
-- Thủ tục chuyển khoản hợp nhất — xử lý CẢ cùng chi nhánh và liên chi nhánh.
--
-- ĐƯỜNG A (cùng chi nhánh / Máy chủ phát hành):
--   Tài khoản đích tìm thấy nội bộ → BEGIN TRANSACTION thông thường.
--   Trên Máy chủ phát hành (NGANHANG_PUB) tất cả tài khoản đều nội bộ, nên đường này
--   luôn được chọn. Không cần MSDTC.
--
-- ĐƯỜNG B (liên chi nhánh trên máy chủ đăng ký nhận):
--   Tài khoản đích KHÔNG tìm thấy nội bộ → BEGIN DISTRIBUTED TRANSACTION qua LINK1.
--   Quy ước đặt tên LINK1 (06_linked_servers.sql):
--     Trên CN1 (NGANHANG_BT): LINK1 → CN2 (NGANHANG_TD)
--     Trên CN2 (NGANHANG_TD): LINK1 → CN1 (NGANHANG_BT)
--   Tên DB từ xa được suy ra từ DB_NAME() — không dùng tên máy chủ vật lý.
--   Mức cô lập SERIALIZABLE bên trong giao dịch phân tán.
--   Yêu cầu MSDTC chạy trên cả hai máy chủ.
--
-- Mã trả về (Quy ước ngân hàng):
--    0 = thành công
--   -1 = tài khoản nguồn không tìm thấy / không hoạt động
--   -2 = số dư không đủ
--   -3 = tài khoản đích không tìm thấy (nội bộ + từ xa)
--   -4 = tài khoản đích không hoạt động / ghi có thất bại
--   -5 = cùng tài khoản
--   -6 = số tiền không hợp lệ
--   -7 = lỗi cấu hình (DB không xác định cho liên chi nhánh)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_CrossBranchTransfer
    @SOTK_CHUYEN nChar(9),
    @SOTK_NHAN   nChar(9),
    @SOTIEN      money,
    @MANV        nChar(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- ── Kiểm tra trước giao dịch ──────────────────────────────────────────

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

    -- Tài khoản nguồn phải tồn tại nội bộ
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

    -- ── Kiểm tra tài khoản đích có nội bộ không ────────────────────────────────────────

    DECLARE @dstStatus nvarchar(10) = NULL;
    SELECT @dstStatus = Status
    FROM   dbo.TAIKHOAN
    WHERE  SOTK = @SOTK_NHAN;

    IF @dstStatus IS NOT NULL
    BEGIN
        -- ═══ ĐƯỜNG A: Cùng chi nhánh (hoặc Máy chủ phát hành) — giao dịch nội bộ ═════════

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

    -- ═══ ĐƯỜNG B: Liên chi nhánh — GIAO DỊCH PHÂN TÁN qua LINK1 ═══════════

    -- Suy ra tên cơ sở dữ liệu từ xa từ tên cơ sở dữ liệu nội bộ.
    -- Trên CN1 (NGANHANG_BT) DB từ xa là NGANHANG_TD và ngược lại.
    -- Trên Máy chủ phát hành (NGANHANG_PUB) giá trị này là NULL — nhưng không bao giờ đến đây
    -- vì Máy chủ phát hành có tất cả tài khoản nội bộ (ĐƯỜNG A luôn được chọn).
    DECLARE @remoteDB nvarchar(128) = CASE DB_NAME()
        WHEN N'NGANHANG_BT' THEN N'NGANHANG_TD'
        WHEN N'NGANHANG_TD' THEN N'NGANHANG_BT'
    END;

    IF @remoteDB IS NULL
    BEGIN
        DECLARE @curDB nvarchar(128) = DB_NAME();
        RAISERROR(N'RC-7: Cross-branch transfer not supported from database [%s]. Expected NGANHANG_BT or NGANHANG_TD.',
              16, 1, @curDB);
        RETURN -7;
    END

    -- Xác minh tài khoản đích tồn tại trên chi nhánh từ xa qua LINK1
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

    -- Xây dựng câu lệnh ghi có từ xa (SQL động cho tên bốn phần qua LINK1)
    DECLARE @creditSql nvarchar(500) =
        N'UPDATE [LINK1].' + QUOTENAME(@remoteDB)
        + N'.dbo.TAIKHOAN SET SODU = SODU + @amt '
        + N'WHERE SOTK = @tk AND Status = N''Active''; '
        + N'SET @rc = @@ROWCOUNT;';

    DECLARE @creditRows int = 0;

    BEGIN TRY
        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
        BEGIN DISTRIBUTED TRANSACTION;

        -- Bên gửi (trừ tiền) tài khoản nguồn (nội bộ)
        UPDATE dbo.TAIKHOAN
        SET    SODU = SODU - @SOTIEN
        WHERE  SOTK   = @SOTK_CHUYEN
          AND  SODU  >= @SOTIEN
          AND  Status = N'Active';

        IF @@ROWCOUNT = 0
            THROW 50002, N'RC-2: Debit failed — balance changed concurrently.', 1;

        -- Bên nhận (cộng tiền) tài khoản đích (từ xa qua LINK1)
        EXEC sp_executesql @creditSql,
            N'@amt money, @tk nChar(9), @rc int OUTPUT',
            @SOTIEN, @SOTK_NHAN, @creditRows OUTPUT;

        IF @creditRows = 0
            THROW 50004, N'RC-4: Remote credit failed — destination may have been modified.', 1;

        -- Ghi nhận chuyển khoản nội bộ (MACN = chi nhánh nguồn)
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
    PRINT '>>> Section D: 8 Transaction SPs created.';
ELSE
    PRINT '>>> WARNING: Section D may be incomplete (SP_CrossBranchTransfer was not created).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN E — Thủ tục lưu trữ Báo cáo
   Nguồn: sql/14-sp-reports.sql  (Chi nhánh + Bank_Main hợp nhất)
   Phía C# gọi: SqlReportRepository

   Sau sao chép, Máy chủ phát hành giữ TẤT CẢ dữ liệu nội bộ, nên phiên bản
   chi nhánh và Bank_Main được hợp nhất thành một phiên bản mỗi SP. Truy vấn sử dụng
   bảng gốc trực tiếp, với bộ lọc @BranchCode tùy chọn.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountStatement
-- Gọi bởi: SqlReportRepository.GetAccountStatementAsync
--
-- Trả về HAI tập kết quả (đọc trong C# bằng NextResultAsync):
--   RS1 (1 dòng):  SOTK nChar(9), OpeningBalance money
--   RS2 (n dòng): MAGD, NGAYGD, LOAIGD, SOTIEN, OpeningBal, RunningBalance,
--                 Description, IsDebit   ORDER BY NGAYGD ASC
--
-- Số dư đầu kỳ = số dư tại thời điểm bắt đầu @TuNgay (số dư hiện tại trừ
-- ảnh hưởng ròng của tất cả giao dịch trong kỳ).
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetAccountStatement
    @SOTK    nChar(9),
    @TuNgay  datetime,
    @DenNgay datetime
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Bước 1: Tính số dư đầu kỳ ───────────────────────────────────────
    DECLARE @OpeningBal money;

    SELECT @OpeningBal =
        ISNULL((SELECT SODU FROM dbo.TAIKHOAN WHERE SOTK = @SOTK), 0)
        -- hoàn ngược các lần rút tiền trong kỳ
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'RT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- hoàn ngược các chuyển khoản đi trong kỳ
        + ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_CHUYEN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- hoàn ngược các lần gửi tiền trong kỳ
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_GOIRUT
                  WHERE SOTK = @SOTK AND LOAIGD = N'GT'
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0)
        -- hoàn ngược các chuyển khoản đến trong kỳ
        - ISNULL((SELECT SUM(SOTIEN) FROM dbo.GD_CHUYENTIEN
                  WHERE SOTK_NHAN = @SOTK
                    AND NGAYGD >= @TuNgay AND Status = N'Completed'), 0);

    -- ── RS1: tài khoản + số dư đầu kỳ ────────────────────────────────────────
    SELECT @SOTK AS SOTK, @OpeningBal AS OpeningBalance;

    -- ── RS2: các dòng giao dịch với số dư lũy kế ──────────────────────────
    ;WITH AllTx AS (
        -- Gửi tiền và rút tiền
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

        -- Chuyển khoản đi
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

        -- Chuyển khoản đến
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAccountsOpenedInPeriod
-- Gọi bởi: SqlReportRepository.GetAccountsOpenedInPeriodAsync
-- HỢP NHẤT: một phiên bản xử lý cả lệnh gọi theo chi nhánh cụ thể và tất cả chi nhánh
-- (Máy chủ phát hành có TẤT CẢ dữ liệu nội bộ).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetTransactionSummary
-- Gọi bởi: SqlReportRepository.GetTransactionSummaryAsync
-- HỢP NHẤT: một phiên bản cho cả lệnh gọi theo chi nhánh cụ thể và tất cả chi nhánh.
--
-- Trả về HAI tập kết quả:
--   RS1 (1 dòng):  TotalCount, DepositCount, WithdrawalCount, TransferCount,
--                 TotalDepositAmount, TotalWithdrawalAmount, TotalTransferAmount
--   RS2 (n dòng): MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV,
--                 SOTK_NHAN, Status, ErrorMessage
--
-- Sử dụng GD_GOIRUT.MACN và GD_CHUYENTIEN.MACN trực tiếp để lọc theo chi nhánh
-- (cả hai bảng đều có MACN sau 02_publisher_schema.sql).
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetTransactionSummary
    @FromDate   datetime,
    @ToDate     datetime,
    @BranchCode nvarchar(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1: Tổng hợp ────────────────────────────────────────────────
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

    -- ── RS2: Các dòng chi tiết giao dịch ───────────────────────────────────────
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

PRINT '>>> Section E: 3 Report SPs created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN F — Thủ tục lưu trữ Xác thực + Chi nhánh
   Nguồn: sql/15-sp-auth.sql
   Phía C# gọi: SqlUserRepository, SqlBranchRepository (cả hai dùng kết nối Bank_Main)

   QUAN TRỌNG: NGUOIDUNG chỉ dùng trên Coordinator và KHÔNG được sao chép.
   Các SP này KHÔNG ĐƯỢC thêm vào bất kỳ publication sao chép nào.
   CHINHANH được sao chép (không lọc dòng), nên các SP của nó có thể sao chép.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── NGƯỜI DÙNG — NGUOIDUNG ────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetUser
-- Gọi bởi: SqlUserRepository.GetUserAsync
-- Trả về: một dòng cho Username đã cho (bao gồm người dùng đã xóa mềm để
--          tầng xác thực có thể từ chối rõ ràng).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetAllUsers
-- Gọi bởi: SqlUserRepository.GetAllUsersAsync
-- Trả về tất cả người dùng bao gồm đã xóa mềm (C# lọc theo TrangThaiXoa).
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- USP_AddUser
-- Gọi bởi: SqlUserRepository.AddUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
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
        RETURN;   -- trùng lặp → 0 dòng bị ảnh hưởng; phía gọi kiểm tra
    INSERT INTO dbo.NGUOIDUNG
        (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa)
    VALUES
        (@Username, @PasswordHash, @UserGroup, @DefaultBranch, @CustomerCMND, @EmployeeId, 0);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateUser
-- Gọi bởi: SqlUserRepository.UpdateUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_SoftDeleteUser  (đặt TrangThaiXoa = 1; giữ lại bản ghi)
-- Gọi bởi: SqlUserRepository.SoftDeleteUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_SoftDeleteUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 1
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 0;   -- bất biến lũy đẳng
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_RestoreUser  (đặt TrangThaiXoa = 0; kích hoạt lại tài khoản)
-- Gọi bởi: SqlUserRepository.RestoreUserAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_RestoreUser
    @Username nvarchar(50)
AS
BEGIN
    SET NOCOUNT OFF;
    UPDATE dbo.NGUOIDUNG
    SET    TrangThaiXoa = 0
    WHERE  Username     = @Username
      AND  TrangThaiXoa = 1;   -- bất biến lũy đẳng
END
GO

-- ── CHI NHÁNH — CHINHANH ──────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_GetBranches
-- Gọi bởi: SqlBranchRepository.GetBranchesAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_GetBranches
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
-- Gọi bởi: SqlBranchRepository.GetBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_AddBranch
-- Gọi bởi: SqlBranchRepository.AddBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_AddBranch
    @MACN   nChar(10),
    @TENCN  nvarchar(50),
    @DIACHI nvarchar(100) = NULL,
    @SODT   varchar(15)   = NULL
AS
BEGIN
    SET NOCOUNT OFF;
    IF EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = @MACN)
        RETURN;   -- trùng PK → 0 dòng bị ảnh hưởng
    INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
    VALUES (@MACN, @TENCN, @DIACHI, @SODT);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_UpdateBranch
-- Gọi bởi: SqlBranchRepository.UpdateBranchAsync
-- ─────────────────────────────────────────────────────────────────────────────
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

-- ─────────────────────────────────────────────────────────────────────────────
-- SP_DeleteBranch
-- Gọi bởi: SqlBranchRepository.DeleteBranchAsync
-- Xóa cứng; phía gọi phải kiểm tra nhân viên/tài khoản phụ thuộc trước.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.SP_DeleteBranch
    @MACN nChar(10)
AS
BEGIN
    SET NOCOUNT OFF;
    DELETE FROM dbo.CHINHANH WHERE MACN = @MACN;
END
GO

PRINT '>>> Section F: 11 Auth + Branch SPs created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN G — Xác minh
   Tóm tắt nhanh xác nhận tất cả 51 đối tượng (1 view + 50 SP) đã được tạo.
   ═══════════════════════════════════════════════════════════════════════════════ */

SELECT
    o.type_desc                                AS ObjectType,
    SCHEMA_NAME(o.schema_id) + '.' + o.name    AS ObjectName,
    o.create_date                               AS Created,
    o.modify_date                               AS LastModified
FROM sys.objects o
WHERE o.is_ms_shipped = 0
  AND o.type IN ('P', 'V')                     -- Thủ tục và View
  AND o.schema_id = SCHEMA_ID('dbo')
  AND (
      o.name LIKE 'SP[_]%'                     -- tất cả SP bắt đầu bằng SP_
      OR o.name = 'view_DanhSachPhanManh'       -- view phân mảnh
  )
ORDER BY o.type_desc DESC, o.name ASC;
GO

PRINT '';
PRINT '=== 03_publisher_sp_views.sql completed successfully ===';
PRINT '    Objects created: 1 view + 50 stored procedures = 51 total';
PRINT '    Database: NGANHANG_PUB';
PRINT '    Next step: 04_publisher_security.sql (Step 4/8)';
GO
