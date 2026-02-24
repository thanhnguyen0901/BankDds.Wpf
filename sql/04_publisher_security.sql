/*=============================================================================
  04_publisher_security.sql
  Vai trò : Máy chủ phát hành / Coordinator (server gốc)
  Chạy trên : DESKTOP-JBB41QU / NGANHANG_PUB
  Mục đích: Bảo mật SQL theo yêu cầu môn học Ngân hàng:
             1. Vai trò cơ sở dữ liệu:  NGANHANG, CHINHANH, KHACHHANG
             2. sp_DangNhap     — xác định đăng nhập SQL hiện tại → vai trò qua sysusers
             3. sp_TaoTaiKhoan  — tạo đăng nhập SQL + người dùng DB + thành viên vai trò
             4. DENY truy cập bảng trực tiếp; GRANT EXECUTE trên SP theo vai trò
             5. Dữ liệu mẫu đăng nhập quản trị (thay thế sa cho demo)

  Ánh xạ vai trò (SQL role → C# UserGroup):
    NGANHANG  → UserGroup.NganHang  (0) — đọc tất cả chi nhánh, quản trị đầy đủ, tạo đăng nhập
    CHINHANH  → UserGroup.ChiNhanh  (1) — CRUD dữ liệu chi nhánh, tạo đăng nhập CHINHANH/KHACHHANG
    KHACHHANG → UserGroup.KhachHang (2) — chỉ đọc: sao kê tài khoản/báo cáo cá nhân

  Bất biến lũy đẳng: CÓ — tất cả đối tượng được bảo vệ bởi IF NOT EXISTS / CREATE OR ALTER.
  THỨ TỰ THỰC THI: Bước 4/8  (Chỉ máy chủ phát hành, sau 03_publisher_sp_views.sql).
=============================================================================*/

USE NGANHANG_PUB;
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — Vai trò cơ sở dữ liệu
   Ba vai trò khớp với mẫu Ngân hàng và enum C# UserGroup.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NGANHANG' AND type = 'R')
BEGIN
    CREATE ROLE NGANHANG;
    PRINT '>>> Role NGANHANG created.';
END
ELSE
    PRINT '>>> Role NGANHANG already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'CHINHANH' AND type = 'R')
BEGIN
    CREATE ROLE CHINHANH;
    PRINT '>>> Role CHINHANH created.';
END
ELSE
    PRINT '>>> Role CHINHANH already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KHACHHANG' AND type = 'R')
BEGIN
    CREATE ROLE KHACHHANG;
    PRINT '>>> Role KHACHHANG created.';
END
ELSE
    PRINT '>>> Role KHACHHANG already exists — skipped.';
GO

PRINT '>>> Section 1: Database roles verified.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — DENY truy cập trực tiếp bảng/view cho cả ba vai trò
   Mọi truy cập dữ liệu phải thông qua stored procedure.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- DENY trực tiếp DML trên tất cả bảng gốc cho cả ba vai trò.
-- SP thực thi dưới quyền dbo (ownership chaining), nên truy cập qua SP vẫn hoạt động.
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.CHINHANH       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NGUOIDUNG      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.KHACHHANG      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.NHANVIEN       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.TAIKHOAN       TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_GOIRUT      TO NGANHANG, CHINHANH, KHACHHANG;
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GD_CHUYENTIEN  TO NGANHANG, CHINHANH, KHACHHANG;
GO

-- LƯU Ý: Các view tiện ích _ALL đã bị loại bỏ.
-- Không cần lệnh DENY cho các view không còn tồn tại.
-- Xem 02_publisher_schema.sql Phần 5 để biết chi tiết xóa/dọn dẹp.

