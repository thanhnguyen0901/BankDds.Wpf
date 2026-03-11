# Hướng Dẫn Setup MS SQL Server (UI-First)

Tài liệu chính thức hiện tại cho dự án NGANHANG theo hướng QLVT:
- Triển khai hạ tầng phân tán bằng **SSMS UI**.
- Không dùng script để auto-setup Replication/Linked Server/Security nền.

---

## 1) Tài liệu sử dụng

1. Runbook thao tác UI:
- `docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md`

2. Checklist nghiệm thu môi trường:
- `docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md`

3. Script runtime (nghiệp vụ):
- `sql/runtime/00_readme_runtime_execution_order.md`

---

## 2) Legacy reference

Tài liệu script-first cũ được lưu tại:
- `docs/sql/archive/SETUP_MS_SQL_DISTRIBUTED_GUIDE_LEGACY.md`

Các script hạ tầng cũ đã archive:
- `sql/archive/05_replication_setup_merge.sql`
- `sql/archive/06_linked_servers.sql`
- `sql/archive/08_subscribers_post_replication_fixups.sql`

Lưu ý: chỉ dùng các script archive để tham chiếu lịch sử hoặc debug migration, không dùng làm flow chính.

---

## 3) Quy trình chuẩn hiện tại

1. Dựng DB/schema/SP runtime:
- Chạy theo `sql/runtime/00_readme_runtime_execution_order.md`.

2. Dựng Replication/Subscription/Linked Server:
- Làm trên SSMS UI theo runbook.

3. Thiết lập role/login/user nền:
- Làm trên SSMS UI theo runbook.

4. Nghiệm thu:
- Điền checklist và lưu bằng chứng screenshot/query.
