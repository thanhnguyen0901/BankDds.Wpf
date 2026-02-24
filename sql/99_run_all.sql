/*=============================================================================
  99_run_all.sql
  Vai trò : Tập lệnh chạy tổng / tự động hóa
  Chạy trên: DESKTOP-JBB41QU (Máy chủ phát hành) sử dụng chế độ SQLCMD
  Mục đích : Điều phối tất cả tập lệnh distributed_banking theo đúng thứ tự.

  DB Máy chủ phát hành: NGANHANG_PUB

  Điều kiện tiên quyết (chạy TRƯỚC tập lệnh này):
    1. Tất cả bốn phiên bản SQL Server đang chạy:
         - DESKTOP-JBB41QU          (mặc định / Máy chủ phát hành)
         - DESKTOP-JBB41QU\SQLSERVER2 (CN1)
         - DESKTOP-JBB41QU\SQLSERVER3 (CN2)
         - DESKTOP-JBB41QU\SQLSERVER4 (TraCuu)
    2. SQL Server Agent đang chạy trên phiên bản Máy chủ phát hành.
    3. Tính năng Sao chép SQL Server đã được cài đặt (kiểm tra trong SQL Server
       Installation Center).
    4. appsettings.json có DataMode = "Sql" (không phải "InMemory").

  Cách sử dụng (SQLCMD từ PowerShell / cmd):
    sqlcmd -S DESKTOP-JBB41QU -E -i "sql\distributed_banking\99_run_all.sql"

  QUAN TRỌNG: Tập lệnh 07 và 08 phải được chạy trên TỪNG PHIÊN BẢN MÁY CHỦ ĐĂNG KÝ NHẬN
  riêng biệt. Không thể chạy qua một lệnh SQLCMD duy nhất tới Máy chủ phát hành.
  Xem ghi chú từng phần bên dưới.

  Idempotent: Mỗi tập lệnh đều idempotent — an toàn khi chạy lại.
=============================================================================*/

:on error exit   -- hủy nếu bất kỳ tập lệnh nào gặp lỗi (chế độ SQLCMD)

PRINT '══════════════════════════════════════════════════════';
PRINT ' BankDds Distributed Banking SQL — Master Runner';
PRINT ' Publisher DB: NGANHANG_PUB';
PRINT '══════════════════════════════════════════════════════';
PRINT '';

/* =========================================================================
   GIAI ĐOẠN 1 — Thiết lập Máy chủ phát hành (chạy trên DESKTOP-JBB41QU)
   ========================================================================= */

PRINT '--- Phase 1: Publisher setup ---';
PRINT '';

PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 1/8: Create Publisher database (NGANHANG_PUB)';
PRINT '          + FULL recovery + enable merge publish';
PRINT '──────────────────────────────────────────────────────';
:r .\01_publisher_create_db.sql

PRINT '';
PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 2/8: Create full schema on NGANHANG_PUB';
PRINT '          Tables + rowguid + MACN indexes + views';
PRINT '──────────────────────────────────────────────────────';
:r .\02_publisher_schema.sql

PRINT '';
PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 3/8: Create stored procedures + views on NGANHANG_PUB';
PRINT '          1 view (view_DanhSachPhanManh) + 50 SPs';
PRINT '          SP_CrossBranchTransfer simplified (no MSDTC)';
PRINT '          GD_GOIRUT/GD_CHUYENTIEN INSERTs populate MACN';
PRINT '──────────────────────────────────────────────────────';
:r .\03_publisher_sp_views.sql

PRINT '';
PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 4/8: SQL-enforced security (Banking pattern)';
PRINT '          3 roles: NGANHANG, CHINHANH, KHACHHANG';
PRINT '          DENY direct table access; GRANT EXECUTE per role';
PRINT '          sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan,';
PRINT '          sp_DoiMatKhau, sp_DanhSachNhanVien';
PRINT '          Seed logins: ADMIN_NH, NV_BT, KH_DEMO';
PRINT '──────────────────────────────────────────────────────';
:r .\04_publisher_security.sql

PRINT '';
PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 5/8: Merge Replication (Banking)';
PRINT '          Part A: Install Distributor';
PRINT '          Part B: Enable merge publish';
PRINT '          Part C: 3 publications + articles + row/join filters';
PRINT '          Part D: 3 push subscriptions (CN1, CN2, TraCuu)';
PRINT '          Part E: Start Snapshot Agents';
PRINT '          SQL Server Agent MUST be running!';
PRINT '──────────────────────────────────────────────────────';
:r .\05_replication_setup_merge.sql

PRINT '';
PRINT '──────────────────────────────────────────────────────';
PRINT 'Step 6/8: Linked Servers (Banking naming: LINK0/LINK1/LINK2)';
PRINT '          Publisher: LINK1→CN1, LINK2→CN2, LINK0→TraCuu';
PRINT '          Auto-detects instance — run on each separately';
PRINT '──────────────────────────────────────────────────────';
-- Chỉ chạy trên Máy chủ phát hành qua 99_run_all.sql (Phần A tự động phát hiện):
:r .\06_linked_servers.sql

