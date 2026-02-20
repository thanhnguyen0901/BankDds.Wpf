# SETUP_MS_SQL_DISTRIBUTED_GUIDE.md

## HƯỚNG DẪN SETUP MS SQL SERVER – ĐỒ ÁN CSDL PHÂN TÁN (DE3 – NGÂN HÀNG)

Tài liệu hướng dẫn setup SQL Server để chạy đồ án **CSDL phân tán – Đề 3 (Ngân hàng)** bằng **SSMS 21** theo đúng:
- Yêu cầu phân tán: **2 chi nhánh + 1 server tra cứu**
- Hạ tầng máy bạn: **4 SQL instances**, trong đó **default instance có SQL Agent** và là **DB gốc/coordinator**

---

## I) MỤC TIÊU & KIẾN TRÚC TRIỂN KHAI

### 1) Yêu cầu phân tán theo đề tài
Ngân hàng có **2 chi nhánh**:
- **BENTHANH**
- **TANDINH**

CSDL **NGANHANG** được phân tán thành **3 phân mảnh**:
- **Server1 (BENTHANH)**: lưu KH đăng ký tại BENTHANH + giao dịch tại BENTHANH
- **Server2 (TANDINH)**: lưu KH đăng ký tại TANDINH + giao dịch tại TANDINH
- **Server3 (TraCuu)**: chứa thông tin **khách hàng của cả 2 chi nhánh** (tổng hợp/tra cứu)

### 2) Mapping triển khai đúng với máy bạn (4 instances)
**Default instance** là nơi có SQL Agent ⇒ dùng làm **Coordinator/DB gốc**.

- **Coordinator (ROOT / DB gốc):** Default instance `DESKTOP-JBB41QU` chứa DB `NGANHANG`
- **Fragment BENTHANH:** `DESKTOP-JBB41QU\SQLSERVER2` chứa DB `NGANHANG_BT`
- **Fragment TANDINH:** `DESKTOP-JBB41QU\SQLSERVER3` chứa DB `NGANHANG_TD`
- **TraCuu:** `DESKTOP-JBB41QU\SQLSERVER4` chứa DB `NGANHANG_TRACUU`

> Lưu ý quan trọng: Trong scripts/SP hiện có, linked server được gọi theo tên cứng: **SERVER1 / SERVER2 / SERVER3**  
> ⇒ **Linked server name phải giữ nguyên đúng 3 tên này**, không đổi sang LS_*.

---

## II) THÔNG TIN MÁY & CÁCH CONNECT SSMS (ĐÓNG CỨNG THEO MÁY BẠN)

- **Machine:** `DESKTOP-JBB41QU`
- **IP:** `192.168.100.46`
- **SQL Auth dùng chung:** `sa / Password!123`

### 0.1 Connect đúng 4 instances
- **Default (MSSQLSERVER / Coordinator):** `DESKTOP-JBB41QU`
- **SQLSERVER2 (BENTHANH):** `DESKTOP-JBB41QU\SQLSERVER2`
- **SQLSERVER3 (TANDINH):** `DESKTOP-JBB41QU\SQLSERVER3`
- **SQLSERVER4 (TRACUU):** `DESKTOP-JBB41QU\SQLSERVER4`

---

## III) MAPPING TÊN LINKED SERVER (BẮT BUỘC)

| Linked Server Name (hardcode) | Vai trò theo đề | Instance thật | Database |
|---|---|---|---|
| **SERVER1** | CN **BENTHANH** | `DESKTOP-JBB41QU\SQLSERVER2` | `NGANHANG_BT` |
| **SERVER2** | CN **TANDINH** | `DESKTOP-JBB41QU\SQLSERVER3` | `NGANHANG_TD` |
| **SERVER3** | **TRACUU** | `DESKTOP-JBB41QU\SQLSERVER4` | `NGANHANG_TRACUU` |

Coordinator/DB gốc: `DESKTOP-JBB41QU` (default) chứa DB `NGANHANG`

---

## IV) CHECKLIST TRƯỚC KHI CHẠY SCRIPTS (LÀM 1 LẦN)

