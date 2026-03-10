# Thứ Tự Thực Thi SQL Ngân Hàng Phân Tán

## Tổng quan

Thư mục này chứa các script SQL đã được tái cấu trúc cho dự án BankDds Ngân Hàng Phân Tán (DE3 — Ngân Hàng).

Các script được tổ chức theo **nơi chạy**, không theo loại thực thể.  
Chạy chúng theo thứ tự đánh số bên dưới.  
Mỗi script đều **idempotent** — an toàn khi chạy lại mà không xóa dữ liệu ngoài ý muốn.

---

## Tham Khảo Nhanh Kiến Trúc Mạng

> Lưu ý cho team: các tên dưới đây là ví dụ theo máy hiện tại.
> Khi chạy trên máy khác, thay `DESKTOP-JBB41QU` bằng máy cục bộ của bạn
> (hoặc giá trị `@@SERVERNAME` tương ứng).

| Vai trò | Máy chủ | Cơ sở dữ liệu |
|---|---|---|
| **Máy chủ phát hành / Điều phối (server gốc)** | `DESKTOP-JBB41QU` (mặc định) | `NGANHANG_PUB` |
| **Máy chủ đăng ký nhận CN1 — Bến Thành** | `DESKTOP-JBB41QU\SQLSERVER2` | `NGANHANG_BT` |
| **Máy chủ đăng ký nhận CN2 — Tân Định** | `DESKTOP-JBB41QU\SQLSERVER3` | `NGANHANG_TD` |
| **Máy chủ đăng ký nhận TraCuu (chỉ đọc)** | `DESKTOP-JBB41QU\SQLSERVER4` | `NGANHANG_TRACUU` |

---

## Thứ Tự Thực Thi

### Luồng khuyến nghị đã kiểm chứng

Thứ tự an toàn để người mới chạy một lần là:

`1 -> 2 -> 3 -> 4 -> 4b -> 7 -> 5 -> 8 -> 6 (optional)`

Lý do:
- Bước `7` cần chạy trước `5` để Subscriber DB đã tồn tại khi tạo push subscription/snapshot.
- Bước `8` chỉ chạy sau khi snapshot/merge đã đẩy schema xuống subscriber.
- Bước `6` (linked servers) độc lập với replication, có thể chạy sau cùng.

### Tiên quyết bắt buộc cho bước 5

Trên máy Publisher, cần có share snapshot `ReplData` (chạy PowerShell bằng quyền Administrator):

```powershell
New-Item -ItemType Directory -Path C:\ReplData -Force
New-SmbShare -Name ReplData -Path C:\ReplData -FullAccess "Everyone"
icacls C:\ReplData /grant "Everyone:(OI)(CI)F" /T
```

### Cấu hình team (một lần trước khi chạy)

Mỗi thành viên cần xác nhận 4 giá trị sau trên máy của mình:

- `Publisher host`: tên máy chạy instance mặc định (ví dụ: `MYPC`).
- `CN1 instance`: mặc định `SQLSERVER2`.
- `CN2 instance`: mặc định `SQLSERVER3`.
- `TraCuu instance`: mặc định `SQLSERVER4`.

Nếu team dùng tên instance khác chuẩn `SQLSERVER2/3/4`, cập nhật một chỗ trong:
- `sql/05_replication_setup_merge.sql` (Part D, biến `@SubscriberInst_*`).
- `sql/06_linked_servers.sql` (Part A/B/C, biến `@Cn1*`, `@Cn2*`, `@Tc*`).

Nếu mật khẩu `sa` khác `Password!123`, cập nhật một chỗ trong:
- `sql/05_replication_setup_merge.sql` (Part D, biến `@SubPassword*`).
- `sql/06_linked_servers.sql` (tham số `@rmtpassword` trong `sp_addlinkedsrvlogin`).

### Bước 1 — Chạy trên Máy chủ phát hành (`DESKTOP-JBB41QU`)

