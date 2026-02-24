/*=============================================================================
  04b_publisher_seed_data.sql
  Vai trò   : Máy chủ phát hành (Publisher)
  Chạy trên : DESKTOP-JBB41QU / NGANHANG_PUB
  Mục đích  : Chèn dữ liệu mẫu thực tế để demo.

  Thứ tự phụ thuộc:
    CHINHANH  →  KHACHHANG (FK MACN)
                 NHANVIEN  (FK MACN)
                    →  TAIKHOAN  (FK CMND, FK MACN)
                        →  GD_GOIRUT     (FK SOTK, FK MANV, FK MACN)
                        →  GD_CHUYENTIEN (FK SOTK_CHUYEN, FK MANV, FK MACN)

  GHI CHÚ QUAN TRỌNG — Sao chép hợp nhất (Merge Replication):
    Dữ liệu được chèn vào bảng của Máy chủ phát hành.
    Sau khi snapshot được tạo (script 05) hoặc sau lần đồng bộ merge tiếp theo,
    dữ liệu sẽ tự động truyền đến các máy chủ đăng ký nhận (CN1, CN2, TraCuu)
    theo bộ lọc hàng đã cấu hình:
      • CN1 (NGANHANG_BT)     : MACN = N'BENTHANH'
      • CN2 (NGANHANG_TD)     : MACN = N'TANDINH'
      • TraCuu (NGANHANG_TRACUU) : CHINHANH (tất cả) + KHACHHANG (TrangThaiXoa=0)

  Bất biến lũy đẳng: CÓ — mỗi INSERT được bảo vệ bởi IF NOT EXISTS.
  An toàn khi chạy lại. KHÔNG xóa/ghi đè dữ liệu hiện có.

  THỨ TỰ THỰC THI: Sau 04, trước 05.
  Nếu replication đã chạy, dữ liệu mới sẽ đồng bộ qua merge agent lần tiếp theo.
=============================================================================*/

USE NGANHANG_PUB;
GO

SET NOCOUNT ON;
PRINT '=== 04b_publisher_seed_data.sql — Bắt đầu chèn dữ liệu mẫu ===';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — CHI NHÁNH (CHINHANH)
   2 chi nhánh: Bến Thành (BENTHANH), Tân Định (TANDINH)
   Khớp chính xác với bộ lọc hàng trong 05_replication_setup_merge.sql.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = N'BENTHANH')
    INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
    VALUES (N'BENTHANH', N'Chi nhánh Bến Thành', N'123 Lê Lợi, Quận 1, TP.HCM', N'028-3822-1111');
PRINT '>>> CHINHANH: BENTHANH — OK';

IF NOT EXISTS (SELECT 1 FROM dbo.CHINHANH WHERE MACN = N'TANDINH')
    INSERT INTO dbo.CHINHANH (MACN, TENCN, DIACHI, SODT)
    VALUES (N'TANDINH', N'Chi nhánh Tân Định', N'456 Hai Bà Trưng, Quận 3, TP.HCM', N'028-3930-2222');
PRINT '>>> CHINHANH: TANDINH — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — NHÂN VIÊN (NHANVIEN)
   4 nhân viên: 2 ở Bến Thành, 2 ở Tân Định.
   MANV sử dụng mã cố định NV00000001..NV00000004 (SEQ_MANV bắt đầu từ 5).

   MANV nChar(10) — sẽ tự động pad thêm dấu cách phía sau.
   CMND là UNIQUE nên mỗi nhân viên cần CMND riêng biệt.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Nhân viên 1 — Bến Thành (CHINHANH login: NV_BT)
IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = N'NV00000001')
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (N'NV00000001', N'Nguyễn Văn', N'An', N'12 Trần Hưng Đạo, Q.1', N'0790000001', N'Nam', N'0901000001', N'BENTHANH', 0);

-- Nhân viên 2 — Bến Thành
IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = N'NV00000002')
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (N'NV00000002', N'Trần Thị', N'Bình', N'34 Nguyễn Huệ, Q.1', N'0790000002', N'Nữ', N'0901000002', N'BENTHANH', 0);

