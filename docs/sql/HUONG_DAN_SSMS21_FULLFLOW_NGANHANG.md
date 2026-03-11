# Hướng Dẫn SSMS 21: Full Flow NGANHANG

Áp dụng: SQL Server + SSMS 21  

## Tài liệu liên quan

- [Yêu cầu đề tài NGANHANG](../requirements/DE3-NGANHANG.md)
- [README dự án](../../README.md)

## 1) Quy ước instance và database

1. Publisher instance: `DESKTOP-JBB41QU` -> DB `NGANHANG_PUB`.
2. Subscriber CN1: `DESKTOP-JBB41QU\SQLSERVER2` -> DB `NGANHANG_BT`.
3. Subscriber CN2: `DESKTOP-JBB41QU\SQLSERVER3` -> DB `NGANHANG_TD`.
4. Subscriber Tra cứu: `DESKTOP-JBB41QU\SQLSERVER4` -> DB `NGANHANG_TRACUU`.

## 2) Thứ tự triển khai full flow

1. Kết nối đủ 4 instance trong SSMS 21.
2. Tạo 4 database đúng theo mapping ở mục 1.
3. Cấu hình Distributor trên Publisher.
4. Chạy bộ script SQL trên Publisher theo thứ tự chuẩn.
5. Tạo 3 Publication trên Publisher.
6. Tạo 3 Push Subscription từ Publisher đến 3 Subscriber.
7. Đẩy SP xuống Subscriber bằng Articles + Snapshot Agent.
8. Theo dõi đồng bộ trong Replication Monitor đến khi `Succeeded`.
9. Cấu hình Linked Server trên Publisher/CN1/CN2.
10. Tạo login/user/role mapping theo 3 nhóm quyền đề tài.
11. Kiểm tra nghiệm thu dữ liệu phân mảnh và quyền truy cập.

## 3) Chi tiết từng bước

### Bước 1: Kết nối 4 instance

1. Mở SSMS 21.
2. Kết nối lần lượt:
`DESKTOP-JBB41QU`, `DESKTOP-JBB41QU\SQLSERVER2`, `DESKTOP-JBB41QU\SQLSERVER3`, `DESKTOP-JBB41QU\SQLSERVER4`.
3. Xác nhận Object Explorer hiển thị đủ 4 node server.

### Bước 2: Tạo database

1. Trên `DESKTOP-JBB41QU`: tạo `NGANHANG_PUB`.
2. Trên `DESKTOP-JBB41QU\SQLSERVER2`: tạo `NGANHANG_BT`.
3. Trên `DESKTOP-JBB41QU\SQLSERVER3`: tạo `NGANHANG_TD`.
4. Trên `DESKTOP-JBB41QU\SQLSERVER4`: tạo `NGANHANG_TRACUU`.

### Bước 3: Cấu hình Distributor

Instance thao tác: `DESKTOP-JBB41QU`.

1. Chuột phải `Replication` -> `Configure Distribution...`.
2. Chọn `This server will act as its own Distributor`.
3. Chọn snapshot folder, ví dụ `C:\ReplData`.
4. Distribution database: dùng `distribution`.
5. `Finish` và kiểm tra `Messages` không lỗi.

### Bước 4: Chạy script SQL trên Publisher

Instance thao tác: `DESKTOP-JBB41QU`.  
Database thao tác: `NGANHANG_PUB`.

Chạy đúng thứ tự:

1. `sql/02_publisher_schema.sql`
Mục đích: tạo schema/bảng/constraint/các object nền tảng.
2. `sql/03_publisher_sp_views.sql`
Mục đích: tạo toàn bộ SP và view nghiệp vụ.
3. `sql/04_publisher_security.sql`
Mục đích: tạo role và grant execute cho SP.
4. `sql/04b_publisher_seed_data.sql` (tùy chọn)
Mục đích: nạp dữ liệu mẫu để test/demo.

Lưu ý:

1. Bước này bắt buộc hoàn tất trước khi tạo Publication.
2. Mỗi script chạy bằng `File -> Open -> File...`, chọn đúng DB `NGANHANG_PUB`, nhấn `Execute`.
3. Tab `Messages` không có lỗi mới chuyển bước tiếp.