| Thứ tự | Script | Mô tả |
|---|---|---|
| 1 | `01_publisher_create_db.sql` | Tạo CSDL `NGANHANG_PUB` nếu chưa tồn tại |
| 2 | `02_publisher_schema.sql` | Tạo tất cả bảng + sequence + view UNION ALL liên chi nhánh |
| 3 | `03_publisher_sp_views.sql` | Tạo tất cả stored procedure trên Máy chủ phát hành |
| 4 | `04_publisher_security.sql` | Tạo SQL login, vai trò, GRANT/DENY theo nhóm vai trò |
| 4b | `04b_publisher_seed_data.sql` | Dữ liệu mẫu demo: chi nhánh, nhân viên, khách hàng, tài khoản, giao dịch, NGUOIDUNG. Idempotent. |
| 5 | `05_replication_setup_merge.sql` | Sao chép hợp nhất: Distributor + 3 publication (PUB_NGANHANG_BT/TD/TRACUU) + article (bảng + 50 SP + view) + bộ lọc hàng/join + 3 push subscription + Tác vụ snapshot. Xem thêm `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md` để biết hướng dẫn dùng SSMS wizard. |
| 6 | `06_linked_servers.sql` | Linked Server (LINK0/LINK1/LINK2) — xem Bước 1b bên dưới |

### Bước 1b — Linked Server (`06_linked_servers.sql`)

Script này được **chia theo máy chủ** — tự động phát hiện `@@SERVERNAME` và chỉ tạo các liên kết phù hợp với máy chủ đó.

**Chạy trên từng máy chủ riêng biệt:**

```powershell
# Publisher — creates LINK1→CN1, LINK2→CN2, LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\06_linked_servers.sql"

# CN1 — creates LINK1→CN2 (other branch), LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\06_linked_servers.sql"

# CN2 — creates LINK1→CN1 (other branch), LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\06_linked_servers.sql"
```

**Quy ước đặt tên — tên giống nhau trên CN1 và CN2:**

| Máy chủ | LINK1 trỏ đến | LINK0 trỏ đến | LINK2 trỏ đến |
|---|---|---|---|
| Máy chủ phát hành | SQLSERVER2 (CN1) | SQLSERVER4 (TraCuu) | SQLSERVER3 (CN2) |
| CN1 (SQLSERVER2) | SQLSERVER3 (CN2) | SQLSERVER4 (TraCuu) | — |
| CN2 (SQLSERVER3) | SQLSERVER2 (CN1) | SQLSERVER4 (TraCuu) | — |

**Bảo mật:** Sử dụng `sa`/`Password!123` (mặc định trong môi trường lab). Xem ghi chú trong script để nâng cấp cho production với login `svc_linkedserver`.

**Tùy chọn bật cho mỗi liên kết:** `rpc`, `rpc out`, `data access` (tất cả `true`).

### Bước 2 — Shell CSDL Máy chủ đăng ký nhận (chạy trên từng máy chủ đăng ký nhận)

**Mục đích:** Tạo các container CSDL trống trên mỗi máy chủ đăng ký nhận.  
Tác vụ snapshot (khởi chạy ở Bước 1, script 05) sẽ đẩy schema + dữ liệu ban đầu vào các shell này.

> **KHÔNG tạo bảng, SP hoặc view trên máy chủ đăng ký nhận thủ công.**  
> Tác vụ snapshot xử lý toàn bộ việc phân phối schema.

| Thứ tự | Script | Mô tả |
|---|---|---|
| 7 | `07_subscribers_create_db.sql` | Tạo shell CSDL máy chủ đăng ký nhận + thiết lập recovery model + snapshot isolation |

**Chạy script 07 trên từng máy chủ đăng ký nhận riêng biệt:**

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

Script tự động phát hiện `@@SERVERNAME` và chỉ tạo CSDL phù hợp:

| Máy chủ | CSDL được tạo | Chi nhánh |
|---|---|---|
| `SQLSERVER2` | `NGANHANG_BT` | Bến Thành |
| `SQLSERVER3` | `NGANHANG_TD` | Tân Định |
| `SQLSERVER4` | `NGANHANG_TRACUU` | Tra cứu (chỉ đọc) |

**Sau script 07:** Chờ Tác vụ snapshot hoàn thành trên Máy chủ phát hành (~1–5 phút với dữ liệu dev). Xác minh bằng:

```sql
-- On each subscriber, confirm tables arrived:
USE NGANHANG_BT;  -- or NGANHANG_TD / NGANHANG_TRACUU
SELECT name FROM sys.tables ORDER BY name;
-- Expected (CN1/CN2): CHINHANH, GD_CHUYENTIEN, GD_GOIRUT, KHACHHANG, NHANVIEN, TAIKHOAN + MSmerge_* metadata
-- Expected (TraCuu):  CHINHANH, KHACHHANG + MSmerge_* metadata
```

### Bước 2b — Hiệu chỉnh bảo mật sau sao chép (chạy SAU KHI Snapshot hoàn thành)

