# HƯỚNG DẪN SETUP MS SQL SERVER — ĐỒ ÁN CSDL PHÂN TÁN (DE3 — NGÂN HÀNG)

> Cập nhật migration QLVT UI-first (2026-03-11):
> - Tài liệu thao tác chính hiện tại: `docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md`
> - Checklist thực thi môi trường: `docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md`
> - Nội dung trong file này giữ để tham chiếu legacy/script-first trong giai đoạn chuyển đổi.

Tài liệu hướng dẫn setup SQL Server để chạy đồ án **CSDL phân tán — Đề 3 (Ngân hàng)** theo đúng:

- Kiến trúc phân tán: **2 chi nhánh + 1 server tra cứu**
- Cơ chế đồng bộ: **Sao chép hợp nhất (Merge Replication)**
- Hạ tầng: **4 SQL Server instance** trên cùng 1 máy, default instance có SQL Agent làm Máy chủ phát hành (Publisher)

---

## I. MỤC TIÊU & KIẾN TRÚC TRIỂN KHAI

### 1. Yêu cầu phân tán theo đề tài

Ngân hàng có **2 chi nhánh**: **BENTHANH** và **TANDINH**.

CSDL `NGANHANG_PUB` trên Publisher chứa **toàn bộ dữ liệu** của cả 2 chi nhánh. Dữ liệu được phân tán qua Sao chép hợp nhất thành **3 phân mảnh**:

| Phân mảnh | Nội dung | Bộ lọc hàng |
|---|---|---|
| **CN1 (Bến Thành)** | Khách hàng, nhân viên, tài khoản, giao dịch thuộc BENTHANH | `MACN = N'BENTHANH'` |
| **CN2 (Tân Định)** | Khách hàng, nhân viên, tài khoản, giao dịch thuộc TANDINH | `MACN = N'TANDINH'` |
| **TraCuu (Tra cứu)** | Bảng CHINHANH (tất cả) + KHACHHANG (tất cả, chỉ đọc) | Không lọc |

> **Lưu ý**: Bảng `CHINHANH` được sao chép đến **tất cả** subscriber **không có bộ lọc hàng** — mọi site đều thấy đầy đủ danh sách chi nhánh.

### 2. Kiến trúc triển khai (đóng cứng theo máy)

```
┌───────────────────────────────────────────────────────────────────────┐
│                     DESKTOP-JBB41QU (1 máy vật lý)                    │
│                                                                       │
│  ┌─────────────────────────────────┐   ┌────────────────────────────┐│
│  │ Default Instance (Publisher)    │   │ SQLSERVER2 (CN1)           ││
│  │ DB: NGANHANG_PUB               │   │ DB: NGANHANG_BT            ││
│  │ • Toàn bộ dữ liệu gốc         │──▶│ • MACN = 'BENTHANH'        ││
│  │ • SQL Server Agent             │   │ • SP nghiệp vụ (replicated)││
│  │ • Distributor                  │   └────────────────────────────┘│
│  │ • 3 Publication                │                                  │
│  │   PUB_NGANHANG_BT             │   ┌────────────────────────────┐│
│  │   PUB_NGANHANG_TD             │──▶│ SQLSERVER3 (CN2)           ││
│  │   PUB_TRACUU                  │   │ DB: NGANHANG_TD            ││
│  │                               │   │ • MACN = 'TANDINH'         ││
│  │                               │   │ • SP nghiệp vụ (replicated)││
│  │                               │   └────────────────────────────┘│
│  │                               │                                  │
│  │                               │   ┌────────────────────────────┐│
│  │                               │──▶│ SQLSERVER4 (TraCuu)        ││
│  │                               │   │ DB: NGANHANG_TRACUU        ││
│  │                               │   │ • CHINHANH + KHACHHANG     ││
│  │                               │   │ • Chỉ đọc (download-only) ││
│  └─────────────────────────────────┘   └────────────────────────────┘│
└───────────────────────────────────────────────────────────────────────┘
```

### 3. Bảng tham chiếu nhanh

| Vai trò | Instance | Cơ sở dữ liệu | Linked Server (từ CN) |
|---|---|---|---|
| **Publisher / Điều phối** | `DESKTOP-JBB41QU` (default) | `NGANHANG_PUB` | — |
| **CN1 — Bến Thành** | `DESKTOP-JBB41QU\SQLSERVER2` | `NGANHANG_BT` | LINK1 (→ CN kia) |
| **CN2 — Tân Định** | `DESKTOP-JBB41QU\SQLSERVER3` | `NGANHANG_TD` | LINK1 (→ CN kia) |
| **TraCuu (chỉ đọc)** | `DESKTOP-JBB41QU\SQLSERVER4` | `NGANHANG_TRACUU` | — |

