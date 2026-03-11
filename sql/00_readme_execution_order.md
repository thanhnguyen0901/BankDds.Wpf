# Hướng Dẫn Chạy SQL Cho Mô Hình CSDL Phân Tán

Tài liệu này hướng dẫn chạy đúng thứ tự các script SQL để khởi tạo Publisher, Subscriber, replication và linked server.

## 1) Thông tin cần kiểm tra theo máy

- `PUBLISHER_HOST`: tên máy chạy instance SQL Server Publisher.
- `CN1_INSTANCE`: mặc định `SQLSERVER2`.
- `CN2_INSTANCE`: mặc định `SQLSERVER3`.
- `TRACUU_INSTANCE`: mặc định `SQLSERVER4`.
- Mật khẩu SQL login nếu khác `Password!123`.

Nếu dùng tên instance khác mặc định, cập nhật trong:
- `sql/05_replication_setup_merge.sql` (Part D, các biến `@SubscriberInst_*`).
- `sql/06_linked_servers.sql` (Part A/B/C, các biến `@Cn1*`, `@Cn2*`, `@Tc*`).

## 2) Thứ tự chạy script

Chạy theo đúng thứ tự sau:

`1 -> 2 -> 3 -> 4 -> 4b -> 7 -> 5 -> 8 -> 6`

Lưu ý:
- Chạy bước `7` trước bước `5` để các database Subscriber đã tồn tại.
- Chạy bước `8` sau bước `5` để xử lý phần hậu replication trên Subscriber.
- Bước `6` (linked server) chạy sau cùng để hoàn tất kết nối giữa các node.

## 3) Điều kiện trước khi chạy bước 5

Trên Publisher, tạo snapshot share `ReplData` (mở PowerShell bằng quyền Administrator):

```powershell
New-Item -ItemType Directory -Path C:\ReplData -Force
New-SmbShare -Name ReplData -Path C:\ReplData -FullAccess "Everyone"
icacls C:\ReplData /grant "Everyone:(OI)(CI)F" /T
```

## 4) Lệnh chạy theo từng bước

### Bước 1-4b trên Publisher

```powershell
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\01_publisher_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\02_publisher_schema.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\03_publisher_sp_views.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\04_publisher_security.sql"
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\04b_publisher_seed_data.sql"
```

### Bước 7 trên từng Subscriber

```powershell
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

### Bước 5 trên Publisher

```powershell
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\05_replication_setup_merge.sql"
```

### Bước 8 trên từng Subscriber (bắt buộc có `-d`)

```powershell
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER2" -E -d "NGANHANG_BT" -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER3" -E -d "NGANHANG_TD" -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER4" -E -d "NGANHANG_TRACUU" -i "sql\08_subscribers_post_replication_fixups.sql"
```

### Bước 6 cấu hình linked server

```powershell
sqlcmd -S "<PUBLISHER_HOST>" -E -i "sql\06_linked_servers.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER2" -E -i "sql\06_linked_servers.sql"
sqlcmd -S "<PUBLISHER_HOST>\SQLSERVER3" -E -i "sql\06_linked_servers.sql"
```

## 5) Kiểm tra sau khi chạy

- Trên Publisher:
  - Có đủ 3 publication merge.
  - Snapshot Agent chạy thành công.
- Trên Subscriber:
  - Có các bảng được đồng bộ từ publication.
  - Chạy bước 08 không còn lỗi SQL.

## 6) Ghi chú quan trọng

- Bước 08 không tự xác định database; luôn truyền đúng tham số `-d`.
- TraCuu chỉ đọc ở mức quyền/role (không đặt database ở chế độ `READ_ONLY`).
- Các script được thiết kế idempotent, có thể chạy lại khi cần.