**Mục đích:** Áp dụng lớp bảo mật mà Sao chép hợp nhất KHÔNG mang theo:
- Vai trò CSDL (NGANHANG, CHINHANH, KHACHHANG)
- DENY truy cập trực tiếp bảng + GRANT EXECUTE trên các SP được sao chép
- SP bảo mật (sp_DangNhap, sp_TaoTaiKhoan, v.v.) — theo từng máy chủ
- Đồng bộ seed login (ADMIN_NH, NV_BT, KH_DEMO)
- Xóa view liên chi nhánh `_ALL` (không có ý nghĩa trên máy chủ đăng ký nhận)
- Tăng cường chỉ đọc TraCuu + view `V_KHACHHANG_ALL`

| Thứ tự | Script | Mô tả |
|---|---|---|
| 8 | `08_subscribers_post_replication_fixups.sql` | Sau snapshot: vai trò, DENY/GRANT, SP bảo mật, seed login, tăng cường TraCuu |

**Chạy script 08 trên từng máy chủ đăng ký nhận riêng biệt:**

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -d "NGANHANG_BT" -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -d "NGANHANG_TD" -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -d "NGANHANG_TRACUU" -i "sql\08_subscribers_post_replication_fixups.sql"
```

Script tự động phát hiện CSDL máy chủ đăng ký nhận trên máy chủ hiện tại và điều chỉnh tự động.

**Xác minh sau script 08:**

```sql
-- Check role membership:
EXEC sp_DangNhap;

-- Test CHINHANH role permissions (as NV_BT):
EXECUTE AS USER = 'NV_BT';
EXEC SP_GetCustomersByBranch @MACN = 'BENTHANH';    -- should succeed
-- SELECT * FROM dbo.KHACHHANG;                      -- should fail (DENY)
REVERT;
```

### Bước 3 — Script tổng hợp (tự động hóa / CI)

| Thứ tự | Script | Mô tả |
|---|---|---|
| – | `99_run_all.sql` | Điều phối các bước 1–8 thông qua `EXEC` hoặc `SQLCMD :r` include |

---

## Tham Chiếu Script Cũ

Các script gốc trong `sql/` được giữ nguyên không thay đổi.  
Thư mục này chứa các bản tương đương đã được tái cấu trúc.  
Bảng ánh xạ:

| Bản gốc | Bản mới tương đương |
|---|---|
| `sql/01-schema.sql` | `02_publisher_schema.sql` |
| `sql/02-seed.sql` | Nằm trong `02_publisher_schema.sql` (phần seed) hoặc chạy thủ công |
| `sql/10-sp-customers.sql` | `03_publisher_sp_views.sql` (phần: Customers) |
| `sql/11-sp-employees.sql` | `03_publisher_sp_views.sql` (phần: Employees) |
| `sql/12-sp-accounts.sql` | `03_publisher_sp_views.sql` (phần: Accounts) |
| `sql/13-sp-transactions.sql` | `03_publisher_sp_views.sql` (phần: Transactions) |
| `sql/14-sp-reports.sql` | `03_publisher_sp_views.sql` (phần: Reports) |
| `sql/15-sp-auth.sql` | `03_publisher_sp_views.sql` (phần: Auth) |
| `sql/16-linked-servers.sql` | `06_linked_servers.sql` |
| `sql/17-security.sql` (dự kiến) | `04_publisher_security.sql` |
| `sql/17-replication-distributor.sql` (dự kiến) | `05_replication_setup_merge.sql` (Phần A) |
| `sql/18-replication-publications.sql` (dự kiến) | `05_replication_setup_merge.sql` (Phần B) |
| `sql/19-replication-subscriptions.sql` (dự kiến) | `05_replication_setup_merge.sql` (Phần C) |
| `sql/20-replication-snapshot.sql` (dự kiến) | `05_replication_setup_merge.sql` (Phần D) |

---

## Ghi Chú

- Tất cả script sử dụng kiểm tra `IF OBJECT_ID(…) IS NOT NULL` / `IF DB_ID(…) IS NULL` để đảm bảo idempotent.
- SP **chỉ được tạo trên Máy chủ phát hành**. Chúng được truyền đến CN1 và CN2 thông qua Sao chép hợp nhất (article `proc schema only`).
- **Không chạy `03_publisher_sp_views.sql` trực tiếp trên CN1, CN2 hoặc TraCuu.** Các máy chủ đó nhận SP thông qua Tác vụ snapshot.
- TraCuu (`NGANHANG_TRACUU`) chỉ nhận bảng `KHACHHANG` + `CHINHANH` qua `PUB_TRACUU`. Không có SP nào được sao chép đến TraCuu.
