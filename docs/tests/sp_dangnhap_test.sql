/*=============================================================================
  sp_dangnhap_test.sql
  Mục đích: Xác minh sp_DangNhap trả về đúng (MANV, HOTEN, TENNHOM, MACN)
             trên Publisher VÀ từng Subscriber.

  Điều kiện tiên quyết:
    - 04_publisher_security.sql đã chạy trên Publisher
    - 04b_publisher_seed_data.sql đã chạy trên Publisher (NGUOIDUNG rows)
    - Snapshot đã đồng bộ xong
    - 08_subscribers_post_replication_fixups.sql đã chạy trên mỗi Subscriber

  Cách dùng:
    PHẦN 1 → chạy trên Publisher (DESKTOP-JBB41QU)
    PHẦN 2 → chạy trên CN1 (SQLSERVER2)
    PHẦN 3 → chạy trên CN2 (SQLSERVER3)
    PHẦN 4 → chạy trên TraCuu (SQLSERVER4) — optional
=============================================================================*/


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — PUBLISHER (NGANHANG_PUB)
   ═══════════════════════════════════════════════════════════════════════════════ */

USE NGANHANG_PUB;
GO

PRINT '';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '  PUBLISHER — sp_DangNhap Tests';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '';

-- ── Test 1.1: Gọi sp_DangNhap bằng login hiện tại (thường là sa / sysadmin) ──
-- Kết quả mong đợi: TENNHOM = 'NGANHANG', MACN = NULL
PRINT '── Test 1.1: Current login (sysadmin → NGANHANG, MACN NULL) ──';
EXEC dbo.sp_DangNhap;
GO

-- ── Test 1.2: Kiểm tra cấu trúc kết quả trả về ──
-- Xác nhận SP trả về đúng 4 cột: MANV, HOTEN, TENNHOM, MACN
PRINT '';
PRINT '── Test 1.2: Verify result schema has exactly 4 columns ──';

DECLARE @TestTable TABLE (MANV nvarchar(50), HOTEN nvarchar(128), TENNHOM nvarchar(128), MACN nChar(10));
INSERT INTO @TestTable EXEC dbo.sp_DangNhap;

SELECT
    CASE WHEN COUNT(*) = 1 THEN 'PASS' ELSE 'FAIL' END AS [Row count = 1],
    MAX(MANV)     AS MANV,
    MAX(HOTEN)    AS HOTEN,
    MAX(TENNHOM)  AS TENNHOM,
    MAX(MACN)     AS MACN
FROM @TestTable;
GO

-- ── Test 1.3: EXECUTE AS ADMIN_NH → NGANHANG, MACN NULL ──
PRINT '';
PRINT '── Test 1.3: ADMIN_NH → NGANHANG, MACN = NULL ──';
EXECUTE AS USER = N'ADMIN_NH';
    EXEC dbo.sp_DangNhap;
REVERT;
GO

-- ── Test 1.4: EXECUTE AS NV_BT → CHINHANH, MACN = BENTHANH ──
-- NV_BT is a CHINHANH role member. NGUOIDUNG has DefaultBranch = 'BENTHANH'.
PRINT '';
PRINT '── Test 1.4: NV_BT → CHINHANH, MACN = BENTHANH ──';
EXECUTE AS USER = N'NV_BT';
    EXEC dbo.sp_DangNhap;
REVERT;
GO

-- ── Test 1.5: EXECUTE AS KH_DEMO → KHACHHANG, MACN = BENTHANH ──
-- KH_DEMO is a KHACHHANG role member. NGUOIDUNG has DefaultBranch = 'BENTHANH'.
PRINT '';
PRINT '── Test 1.5: KH_DEMO → KHACHHANG, MACN = BENTHANH ──';
EXECUTE AS USER = N'KH_DEMO';
    EXEC dbo.sp_DangNhap;
REVERT;
GO

-- ── Test 1.6: Xác minh NGUOIDUNG fallback hoạt động đúng ──
-- Kiểm tra NGUOIDUNG chứa đúng mapping cho seed logins
PRINT '';
PRINT '── Test 1.6: NGUOIDUNG mapping verification ──';
SELECT
    Username,
    UserGroup,
    DefaultBranch,
    EmployeeId,
    CustomerCMND
FROM dbo.NGUOIDUNG
WHERE TrangThaiXoa = 0
ORDER BY Username;
GO

