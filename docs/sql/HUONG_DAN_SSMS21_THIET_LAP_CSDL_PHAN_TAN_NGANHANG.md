# Hướng Dẫn SSMS 21: Thiết Lập CSDL Phân Tán NGANHANG

Ngày cập nhật: 2026-03-11  
Áp dụng: SQL Server + SSMS 21  
Mục tiêu: người đọc làm theo từng bước trên giao diện SSMS để dựng phân tán, phân quyền, bảo mật.

## Tài liệu liên quan

- [Hướng dẫn chạy SP NGANHANG trên SSMS 21](HUONG_DAN_SSMS21_CHAY_SP_NGANHANG.md)
- [Yêu cầu đề tài NGANHANG](../requirements/DE3-NGANHANG.md)

## 1) Kiến trúc cần có

1. Publisher: `NGANHANG_PUB`
2. Subscriber CN1: `NGANHANG_BT`
3. Subscriber CN2: `NGANHANG_TD`
4. Subscriber TraCuu: `NGANHANG_TRACUU`

Ví dụ instance:
- `DESKTOP-JBB41QU` (Publisher)
- `DESKTOP-JBB41QU\SQLSERVER2` (CN1)
- `DESKTOP-JBB41QU\SQLSERVER3` (CN2)
- `DESKTOP-JBB41QU\SQLSERVER4` (TraCuu)

## 2) Điều kiện trước khi thao tác

1. SQL Server Agent đang chạy trên Publisher.
2. SQL Server Browser đang chạy.
3. Tài khoản thao tác có quyền `sysadmin` (hoặc đủ quyền replication/security).
4. Đã cài SSMS 21.

## 3) Tạo database bằng UI trên SSMS 21

Thực hiện trên từng instance:

1. Mở SSMS 21, kết nối instance.
2. Trong Object Explorer, chuột phải `Databases` -> `New Database...`
3. Nhập tên DB:
- Publisher: `NGANHANG_PUB`
- CN1: `NGANHANG_BT`
- CN2: `NGANHANG_TD`
- TraCuu: `NGANHANG_TRACUU`
4. Nhấn `OK`.

## 4) Cấu hình Distributor bằng Wizard

Thực hiện trên Publisher:

1. Mở node `Replication`.
2. Chuột phải `Replication` -> `Configure Distribution...`
3. Chọn `Server will act as its own Distributor`.
4. Chọn thư mục snapshot (ví dụ `C:\ReplData`).
5. Nhấn `Next` đến cuối và `Finish`.
6. Kiểm tra hoàn tất trong tab `Messages`.

## 5) Tạo Publication cho BT, TD, TraCuu

Làm trên Publisher, tạo 3 publication merge:

1. `PUB_NGANHANG_BT`
2. `PUB_NGANHANG_TD`
3. `PUB_TRACUU`

Các bước tạo mỗi publication:

1. `Replication` -> `Local Publications` -> chuột phải -> `New Publication...`
2. Chọn DB nguồn `NGANHANG_PUB`.
3. Chọn loại `Merge publication`.
4. Chọn article:
- BT/TD: `CHINHANH`, `KHACHHANG`, `NHANVIEN`, `TAIKHOAN`, `GD_GOIRUT`, `GD_CHUYENTIEN`.
- TraCuu: `CHINHANH`, `KHACHHANG`.
5. Đặt row filter:
- BT: `MACN = N'BENTHANH'`
- TD: `MACN = N'TANDINH'`
- TraCuu: không filter cho `KHACHHANG` (nhận toàn hệ).
6. Đặt tên publication và `Finish`.

## 6) Đẩy Stored Procedure xuống phân mảnh (UI)

Thực hiện đúng thao tác SSMS 21:

1. Vào `Replication`, chọn publication muốn đẩy xuống.
2. Chuột phải publication -> `Properties`.
3. Chọn `Articles` -> bỏ dấu `Show only checked articles in the list`.
4. Tick các stored procedure cần dùng tại phân mảnh.
5. Nhấn `OK` để lưu.
6. Chuột phải publication -> `View Snapshot Agent Status` -> `Start` để đẩy snapshot.

## 7) Tạo Push Subscription

Làm trên Publisher:

1. Chuột phải publication -> `New Subscriptions...`
2. Chọn `Push subscriptions`.
3. Mapping:
- `PUB_NGANHANG_BT` -> Subscriber `SQLSERVER2`, DB `NGANHANG_BT`
- `PUB_NGANHANG_TD` -> Subscriber `SQLSERVER3`, DB `NGANHANG_TD`
- `PUB_TRACUU` -> Subscriber `SQLSERVER4`, DB `NGANHANG_TRACUU`
4. Cấu hình Agent Security.
5. Chọn schedule phù hợp.
6. `Finish`.

## 8) Theo dõi đồng bộ trên Replication Monitor

1. Mở `Replication` -> `Launch Replication Monitor`.
2. Kiểm tra Snapshot Agent và Merge Agent đều `Succeeded`.
3. Nếu lỗi, mở chi tiết job để đọc lỗi permission/network/filter.

## 9) Cấu hình Linked Server bằng UI

Mỗi instance thao tác tại:
`Server Objects` -> `Linked Servers` -> chuột phải -> `New Linked Server...`

Mapping khuyến nghị:

1. Publisher:
- `LINK0` -> TraCuu
- `LINK1` -> CN1
- `LINK2` -> CN2

2. CN1:
- `LINK0` -> TraCuu
- `LINK1` -> CN2

3. CN2:
- `LINK0` -> TraCuu
- `LINK1` -> CN1

Thiết lập bắt buộc trong `Server Options`:
- `Data Access` = `True`
- `RPC` = `True`
- `RPC Out` = `True`

## 10) Thiết lập security: role, login, user, quyền

### 10.1 Tạo role trong từng database

1. Mở database -> `Security` -> `Roles` -> `Database Roles`.
2. Chuột phải -> `New Database Role...`
3. Tạo 3 role:
- `NGANHANG`
- `CHINHANH`
- `KHACHHANG`

### 10.2 Tạo login ở mức server

1. Mở instance -> `Security` -> `Logins`.
2. Chuột phải -> `New Login...`
3. Tạo login theo tài khoản nghiệp vụ.

### 10.3 Mapping user + role

1. Mở `Login Properties` -> tab `User Mapping`.
2. Tick DB đích.
3. Chọn role phù hợp (`NGANHANG`, `CHINHANH`, `KHACHHANG`).
4. `OK`.

## 11) Kiểm tra nhanh sau cấu hình

1. Test login mỗi role đăng nhập DB được.
2. Chạy `EXEC sp_DangNhap;` để xác nhận role và branch.
3. Test query qua linked server.
4. Kiểm tra dữ liệu đã sync đúng phân mảnh.

## 12) Checklist nghiệm thu tối thiểu

1. Distributor cấu hình thành công.
2. 3 publication có trạng thái hoạt động.
3. 3 subscription sync thành công.
4. SP runtime đã được đẩy đúng subscriber.
5. Linked server test qua được.
6. Role/login/user mapping đúng yêu cầu đề tài.