-- Nhân viên 3 — Tân Định
IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = N'NV00000003')
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (N'NV00000003', N'Lê Hoàng', N'Cường', N'56 Võ Văn Tần, Q.3', N'0790000003', N'Nam', N'0901000003', N'TANDINH', 0);

-- Nhân viên 4 — Tân Định
IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MANV = N'NV00000004')
    INSERT INTO dbo.NHANVIEN (MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa)
    VALUES (N'NV00000004', N'Phạm Mai', N'Dung', N'78 Hai Bà Trưng, Q.3', N'0790000004', N'Nữ', N'0901000004', N'TANDINH', 0);

PRINT '>>> NHANVIEN: 4 nhân viên (2 BT + 2 TD) — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — KHÁCH HÀNG (KHACHHANG)
   6 khách hàng: 3 ở Bến Thành, 3 ở Tân Định.
   CMND là PK nChar(10).
   NGAYCAP (ngày cấp CMND) — bắt buộc NOT NULL.
   PHAI phải là N'Nam' hoặc N'Nữ'.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Khách hàng 1 — Bến Thành
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800100001')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800100001', N'Hoàng Minh', N'Tuấn', '1990-03-15', N'10 Lý Tự Trọng, Q.1', '2010-05-20', N'0912000001', N'Nam', N'BENTHANH', 0);

-- Khách hàng 2 — Bến Thành
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800100002')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800100002', N'Vũ Thị', N'Lan', '1985-07-22', N'25 Đồng Khởi, Q.1', '2005-09-11', N'0912000002', N'Nữ', N'BENTHANH', 0);

-- Khách hàng 3 — Bến Thành
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800100003')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800100003', N'Đỗ Thanh', N'Hải', '1992-11-08', N'40 Pasteur, Q.1', '2012-01-15', N'0912000003', N'Nam', N'BENTHANH', 0);

-- Khách hàng 4 — Tân Định
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800200001')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800200001', N'Ngô Bảo', N'Ngọc', '1988-01-30', N'15 Trần Quang Khải, Q.3', '2008-04-18', N'0912000004', N'Nữ', N'TANDINH', 0);

-- Khách hàng 5 — Tân Định
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800200002')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800200002', N'Bùi Đức', N'Phú', '1995-06-12', N'30 Bà Huyện Thanh Quan, Q.3', '2015-08-25', N'0912000005', N'Nam', N'TANDINH', 0);

-- Khách hàng 6 — Tân Định
IF NOT EXISTS (SELECT 1 FROM dbo.KHACHHANG WHERE CMND = N'0800200003')
    INSERT INTO dbo.KHACHHANG (CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa)
    VALUES (N'0800200003', N'Lý Khánh', N'Vy', '1993-09-05', N'50 Nam Kỳ Khởi Nghĩa, Q.3', '2013-12-01', N'0912000006', N'Nữ', N'TANDINH', 0);

PRINT '>>> KHACHHANG: 6 khách hàng (3 BT + 3 TD) — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — TÀI KHOẢN (TAIKHOAN)
   8 tài khoản — một số khách hàng có tài khoản ở CHỈ chi nhánh mình.
   Khách hàng KH1 (BT) và KH4 (TD) có tài khoản ở CẢ HAI chi nhánh
   để minh họa kịch bản chuyển tiền liên chi nhánh.

   SOTK nChar(9): format MACN(viết tắt) + sequence.
   SODU phải >= 0. Khởi tạo với số dư hợp lý cho demo.
   MACN phải khớp chính xác với chi nhánh trong bộ lọc replication.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- TK1: KH1 (Tuấn) — tại Bến Thành, số dư 50 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'BT0000001')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'BT0000001', N'0800100001', 50000000, N'BENTHANH', '2024-01-10', 'Active');

-- TK2: KH2 (Lan) — tại Bến Thành, số dư 20 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'BT0000002')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'BT0000002', N'0800100002', 20000000, N'BENTHANH', '2024-02-15', 'Active');

-- TK3: KH3 (Hải) — tại Bến Thành, số dư 10 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'BT0000003')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'BT0000003', N'0800100003', 10000000, N'BENTHANH', '2024-03-20', 'Active');

