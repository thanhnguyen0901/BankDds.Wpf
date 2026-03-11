# Hướng Dẫn Thực Thi SQL - Chế Độ UI-First (theo QLVT)

Tài liệu này đã chuyển sang hướng **UI-first**:
- Replication/Subscription/Linked Server/Role-Login-User nền thực hiện bằng **SSMS UI**.
- Không dùng script auto-setup hạ tầng làm đường chính.

Tài liệu chính cần dùng:
- `docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md`
- `docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md`

---

## 1) Script còn dùng trong flow chính

Chỉ dùng script cho phần schema/SP runtime dữ liệu nghiệp vụ:

```powershell
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\01_publisher_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\02_publisher_schema.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\runtime\01_runtime_business_report_branch_sp.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\runtime\02_runtime_auth_account_sp.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\runtime\03_transitional_user_crud_sp.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\04b_publisher_seed_data.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\runtime\90_cleanup_unused_nonruntime_sp.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

Lưu ý:
- `03_transitional_user_crud_sp.sql` chỉ là tạm thời trước khi hoàn tất Phase D.
- Role/login/user nền thao tác bằng SSMS UI theo kế hoạch migration.

---

## 2) Script legacy (không dùng mặc định)

Các script dưới đây giữ để tham chiếu/migration, không dùng làm đường chính:
- `sql/archive/05_replication_setup_merge.sql`
- `sql/archive/06_linked_servers.sql`
- `sql/archive/08_subscribers_post_replication_fixups.sql`

Mọi thao tác tương ứng phải thực hiện bằng SSMS UI theo runbook.

---

## 3) Điều kiện bắt buộc

- SQL Server Agent đang chạy.
- SQL Server Browser đang chạy.
- Mixed Mode Authentication bật trên các instance.
- Có quyền thao tác Replication + Linked Server + Security trên môi trường.
