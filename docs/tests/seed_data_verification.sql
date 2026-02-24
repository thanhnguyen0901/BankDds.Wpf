/*=============================================================================
  seed_data_verification.sql
  Mục đích: Xác minh dữ liệu mẫu đã được đồng bộ đúng đến từng subscriber
             sau khi Snapshot Agent hoàn thành.

  Cách dùng:
    1. Chạy PHẦN 1 trên Publisher (DESKTOP-JBB41QU / NGANHANG_PUB)
    2. Chạy PHẦN 2 trên CN1      (SQLSERVER2 / NGANHANG_BT)
    3. Chạy PHẦN 3 trên CN2      (SQLSERVER3 / NGANHANG_TD)
    4. Chạy PHẦN 4 trên TraCuu   (SQLSERVER4 / NGANHANG_TRACUU)

  Kết quả mong đợi được ghi rõ bên cạnh mỗi truy vấn.
=============================================================================*/


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — XÁC MINH TRÊN PUBLISHER (NGANHANG_PUB)
   Publisher phải chứa TẤT CẢ dữ liệu của mọi chi nhánh.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chạy trên: DESKTOP-JBB41QU
USE NGANHANG_PUB;
GO

PRINT '=== PUBLISHER: Kiểm tra tổng số hàng ===';

-- Kết quả mong đợi: CHINHANH=2, NHANVIEN>=4, KHACHHANG>=6, TAIKHOAN>=8, GD_GOIRUT>=7, GD_CHUYENTIEN>=3
SELECT 'CHINHANH'      AS [Bảng], COUNT(*) AS [Tổng] FROM dbo.CHINHANH       UNION ALL
SELECT 'NHANVIEN',                COUNT(*)            FROM dbo.NHANVIEN       UNION ALL
SELECT 'KHACHHANG',               COUNT(*)            FROM dbo.KHACHHANG      UNION ALL
SELECT 'TAIKHOAN',                COUNT(*)            FROM dbo.TAIKHOAN       UNION ALL
SELECT 'GD_GOIRUT',               COUNT(*)            FROM dbo.GD_GOIRUT      UNION ALL
SELECT 'GD_CHUYENTIEN',           COUNT(*)            FROM dbo.GD_CHUYENTIEN
ORDER BY [Bảng];
GO

PRINT '=== PUBLISHER: Phân bố theo chi nhánh ===';

-- Kết quả mong đợi: mỗi bảng có hàng thuộc CẢ 'BENTHANH' VÀ 'TANDINH'
SELECT 'KHACHHANG' AS [Bảng], RTRIM(MACN) AS MACN, COUNT(*) AS [Số hàng]
FROM dbo.KHACHHANG GROUP BY MACN
UNION ALL
SELECT 'NHANVIEN', RTRIM(MACN), COUNT(*) FROM dbo.NHANVIEN GROUP BY MACN
UNION ALL
SELECT 'TAIKHOAN', RTRIM(MACN), COUNT(*) FROM dbo.TAIKHOAN GROUP BY MACN
UNION ALL
SELECT 'GD_GOIRUT', RTRIM(MACN), COUNT(*) FROM dbo.GD_GOIRUT GROUP BY MACN
UNION ALL
SELECT 'GD_CHUYENTIEN', RTRIM(MACN), COUNT(*) FROM dbo.GD_CHUYENTIEN GROUP BY MACN
ORDER BY [Bảng], MACN;
GO

PRINT '=== PUBLISHER: Chi tiết chi nhánh ===';
SELECT RTRIM(MACN) AS MACN, TENCN, DIACHI, SODT FROM dbo.CHINHANH ORDER BY MACN;
GO

PRINT '=== PUBLISHER: Tài khoản liên chi nhánh ===';
-- Kết quả mong đợi: KH 0800100001 (Tuấn) có TK ở cả BT và TD
--                    KH 0800200001 (Ngọc) có TK ở cả TD và BT
SELECT
    RTRIM(tk.SOTK) AS SOTK,
    RTRIM(tk.CMND) AS CMND,
    kh.HO + N' ' + kh.TEN AS [Tên KH],
    RTRIM(tk.MACN) AS [CN Tài khoản],
    RTRIM(kh.MACN) AS [CN Khách hàng],
    tk.SODU,
    tk.Status
FROM dbo.TAIKHOAN tk
JOIN dbo.KHACHHANG kh ON tk.CMND = kh.CMND
ORDER BY tk.CMND, tk.MACN;
GO