-- ── Test 1.7: Kiểm tra ownership chaining ──
-- sp_DangNhap (dbo) đọc NGUOIDUNG (dbo) → bypass DENY SELECT.
-- NV_BT has DENY SELECT ON NGUOIDUNG nhưng SP vẫn đọc được nhờ chaining.
PRINT '';
PRINT '── Test 1.7: Ownership chaining verification ──';
PRINT '  NV_BT has DENY SELECT on NGUOIDUNG, but sp_DangNhap reads it via ownership chain:';
EXECUTE AS USER = N'NV_BT';
    -- Direct SELECT should fail:
    BEGIN TRY
        DECLARE @chk nvarchar(50);
        SELECT @chk = Username FROM dbo.NGUOIDUNG WHERE Username = N'NV_BT';
        PRINT '  FAIL — direct SELECT on NGUOIDUNG should have been denied!';
    END TRY
    BEGIN CATCH
        PRINT '  PASS — direct SELECT denied as expected: ' + ERROR_MESSAGE();
    END CATCH

    -- sp_DangNhap should succeed and return MACN = BENTHANH:
    PRINT '  sp_DangNhap result (should show MACN = BENTHANH):';
    EXEC dbo.sp_DangNhap;
REVERT;
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — CN1 (NGANHANG_BT — Bến Thành)
   Chạy trên: DESKTOP-JBB41QU\SQLSERVER2

   Kết quả mong đợi cho CHINHANH/KHACHHANG roles:
     MACN = 'BENTHANH  ' (nChar(10), padded)
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bỏ comment khi chạy trên SQLSERVER2:
-- USE NGANHANG_BT;
-- GO

PRINT '';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '  CN1 (NGANHANG_BT) — sp_DangNhap Tests';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '';

-- ── Test 2.1: Current login (sysadmin → NGANHANG, MACN NULL) ──
PRINT '── Test 2.1: Current login → NGANHANG, MACN NULL ──';
-- EXEC dbo.sp_DangNhap;
PRINT '(uncomment and run on SQLSERVER2)';
GO

-- ── Test 2.2: NV_BT → CHINHANH, MACN = BENTHANH ──
-- DB_NAME() = 'NGANHANG_BT' → CASE returns 'BENTHANH'
PRINT '';
PRINT '── Test 2.2: NV_BT → CHINHANH, MACN = BENTHANH ──';
-- EXECUTE AS USER = N'NV_BT';
--     EXEC dbo.sp_DangNhap;
-- REVERT;
PRINT '(uncomment and run on SQLSERVER2)';
GO

-- ── Test 2.3: Verify CHINHANH has 2 rows (no row filter) ──
-- Confirms that the old TOP 1 approach would be unreliable.
PRINT '';
PRINT '── Test 2.3: CHINHANH has 2 rows (no filter → old TOP 1 was unreliable) ──';
-- SELECT RTRIM(MACN) AS MACN, TENCN FROM dbo.CHINHANH ORDER BY MACN;
-- Expected: BENTHANH + TANDINH (2 rows)
PRINT '(uncomment and run on SQLSERVER2)';
GO

-- ── Test 2.4: Verify DB_NAME() mapping ──
PRINT '';
PRINT '── Test 2.4: DB_NAME() mapping sanity check ──';
-- SELECT
--     DB_NAME() AS [DB_NAME()],
--     CASE DB_NAME()
--         WHEN N'NGANHANG_BT' THEN N'BENTHANH'
--         WHEN N'NGANHANG_TD' THEN N'TANDINH'
--         ELSE N'(unmapped)'
--     END AS [Expected MACN];
-- Expected: NGANHANG_BT → BENTHANH
PRINT '(uncomment and run on SQLSERVER2)';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — CN2 (NGANHANG_TD — Tân Định)
   Chạy trên: DESKTOP-JBB41QU\SQLSERVER3

   Kết quả mong đợi cho CHINHANH/KHACHHANG roles:
     MACN = 'TANDINH   ' (nChar(10), padded)
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bỏ comment khi chạy trên SQLSERVER3:
-- USE NGANHANG_TD;
-- GO

PRINT '';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '  CN2 (NGANHANG_TD) — sp_DangNhap Tests';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '';

-- ── Test 3.1: Current login → NGANHANG, MACN NULL ──
PRINT '── Test 3.1: Current login → NGANHANG, MACN NULL ──';
-- EXEC dbo.sp_DangNhap;
PRINT '(uncomment and run on SQLSERVER3)';
GO

-- ── Test 3.2: NV_BT → CHINHANH, MACN = TANDINH ──
-- NV_BT on CN2 has role CHINHANH. DB_NAME() = 'NGANHANG_TD' → TANDINH.
-- Note: even though login is named "NV_BT" (Bến Thành), on the TD subscriber
-- the branch is always TANDINH — this is correct subscriber behavior.
PRINT '';
PRINT '── Test 3.2: CHINHANH user → MACN = TANDINH (always, regardless of login name) ──';
-- EXECUTE AS USER = N'NV_BT';
--     EXEC dbo.sp_DangNhap;
-- REVERT;
PRINT '(uncomment and run on SQLSERVER3)';
GO