### 1) Bật SQL Server Browser (khuyến nghị)
Vì dùng named instances (`SQLSERVER2/3/4`) nên bật Browser để connect ổn định.
- SQL Server Configuration Manager
- SQL Server Services → SQL Server Browser → Start
- Startup type: Automatic

### 2) Bật Mixed Mode (nếu cần)
Trên từng instance:
- SSMS → Server Properties → Security
- Chọn: SQL Server and Windows Authentication mode
- Restart service instance

### 3) Verify connect được
Chạy trên mỗi instance:

```sql
SELECT @@SERVERNAME AS ServerName, @@VERSION AS Version;
````

---

## V) TẠO DATABASES (BẮT BUỘC ĐÚNG TÊN)

> Nếu DB đã tồn tại và bạn muốn chạy lại sạch: backup/drop trước.

### 1) Default (Coordinator): tạo DB `NGANHANG`

Connect: `DESKTOP-JBB41QU`

```sql
IF DB_ID('NGANHANG') IS NULL
    CREATE DATABASE NGANHANG;
GO
```

### 2) SQLSERVER2 (BENTHANH): tạo DB `NGANHANG_BT`

Connect: `DESKTOP-JBB41QU\SQLSERVER2`

```sql
IF DB_ID('NGANHANG_BT') IS NULL
    CREATE DATABASE NGANHANG_BT;
GO
```

### 3) SQLSERVER3 (TANDINH): tạo DB `NGANHANG_TD`

Connect: `DESKTOP-JBB41QU\SQLSERVER3`

```sql
IF DB_ID('NGANHANG_TD') IS NULL
    CREATE DATABASE NGANHANG_TD;
GO
```

### 4) SQLSERVER4 (TRACUU): tạo DB `NGANHANG_TRACUU`

Connect: `DESKTOP-JBB41QU\SQLSERVER4`

```sql
IF DB_ID('NGANHANG_TRACUU') IS NULL
    CREATE DATABASE NGANHANG_TRACUU;