PRINT '=== PUBLISHER: Tổng hợp giao dịch gửi/rút ===';
SELECT
    RTRIM(MACN) AS MACN,
    LOAIGD,
    COUNT(*) AS [Số GD],
    SUM(SOTIEN) AS [Tổng tiền]
FROM dbo.GD_GOIRUT
GROUP BY MACN, LOAIGD
ORDER BY MACN, LOAIGD;
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — XÁC MINH TRÊN CN1 (NGANHANG_BT — Bến Thành)
   CN1 chỉ nhận dữ liệu với MACN = 'BENTHANH' (trừ CHINHANH không filter).
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chạy trên: DESKTOP-JBB41QU\SQLSERVER2
-- USE NGANHANG_BT;
-- GO

PRINT '=== CN1 (NGANHANG_BT): Kiểm tra bộ lọc hàng ===';

-- CHINHANH: phải có TẤT CẢ chi nhánh (CHINHANH replicate không filter)
-- Kết quả mong đợi: 2 hàng (BENTHANH + TANDINH)
PRINT '-- CHINHANH (không filter → cả 2 chi nhánh):';
-- SELECT RTRIM(MACN) AS MACN, TENCN FROM dbo.CHINHANH ORDER BY MACN;

-- KHACHHANG: chỉ MACN = 'BENTHANH'
-- Kết quả mong đợi: 3 hàng (0800100001, 0800100002, 0800100003)
PRINT '-- KHACHHANG (filter MACN=BENTHANH):';
-- SELECT RTRIM(CMND) AS CMND, HO + N'' '' + TEN AS [Tên], RTRIM(MACN) AS MACN FROM dbo.KHACHHANG ORDER BY CMND;
-- SELECT DISTINCT RTRIM(MACN) AS MACN FROM dbo.KHACHHANG;
-- Kết quả: chỉ 'BENTHANH'

-- NHANVIEN: chỉ MACN = 'BENTHANH'
-- Kết quả mong đợi: 2 hàng (NV00000001, NV00000002)
PRINT '-- NHANVIEN (filter MACN=BENTHANH):';
-- SELECT RTRIM(MANV) AS MANV, HO + N'' '' + TEN AS [Tên], RTRIM(MACN) AS MACN FROM dbo.NHANVIEN ORDER BY MANV;
-- SELECT DISTINCT RTRIM(MACN) AS MACN FROM dbo.NHANVIEN;
-- Kết quả: chỉ 'BENTHANH'

-- TAIKHOAN: chỉ MACN = 'BENTHANH'
-- Kết quả mong đợi: 4 hàng (BT0000001, BT0000002, BT0000003, BT0000004)
-- Lưu ý: BT0000004 thuộc KH 0800200001 (Ngọc) đăng ký ở TD nhưng mở TK ở BT
PRINT '-- TAIKHOAN (filter MACN=BENTHANH):';
-- SELECT RTRIM(SOTK) AS SOTK, RTRIM(CMND) AS CMND, SODU, RTRIM(MACN) AS MACN, Status FROM dbo.TAIKHOAN ORDER BY SOTK;

-- GD_GOIRUT: chỉ MACN = 'BENTHANH' (join filter qua TAIKHOAN)
-- Kết quả mong đợi: 4 giao dịch (GD1 GT, GD2 GT, GD3 RT, GD7 GT)
PRINT '-- GD_GOIRUT (filter MACN=BENTHANH):';
-- SELECT MAGD, RTRIM(SOTK) AS SOTK, LOAIGD, NGAYGD, SOTIEN, RTRIM(MACN) AS MACN FROM dbo.GD_GOIRUT ORDER BY NGAYGD;

-- GD_CHUYENTIEN: chỉ MACN = 'BENTHANH'
-- Kết quả mong đợi: 2 giao dịch (CT1 nội bộ BT, CT3 liên CN BT→TD)
-- CT3 có MACN = BENTHANH vì nguồn ở BT
PRINT '-- GD_CHUYENTIEN (filter MACN=BENTHANH):';
-- SELECT MAGD, RTRIM(SOTK_CHUYEN) AS [Nguồn], RTRIM(SOTK_NHAN) AS [Đích], SOTIEN, RTRIM(MACN) AS MACN FROM dbo.GD_CHUYENTIEN ORDER BY NGAYGD;
GO