PRINT '>>> Section 2: DENY direct table access applied.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — GRANT EXECUTE trên stored procedure theo vai trò

   Ma trận phân quyền:
   ┌─────────────────────────────────────────┬──────────┬──────────┬───────────┐
   │ SP                                      │ NGANHANG │ CHINHANH │ KHACHHANG │
   ├─────────────────────────────────────────┼──────────┼──────────┼───────────┤
   │ ── SP KHÁCH HÀNG ──                     │          │          │           │
   │ SP_GetCustomersByBranch                 │    ✓     │    ✓     │           │
   │ SP_GetCustomerByCMND                    │    ✓     │    ✓     │           │
   │ SP_AddCustomer                          │    ✓     │    ✓     │           │
   │ SP_UpdateCustomer                       │    ✓     │    ✓     │           │
   │ SP_DeleteCustomer                       │    ✓     │    ✓     │           │
   │ SP_RestoreCustomer                      │    ✓     │    ✓     │           │
   │ SP_GetAllCustomers                      │    ✓     │          │           │
   │ ── SP NHÂN VIÊN ──                      │          │          │           │
   │ SP_GetEmployeesByBranch                 │    ✓     │    ✓     │           │
   │ SP_GetEmployee                          │    ✓     │    ✓     │           │
   │ SP_AddEmployee                          │    ✓     │    ✓     │           │
   │ SP_UpdateEmployee                       │    ✓     │    ✓     │           │
   │ SP_DeleteEmployee                       │    ✓     │    ✓     │           │
   │ SP_RestoreEmployee                      │    ✓     │    ✓     │           │
   │ SP_TransferEmployee                     │    ✓     │          │           │
   │ SP_EmployeeExists                       │    ✓     │    ✓     │           │
   │ SP_GetAllEmployees                      │    ✓     │          │           │
   │ SP_GetNextManv                          │    ✓     │    ✓     │           │
   │ ── SP TÀI KHOẢN ──                      │          │          │           │
   │ SP_GetAccountsByBranch                  │    ✓     │    ✓     │           │
   │ SP_GetAccountsByCustomer                │    ✓     │    ✓     │    ✓      │
   │ SP_GetAccount                           │    ✓     │    ✓     │    ✓      │
   │ SP_AddAccount                           │    ✓     │    ✓     │           │
   │ SP_UpdateAccount                        │    ✓     │    ✓     │           │
   │ SP_DeleteAccount                        │    ✓     │    ✓     │           │
   │ SP_CloseAccount                         │    ✓     │    ✓     │           │
   │ SP_ReopenAccount                        │    ✓     │    ✓     │           │
   │ SP_DeductFromAccount                    │    ✓     │    ✓     │           │
   │ SP_AddToAccount                         │    ✓     │    ✓     │           │
   │ SP_GetAllAccounts                       │    ✓     │          │           │
   │ ── SP GIAO DỊCH ──                      │          │          │           │
   │ SP_GetTransactionsByAccount             │    ✓     │    ✓     │    ✓      │
   │ SP_GetTransactionsByBranch              │    ✓     │    ✓     │           │
   │ SP_GetDailyWithdrawalTotal              │    ✓     │    ✓     │           │
   │ SP_GetDailyTransferTotal                │    ✓     │    ✓     │           │
   │ SP_Deposit                              │    ✓     │    ✓     │           │
   │ SP_Withdraw                             │    ✓     │    ✓     │           │
   │ SP_CreateTransferTransaction            │    ✓     │    ✓     │           │
   │ SP_CrossBranchTransfer                  │    ✓     │    ✓     │           │
   │ ── SP BÁO CÁO ──                        │          │          │           │
   │ SP_GetAccountStatement                  │    ✓     │    ✓     │    ✓      │
   │ SP_GetAccountsOpenedInPeriod            │    ✓     │    ✓     │           │
   │ SP_GetTransactionSummary                │    ✓     │    ✓     │           │
   │ ── SP XÁC THỰC + CHI NHÁNH ──          │          │          │           │
   │ SP_GetUser                              │    ✓     │    ✓     │           │
   │ SP_GetAllUsers                          │    ✓     │    ✓     │           │
   │ SP_AddUser                              │    ✓     │    ✓     │           │
   │ SP_UpdateUser                           │    ✓     │    ✓     │           │
   │ SP_SoftDeleteUser                       │    ✓     │    ✓     │           │
   │ SP_RestoreUser                          │    ✓     │          │           │
   │ SP_GetBranches                          │    ✓     │    ✓     │    ✓      │
   │ SP_GetBranch                            │    ✓     │    ✓     │    ✓      │
   │ SP_AddBranch                            │    ✓     │          │           │
   │ SP_UpdateBranch                         │    ✓     │          │           │
   │ SP_DeleteBranch                         │    ✓     │          │           │
   │ ── SP BẢO MẬT (file này) ──             │          │          │           │
   │ sp_DangNhap                             │  PUBLIC  │  PUBLIC  │  PUBLIC   │
   │ sp_TaoTaiKhoan                          │    ✓     │    ✓     │           │
   └─────────────────────────────────────────┴──────────┴──────────┴───────────┘
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── 3A. Cấp quyền cho NGANHANG (toàn quyền truy cập tất cả SP nghiệp vụ) ─────────────────
-- Khách hàng
GRANT EXECUTE ON dbo.SP_GetCustomersByBranch     TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetCustomerByCMND        TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddCustomer              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateCustomer           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteCustomer           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreCustomer          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllCustomers          TO NGANHANG;
-- Nhân viên
GRANT EXECUTE ON dbo.SP_GetEmployeesByBranch     TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetEmployee              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddEmployee              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateEmployee           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteEmployee           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreEmployee          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_TransferEmployee         TO NGANHANG;
GRANT EXECUTE ON dbo.SP_EmployeeExists           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllEmployees          TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetNextManv              TO NGANHANG;
-- Tài khoản
GRANT EXECUTE ON dbo.SP_GetAccountsByBranch      TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccount               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddAccount               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CloseAccount             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_ReopenAccount            TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeductFromAccount        TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddToAccount             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllAccounts           TO NGANHANG;
-- Giao dịch
GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetTransactionsByBranch  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetDailyWithdrawalTotal  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetDailyTransferTotal    TO NGANHANG;
GRANT EXECUTE ON dbo.SP_Deposit                  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_Withdraw                 TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CreateTransferTransaction TO NGANHANG;
GRANT EXECUTE ON dbo.SP_CrossBranchTransfer      TO NGANHANG;
-- Báo cáo
GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAccountsOpenedInPeriod TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetTransactionSummary    TO NGANHANG;
-- Xác thực + Chi nhánh (quản trị đầy đủ bao gồm quản lý chi nhánh)
GRANT EXECUTE ON dbo.SP_GetUser                  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetAllUsers              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddUser                  TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateUser               TO NGANHANG;
GRANT EXECUTE ON dbo.SP_SoftDeleteUser           TO NGANHANG;
GRANT EXECUTE ON dbo.SP_RestoreUser              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetBranches              TO NGANHANG;
GRANT EXECUTE ON dbo.SP_GetBranch                TO NGANHANG;
GRANT EXECUTE ON dbo.SP_AddBranch                TO NGANHANG;
GRANT EXECUTE ON dbo.SP_UpdateBranch             TO NGANHANG;
GRANT EXECUTE ON dbo.SP_DeleteBranch             TO NGANHANG;
GO