GO
```

---

## VI) RUN `01-schema.sql` (TẠO BẢNG/VIEW NỀN)

> File schema thường chia SECTION theo node.
> Cách đúng: **bôi đen SECTION phù hợp** → F5.

### 1) Coordinator (Default) / DB `NGANHANG`

* Connect: `DESKTOP-JBB41QU`
* Open: `01-schema.sql`
* Chạy SECTION dành cho MAIN (phải có `USE NGANHANG;`)

Verify:

```sql
USE NGANHANG;
GO
SELECT TOP 50 name FROM sys.tables ORDER BY name;
GO
```

### 2) BENTHANH (SQLSERVER2) / DB `NGANHANG_BT`

* Connect: `DESKTOP-JBB41QU\SQLSERVER2`
* Open: `01-schema.sql`
* Chạy SECTION branch (phải có `USE NGANHANG_BT;`)

Verify:

```sql
USE NGANHANG_BT;
GO
SELECT TOP 50 name FROM sys.tables ORDER BY name;
GO
```

### 3) TANDINH (SQLSERVER3) / DB `NGANHANG_TD`

* Connect: `DESKTOP-JBB41QU\SQLSERVER3`
* Open: `01-schema.sql`
* Chạy SECTION branch (phải có `USE NGANHANG_TD;`)

Verify:

```sql
USE NGANHANG_TD;
GO
SELECT TOP 50 name FROM sys.tables ORDER BY name;
GO
```

### 4) TRACUU (SQLSERVER4) / DB `NGANHANG_TRACUU`

* Connect: `DESKTOP-JBB41QU\SQLSERVER4`
* Tạo schema cho TraCuu theo thiết kế:

  * Nếu có SECTION riêng trong `01-schema.sql` cho TraCuu: chạy section đó
  * Nếu chưa có: tối thiểu tạo các object phục vụ tra cứu (view/tables) ở bước “TraCuu View/Sync” phía dưới

---

## VII) RUN `16-linked-servers.sql` (CỰC KỲ QUAN TRỌNG)

### 1) Mục tiêu linked servers

* **Coordinator (default)**: tạo linked server tới **SERVER1, SERVER2, SERVER3**
* **SERVER1 (SQLSERVER2)**: tạo linked server tới **SERVER2** (bắt buộc cho chuyển tiền liên CN)
* **SERVER2 (SQLSERVER3)**: tạo linked server tới **SERVER1** (bắt buộc cho chuyển tiền liên CN)
* **SERVER3 (SQLSERVER4 – TRACUU)**: tạo linked server tới **SERVER1** và **SERVER2** (bắt buộc để tổng hợp KH)

### 2) BẮT BUỘC: linked server name giữ nguyên `SERVER1/SERVER2/SERVER3`

Không đổi tên, vì scripts/SP query dạng:

```sql
SELECT ... FROM [SERVER1].[NGANHANG_BT].dbo....
```

### 3) BẮT BUỘC: datasrc trỏ đúng instance thật

* SERVER1 → `DESKTOP-JBB41QU\SQLSERVER2`
* SERVER2 → `DESKTOP-JBB41QU\SQLSERVER3`
* SERVER3 → `DESKTOP-JBB41QU\SQLSERVER4`

### 4) BẮT BUỘC: bật options

Sau khi tạo linked server, trên mỗi instance cần:

* Data Access = True
* RPC = True
* RPC Out = True
* Enable Promotion of Distributed Transactions = True (nếu có)

### 5) Chạy đúng SECTION theo instance

File `16-linked-servers.sql` phải chia 4 section:

* SECTION A: Run on **default (Coordinator)**
* SECTION B: Run on **SQLSERVER2 (SERVER1)**
* SECTION C: Run on **SQLSERVER3 (SERVER2)**
* SECTION D: Run on **SQLSERVER4 (SERVER3 – TRACUU)**

### 6) Verify linked servers

Trên từng instance:

```sql
EXEC sp_linkedservers;
GO
SELECT name, data_source FROM sys.servers ORDER BY name;
GO
```

Test nhanh (tùy nơi chạy):

* Trên TRACUU (SQLSERVER4): test đọc từ 2 chi nhánh (bảng KH)
* Trên default: test đọc từ 3 node

---

## VIII) TRACUU: TẠO VIEW / CƠ CHẾ TỔNG HỢP KHÁCH HÀNG

Theo đề bài: **Server3 (TraCuu)** phải có **KH của cả 2 chi nhánh**.

Có 2 cách:

### Cách A (khuyến nghị demo nhanh): TraCuu là VIEW UNION

Trên `SQLSERVER4` / DB `NGANHANG_TRACUU`, tạo view union từ 2 branch:

* Yêu cầu: linked server từ TRACUU → SERVER1 & SERVER2 đã OK

Ví dụ (tùy đúng tên bảng KH trong schema):

```sql
USE NGANHANG_TRACUU;
GO