### Bước 5: Tạo Publication

Instance thao tác: `DESKTOP-JBB41QU`.

Tạo 3 publication:

1. `PUB_NGANHANG_BT`
Article: `CHINHANH`, `KHACHHANG`, `NHANVIEN`, `TAIKHOAN`, `GD_GOIRUT`, `GD_CHUYENTIEN`.
Filter: `MACN = N'BENTHANH'`.
2. `PUB_NGANHANG_TD`
Article: như BT.
Filter: `MACN = N'TANDINH'`.
3. `PUB_TRACUU`
Article: `CHINHANH`, `KHACHHANG`.
Không filter cho `KHACHHANG`.

### Bước 6: Tạo Push Subscription

Instance thao tác: `DESKTOP-JBB41QU`.

1. `PUB_NGANHANG_BT` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER2`, DB `NGANHANG_BT`.
2. `PUB_NGANHANG_TD` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER3`, DB `NGANHANG_TD`.
3. `PUB_TRACUU` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER4`, DB `NGANHANG_TRACUU`.

### Bước 7: Đẩy SP xuống Subscriber

Instance thao tác: `DESKTOP-JBB41QU`.

Lặp lại cho từng publication:

1. `Properties` -> `Articles`.
2. Bỏ `Show only checked articles in the list`.
3. Tick các SP cần replicate.
4. `OK`.
5. `View Snapshot Agent Status` -> `Start`.

### Bước 8: Theo dõi đồng bộ

Instance thao tác: `DESKTOP-JBB41QU`.

1. Mở `Launch Replication Monitor`.
2. Kiểm tra Snapshot Agent và Merge Agent đều `Succeeded`.
3. Nếu lỗi, mở chi tiết job để xử lý theo log.

### Bước 9: Cấu hình Linked Server

Mục tiêu: các SP phân tán gọi được dữ liệu liên server.

### 9.1 Bảng mapping link cần tạo

Trên `DESKTOP-JBB41QU`:
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`.
3. `LINK2` -> `DESKTOP-JBB41QU\SQLSERVER3`.

Trên `DESKTOP-JBB41QU\SQLSERVER2`:
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER3`.

Trên `DESKTOP-JBB41QU\SQLSERVER3`:
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`.

### 9.2 Cách tạo 1 linked server (lặp lại cho từng dòng mapping)

1. Chọn đúng instance cần thao tác trong Object Explorer.
2. Vào `Server Objects` -> `Linked Servers`.
3. Chuột phải `Linked Servers` -> `New Linked Server...`.
4. Tab `General`:
`Linked server`: nhập tên link theo mapping (`LINK0`/`LINK1`/`LINK2`).
`Server type`: chọn `SQL Server`.
5. Tab `Security`:
chọn phương án đăng nhập phù hợp môi trường lab của bạn (Windows pass-through hoặc SQL Login).
6. Tab `Server Options`:
đặt `Data Access` = `True`.
đặt `RPC` = `True`.
đặt `RPC Out` = `True`.
7. Bấm `OK`.

### 9.3 Kiểm tra sau khi tạo link

Chạy trên từng instance vừa cấu hình link:

1. `EXEC sp_testlinkedserver N'LINK0';`
2. `EXEC sp_testlinkedserver N'LINK1';`
3. Với Publisher, chạy thêm `EXEC sp_testlinkedserver N'LINK2';`

Nếu lỗi:
1. Kiểm tra lại tab `Security` của linked server.
2. Kiểm tra `RPC Out` đã bật.
3. Kiểm tra instance đích có đang chạy và truy cập được qua mạng.

### Bước 10: Security theo đề tài

Mục tiêu: người dùng đăng nhập đúng vai trò và chỉ làm được chức năng được phép.

### 10.1 Chạy script security

