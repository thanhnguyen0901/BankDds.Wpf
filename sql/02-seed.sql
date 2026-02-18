/*=============================================================================
  02-seed.sql — BankDds Sample Data
  Generated: 2026-02-18

  IMPORTANT — PasswordHash values
  --------------------------------
  SQL Server has no native BCrypt support.  Generate hashes by running:

      dotnet-script (or a minimal console app):
          using BCrypt.Net;
          Console.WriteLine(BCrypt.HashPassword("123"));   // cost defaults to 11

  Then replace each  <BCRYPT_HASH_OF_123>  placeholder below with the output.
  Every user in this seed uses the password "123" for development convenience.
  The application's password validator (min 8 chars etc.) only fires on the
  Add/Edit form; it does NOT re-validate the BCrypt hash at login time.

  EXECUTION ORDER
  ---------------
  1. Run on SERVER3 (NGANHANG):     SECTION A — CHINHANH + NGUOIDUNG
  2. Run on SERVER1 (NGANHANG_BT):  SECTION B — BENTHANH branch data
  3. Run on SERVER2 (NGANHANG_TD):  SECTION C — TANDINH  branch data
=============================================================================*/


/* =========================================================================
   SECTION A — Bank_Main (SERVER3 / NGANHANG)
   ========================================================================= */
USE NGANHANG;
GO

-- ── CHINHANH ────────────────────────────────────────────────────────────────
INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
VALUES
    (N'BENTHANH', N'Chi nhánh Bến Thành', N'1 Công Trường Quách Thị Trang, Quận 1, TP.HCM', N'02838295800'),
    (N'TANDINH',  N'Chi nhánh Tân Định',  N'50 Hai Bà Trưng, Quận 1, TP.HCM',              N'02838203800');
GO

-- ── NGUOIDUNG ────────────────────────────────────────────────────────────────
-- UserGroup: 0=NganHang (bank-level admin), 1=ChiNhanh (branch admin), 2=KhachHang (customer)
-- DefaultBranch 'ALL' is allowed for NganHang users (they span all branches).
-- Replace <BCRYPT_HASH_OF_123> with: BCrypt.Net.BCrypt.HashPassword("123")
INSERT INTO dbo.NGUOIDUNG (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa)
VALUES
    -- Bank-level administrator
    (N'admin',   N'<BCRYPT_HASH_OF_123>', 0, N'ALL',      NULL,           N'NV00000001', 0),
    -- Branch administrator — Bến Thành
    (N'btuser',  N'<BCRYPT_HASH_OF_123>', 1, N'BENTHANH', NULL,           N'NV00000002', 0),
    -- Branch administrator — Tân Định
    (N'tduser',  N'<BCRYPT_HASH_OF_123>', 1, N'TANDINH',  NULL,           N'NV00000003', 0),
    -- Customer account (CMND links to KHACHHANG record)
    (N'c123456', N'<BCRYPT_HASH_OF_123>', 2, N'BENTHANH', N'0056789012',  NULL,          0);
GO


/* =========================================================================
   SECTION B — BENTHANH branch (SERVER1 / NGANHANG_BT)
   ========================================================================= */
USE NGANHANG_BT;
GO

-- ── NHANVIEN ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SDT, MACN, TrangThaiXoa)
VALUES
    (N'NV00000001', N'Nguyen', N'Admin',   N'123 Admin St',    N'0011111111', N'Nam', N'0911111111', N'BENTHANH', 0),
    (N'NV00000002', N'Tran',   N'Manager', N'456 Manager Ave', N'0022222222', N'Nam', N'0922222222', N'BENTHANH', 0);
GO

-- ── KHACHHANG ────────────────────────────────────────────────────────────────
INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SDT, PHAI, MACN, TrangThaiXoa)
VALUES
    (N'0012345678', N'Nguyen Van', N'A',    '1990-01-15', N'123 Le Loi',      '2008-01-20', N'0901234567', N'Nam', N'BENTHANH', 0),
    (N'0023456789', N'Tran Thi',   N'B',    '1992-05-10', N'456 Nguyen Hue',  '2010-05-15', N'0902345678', N'Nữ', N'BENTHANH', 0),
    (N'0056789012', N'Khach',      N'Hang', '1988-07-08', N'999 Customer St', '2006-07-15', N'0905678901', N'Nam', N'BENTHANH', 0);
GO

-- ── TAIKHOAN ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
VALUES
    (N'TK0000001', N'0012345678', 10000000, N'BENTHANH', '2025-08-18', N'Active'),
    (N'TK0000002', N'0023456789',  5000000, N'BENTHANH', '2025-11-18', N'Active'),
    (N'TK0000005', N'0056789012',  3000000, N'BENTHANH', '2026-01-18', N'Active');
GO

-- ── GD_GOIRUT ─────────────────────────────────────────────────────────────────
-- Seed transaction IDs use short strings (GD001…); generated IDs will be GD000000006+.
INSERT INTO dbo.GD_GOIRUT (MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
VALUES
    (N'GD001', N'TK0000001', N'GT', '2026-02-08', 2000000, N'NV00000001', N'Completed'),
    (N'GD002', N'TK0000001', N'RT', '2026-02-13',  500000, N'NV00000001', N'Completed'),
    (N'GD003', N'TK0000002', N'GT', '2026-02-11', 1000000, N'NV00000002', N'Completed'),
    (N'GD005', N'TK0000005', N'GT', '2026-02-17',  500000, N'NV00000001', N'Completed');
GO


/* =========================================================================
   SECTION C — TANDINH branch (SERVER2 / NGANHANG_TD)
   ========================================================================= */
USE NGANHANG_TD;
GO

-- ── NHANVIEN ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SDT, MACN, TrangThaiXoa)
VALUES
    (N'NV00000003', N'Le',   N'Teller', N'789 Teller Rd',  N'0033333333', N'Nữ', N'0933333333', N'TANDINH', 0),
    (N'NV00000004', N'Pham', N'Staff',  N'321 Staff Blvd', N'0044444444', N'Nữ', N'0944444444', N'TANDINH', 0);
GO

-- ── KHACHHANG ────────────────────────────────────────────────────────────────
INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SDT, PHAI, MACN, TrangThaiXoa)
VALUES
    (N'0034567890', N'Le Van',  N'C', '1985-08-25', N'789 Tran Hung Dao', '2003-09-01', N'0903456789', N'Nam', N'TANDINH', 0),
    (N'0045678901', N'Pham Thi',N'D', '1995-03-12', N'321 Hai Ba Trung',  '2013-03-20', N'0904567890', N'Nữ', N'TANDINH', 0);
GO

-- ── TAIKHOAN ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
VALUES
    (N'TK0000003', N'0034567890',  8000000, N'TANDINH', '2025-02-18', N'Active'),
    (N'TK0000004', N'0045678901', 15000000, N'TANDINH', '2025-06-18', N'Active');
GO

-- ── GD_GOIRUT ─────────────────────────────────────────────────────────────────
INSERT INTO dbo.GD_GOIRUT (MAGD, SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, Status)
VALUES
    (N'GD004', N'TK0000003', N'GT', '2026-02-15', 3000000, N'NV00000003', N'Completed');
GO