---

## II. THÔNG TIN KẾT NỐI

- **Máy tính:** `DESKTOP-JBB41QU`
- **IP:** `192.168.100.46`
- **SQL Auth mặc định (lab):** `sa / Password!123`

### Kết nối SSMS đến 4 instance

| Instance | Chuỗi kết nối SSMS |
|---|---|
| Publisher (default) | `DESKTOP-JBB41QU` |
| CN1 (Bến Thành) | `DESKTOP-JBB41QU\SQLSERVER2` |
| CN2 (Tân Định) | `DESKTOP-JBB41QU\SQLSERVER3` |
| TraCuu | `DESKTOP-JBB41QU\SQLSERVER4` |

### Connection Strings (cho ứng dụng WPF)

```
-- Publisher
Server=DESKTOP-JBB41QU;Database=NGANHANG_PUB;TrustServerCertificate=True;

-- CN1 Bến Thành
Server=DESKTOP-JBB41QU\SQLSERVER2;Database=NGANHANG_BT;TrustServerCertificate=True;

-- CN2 Tân Định
Server=DESKTOP-JBB41QU\SQLSERVER3;Database=NGANHANG_TD;TrustServerCertificate=True;

-- TraCuu
Server=DESKTOP-JBB41QU\SQLSERVER4;Database=NGANHANG_TRACUU;TrustServerCertificate=True;
```

> Ứng dụng WPF sử dụng SQL Authentication — `User Id` và `Password` được điền từ form đăng nhập, không đóng cứng trong connection string.

### Đồng bộ cấu hình ứng dụng (`appsettings.json`)

Ứng dụng WPF đọc cấu hình từ `BankDds.Wpf/appsettings.json`. Các key bắt buộc:

| Key trong `appsettings.json` | Giá trị mẫu | Mục đích |
|---|---|---|
| `ConnectionStrings:Publisher` | `Server=DESKTOP-JBB41QU;Database=NGANHANG_PUB;TrustServerCertificate=True;` | Kết nối Publisher — dùng cho đăng nhập (`sp_DangNhap`), báo cáo tổng hợp, danh sách chi nhánh |
| `ConnectionStrings:Branch_BENTHANH` | `Server=DESKTOP-JBB41QU\SQLSERVER2;Database=NGANHANG_BT;TrustServerCertificate=True;` | CN1 Bến Thành — giao dịch gửi/rút/chuyển tiền |
| `ConnectionStrings:Branch_TANDINH` | `Server=DESKTOP-JBB41QU\SQLSERVER3;Database=NGANHANG_TD;TrustServerCertificate=True;` | CN2 Tân Định — giao dịch gửi/rút/chuyển tiền |
| `ConnectionStrings:LookupDatabase` | `Server=DESKTOP-JBB41QU\SQLSERVER4;Database=NGANHANG_TRACUU;TrustServerCertificate=True;` | *(Tùy chọn)* Tra cứu KH toàn hệ thống — chỉ NGANHANG role sử dụng |
| `DatabaseSettings:DefaultBranch` | `BENTHANH` | Chi nhánh mặc định khi không xác định được từ `sp_DangNhap` |

> **Lookup fallback:** Nếu key `ConnectionStrings:LookupDatabase` không được cấu hình, chức năng tra cứu tự động chuyển sang dùng Publisher connection (cũng có đủ dữ liệu, nhưng không demo được phân tán).

**Quy tắc đặt tên key chi nhánh:** `ConnectionStrings:Branch_{MACN}` — trong đó `MACN` là mã chi nhánh (nChar 10, trim khoảng trắng). Lớp `ConnectionStringProvider` tự ghép key theo `$"ConnectionStrings:Branch_{branch}"`.

Ví dụ file `appsettings.json` tối thiểu:

```json
{
  "ConnectionStrings": {
    "Publisher": "Server=DESKTOP-JBB41QU;Database=NGANHANG_PUB;TrustServerCertificate=True;",
    "Branch_BENTHANH": "Server=DESKTOP-JBB41QU\\SQLSERVER2;Database=NGANHANG_BT;TrustServerCertificate=True;",
    "Branch_TANDINH": "Server=DESKTOP-JBB41QU\\SQLSERVER3;Database=NGANHANG_TD;TrustServerCertificate=True;",
    "LookupDatabase": "Server=DESKTOP-JBB41QU\\SQLSERVER4;Database=NGANHANG_TRACUU;TrustServerCertificate=True;"
  },
  "DatabaseSettings": {
    "DefaultBranch": "BENTHANH"
  }
}
```