PRINT '';
PRINT '>>> Bỏ comment (--) các lệnh SELECT ở trên và chạy trên SQLSERVER2/NGANHANG_BT';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — XÁC MINH TRÊN CN2 (NGANHANG_TD — Tân Định)
   CN2 chỉ nhận dữ liệu với MACN = 'TANDINH'.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chạy trên: DESKTOP-JBB41QU\SQLSERVER3
-- USE NGANHANG_TD;
-- GO

PRINT '=== CN2 (NGANHANG_TD): Kiểm tra bộ lọc hàng ===';

-- CHINHANH: 2 hàng (cả 2 CN — không filter)
-- KHACHHANG: 3 hàng (0800200001, 0800200002, 0800200003) — chỉ TANDINH
-- NHANVIEN: 2 hàng (NV00000003, NV00000004) — chỉ TANDINH
-- TAIKHOAN: 4 hàng (TD0000001, TD0000002, TD0000003, TD0000004)
--   TD0000004 thuộc KH 0800100001 (Tuấn) đăng ký ở BT nhưng mở TK ở TD
-- GD_GOIRUT: 3 giao dịch (GD4 GT, GD5 GT, GD6 RT) — chỉ TANDINH
-- GD_CHUYENTIEN: 1 giao dịch (CT2 nội bộ TD)
--   CT3 (BT→TD) có MACN = BENTHANH nên KHÔNG xuất hiện ở CN2

PRINT '-- Bỏ comment các SELECT bên dưới và chạy trên SQLSERVER3/NGANHANG_TD:';
PRINT '';
-- SELECT RTRIM(MACN) AS MACN, TENCN FROM dbo.CHINHANH ORDER BY MACN;
-- SELECT RTRIM(CMND) AS CMND, HO + N'' '' + TEN AS [Tên], RTRIM(MACN) AS MACN FROM dbo.KHACHHANG ORDER BY CMND;
-- SELECT RTRIM(MANV) AS MANV, HO + N'' '' + TEN AS [Tên], RTRIM(MACN) AS MACN FROM dbo.NHANVIEN ORDER BY MANV;
-- SELECT RTRIM(SOTK) AS SOTK, RTRIM(CMND) AS CMND, SODU, RTRIM(MACN) AS MACN FROM dbo.TAIKHOAN ORDER BY SOTK;
-- SELECT MAGD, RTRIM(SOTK) AS SOTK, LOAIGD, SOTIEN, RTRIM(MACN) AS MACN FROM dbo.GD_GOIRUT ORDER BY NGAYGD;
-- SELECT MAGD, RTRIM(SOTK_CHUYEN) AS [Nguồn], RTRIM(SOTK_NHAN) AS [Đích], SOTIEN, RTRIM(MACN) AS MACN FROM dbo.GD_CHUYENTIEN ORDER BY NGAYGD;
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — XÁC MINH TRÊN TRACUU (NGANHANG_TRACUU)
   TraCuu chỉ nhận: CHINHANH (tất cả) + KHACHHANG (TrangThaiXoa = 0, tất cả CN).
   KHÔNG nhận: NHANVIEN, TAIKHOAN, GD_GOIRUT, GD_CHUYENTIEN.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chạy trên: DESKTOP-JBB41QU\SQLSERVER4
-- USE NGANHANG_TRACUU;
-- GO

PRINT '=== TRACUU (NGANHANG_TRACUU): Kiểm tra dữ liệu ===';

-- CHINHANH: 2 hàng (tất cả CN, download-only)
PRINT '-- CHINHANH (tất cả, download-only):';
-- SELECT RTRIM(MACN) AS MACN, TENCN FROM dbo.CHINHANH ORDER BY MACN;
-- Kết quả: BENTHANH, TANDINH

-- KHACHHANG: 6 hàng (TẤT CẢ CN, chỉ TrangThaiXoa = 0, download-only)
PRINT '-- KHACHHANG (tất cả CN, chỉ active):';
-- SELECT RTRIM(CMND) AS CMND, HO + N'' '' + TEN AS [Tên], RTRIM(MACN) AS MACN, TrangThaiXoa FROM dbo.KHACHHANG ORDER BY MACN, CMND;
-- Kết quả: 3 BT + 3 TD, tất cả TrangThaiXoa = 0
-- SELECT RTRIM(MACN) AS MACN, COUNT(*) AS [Số KH] FROM dbo.KHACHHANG GROUP BY MACN;
-- Kết quả: BENTHANH=3, TANDINH=3