-- ── 3B. Cấp quyền cho CHINHANH (CRUD cấp chi nhánh, quản lý người dùng, không quản trị chi nhánh) ──
-- Khách hàng
GRANT EXECUTE ON dbo.SP_GetCustomersByBranch     TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetCustomerByCMND        TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddCustomer              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateCustomer           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteCustomer           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_RestoreCustomer          TO CHINHANH;
-- Nhân viên
GRANT EXECUTE ON dbo.SP_GetEmployeesByBranch     TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetEmployee              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddEmployee              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateEmployee           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteEmployee           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_RestoreEmployee          TO CHINHANH;
GRANT EXECUTE ON dbo.SP_EmployeeExists           TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetNextManv              TO CHINHANH;
-- Tài khoản
GRANT EXECUTE ON dbo.SP_GetAccountsByBranch      TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccount               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddAccount               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeleteAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CloseAccount             TO CHINHANH;
GRANT EXECUTE ON dbo.SP_ReopenAccount            TO CHINHANH;
GRANT EXECUTE ON dbo.SP_DeductFromAccount        TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddToAccount             TO CHINHANH;
-- Giao dịch
GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetTransactionsByBranch  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetDailyWithdrawalTotal  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetDailyTransferTotal    TO CHINHANH;
GRANT EXECUTE ON dbo.SP_Deposit                  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_Withdraw                 TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CreateTransferTransaction TO CHINHANH;
GRANT EXECUTE ON dbo.SP_CrossBranchTransfer      TO CHINHANH;
-- Báo cáo
GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAccountsOpenedInPeriod TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetTransactionSummary    TO CHINHANH;
-- Xác thực (xem + tạo người dùng, không quản trị chi nhánh)
GRANT EXECUTE ON dbo.SP_GetUser                  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetAllUsers              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_AddUser                  TO CHINHANH;
GRANT EXECUTE ON dbo.SP_UpdateUser               TO CHINHANH;
GRANT EXECUTE ON dbo.SP_SoftDeleteUser           TO CHINHANH;
-- Chi nhánh (chỉ đọc cho CHINHANH; không Thêm/Sửa/Xóa)
GRANT EXECUTE ON dbo.SP_GetBranches              TO CHINHANH;
GRANT EXECUTE ON dbo.SP_GetBranch                TO CHINHANH;
GO