> **Lưu ý:** `User Id` và `Password` **không** nằm trong file cấu hình. Chúng được inject tại runtime bởi `ConnectionStringProvider.SetSqlLoginCredentials()` sau khi người dùng đăng nhập thành công.

---

## III. CHECKLIST TRƯỚC KHI CHẠY SCRIPT (LÀM 1 LẦN)

### 1. Bật SQL Server Browser

Bắt buộc khi dùng named instance (`SQLSERVER2/3/4`):

1. Mở **SQL Server Configuration Manager**
2. **SQL Server Services** → **SQL Server Browser** → Start
3. Đặt **Startup type: Automatic**

### 2. Bật Mixed Mode Authentication

Trên **từng instance** (4 lần):

1. SSMS → nhấp phải server → **Properties** → **Security**
2. Chọn: **SQL Server and Windows Authentication mode**
3. **Restart** service của instance đó

### 3. Bật SQL Server Agent trên default instance

SQL Server Agent **bắt buộc** cho Sao chép hợp nhất (chạy Snapshot Agent + Merge Agent).

```powershell
# Kiểm tra
Get-Service SQLSERVERAGENT | Select-Object Status

# Khởi động nếu cần
Start-Service SQLSERVERAGENT
```

Hoặc: SSMS → Object Explorer → **SQL Server Agent** → nhấp phải → **Start**

### 4. Cài đặt tính năng Replication

Kiểm tra trong **SQL Server Installation Center** → **New SQL Server stand-alone installation** → xác nhận **Replication** đã được chọn.

### 5. Xác minh kết nối

Chạy trên mỗi instance:

```sql
SELECT @@SERVERNAME AS ServerName, @@VERSION AS [Version];
```

---

## IV. THỨ TỰ THỰC THI SCRIPT SQL

### Tổng quan

Tất cả script nằm trong thư mục `sql/`. Mỗi script đều **idempotent** — an toàn khi chạy lại.

```
Giai đoạn 1 — Publisher (chạy trên default instance)
  01 → 02 → 03 → 04 → 04b → 05 → 06

Giai đoạn 2 — Subscriber (chạy trên từng instance riêng)
  07 → [chờ snapshot] → 08 → 06

Giai đoạn 3 — Xác minh
```

### Bảng chi tiết: Script nào chạy ở đâu

| Bước | Script | Chạy trên instance | Cơ sở dữ liệu | Mô tả |
|---|---|---|---|---|
| 1 | `01_publisher_create_db.sql` | `DESKTOP-JBB41QU` | master → NGANHANG_PUB | Tạo DB Publisher, FULL recovery, bật merge publish |
| 2 | `02_publisher_schema.sql` | `DESKTOP-JBB41QU` | NGANHANG_PUB | Tạo bảng + sequence + rowguid + index MACN |
| 3 | `03_publisher_sp_views.sql` | `DESKTOP-JBB41QU` | NGANHANG_PUB | 1 view + 50 stored procedure |
| 4 | `04_publisher_security.sql` | `DESKTOP-JBB41QU` | NGANHANG_PUB | Vai trò (NGANHANG/CHINHANH/KHACHHANG), DENY/GRANT, sp_DangNhap, seed login |
| 4b | `04b_publisher_seed_data.sql` | `DESKTOP-JBB41QU` | NGANHANG_PUB | Dữ liệu mẫu demo: chi nhánh, nhân viên, khách hàng, tài khoản, giao dịch |
| 5 | `05_replication_setup_merge.sql` | `DESKTOP-JBB41QU` | NGANHANG_PUB | Distributor + 3 Publication + Article + Filter + 3 Push Subscription + Snapshot |
| 6 | `06_linked_servers.sql` | **Từng instance** (\*) | master | Linked Server: LINK0/LINK1/LINK2 |
| 7 | `07_subscribers_create_db.sql` | **Từng subscriber** (\*\*) | master | Tạo DB shell rỗng trên subscriber |
| 8 | `08_subscribers_post_replication_fixups.sql` | **Từng subscriber** (\*\*) | NGANHANG_BT/TD/TRACUU | Vai trò, DENY/GRANT, SP bảo mật, seed login, tăng cường TraCuu |

> (\*) Script 06 tự phát hiện `@@SERVERNAME` — chạy cùng file trên Publisher + CN1 + CN2.  
> (\*\*) Script 07 và 08 tự phát hiện instance — chạy cùng file trên SQLSERVER2, SQLSERVER3, SQLSERVER4.

