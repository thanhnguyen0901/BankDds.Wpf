# Runtime SQL Package (UI-First)

Mục đích: bộ script runtime-only cho dự án NGANHANG theo hướng QLVT UI-first.

Không chứa:
- Lệnh tạo Distributor/Publication/Subscription.
- Lệnh tạo Linked Server.
- Lệnh setup login/user/role nền hàng loạt.

Các phần trên phải thao tác bằng SSMS UI theo:
- `docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md`

---

## Thứ tự chạy khuyến nghị (Publisher)

1. `sql/01_publisher_create_db.sql`
2. `sql/02_publisher_schema.sql`
3. `sql/runtime/01_runtime_business_report_branch_sp.sql`
4. `sql/runtime/02_runtime_auth_account_sp.sql`
5. `sql/runtime/03_transitional_user_crud_sp.sql` (chỉ dùng tạm trước khi hoàn tất Phase D)
6. `sql/04b_publisher_seed_data.sql` (nếu cần data demo)
7. `sql/runtime/90_cleanup_unused_nonruntime_sp.sql`

---

## Ghi chú về transitional SP

`03_transitional_user_crud_sp.sql` hiện vẫn cần vì app chưa refactor xong module Admin.

Sau khi hoàn tất Phase D (chuyển sang `sp_TaoTaiKhoan`/`sp_XoaTaiKhoan`/`sp_DoiMatKhau`), file transitional sẽ bị loại bỏ.

---

## Danh sách SP đã loại khỏi runtime package

- `SP_CreateTransferTransaction` (không được app gọi trực tiếp; đã thay bằng `SP_CrossBranchTransfer`)
- `sp_SafeAddMergeProcArticle` (hỗ trợ setup replication script-first)
- `sp_SafeAddMergeViewArticle` (hỗ trợ setup replication script-first)
- `sp_SafeGrantExec` (hỗ trợ fixup script-first trên subscriber)