-- ── 3C. Cấp quyền cho KHACHHANG (tối thiểu: đọc tài khoản cá nhân + sao kê) ──────────
-- Tài khoản (chỉ đọc cho tài khoản cá nhân)
GRANT EXECUTE ON dbo.SP_GetAccountsByCustomer    TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetAccount               TO KHACHHANG;
-- Giao dịch (lịch sử chỉ đọc cho tài khoản cá nhân)
GRANT EXECUTE ON dbo.SP_GetTransactionsByAccount TO KHACHHANG;
-- Báo cáo (chỉ sao kê tài khoản cá nhân)
GRANT EXECUTE ON dbo.SP_GetAccountStatement      TO KHACHHANG;
-- Chi nhánh (chỉ đọc cho danh sách chi nhánh)
GRANT EXECUTE ON dbo.SP_GetBranches              TO KHACHHANG;
GRANT EXECUTE ON dbo.SP_GetBranch                TO KHACHHANG;
GO

PRINT '>>> Section 3: GRANT EXECUTE permissions applied.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — sp_DangNhap  (Publisher version)
   Xử lý đăng nhập ngân hàng: truy vấn catalog hệ thống SQL Server để xác định
   vai trò cơ sở dữ liệu của đăng nhập SQL hiện tại.

   Trả về một tập kết quả:
     MANV      nvarchar(50)  — tên đăng nhập SQL (SYSTEM_USER)
     HOTEN     nvarchar(128) — tên hiển thị (USER_NAME())
     TENNHOM   nvarchar(128) — tên vai trò (NGANHANG / CHINHANH / KHACHHANG)
     MACN      nChar(10)     — mã chi nhánh mặc định (NULL cho NGANHANG)

   Giải thuật giải quyết DefaultBranch (MACN):
     1. NGUOIDUNG.DefaultBranch  (hub-only, maps SQL login → branch)
     2. NHANVIEN.MACN            (WHERE MANV = SYSTEM_USER)
     3. KHACHHANG.MACN           (WHERE CMND = SYSTEM_USER)

   Sử dụng ownership chaining: dbo SP đọc dbo table → bỏ qua DENY SELECT.

   Sử dụng từ C#:
     var result = await connection.QuerySingleAsync<LoginResult>("EXEC sp_DangNhap");
   ═══════════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE dbo.sp_DangNhap
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Step 1: Resolve role via system catalogs (QLVT pattern) ──
    -- Priority: NGANHANG > CHINHANH > KHACHHANG.
    DECLARE @MANV     nvarchar(50)  = SYSTEM_USER;   -- SQL login name
    DECLARE @HOTEN    nvarchar(128) = USER_NAME();    -- DB user name
    DECLARE @TENNHOM  nvarchar(128) = NULL;
    DECLARE @MACN     nChar(10)     = NULL;           -- default branch code

    SELECT TOP 1 @TENNHOM = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = USER_NAME()
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END ASC;

    -- Fallback: db_owner / sysadmin → NGANHANG; otherwise lowest privilege.
    IF @TENNHOM IS NULL
    BEGIN
        IF IS_MEMBER('db_owner') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1
            SET @TENNHOM = N'NGANHANG';
        ELSE
            SET @TENNHOM = N'KHACHHANG';
    END

    -- ── Step 2: Resolve DefaultBranch (MACN) for CHINHANH / KHACHHANG ──
    -- Ownership chaining: dbo SP → dbo tables, DENY SELECT on caller is bypassed.
    IF @TENNHOM IN (N'CHINHANH', N'KHACHHANG')
    BEGIN
        -- 1st: NGUOIDUNG table (hub-only, maps SQL login → branch directly)
        --      NULLIF handles empty-string DefaultBranch rows (e.g. NGANHANG users).
        IF OBJECT_ID(N'dbo.NGUOIDUNG', N'U') IS NOT NULL
            SELECT @MACN = NULLIF(RTRIM(DefaultBranch), N'')
            FROM   dbo.NGUOIDUNG
            WHERE  Username = SYSTEM_USER AND TrangThaiXoa = 0;

        -- 2nd: NHANVIEN (employee login whose MANV = SQL login name)
        IF @MACN IS NULL
            SELECT @MACN = MACN FROM dbo.NHANVIEN
            WHERE  MANV = SYSTEM_USER AND TrangThaiXoa = 0;

        -- 3rd: KHACHHANG (customer login whose CMND = SQL login name)
        IF @MACN IS NULL
            SELECT TOP 1 @MACN = MACN FROM dbo.KHACHHANG
            WHERE  CMND = SYSTEM_USER AND TrangThaiXoa = 0;
    END

    SELECT @MANV AS MANV, @HOTEN AS HOTEN, @TENNHOM AS TENNHOM, @MACN AS MACN;