1. Mở query trên instance `DESKTOP-JBB41QU`, database `NGANHANG_PUB`.
2. Chạy `sql/04_publisher_security.sql`.
3. Kết quả mong đợi:
đã có 3 role `NGANHANG`, `CHINHANH`, `KHACHHANG`.
đã có `sp_DangNhap`, `sp_TaoTaiKhoan`, các GRANT EXECUTE theo role.

### 10.2 Tạo login cho tài khoản nghiệp vụ

Có 2 cách:

1. Cách khuyến nghị: gọi `sp_TaoTaiKhoan` trên `NGANHANG_PUB`.
2. Cách thủ công UI: `Security` -> `Logins` -> `New Login...`.

Lưu ý quan trọng:
login là đối tượng mức server.
người dùng đăng nhập vào instance nào thì login phải tồn tại ở instance đó.

### 10.3 Gán user vào database và role

1. Mở `Login Properties` -> `User Mapping`.
2. Tick DB đích.
3. Tick đúng role:
`NGANHANG` hoặc `CHINHANH` hoặc `KHACHHANG`.
4. Bấm `OK`.

### 10.4 Kiểm tra quyền theo đề tài

1. Đăng nhập bằng tài khoản mẫu mỗi nhóm.
2. Chạy `EXEC dbo.sp_DangNhap;` để xác nhận `TENNHOM` và `MACN`.
3. Test nghiệp vụ:
`NGANHANG` xem báo cáo toàn hệ.
`CHINHANH` thao tác cập nhật trong chi nhánh đăng nhập.
`KHACHHANG` chỉ xem sao kê tài khoản của chính mình.

### Bước 11: Kiểm tra nghiệm thu cuối

1. Trên Publisher có đủ 3 publication.
2. Có đủ 3 push subscription đúng mapping.
3. Agent trạng thái `Succeeded`.
4. Subscriber nhận đúng dữ liệu phân mảnh.
5. SP có mặt trên các DB subscriber theo publication tương ứng.
6. Login theo từng nhóm chạy đúng quyền.

## 4) Khi nào phải chạy lại script/SP

1. Sửa cấu trúc bảng hoặc ràng buộc: chạy lại `02_publisher_schema.sql` trên Publisher.
2. Sửa SP/view nghiệp vụ: chạy lại `03_publisher_sp_views.sql` trên Publisher.
3. Sửa grant/role: chạy lại `04_publisher_security.sql` trên Publisher.
4. Muốn nạp lại dữ liệu demo: chạy `04b_publisher_seed_data.sql`.
5. Sau khi thay đổi SP cần phát tán: vào Publication tick lại Articles SP và chạy Snapshot Agent.

## 5) Đối chiếu chức năng đề tài và SP chính

1. Cập nhật khách hàng:
`SP_AddCustomer`, `SP_UpdateCustomer`, `SP_DeleteCustomer`, `SP_RestoreCustomer`, `SP_GetCustomersByBranch`.
2. Mở và quản lý tài khoản:
`SP_AddAccount`, `SP_UpdateAccount`, `SP_CloseAccount`, `SP_ReopenAccount`, `SP_GetAccountsByCustomer`.
3. Cập nhật nhân viên và chuyển chi nhánh:
`SP_AddEmployee`, `SP_UpdateEmployee`, `SP_DeleteEmployee`, `SP_RestoreEmployee`, `SP_TransferEmployee`.
4. Giao dịch gửi/rút/chuyển:
`SP_Deposit`, `SP_Withdraw`, `SP_CrossBranchTransfer`.
5. Báo cáo sao kê:
`SP_GetAccountStatement` (trả số dư đầu kỳ + danh sách giao dịch có RunningBalance).
6. Liệt kê tài khoản mở trong kỳ:
`SP_GetAccountsOpenedInPeriod` (có tham số chi nhánh hoặc toàn hệ).
7. Liệt kê khách hàng theo chi nhánh/tất cả, sắp xếp họ tên:
`SP_GetCustomersByBranch` (ORDER BY `HO`, `TEN`).
8. Phân quyền đăng nhập:
`sp_DangNhap`, `sp_TaoTaiKhoan`, nhóm role `NGANHANG`/`CHINHANH`/`KHACHHANG`.