-- ── Test 3.3: Verify DB_NAME() mapping ──
PRINT '';
PRINT '── Test 3.3: DB_NAME() mapping sanity check ──';
-- SELECT
--     DB_NAME() AS [DB_NAME()],
--     CASE DB_NAME()
--         WHEN N'NGANHANG_BT' THEN N'BENTHANH'
--         WHEN N'NGANHANG_TD' THEN N'TANDINH'
--         ELSE N'(unmapped)'
--     END AS [Expected MACN];
-- Expected: NGANHANG_TD → TANDINH
PRINT '(uncomment and run on SQLSERVER3)';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — TRACUU (NGANHANG_TRACUU)
   Chạy trên: DESKTOP-JBB41QU\SQLSERVER4

   TraCuu không có CHINHANH/KHACHHANG employees, nhưng sp_DangNhap
   phải vẫn chạy đúng. MACN = NULL cho mọi role trên TraCuu.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Bỏ comment khi chạy trên SQLSERVER4:
-- USE NGANHANG_TRACUU;
-- GO

PRINT '';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '  TRACUU (NGANHANG_TRACUU) — sp_DangNhap Tests';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '';

-- ── Test 4.1: Current login → NGANHANG, MACN NULL ──
PRINT '── Test 4.1: Current login → NGANHANG, MACN NULL ──';
-- EXEC dbo.sp_DangNhap;
PRINT '(uncomment and run on SQLSERVER4)';
GO

-- ── Test 4.2: DB_NAME() mapping → NULL (TRACUU not mapped) ──
PRINT '';
PRINT '── Test 4.2: DB_NAME() mapping → NULL (no branch for TRACUU) ──';
-- SELECT
--     DB_NAME() AS [DB_NAME()],
--     CASE DB_NAME()
--         WHEN N'NGANHANG_BT' THEN N'BENTHANH'
--         WHEN N'NGANHANG_TD' THEN N'TANDINH'
--         ELSE N'(unmapped / NULL)'
--     END AS [Expected MACN];
-- Expected: NGANHANG_TRACUU → (unmapped / NULL)
PRINT '(uncomment and run on SQLSERVER4)';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — TÓM TẮT EXPECTED RESULTS
   ═══════════════════════════════════════════════════════════════════════════════ */

PRINT '';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '  Expected Results Summary';
PRINT '══════════════════════════════════════════════════════════════';
PRINT '';
PRINT '  ┌─────────────┬───────────┬──────────┬───────────────────┐';
PRINT '  │ Server      │ Login     │ TENNHOM  │ MACN (trimmed)    │';
PRINT '  ├─────────────┼───────────┼──────────┼───────────────────┤';
PRINT '  │ Publisher   │ sa        │ NGANHANG │ NULL              │';
PRINT '  │ Publisher   │ ADMIN_NH  │ NGANHANG │ NULL              │';
PRINT '  │ Publisher   │ NV_BT     │ CHINHANH │ BENTHANH          │';
PRINT '  │ Publisher   │ KH_DEMO   │ KHACHHANG│ BENTHANH          │';
PRINT '  ├─────────────┼───────────┼──────────┼───────────────────┤';
PRINT '  │ CN1 (BT)    │ sa        │ NGANHANG │ NULL              │';
PRINT '  │ CN1 (BT)    │ NV_BT     │ CHINHANH │ BENTHANH          │';
PRINT '  │ CN1 (BT)    │ KH_DEMO   │ KHACHHANG│ BENTHANH          │';
PRINT '  ├─────────────┼───────────┼──────────┼───────────────────┤';
PRINT '  │ CN2 (TD)    │ sa        │ NGANHANG │ NULL              │';
PRINT '  │ CN2 (TD)    │ NV_BT     │ CHINHANH │ TANDINH           │';
PRINT '  │ CN2 (TD)    │ KH_DEMO   │ KHACHHANG│ TANDINH           │';
PRINT '  ├─────────────┼───────────┼──────────┼───────────────────┤';
PRINT '  │ TraCuu      │ sa        │ NGANHANG │ NULL              │';
PRINT '  │ TraCuu      │ *any*     │ *any*    │ NULL              │';
PRINT '  └─────────────┴───────────┴──────────┴───────────────────┘';
PRINT '';
PRINT '  Key insight: On subscribers, MACN is derived from DB_NAME(),';
PRINT '  NOT from CHINHANH table (which has all branches, no row filter).';
PRINT '  On Publisher, MACN is resolved via NGUOIDUNG → NHANVIEN → KHACHHANG cascade.';
GO