END
GO

-- sp_DangNhap must be callable by any login (pre-authentication resolver)
GRANT EXECUTE ON dbo.sp_DangNhap TO PUBLIC;
GO

PRINT '>>> Section 4: sp_DangNhap created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — sp_TaoTaiKhoan
   Tạo tài khoản ngân hàng: bao bọc sp_addlogin / sp_grantdbaccess /
   sp_addrolemember để tạo đăng nhập SQL Server, ánh xạ vào DB hiện tại,
   và gán vào vai trò cơ sở dữ liệu phù hợp.

   Tham số:
     @LOGIN    nvarchar(50)   — tên đăng nhập SQL cần tạo
     @PASS     nvarchar(128)  — mật khẩu ban đầu
     @TENNHOM  nvarchar(128)  — tên vai trò: 'NGANHANG', 'CHINHANH', hoặc 'KHACHHANG'

   Phân quyền:
     • Thành viên NGANHANG có thể tạo đăng nhập cho bất kỳ vai trò nào.
     • Thành viên CHINHANH chỉ có thể tạo đăng nhập CHINHANH và KHACHHANG.
     • Thành viên KHACHHANG không thể gọi SP này (không có GRANT).

   Sử dụng từ C#:
     await connection.ExecuteAsync("EXEC sp_TaoTaiKhoan @LOGIN, @PASS, @TENNHOM",
         new { LOGIN = username, PASS = password, TENNHOM = "CHINHANH" });
   ═══════════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoan
    @LOGIN    nvarchar(50),
    @PASS     nvarchar(128),
    @TENNHOM  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Kiểm tra tên vai trò ───────────────────────────────────────────────────────
    IF @TENNHOM NOT IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    BEGIN
        RAISERROR(N'Invalid role name: %s. Must be NGANHANG, CHINHANH, or KHACHHANG.', 16, 1, @TENNHOM);
        RETURN;
    END

    -- ── Thực thi phân quyền: ai có thể tạo gì ───────────────────────────────
    DECLARE @CallerRole nvarchar(128);
    SELECT TOP 1 @CallerRole = r.name
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = USER_NAME()
      AND  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
    ORDER BY
        CASE r.name
            WHEN N'NGANHANG'  THEN 1
            WHEN N'CHINHANH'  THEN 2
            WHEN N'KHACHHANG' THEN 3
        END ASC;

    -- db_owner / sysadmin → được xử lý như NGANHANG
    IF @CallerRole IS NULL AND (IS_MEMBER('db_owner') = 1 OR IS_SRVROLEMEMBER('sysadmin') = 1)
        SET @CallerRole = N'NGANHANG';

    IF @CallerRole IS NULL
    BEGIN
        RAISERROR(N'Caller does not belong to any recognized role. Cannot create accounts.', 16, 1);
        RETURN;
    END

    -- CHINHANH chỉ có thể tạo đăng nhập CHINHANH hoặc KHACHHANG
    IF @CallerRole = N'CHINHANH' AND @TENNHOM = N'NGANHANG'
    BEGIN
        RAISERROR(N'CHINHANH members cannot create NGANHANG logins.', 16, 1);
        RETURN;
    END

    -- KHACHHANG không thể tạo đăng nhập (không nên đến đây nhờ GRANT)
    IF @CallerRole = N'KHACHHANG'
    BEGIN
        RAISERROR(N'KHACHHANG members cannot create logins.', 16, 1);
        RETURN;
    END

    -- ── Bước 1: Tạo đăng nhập SQL Server (cấp server) ───────────────────
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
    BEGIN
        -- sp_addlogin tạo đăng nhập SQL Server với cơ sở dữ liệu mặc định
        EXEC sp_addlogin @loginame = @LOGIN, @passwd = @PASS, @defdb = N'NGANHANG_PUB';
        PRINT 'Login created: ' + @LOGIN;
    END
    ELSE
        PRINT 'Login already exists: ' + @LOGIN;

    -- ── Bước 2: Ánh xạ đăng nhập sang người dùng cơ sở dữ liệu ───────────────
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
    BEGIN
        EXEC sp_grantdbaccess @loginame = @LOGIN, @name_in_db = @LOGIN;
        PRINT 'DB user created: ' + @LOGIN;
    END
    ELSE
        PRINT 'DB user already exists: ' + @LOGIN;

    -- ── Bước 3: Thêm người dùng vào vai trò chỉ định ───────────────────────
    -- Kiểm tra nếu đã là thành viên (tránh lỗi khi chạy lại)
    IF NOT EXISTS (
        SELECT 1
        FROM   sys.database_role_members rm
        JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
        JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
        WHERE  u.name = @LOGIN AND r.name = @TENNHOM
    )
    BEGIN
        EXEC sp_addrolemember @rolename = @TENNHOM, @membername = @LOGIN;
        PRINT 'User ' + @LOGIN + ' added to role ' + @TENNHOM;
    END
    ELSE
        PRINT 'User ' + @LOGIN + ' already in role ' + @TENNHOM;