---

## V. HƯỚNG DẪN CHI TIẾT TỪNG GIAI ĐOẠN

### Giai đoạn 1 — Thiết lập Publisher (chạy trên `DESKTOP-JBB41QU`)

#### Bước 1: Tạo cơ sở dữ liệu Publisher

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\01_publisher_create_db.sql"
```

Xác minh:

```sql
SELECT name, recovery_model_desc, is_merge_published
FROM sys.databases WHERE name = N'NGANHANG_PUB';
-- Kết quả: NGANHANG_PUB | FULL | 1
```

#### Bước 2: Tạo lược đồ (bảng + view)

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\02_publisher_schema.sql"
```

Bảng được tạo:

| Bảng | Loại | Sao chép? | Ghi chú |
|---|---|---|---|
| CHINHANH | Tham chiếu | Có (không filter) | Mọi site đều thấy đầy đủ |
| NGUOIDUNG | Hub-only | Không | Mapping login → branch |
| SEQ_MANV | Hub-only | Không | Sequence mã nhân viên |
| KHACHHANG | Nghiệp vụ | Có (filter MACN) | + rowguid |
| NHANVIEN | Nghiệp vụ | Có (filter MACN) | + rowguid |
| TAIKHOAN | Nghiệp vụ | Có (filter MACN) | + rowguid |
| GD_GOIRUT | Giao dịch | Có (join filter) | + rowguid, IDENTITY |
| GD_CHUYENTIEN | Giao dịch | Có (join filter) | + rowguid, IDENTITY |

#### Bước 3: Tạo stored procedure + view

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\03_publisher_sp_views.sql"
```

> **KHÔNG** chạy script này trên subscriber. SP được sao chép qua Merge Replication dưới dạng article `proc schema only`.

#### Bước 4: Thiết lập bảo mật

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\04_publisher_security.sql"
```

Tạo:
- 3 vai trò DB: `NGANHANG`, `CHINHANH`, `KHACHHANG`
- DENY SELECT trực tiếp bảng + GRANT EXECUTE theo vai trò
- SP bảo mật: `sp_DangNhap`, `sp_TaoTaiKhoan`, `sp_XoaTaiKhoan`, `sp_DoiMatKhau`, `sp_DanhSachNhanVien`
- Seed login: `ADMIN_NH` (NGANHANG), `NV_BT` (CHINHANH), `KH_DEMO` (KHACHHANG)

#### Bước 4b: Dữ liệu mẫu demo

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\04b_publisher_seed_data.sql"
```

Dữ liệu: 2 chi nhánh, 4 nhân viên, 6 khách hàng, 8 tài khoản (bao gồm liên chi nhánh), 10 giao dịch, 3 NGUOIDUNG.

#### Bước 5: Cấu hình Sao chép hợp nhất (Merge Replication)

> **Điều kiện tiên quyết:** SQL Server Agent PHẢI đang chạy. DB subscriber (bước 7) nên được tạo trước.

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\archive\05_replication_setup_merge.sql"
```

Script thực hiện 5 phần tuần tự:

| Phần | Nội dung |
|---|---|
| **A** | Cài đặt Distributor trên default instance |
| **B** | Bật merge publish cho NGANHANG_PUB |
| **C** | Tạo 3 Publication + Article + Bộ lọc hàng/join |
| **D** | Tạo 3 Push Subscription |
| **E** | Khởi chạy Snapshot Agent |

Chi tiết 3 Publication:

| Publication | Subscriber | Bộ lọc hàng | Đối tượng |
|---|---|---|---|
| `PUB_NGANHANG_BT` | SQLSERVER2 / NGANHANG_BT | `MACN = N'BENTHANH'` | 6 bảng + 50 SP + 1 view |
| `PUB_NGANHANG_TD` | SQLSERVER3 / NGANHANG_TD | `MACN = N'TANDINH'` | 6 bảng + 50 SP + 1 view |
| `PUB_TRACUU` | SQLSERVER4 / NGANHANG_TRACUU | Không lọc (chỉ CHINHANH + KHACHHANG) | 2 bảng, download-only |

Theo dõi tiến trình snapshot: SSMS → **Replication** → **Replication Monitor** → tab **Agents**.

#### Bước 6: Linked Server (chạy trên từng instance)

```powershell
# Publisher — tạo LINK1→CN1, LINK2→CN2, LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\archive\06_linked_servers.sql"

# CN1 — tạo LINK1→CN2, LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\archive\06_linked_servers.sql"

# CN2 — tạo LINK1→CN1, LINK0→TraCuu
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\archive\06_linked_servers.sql"
```