-- QUAN TRỌNG: Cũng cần chạy tập lệnh này trên CN1 và CN2 riêng biệt:
--   sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\distributed_banking\06_linked_servers.sql"
--   sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\distributed_banking\06_linked_servers.sql"

PRINT '';
PRINT '--- Phase 1 complete ---';
PRINT '';

/* =========================================================================
   GIAI ĐOẠN 2 — Thiết lập Máy chủ đăng ký nhận
   =========================================================================
   QUAN TRỌNG: Các tập lệnh Giai đoạn 2 KHÔNG THỂ chạy từ Máy chủ phát hành qua :r include
   vì chúng phải kết nối tới các phiên bản SQL Server KHÁC NHAU.

   Chạy từng tập lệnh máy chủ đăng ký nhận thủ công như bên dưới:

   ── CN1 (Bến Thành) ──────────────────────────────────────────────────────
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER2 -E -i "sql\distributed_banking\07_subscribers_create_db.sql"
     (Sau khi Tác vụ Snapshot hoàn thành cho PUB_NGANHANG_BT:)
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER2 -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"
     -- 08 tự phát hiện NGANHANG_BT; tạo vai trò, DENY/GRANT, SP bảo mật, đăng nhập mẫu

   ── CN2 (Tân Định) ────────────────────────────────────────────────────────
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER3 -E -i "sql\distributed_banking\07_subscribers_create_db.sql"
     (Sau khi Tác vụ Snapshot hoàn thành cho PUB_NGANHANG_TD:)
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER3 -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"
     -- 08 tự phát hiện NGANHANG_TD; sao chép bảo mật + xóa view liên chi nhánh

   ── TraCuu ─────────────────────────────────────────────────────────────────
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER4 -E -i "sql\distributed_banking\07_subscribers_create_db.sql"
     (Sau khi Tác vụ Snapshot hoàn thành cho PUB_TRACUU:)
     sqlcmd -S DESKTOP-JBB41QU\SQLSERVER4 -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"
     -- 08 tự phát hiện NGANHANG_TRACUU; thêm bảo vệ chỉ đọc + view V_KHACHHANG_ALL
   ========================================================================= */

PRINT 'Phase 2 (Subscriber setup): Run 07 and 08 manually on each subscriber.';
PRINT 'See comments in 99_run_all.sql for exact sqlcmd commands.';
PRINT '';

/* =========================================================================
   GIAI ĐOẠN 3 — Xác minh (chạy trên Máy chủ phát hành sau khi Snapshot hoàn thành)
   ========================================================================= */

PRINT '--- Phase 3: Post-replication verification ---';
PRINT '';

-- CẦN LÀM-V01: Bỏ ghi chú sau khi tất cả snapshot đã được áp dụng (~1–5 phút):
/*
-- Kiểm tra trạng thái tác vụ sao chép
USE distribution;
SELECT
    a.name       AS AgentName,
    h.start_time AS LastRun,
    h.runstatus  AS Status,  -- 1=Bắt đầu, 2=Thành công, 3=Đang chạy, 4=Rỗi, 6=Thất bại
    h.comments   AS Message
FROM dbo.MSmerge_agents a
JOIN dbo.MSmerge_history h ON a.id = h.agent_id
ORDER BY h.start_time DESC;
*/

-- CẦN LÀM-V02: Kiểm tra số lượng bản ghi theo chi nhánh trên NGANHANG_PUB
/*
USE NGANHANG_PUB;
SELECT 'Publisher KHACHHANG' AS Source, MACN, COUNT(*) AS Cnt
FROM dbo.KHACHHANG GROUP BY MACN;

SELECT 'Publisher TAIKHOAN' AS Source, MACN, COUNT(*) AS Cnt
FROM dbo.TAIKHOAN GROUP BY MACN;
*/

PRINT '--- Master runner complete ---';
PRINT 'Next: Run Phase 2 subscriber scripts, then run app with DataMode=Sql.';
GO

/*=============================================================================
  Bắt đầu nhanh — lệnh PowerShell một dòng (chạy từ thư mục gốc workspace):
  ----------------------------------------------------------------------------

  sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\distributed_banking\99_run_all.sql"

  Cho máy chủ đăng ký nhận (lặp lại cho SQLSERVER3, SQLSERVER4):
  sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\distributed_banking\07_subscribers_create_db.sql"
  sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\distributed_banking\08_subscribers_post_replication_fixups.sql"

  CẦN LÀM-RUN01: Trước khi demo, xác minh DataMode = "Sql" trong:
              BankDds.Wpf/appsettings.json   (hoặc appsettings.Development.json)

  CẦN LÀM-RUN02: Xác minh SQL Server Agent đang chạy trên Máy chủ phát hành:
              Get-Service -ComputerName localhost -Name 'SQLSERVERAGENT' | Select Status
=============================================================================*/