END
GO

-- GRANT EXECUTE cho NGANHANG và CHINHANH (hai vai trò có thể tạo tài khoản)
GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO NGANHANG;
GRANT EXECUTE ON dbo.sp_TaoTaiKhoan TO CHINHANH;
GO

PRINT '>>> Section 5: sp_TaoTaiKhoan created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 6 — sp_XoaTaiKhoan
   Xóa đăng nhập SQL hiện có và người dùng cơ sở dữ liệu của nó. Chỉ thành viên NGANHANG
   mới có thể thực thi (quyền quản trị đầy đủ).
   ═══════════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE dbo.sp_XoaTaiKhoan
    @LOGIN nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Chỉ NGANHANG / db_owner / sysadmin được phép xóa đăng nhập
    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Only NGANHANG members can delete login accounts.', 16, 1);
        RETURN;
    END

    -- Xóa người dùng DB trước
    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LOGIN AND type IN ('S', 'U'))
    BEGIN
        EXEC sp_revokedbaccess @name_in_db = @LOGIN;
        PRINT 'DB user dropped: ' + @LOGIN;
    END

    -- Xóa đăng nhập server
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LOGIN)
    BEGIN
        EXEC sp_droplogin @loginame = @LOGIN;
        PRINT 'Login dropped: ' + @LOGIN;
    END
END
GO

GRANT EXECUTE ON dbo.sp_XoaTaiKhoan TO NGANHANG;
GO