**Quy ước đặt tên Linked Server:**

| Instance chạy | LINK0 → | LINK1 → | LINK2 → |
|---|---|---|---|
| Publisher (default) | SQLSERVER4 (TraCuu) | SQLSERVER2 (CN1 — BT) | SQLSERVER3 (CN2 — TD) |
| CN1 (SQLSERVER2) | SQLSERVER4 (TraCuu) | SQLSERVER3 (CN2 — **chi nhánh kia**) | — |
| CN2 (SQLSERVER3) | SQLSERVER4 (TraCuu) | SQLSERVER2 (CN1 — **chi nhánh kia**) | — |

> **Thiết kế đối xứng:** Trên cả CN1 và CN2, `LINK1` luôn trỏ đến "chi nhánh kia". Nhờ vậy SP `SP_CrossBranchTransfer` dùng chung 1 mã nguồn mà không cần biết đang chạy ở CN nào.

Tùy chọn bật cho mỗi liên kết: `rpc = true`, `rpc out = true`, `data access = true`.

Xác minh:

```sql
SELECT name, data_source FROM sys.servers WHERE is_linked = 1 ORDER BY name;
```

---

### Giai đoạn 2 — Thiết lập Subscriber

#### Bước 7: Tạo DB shell trên subscriber

Chạy **cùng 1 script** trên từng instance subscriber. Script tự phát hiện `@@SERVERNAME` và tạo DB phù hợp:

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

| Instance | DB được tạo | Chi nhánh |
|---|---|---|
| SQLSERVER2 | `NGANHANG_BT` | Bến Thành |
| SQLSERVER3 | `NGANHANG_TD` | Tân Định |
| SQLSERVER4 | `NGANHANG_TRACUU` | Tra cứu (chỉ đọc) |

> **KHÔNG tạo bảng, SP hoặc view thủ công trên subscriber.** Snapshot Agent sẽ đẩy schema + dữ liệu từ Publisher.

#### Chờ Snapshot hoàn tất

Sau bước 5 và 7, Snapshot Agent sẽ đồng bộ schema + dữ liệu từ Publisher đến subscriber. Thường mất **1–5 phút** với dữ liệu dev.

Xác minh trên từng subscriber:

```sql
-- Chạy trên SQLSERVER2 / NGANHANG_BT
USE NGANHANG_BT;
SELECT name FROM sys.tables WHERE name NOT LIKE 'MSmerge_%' ORDER BY name;
-- Kết quả mong đợi (CN1/CN2): CHINHANH, GD_CHUYENTIEN, GD_GOIRUT, KHACHHANG, NHANVIEN, TAIKHOAN

-- Chạy trên SQLSERVER4 / NGANHANG_TRACUU
USE NGANHANG_TRACUU;
SELECT name FROM sys.tables WHERE name NOT LIKE 'MSmerge_%' ORDER BY name;
-- Kết quả mong đợi (TraCuu): CHINHANH, KHACHHANG
```

#### Bước 8: Hiệu chỉnh bảo mật sau sao chép

Chạy **SAU KHI** Snapshot hoàn tất:

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\archive\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\archive\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\archive\08_subscribers_post_replication_fixups.sql"
```

Script tự phát hiện DB subscriber và thực hiện:

| Nội dung | CN1/CN2 | TraCuu |
|---|---|---|
| Tạo vai trò DB (NGANHANG, CHINHANH, KHACHHANG) | ✅ | ✅ |
| DENY SELECT trực tiếp bảng | ✅ | ✅ |
| GRANT EXECUTE trên SP (cùng ma trận với Publisher) | ✅ | — |
| SP bảo mật: sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau | ✅ | ✅ |
| Seed login (ADMIN_NH, NV_BT, KH_DEMO) | ✅ | ✅ |
| Xóa view `_ALL` liên chi nhánh (không cần trên subscriber) | ✅ | — |
| Tăng cường chỉ đọc + view `V_KHACHHANG_ALL` | — | ✅ |

> **sp_DangNhap trên subscriber** sử dụng `DB_NAME()` để xác định chi nhánh mặc định (MACN):
> - `NGANHANG_BT` → `BENTHANH`
> - `NGANHANG_TD` → `TANDINH`
> - `NGANHANG_TRACUU` → `NULL`

Xác minh:

```sql
-- Trên mỗi subscriber:
EXEC sp_DangNhap;
-- Kết quả: MANV, HOTEN, TENNHOM, MACN (4 cột)
```

---

### Giai đoạn 3 — Xác minh tổng thể

Chạy tuần tự các script bằng `sqlcmd` theo đúng thứ tự đã chuẩn hóa:

```powershell
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\01_publisher_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\02_publisher_schema.sql"
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\03_publisher_sp_views.sql"
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\04_publisher_security.sql"
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\04b_publisher_seed_data.sql"

sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"

sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\archive\05_replication_setup_merge.sql"

sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -d "NGANHANG_BT" -i "sql\archive\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -d "NGANHANG_TD" -i "sql\archive\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -d "NGANHANG_TRACUU" -i "sql\archive\08_subscribers_post_replication_fixups.sql"

sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\archive\06_linked_servers.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\archive\06_linked_servers.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\archive\06_linked_servers.sql"
```

---

## VI. LINKED SERVER — CHI TIẾT

### Quy ước đặt tên LINK0 / LINK1 / LINK2

Dự án sử dụng **LINK0/LINK1/LINK2** (KHÔNG phải SERVER1/SERVER2/SERVER3).

| Tên | Ý nghĩa |
|---|---|
| **LINK0** | Luôn trỏ đến **TraCuu** (SQLSERVER4) |
| **LINK1** | Trỏ đến **chi nhánh kia** (đối xứng giữa CN1 ↔ CN2) |
| **LINK2** | Chỉ có trên Publisher — trỏ đến CN2 (SQLSERVER3) |

### Datasrc thực tế

| Tên Linked Server | Datasrc (data_source) |
|---|---|
| LINK0 | `DESKTOP-JBB41QU\SQLSERVER4` |
| LINK1 (từ CN1) | `DESKTOP-JBB41QU\SQLSERVER3` |
| LINK1 (từ CN2) | `DESKTOP-JBB41QU\SQLSERVER2` |
| LINK1 (từ Publisher) | `DESKTOP-JBB41QU\SQLSERVER2` |
| LINK2 (từ Publisher) | `DESKTOP-JBB41QU\SQLSERVER3` |

### Tùy chọn bắt buộc

Sau khi tạo linked server, các tùy chọn sau phải được bật:

- **Data Access** = True — cho phép truy vấn tên 4 phần: `[LINK1].[DB].[dbo].[Table]`
- **RPC** = True — cho phép `EXEC` SP từ xa
- **RPC Out** = True — cho phép kết quả trả về từ SP từ xa

### Xác minh

Trên từng instance:

```sql
EXEC sp_linkedservers;
SELECT name, data_source FROM sys.servers WHERE is_linked = 1 ORDER BY name;
```

---

## VII. SAO CHÉP HỢP NHẤT (MERGE REPLICATION) — CHI TIẾT

### Các thành phần

| Thành phần | Vai trò |
|---|---|
| **Distributor** | Cài đặt trên default instance (cùng Publisher), DB `distribution` |
| **Publisher** | `NGANHANG_PUB` — nguồn dữ liệu gốc |
| **3 Publication** | PUB_NGANHANG_BT, PUB_NGANHANG_TD, PUB_TRACUU |
| **3 Push Subscription** | Publisher đẩy dữ liệu đến subscriber |
| **Snapshot Agent** | Tạo bản chụp ban đầu (chạy 1 lần) |
| **Merge Agent** | Đồng bộ 2 chiều liên tục (CN1/CN2), 1 chiều download-only (TraCuu) |

### Đối tượng phát hành (Article)

| Loại | PUB_NGANHANG_BT / PUB_NGANHANG_TD | PUB_TRACUU |
|---|---|---|
| **Bảng dữ liệu** | CHINHANH (không filter) | CHINHANH (không filter) |
| | KHACHHANG (filter MACN) | KHACHHANG (không filter, download-only) |
| | NHANVIEN (filter MACN) | |
| | TAIKHOAN (filter MACN) | |
| | GD_GOIRUT (join filter qua TAIKHOAN) | |
| | GD_CHUYENTIEN (join filter qua TAIKHOAN) | |
| **SP (proc schema only)** | 50 stored procedure | — |
| **View (schema only)** | view_DanhSachPhanManh | — |

### Bộ lọc hàng

| Bảng | Publication BT | Publication TD |
|---|---|---|
| KHACHHANG | `MACN = N'BENTHANH'` | `MACN = N'TANDINH'` |
| NHANVIEN | `MACN = N'BENTHANH'` | `MACN = N'TANDINH'` |
| TAIKHOAN | `MACN = N'BENTHANH'` | `MACN = N'TANDINH'` |
| GD_GOIRUT | Join filter qua TAIKHOAN.SOTK | Join filter qua TAIKHOAN.SOTK |
| GD_CHUYENTIEN | Join filter qua TAIKHOAN.SOTK | Join filter qua TAIKHOAN.SOTK |
| CHINHANH | Không lọc (tất cả) | Không lọc (tất cả) |

### Xử lý xung đột

Chính sách: **Publisher wins** — khi có xung đột dữ liệu, phiên bản trên Publisher luôn thắng.

### Xử lý sự cố

| Lỗi | Giải pháp |
|---|---|
| "SQL Server Agent is not running" | `Start-Service SQLSERVERAGENT` hoặc SSMS → Agent → Start |
| "Subscriber database does not exist" | Chạy `07_subscribers_create_db.sql` trên subscriber trước |
| "Cannot add articles after snapshot" | `EXEC sp_reinitmergesubscription` rồi chạy lại snapshot |
| "Cannot connect to Subscriber" | Kiểm tra Browser service + named instance đang chạy |
| Xung đột hợp nhất | Replication Monitor → nhấp phải agent → View Details |

**Xóa sạch replication để bắt đầu lại:**

```sql
-- Trên Publisher:
EXEC sp_removedbreplication @dbname = N'NGANHANG_PUB';
EXEC sp_dropdistpublisher @@SERVERNAME;
EXEC sp_dropdistributiondb N'distribution';
EXEC sp_dropdistributor;
-- Sau đó chạy lại từ script 05.
```

---

## VIII. BẬT MSDTC — GIAO DỊCH PHÂN TÁN LIÊN CHI NHÁNH

SP `SP_CrossBranchTransfer` sử dụng `BEGIN DISTRIBUTED TRANSACTION` qua `LINK1` khi chuyển tiền liên chi nhánh. Yêu cầu bật MSDTC trên Windows.

### Các bước bật MSDTC

1. **Win+R** → `dcomcnfg` → Enter
2. Điều hướng: **Component Services** → **Computers** → **My Computer** → **Properties**
3. Tab **MSDTC** → **Security Configuration**:
   - ✅ Network DTC Access
   - ✅ Allow Remote Clients
   - ✅ Allow Inbound
   - ✅ Allow Outbound
   - Authentication: **No Authentication Required** (môi trường lab)
4. **Apply** → **OK**
5. Restart service:

```powershell
Restart-Service MSDTC
```

### Xác minh MSDTC

```sql
-- Chạy trên CN1 (SQLSERVER2 / NGANHANG_BT):
BEGIN DISTRIBUTED TRANSACTION;
    SELECT 1 AS Test FROM [LINK1].[NGANHANG_TD].dbo.CHINHANH;