-- TK4: KH4 (Ngọc) — tại Tân Định, số dư 35 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'TD0000001')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'TD0000001', N'0800200001', 35000000, N'TANDINH', '2024-01-20', 'Active');

-- TK5: KH5 (Phú) — tại Tân Định, số dư 15 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'TD0000002')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'TD0000002', N'0800200002', 15000000, N'TANDINH', '2024-04-05', 'Active');

-- TK6: KH6 (Vy) — tại Tân Định, số dư 8 triệu
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'TD0000003')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'TD0000003', N'0800200003', 8000000, N'TANDINH', '2024-05-12', 'Active');

-- TK7: KH1 (Tuấn) — tài khoản THỨ HAI ở Tân Định (liên chi nhánh)
-- Minh họa: khách hàng đăng ký tại BT nhưng mở thêm TK ở TD
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'TD0000004')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'TD0000004', N'0800100001', 5000000, N'TANDINH', '2024-06-01', 'Active');

-- TK8: KH4 (Ngọc) — tài khoản THỨ HAI ở Bến Thành (liên chi nhánh)
-- Minh họa: khách hàng đăng ký tại TD nhưng mở thêm TK ở BT
IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE SOTK = N'BT0000004')
    INSERT INTO dbo.TAIKHOAN (SOTK, CMND, SODU, MACN, NGAYMOTK, Status)
    VALUES (N'BT0000004', N'0800200001', 12000000, N'BENTHANH', '2024-06-15', 'Active');

PRINT '>>> TAIKHOAN: 8 tài khoản (4 BT + 4 TD, 2 khách hàng liên chi nhánh) — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — GIAO DỊCH GỬI / RÚT (GD_GOIRUT)
   7 giao dịch: hỗn hợp gửi tiền (GT) và rút tiền (RT).
   SOTIEN >= 100000 (ràng buộc CHECK).
   MAGD là IDENTITY — không chỉ định giá trị, để SQL Server tự gán.

   GHI CHÚ: Số dư trong TAIKHOAN ở Phần 4 đã PHẢN ÁNH kết quả cuối cùng
   sau tất cả giao dịch bên dưới. Nói cách khác, SODU = số dư ban đầu +/- các GD.
   Điều này giữ tính nhất quán dữ liệu mà KHÔNG cần chạy SP giao dịch.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chèn giao dịch gửi/rút, bảo vệ lũy đẳng bằng kiểm tra tổ hợp (SOTK + NGAYGD + SOTIEN + LOAIGD).
-- Dùng bảng tạm để tránh IDENTITY_INSERT conflict.

-- GD1: Gửi 10 triệu vào TK1 (BT) — nhân viên NV00000001
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'BT0000001' AND LOAIGD = N'GT' AND SOTIEN = 10000000 AND NGAYGD = '2024-07-01'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000001', N'GT', '2024-07-01', 10000000, N'NV00000001', N'BENTHANH', 'Completed');

-- GD2: Gửi 5 triệu vào TK2 (BT) — nhân viên NV00000002
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'BT0000002' AND LOAIGD = N'GT' AND SOTIEN = 5000000 AND NGAYGD = '2024-07-05'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000002', N'GT', '2024-07-05', 5000000, N'NV00000002', N'BENTHANH', 'Completed');

-- GD3: Rút 2 triệu từ TK1 (BT) — nhân viên NV00000001
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'BT0000001' AND LOAIGD = N'RT' AND SOTIEN = 2000000 AND NGAYGD = '2024-07-10'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000001', N'RT', '2024-07-10', 2000000, N'NV00000001', N'BENTHANH', 'Completed');

-- GD4: Gửi 15 triệu vào TK4 (TD) — nhân viên NV00000003
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'TD0000001' AND LOAIGD = N'GT' AND SOTIEN = 15000000 AND NGAYGD = '2024-07-02'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'TD0000001', N'GT', '2024-07-02', 15000000, N'NV00000003', N'TANDINH', 'Completed');