PRINT '>>> Section 6: sp_XoaTaiKhoan created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 7 — sp_DoiMatKhau
   Đổi mật khẩu đăng nhập SQL. Người dùng có thể đổi mật khẩu của mình;
   thành viên NGANHANG có thể đặt lại mật khẩu của bất kỳ người dùng nào.
   ═══════════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE dbo.sp_DoiMatKhau
    @LOGIN    nvarchar(50),
    @PASSCU   nvarchar(128),
    @PASSMOI  nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    -- Tự đổi: người gọi đổi mật khẩu của mình
    IF @LOGIN = SYSTEM_USER
    BEGIN
        -- sp_password kiểm tra mật khẩu cũ nội bộ
        EXEC sp_password @old = @PASSCU, @new = @PASSMOI, @loginame = @LOGIN;
        PRINT 'Password changed for: ' + @LOGIN;
        RETURN;
    END

    -- Đặt lại bởi quản trị: chỉ NGANHANG / db_owner / sysadmin
    IF IS_MEMBER('NGANHANG') <> 1
       AND IS_MEMBER('db_owner') <> 1
       AND IS_SRVROLEMEMBER('sysadmin') <> 1
    BEGIN
        RAISERROR(N'Only NGANHANG members can reset other users'' passwords.', 16, 1);
        RETURN;
    END

    -- Quản trị viên không cần mật khẩu cũ
    EXEC sp_password @old = NULL, @new = @PASSMOI, @loginame = @LOGIN;
    PRINT 'Password reset for: ' + @LOGIN;
END
GO

GRANT EXECUTE ON dbo.sp_DoiMatKhau TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO CHINHANH;
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO KHACHHANG;
GO

PRINT '>>> Section 7: sp_DoiMatKhau created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 8 — sp_DanhSachNhanVien (liệt kê tất cả người dùng DB và vai trò)
   SP tiện ích cho trang quản trị — hiển thị ai có quyền truy cập và vai trò nào.
   Chỉ NGANHANG và CHINHANH có thể gọi SP này.
   ═══════════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE dbo.sp_DanhSachNhanVien
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.name                      AS LOGINNAME,
        r.name                      AS TENNHOM,
        u.create_date               AS NGAYTAO
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
      AND  u.type IN ('S', 'U')     -- chỉ người dùng SQL và Windows
    ORDER BY r.name ASC, u.name ASC;
END
GO

GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO NGANHANG;
GRANT EXECUTE ON dbo.sp_DanhSachNhanVien TO CHINHANH;
GO

PRINT '>>> Section 8: sp_DanhSachNhanVien created.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 9 — Dữ liệu mẫu: đăng nhập quản trị demo
   Tạo đăng nhập quản trị NGANHANG mặc định để truy cập hệ thống ban đầu.
   Đây là tài khoản khởi tạo — sử dụng sp_TaoTaiKhoan cho tất cả
   việc tạo tài khoản tiếp theo.

   CẢNH BÁO: Đổi mật khẩu trước khi sử dụng cho sản xuất hoặc thuyết trình.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Dữ liệu mẫu đăng nhập: ADMIN_NH / Admin@123
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ADMIN_NH')
BEGIN
    EXEC sp_addlogin @loginame = N'ADMIN_NH', @passwd = N'Admin@123', @defdb = N'NGANHANG_PUB';
    PRINT '>>> Seed login ADMIN_NH created.';
END
ELSE
    PRINT '>>> Seed login ADMIN_NH already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ADMIN_NH' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'ADMIN_NH', @name_in_db = N'ADMIN_NH';
    PRINT '>>> Seed DB user ADMIN_NH mapped.';
END
ELSE
    PRINT '>>> Seed DB user ADMIN_NH already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'ADMIN_NH' AND r.name = N'NGANHANG'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'NGANHANG', @membername = N'ADMIN_NH';
    PRINT '>>> Seed user ADMIN_NH added to role NGANHANG.';
END
ELSE
    PRINT '>>> Seed user ADMIN_NH already in role NGANHANG — skipped.';
GO

-- Dữ liệu mẫu đăng nhập: NV_BT (nhân viên chi nhánh Bến Thành)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'NV_BT')
BEGIN
    EXEC sp_addlogin @loginame = N'NV_BT', @passwd = N'NhanVien@123', @defdb = N'NGANHANG_PUB';
    PRINT '>>> Seed login NV_BT created.';