COMMIT;
-- Nếu không có lỗi → MSDTC hoạt động đúng
```

### Cơ chế chuyển tiền liên chi nhánh

`SP_CrossBranchTransfer` hoạt động như sau:

1. Kiểm tra tài khoản nguồn tại **chi nhánh cục bộ**
2. Nếu tài khoản đích cũng ở cùng chi nhánh → giao dịch cục bộ (không cần MSDTC)
3. Nếu tài khoản đích ở **chi nhánh khác** → `BEGIN DISTRIBUTED TRANSACTION` qua `LINK1`:
   - Trừ tiền tài khoản nguồn (cục bộ)
   - Cộng tiền tài khoản đích (qua `[LINK1].[DB_remote].dbo.TAIKHOAN`)
   - Ghi nhận giao dịch GD_CHUYENTIEN
4. COMMIT hoặc ROLLBACK toàn bộ phân tán

---

## IX. TRACUU — CƠ CHẾ TỔNG HỢP KHÁCH HÀNG

TraCuu (`NGANHANG_TRACUU`) nhận dữ liệu từ Publisher qua Merge Replication:

- **CHINHANH**: tất cả chi nhánh (không filter)
- **KHACHHANG**: tất cả khách hàng đang hoạt động (`TrangThaiXoa = 0`), download-only

TraCuu **KHÔNG** nhận: NHANVIEN, TAIKHOAN, GD_GOIRUT, GD_CHUYENTIEN, SP, View.

Script `08_subscribers_post_replication_fixups.sql` tạo thêm:
- View `V_KHACHHANG_ALL` trên TraCuu (tra cứu thông tin khách hàng)
- Tăng cường chỉ đọc cho DB NGANHANG_TRACUU

Xác minh:

```sql
-- Chạy trên SQLSERVER4 / NGANHANG_TRACUU
USE NGANHANG_TRACUU;
SELECT RTRIM(MACN) AS MACN, COUNT(*) AS SoKH
FROM dbo.KHACHHANG GROUP BY MACN;
-- Kết quả: BENTHANH = 3, TANDINH = 3 (với seed data)
```

---

## X. SMOKE TESTS — KIỂM TRA TRƯỚC KHI MỞ ỨNG DỤNG

### 1. Kiểm tra bảng + SP trên từng DB

```sql
-- Trên mỗi instance:
SELECT name FROM sys.tables WHERE name NOT LIKE 'MSmerge_%' ORDER BY name;
SELECT name FROM sys.procedures ORDER BY name;
```

### 2. Kiểm tra linked server

```sql
-- Trên mỗi instance:
SELECT name, data_source FROM sys.servers WHERE is_linked = 1 ORDER BY name;
```

### 3. Kiểm tra sp_DangNhap

```sql
-- Trên Publisher (NGANHANG_PUB):
EXECUTE AS USER = N'NV_BT';
EXEC sp_DangNhap;
-- Kết quả mong đợi: MANV=NV_BT, TENNHOM=CHINHANH, MACN=BENTHANH
REVERT;