-- GD5: Gửi 3 triệu vào TK5 (TD) — nhân viên NV00000004
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'TD0000002' AND LOAIGD = N'GT' AND SOTIEN = 3000000 AND NGAYGD = '2024-07-08'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'TD0000002', N'GT', '2024-07-08', 3000000, N'NV00000004', N'TANDINH', 'Completed');

-- GD6: Rút 5 triệu từ TK4 (TD) — nhân viên NV00000003
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'TD0000001' AND LOAIGD = N'RT' AND SOTIEN = 5000000 AND NGAYGD = '2024-07-15'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'TD0000001', N'RT', '2024-07-15', 5000000, N'NV00000003', N'TANDINH', 'Completed');

-- GD7: Gửi 500 ngàn vào TK3 (BT) — nhân viên NV00000001 (kiểm tra SOTIEN tối thiểu = 100000)
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_GOIRUT
    WHERE SOTK = N'BT0000003' AND LOAIGD = N'GT' AND SOTIEN = 500000 AND NGAYGD = '2024-08-01'
)
    INSERT INTO dbo.GD_GOIRUT (SOTK, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000003', N'GT', '2024-08-01', 500000, N'NV00000001', N'BENTHANH', 'Completed');

PRINT '>>> GD_GOIRUT: 7 giao dịch gửi/rút (4 BT + 3 TD) — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 6 — GIAO DỊCH CHUYỂN TIỀN (GD_CHUYENTIEN)
   3 giao dịch chuyển tiền:
     • 1 chuyển cùng chi nhánh (BT → BT)
     • 1 chuyển cùng chi nhánh (TD → TD)
     • 1 chuyển liên chi nhánh (BT → TD) — minh họa cross-branch transfer

   MACN = chi nhánh tài khoản NGUỒN (SOTK_CHUYEN).
   LOAIGD luôn = 'CT'. SOTIEN > 0.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- CT1: Chuyển 1 triệu từ TK1 (BT) → TK2 (BT) — cùng chi nhánh
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_CHUYENTIEN
    WHERE SOTK_CHUYEN = N'BT0000001' AND SOTK_NHAN = N'BT0000002' AND SOTIEN = 1000000 AND NGAYGD = '2024-08-05'
)
    INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000001', N'BT0000002', N'CT', '2024-08-05', 1000000, N'NV00000001', N'BENTHANH', 'Completed');

-- CT2: Chuyển 2 triệu từ TK4 (TD) → TK5 (TD) — cùng chi nhánh
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_CHUYENTIEN
    WHERE SOTK_CHUYEN = N'TD0000001' AND SOTK_NHAN = N'TD0000002' AND SOTIEN = 2000000 AND NGAYGD = '2024-08-10'
)
    INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'TD0000001', N'TD0000002', N'CT', '2024-08-10', 2000000, N'NV00000003', N'TANDINH', 'Completed');

-- CT3: Chuyển 3 triệu từ TK1 (BT) → TK4 (TD) — LIÊN CHI NHÁNH
-- Minh họa SP_CrossBranchTransfer: nguồn ở BT, đích ở TD, ghi nhận tại BT
IF NOT EXISTS (
    SELECT 1 FROM dbo.GD_CHUYENTIEN
    WHERE SOTK_CHUYEN = N'BT0000001' AND SOTK_NHAN = N'TD0000001' AND SOTIEN = 3000000 AND NGAYGD = '2024-08-15'
)
    INSERT INTO dbo.GD_CHUYENTIEN (SOTK_CHUYEN, SOTK_NHAN, LOAIGD, NGAYGD, SOTIEN, MANV, MACN, Status)
    VALUES (N'BT0000001', N'TD0000001', N'CT', '2024-08-15', 3000000, N'NV00000001', N'BENTHANH', 'Completed');

PRINT '>>> GD_CHUYENTIEN: 3 giao dịch chuyển tiền (2 nội bộ + 1 liên CN) — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 7 — CẬP NHẬT NGUOIDUNG (hub-only, tùy chọn)
   Ánh xạ seed login (từ script 04) sang nhân viên/khách hàng cụ thể
   để sp_DangNhap có thể phân giải DefaultBranch.
   NGUOIDUNG KHÔNG được replicate — chỉ tồn tại trên Publisher.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Ánh xạ NV_BT (CHINHANH) → nhân viên NV00000001, chi nhánh BENTHANH