END
ELSE
    PRINT '>>> Seed login NV_BT already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'NV_BT' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'NV_BT', @name_in_db = N'NV_BT';
    PRINT '>>> Seed DB user NV_BT mapped.';
END
ELSE
    PRINT '>>> Seed DB user NV_BT already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'NV_BT' AND r.name = N'CHINHANH'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'CHINHANH', @membername = N'NV_BT';
    PRINT '>>> Seed user NV_BT added to role CHINHANH.';
END
ELSE
    PRINT '>>> Seed user NV_BT already in role CHINHANH — skipped.';
GO

-- Dữ liệu mẫu đăng nhập: KH_DEMO (đăng nhập khách hàng demo)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'KH_DEMO')
BEGIN
    EXEC sp_addlogin @loginame = N'KH_DEMO', @passwd = N'KhachHang@123', @defdb = N'NGANHANG_PUB';
    PRINT '>>> Seed login KH_DEMO created.';
END
ELSE
    PRINT '>>> Seed login KH_DEMO already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'KH_DEMO' AND type IN ('S', 'U'))
BEGIN
    EXEC sp_grantdbaccess @loginame = N'KH_DEMO', @name_in_db = N'KH_DEMO';
    PRINT '>>> Seed DB user KH_DEMO mapped.';
END
ELSE
    PRINT '>>> Seed DB user KH_DEMO already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.database_role_members rm
    JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
    JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
    WHERE  u.name = N'KH_DEMO' AND r.name = N'KHACHHANG'
)
BEGIN
    EXEC sp_addrolemember @rolename = N'KHACHHANG', @membername = N'KH_DEMO';
    PRINT '>>> Seed user KH_DEMO added to role KHACHHANG.';
END
ELSE
    PRINT '>>> Seed user KH_DEMO already in role KHACHHANG — skipped.';
GO

PRINT '>>> Section 9: Seed demo logins created (ADMIN_NH, NV_BT, KH_DEMO).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 10 — Xác minh
   Truy vấn tổng hợp hiển thị tất cả vai trò, thành viên và quyền SP.
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '─── Role Membership ────────────────────────────────────';
SELECT
    r.name   AS RoleName,
    u.name   AS MemberName,
    u.type_desc AS MemberType
FROM   sys.database_role_members rm
JOIN   sys.database_principals   u ON u.principal_id = rm.member_principal_id
JOIN   sys.database_principals   r ON r.principal_id = rm.role_principal_id
WHERE  r.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG')
ORDER BY r.name, u.name;
GO

PRINT '';
PRINT '─── SP Permission Counts per Role ──────────────────────';
SELECT
    dp.name                     AS RoleName,
    COUNT(*)                    AS GrantedSPCount
FROM   sys.database_permissions p
JOIN   sys.database_principals  dp ON dp.principal_id = p.grantee_principal_id
JOIN   sys.objects              o  ON o.object_id     = p.major_id
WHERE  p.permission_name = 'EXECUTE'
  AND  o.type = 'P'
  AND  dp.name IN (N'NGANHANG', N'CHINHANH', N'KHACHHANG', N'public')
GROUP BY dp.name
ORDER BY dp.name;
GO

PRINT '';
PRINT '─── Security SPs Created ───────────────────────────────';
SELECT name, type_desc, create_date
FROM   sys.objects
WHERE  type = 'P'
  AND  name IN ('sp_DangNhap', 'sp_TaoTaiKhoan', 'sp_XoaTaiKhoan',
                'sp_DoiMatKhau', 'sp_DanhSachNhanVien')
ORDER BY name;
GO

PRINT '';
PRINT '=== 04_publisher_security.sql completed successfully ===';
PRINT '    Roles:   NGANHANG, CHINHANH, KHACHHANG';
PRINT '    SPs:     sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan,';
PRINT '             sp_DoiMatKhau, sp_DanhSachNhanVien';
PRINT '    Seeds:   ADMIN_NH (NGANHANG), NV_BT (CHINHANH), KH_DEMO (KHACHHANG)';
PRINT '    Next:    05_replication_setup_merge.sql (Step 5/8)';
GO