-- Xác nhận KHÔNG có bảng giao dịch
PRINT '-- Kiểm tra KHÔNG tồn tại bảng giao dịch:';
-- SELECT name FROM sys.tables WHERE name IN ('NHANVIEN', 'TAIKHOAN', 'GD_GOIRUT', 'GD_CHUYENTIEN') ORDER BY name;
-- Kết quả mong đợi: TRỐNG (không có hàng nào)

-- Xác nhận chỉ có 2 bảng nghiệp vụ (+ MSmerge_* metadata)
PRINT '-- Danh sách bảng:';
-- SELECT name FROM sys.tables WHERE name NOT LIKE 'MSmerge_%' AND name NOT LIKE 'sysmergearticles%' ORDER BY name;
-- Kết quả mong đợi: CHINHANH, KHACHHANG
GO

PRINT '';
PRINT '>>> Bỏ comment (--) các lệnh SELECT và chạy trên SQLSERVER4/NGANHANG_TRACUU';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — KIỂM TRA TÍNH NHẤT QUÁN SỐ DƯ (chạy trên Publisher)
   Xác minh số dư tài khoản khớp với giao dịch seed.

   Kịch bản tính toán cho TK BT0000001 (Hoàng Minh Tuấn):
     Số dư ban đầu giả định khi mở TK          : 36.000.000
     + GD1: Gửi 10.000.000                      : 46.000.000
     - GD3: Rút  2.000.000                      : 44.000.000
     - CT1: Chuyển 1.000.000 → BT0000002        : 43.000.000
     - CT3: Chuyển 3.000.000 → TD0000001        : 40.000.000
     ❌ Nhưng SODU seed = 50.000.000 (đã set sẵn)
     → Không cần khớp chính xác vì seed INSERT SODU là giá trị trực tiếp,
       KHÔNG đi qua SP_Deposit/SP_Withdraw.

   GHI CHÚ: Dữ liệu seed chèn trực tiếp bảng (không gọi SP), nên SODU
   và giao dịch chỉ mang tính minh họa. Trong sử dụng thực tế, tất cả thay đổi
   số dư phải đi qua SP_Deposit / SP_Withdraw / SP_CrossBranchTransfer.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Chạy trên Publisher
PRINT '=== PUBLISHER: Tóm tắt tài khoản + số dư ===';

SELECT
    RTRIM(tk.SOTK)          AS SOTK,
    RTRIM(tk.CMND)          AS CMND,
    kh.HO + N' ' + kh.TEN  AS [Tên KH],
    RTRIM(tk.MACN)          AS MACN,
    tk.SODU,
    tk.Status,
    ISNULL(gr_gt.TongGui, 0) AS [Tổng gửi],
    ISNULL(gr_rt.TongRut, 0) AS [Tổng rút],
    ISNULL(ct_out.TongChuyen, 0) AS [Tổng chuyển đi],
    ISNULL(ct_in.TongNhan, 0)    AS [Tổng nhận về]
FROM dbo.TAIKHOAN tk
JOIN dbo.KHACHHANG kh ON tk.CMND = kh.CMND
LEFT JOIN (
    SELECT SOTK, SUM(SOTIEN) AS TongGui
    FROM dbo.GD_GOIRUT WHERE LOAIGD = N'GT' GROUP BY SOTK
) gr_gt ON gr_gt.SOTK = tk.SOTK
LEFT JOIN (
    SELECT SOTK, SUM(SOTIEN) AS TongRut
    FROM dbo.GD_GOIRUT WHERE LOAIGD = N'RT' GROUP BY SOTK
) gr_rt ON gr_rt.SOTK = tk.SOTK
LEFT JOIN (
    SELECT SOTK_CHUYEN AS SOTK, SUM(SOTIEN) AS TongChuyen
    FROM dbo.GD_CHUYENTIEN GROUP BY SOTK_CHUYEN
) ct_out ON ct_out.SOTK = tk.SOTK
LEFT JOIN (
    SELECT SOTK_NHAN AS SOTK, SUM(SOTIEN) AS TongNhan
    FROM dbo.GD_CHUYENTIEN GROUP BY SOTK_NHAN
) ct_in ON ct_in.SOTK = tk.SOTK
ORDER BY tk.MACN, tk.SOTK;
GO

PRINT '';
PRINT '=== Xác minh hoàn tất. So sánh kết quả với giá trị mong đợi ở trên. ===';
GO