-- Example only: replace table/columns to match your schema
CREATE OR ALTER VIEW dbo.V_KHACHHANG_ALL
AS
SELECT * FROM [SERVER1].[NGANHANG_BT].dbo.KHACHHANG
UNION ALL
SELECT * FROM [SERVER2].[NGANHANG_TD].dbo.KHACHHANG;
GO
```

### Cách B: TraCuu là BẢNG TỔNG HỢP + SQL Agent Job Sync

* Tạo bảng `KHACHHANG_TRACUU`
* Tạo SP sync: xóa + insert hoặc upsert từ 2 branch
* Tạo SQL Agent Job trên default để chạy sync theo lịch

> Khi bạn gửi nội dung scripts, mình sẽ viết chuẩn SP sync + job step theo đúng tên bảng/cột.

---

## IX) RUN `02-seed.sql` (SEED DATA)

Chạy đúng SECTION theo node (tùy file chia section):

* Default / `NGANHANG`: SECTION MAIN (nếu có)
* SERVER1 / `NGANHANG_BT`: section branch
* SERVER2 / `NGANHANG_TD`: section branch
* TRACUU / `NGANHANG_TRACUU`: nếu TraCuu dùng bảng tổng hợp thì seed bảng đó (optional)

Verify: chọn vài bảng chính để check data.

---

## X) RUN STORED PROCEDURES (10 → 15)

> Nguyên tắc: chạy đúng DB theo role của script.
> Khi bạn gửi nội dung từng file SP, mình sẽ “đóng đinh” chính xác file nào chạy ở đâu.

Khuyến nghị rule cơ bản:

* Branch (SERVER1/SERVER2): SP nghiệp vụ branch (customer/account/transaction…)
* Coordinator (default/NGANHANG): SP report/auth/quản trị (nếu thiết kế đặt ở main)
* TRACUU: thường không cần SP nghiệp vụ, chỉ cần view/report tra cứu

Sau khi chạy SP ở mỗi DB:

```sql
SELECT TOP 200 name FROM sys.procedures ORDER BY name;
GO
```

---

## XI) BẬT MSDTC (DISTRIBUTED TRANSACTION) – CHUYỂN TIỀN LIÊN CHI NHÁNH

Trên Windows:

1. Win+R → `dcomcnfg`
2. Component Services → Computers → My Computer → Properties
3. Tab MSDTC → Security Configuration:

   * Network DTC Access ✅
   * Allow Remote Clients ✅
   * Allow Inbound ✅
   * Allow Outbound ✅
   * Authentication: No Authentication Required (lab)
4. Apply → OK
5. Restart service: Distributed Transaction Coordinator

Verify: chạy SP chuyển tiền cross-branch nếu có.

---

## XII) CONNECTION STRINGS

### Coordinator (Default) – DB gốc `NGANHANG`

```txt
Server=DESKTOP-JBB41QU;Database=NGANHANG;User Id=sa;Password=Password!123;TrustServerCertificate=True;Encrypt=False;
```

### BENTHANH (SQLSERVER2) – `NGANHANG_BT`

```txt
Server=DESKTOP-JBB41QU\SQLSERVER2;Database=NGANHANG_BT;User Id=sa;Password=Password!123;TrustServerCertificate=True;Encrypt=False;
```

### TANDINH (SQLSERVER3) – `NGANHANG_TD`

```txt
Server=DESKTOP-JBB41QU\SQLSERVER3;Database=NGANHANG_TD;User Id=sa;Password=Password!123;TrustServerCertificate=True;Encrypt=False;
```

### TRACUU (SQLSERVER4) – `NGANHANG_TRACUU`

```txt
Server=DESKTOP-JBB41QU\SQLSERVER4;Database=NGANHANG_TRACUU;User Id=sa;Password=Password!123;TrustServerCertificate=True;Encrypt=False;
```

---

## XIII) SMOKE TESTS (BẮT BUỘC TRƯỚC KHI MỞ APP)

### 1) Check tables/procedures

Trong từng DB:

```sql
SELECT TOP 50 name FROM sys.tables ORDER BY name;
GO
SELECT TOP 200 name FROM sys.procedures ORDER BY name;
GO
```

### 2) Check linked servers

Trong từng instance:

```sql
EXEC sp_linkedservers;
GO
```

### 3) Test TraCuu union

Trên `SQLSERVER4` / `NGANHANG_TRACUU`:

```sql
SELECT TOP 10 * FROM dbo.V_KHACHHANG_ALL;
GO
```

---

## XIV) CHECKLIST DEMO GIẢNG VIÊN

* [ ] SSMS show 4 connections: Default, SQLSERVER2, SQLSERVER3, SQLSERVER4
* [ ] Có 4 DB: `NGANHANG` (default), `NGANHANG_BT`, `NGANHANG_TD`, `NGANHANG_TRACUU`
* [ ] Linked Servers tạo đúng name `SERVER1/SERVER2/SERVER3` theo mapping
* [ ] Branch có thể query chéo (phục vụ chuyển tiền liên CN)
* [ ] TRACUU đọc được KH từ cả 2 CN (view union hoặc bảng sync)
* [ ] App chạy `DataMode=Sql`
* [ ] Demo: mở TK / gửi-rút / chuyển tiền liên chi nhánh
* [ ] Quay lại SSMS: query bảng giao dịch để chứng minh ghi đúng phân mảnh