-- Trên CN1 (NGANHANG_BT):
EXEC sp_DangNhap;
-- Kết quả mong đợi: TENNHOM theo vai trò, MACN=BENTHANH (derived from DB_NAME())

-- Trên CN2 (NGANHANG_TD):
EXEC sp_DangNhap;
-- Kết quả mong đợi: MACN=TANDINH
```

### 4. Kiểm tra phân mảnh dữ liệu

```sql
-- Trên CN1 (NGANHANG_BT):
SELECT RTRIM(MACN) AS MACN, COUNT(*) AS Cnt FROM dbo.KHACHHANG GROUP BY MACN;
-- Kết quả: chỉ BENTHANH

-- Trên CN2 (NGANHANG_TD):
SELECT RTRIM(MACN) AS MACN, COUNT(*) AS Cnt FROM dbo.KHACHHANG GROUP BY MACN;
-- Kết quả: chỉ TANDINH

-- Ngoại lệ: CHINHANH có 2 hàng trên MỌI subscriber (không filter)
SELECT COUNT(*) FROM dbo.CHINHANH;
-- Kết quả: 2 (trên CN1, CN2, TraCuu)
```

### 5. Kiểm tra ứng dụng WPF

Xác minh `appsettings.json` đã có đủ `ConnectionStrings` cho Publisher/2 chi nhánh/Lookup.

Mở ứng dụng → đăng nhập với:
- `ADMIN_NH / Admin@123` — vai trò NGANHANG (quản trị, xem tất cả)
- `NV_BT / NhanVien@123` — vai trò CHINHANH (nhân viên chi nhánh)
- `KH_DEMO / KhachHang@123` — vai trò KHACHHANG (khách hàng)

---

## XI. CHECKLIST DEMO GIẢNG VIÊN

- [ ] SSMS hiển thị 4 kết nối: Default, SQLSERVER2, SQLSERVER3, SQLSERVER4
- [ ] Có 4 DB: `NGANHANG_PUB` (default), `NGANHANG_BT`, `NGANHANG_TD`, `NGANHANG_TRACUU`
- [ ] Replication Monitor hiển thị 3 publication đang hoạt động
- [ ] Linked Server tạo đúng: LINK0/LINK1/LINK2 theo quy ước
- [ ] Chi nhánh có thể query chéo qua LINK1 (phục vụ chuyển tiền liên CN)
- [ ] TraCuu đọc được KH từ cả 2 CN (KHACHHANG có cả BENTHANH + TANDINH)
- [ ] `sp_DangNhap` trả về đúng MACN trên cả Publisher và Subscriber
- [ ] App đọc đúng các `ConnectionStrings` SQL phân tán trong `appsettings.json`
- [ ] Demo: Mở TK → Gửi tiền → Rút tiền → Chuyển tiền liên chi nhánh
- [ ] Quay lại SSMS: query bảng giao dịch trên CN1 + CN2 để chứng minh dữ liệu phân mảnh đúng
- [ ] Demo TraCuu: query KHACHHANG trên SQLSERVER4 thấy KH từ cả 2 chi nhánh

---

## XII. TÀI LIỆU THAM KHẢO

| Tài liệu | Đường dẫn |
|---|---|
| Thứ tự thực thi SQL | `sql/00_readme_execution_order.md` |
| Test sp_DangNhap | `docs/tests/sp_dangnhap_test.sql` |
| Test seed data | `docs/tests/seed_data_verification.sql` |
| Test login branch resolution | `docs/tests/login_branch_resolution.md` |
| Trạng thái dự án | `docs/status/PROJECT_CURRENT_STATUS.md` |