IF NOT EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = N'NV_BT')
    INSERT INTO dbo.NGUOIDUNG (Username, PasswordHash, UserGroup, DefaultBranch, EmployeeId, TrangThaiXoa)
    VALUES (N'NV_BT', N'N/A-SQL-AUTH', 1, N'BENTHANH', N'NV00000001', 0);
PRINT '>>> NGUOIDUNG: NV_BT → BENTHANH — OK';

-- Ánh xạ KH_DEMO (KHACHHANG) → khách hàng 0800100001, chi nhánh BENTHANH
IF NOT EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = N'KH_DEMO')
    INSERT INTO dbo.NGUOIDUNG (Username, PasswordHash, UserGroup, DefaultBranch, CustomerCMND, TrangThaiXoa)
    VALUES (N'KH_DEMO', N'N/A-SQL-AUTH', 2, N'BENTHANH', N'0800100001', 0);
PRINT '>>> NGUOIDUNG: KH_DEMO → BENTHANH — OK';

-- Ánh xạ ADMIN_NH (NGANHANG) — không cần DefaultBranch cố định (chọn từ dropdown)
IF NOT EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = N'ADMIN_NH')
    INSERT INTO dbo.NGUOIDUNG (Username, PasswordHash, UserGroup, DefaultBranch, TrangThaiXoa)
    VALUES (N'ADMIN_NH', N'N/A-SQL-AUTH', 0, N'BENTHANH', 0);
PRINT '>>> NGUOIDUNG: ADMIN_NH — OK';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 8 — TỔNG KẾT XÁC MINH
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '=== TÓM TẮT DỮ LIỆU MẪU ===';

SELECT 'CHINHANH'      AS [Bảng], COUNT(*) AS [Số hàng] FROM dbo.CHINHANH       UNION ALL
SELECT 'NHANVIEN',                COUNT(*)               FROM dbo.NHANVIEN       UNION ALL
SELECT 'KHACHHANG',               COUNT(*)               FROM dbo.KHACHHANG      UNION ALL
SELECT 'TAIKHOAN',                COUNT(*)               FROM dbo.TAIKHOAN       UNION ALL
SELECT 'GD_GOIRUT',               COUNT(*)               FROM dbo.GD_GOIRUT      UNION ALL
SELECT 'GD_CHUYENTIEN',           COUNT(*)               FROM dbo.GD_CHUYENTIEN  UNION ALL
SELECT 'NGUOIDUNG',               COUNT(*)               FROM dbo.NGUOIDUNG;

PRINT '';
PRINT '=== PHÂN BỐ THEO CHI NHÁNH ===';

SELECT 'KHACHHANG' AS [Bảng], RTRIM(MACN) AS MACN, COUNT(*) AS [Số hàng] FROM dbo.KHACHHANG  GROUP BY MACN UNION ALL
SELECT 'NHANVIEN',             RTRIM(MACN),          COUNT(*)              FROM dbo.NHANVIEN   GROUP BY MACN UNION ALL
SELECT 'TAIKHOAN',             RTRIM(MACN),          COUNT(*)              FROM dbo.TAIKHOAN   GROUP BY MACN UNION ALL
SELECT 'GD_GOIRUT',            RTRIM(MACN),          COUNT(*)              FROM dbo.GD_GOIRUT  GROUP BY MACN UNION ALL
SELECT 'GD_CHUYENTIEN',        RTRIM(MACN),          COUNT(*)              FROM dbo.GD_CHUYENTIEN GROUP BY MACN
ORDER BY [Bảng], MACN;
GO

PRINT '';
PRINT '=== 04b_publisher_seed_data.sql hoàn thành thành công ===';
PRINT '    Dữ liệu sẽ được truyền đến CN1/CN2/TraCuu sau khi Snapshot Agent chạy.';
PRINT '    Nếu replication đã hoạt động, Merge Agent sẽ đồng bộ trong lần tiếp theo.';
GO
