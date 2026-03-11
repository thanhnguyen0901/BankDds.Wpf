# TraCuu Demo — Tra cứu khách hàng toàn hệ thống

## Mục đích

Chức năng **Tra cứu KH** cho phép người dùng vai trò **NGANHANG** (ngân hàng)
tìm kiếm khách hàng trên **tất cả chi nhánh** thông qua CSDL tra cứu chỉ đọc
`NGANHANG_TRACUU` (SQLSERVER4).

CSDL này nhận bảng **KHACHHANG** + **CHINHANH** qua Sao chép hợp nhất từ
Publisher — không lọc theo chi nhánh — nên chứa dữ liệu của cả Bến Thành và
Tân Định.

## Điều kiện tiên quyết

| # | Yêu cầu | Cách kiểm tra |
|---|---------|---------------|
| 1 | Đã chạy đủ 8 script SQL (bao gồm 08_subscribers_post_replication_fixups.sql) | SSMS: 4 DB tồn tại |
| 2 | Snapshot Agent cho PUB_TRACUU đã hoàn thành | Replication Monitor: status OK |
| 3 | `appsettings.json` có key `ConnectionStrings:LookupDatabase` | Xem file |
| 4 | App cấu hình đủ các `ConnectionStrings` SQL phân tán | appsettings.json |

## Các bước demo

### 1. Đăng nhập vai trò NGANHANG

```
Login:    NV_BT  (hoặc bất kỳ login thuộc vai trò NGANHANG)
Password: 123
```

### 2. Mở tab "Tra cứu KH"

Sau khi đăng nhập NGANHANG, menu trên cùng sẽ hiển thị nút **"Tra cứu KH"**
(chỉ visible cho NGANHANG).

Click vào nút → mở màn hình tra cứu.

### 3. Tìm theo CMND (chính xác)

- Chọn radio **"Tìm theo CMND (chính xác)"**
- Nhập CMND: `0301234567`
- Nhấn **"Tra cứu"** hoặc Enter
- Kết quả: Hiển thị 1 dòng với đầy đủ thông tin KH (nếu tồn tại)

### 4. Tìm theo họ tên (gần đúng)

- Chọn radio **"Tìm theo họ tên (gần đúng)"**
- Nhập: `Nguyễn`
- Nhấn **"Tra cứu"**
- Kết quả: Hiển thị tất cả KH có họ/tên chứa "Nguyễn" từ **cả 2 chi nhánh**
  (cột "Chi nhánh" hiển thị BENTHANH hoặc TANDINH)

### 5. Xác minh dữ liệu phân tán

Mở SSMS kết nối đến SQLSERVER4, chạy:

```sql
USE NGANHANG_TRACUU;
SELECT CMND, HO, TEN, MACN FROM dbo.KHACHHANG ORDER BY MACN;
```

So sánh kết quả với dữ liệu hiển thị trên app — phải khớp.

## Ghi chú kỹ thuật

- **Connection**: App kết nối trực tiếp đến `NGANHANG_TRACUU` (SQLSERVER4)
  qua key `ConnectionStrings:LookupDatabase` trong `appsettings.json`.
- **Fallback**: Nếu key LookupDatabase không được cấu hình, app tự động chuyển sang
  dùng Publisher connection (cũng có đủ dữ liệu, nhưng không demo được phân tán).
- **Bảo mật**: Chỉ vai trò NGANHANG mới thấy nút "Tra cứu KH". Service layer
  (`CustomerLookupService`) kiểm tra quyền trước khi truy vấn.
- **Read-only**: `NGANHANG_TRACUU` được gia cố chỉ đọc (DENY INSERT/UPDATE/DELETE)
  trong script 08 §7.

## Kiến trúc luồng dữ liệu

```
[WPF App]
   │
   ├─ CustomerLookupViewModel
   │     └─ ICustomerLookupService (CustomerLookupService)
   │           └─ ICustomerLookupRepository (CustomerLookupRepository)
   │                 └─ ConnectionStringProvider.GetLookupConnection()
   │                       └─ appsettings → "ConnectionStrings:LookupDatabase"
   │                             └─ SQLSERVER4 / NGANHANG_TRACUU
   │                                   └─ SELECT dbo.KHACHHANG (replicated, all branches)
   │
   └─ [Existing branch views continue using Branch_BENTHANH / Branch_TANDINH]
```
