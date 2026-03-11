# Hướng Dẫn SSMS 21: Chạy SP NGANHANG

Ngày cập nhật: 2026-03-11  
Áp dụng: SSMS 21  
Mục tiêu: triển khai và kiểm tra các stored procedure nghiệp vụ trên SQL Server bằng thao tác chuẩn.

## Tài liệu liên quan

- [Hướng dẫn thiết lập CSDL phân tán trên SSMS 21](HUONG_DAN_SSMS21_THIET_LAP_CSDL_PHAN_TAN_NGANHANG.md)
- [Yêu cầu đề tài NGANHANG](../requirements/DE3-NGANHANG.md)

## 1) Bộ script SP nên dùng

Trên Publisher:

1. `sql/02_publisher_schema.sql`
2. `sql/03_publisher_sp_views.sql`
3. `sql/04_publisher_security.sql`
4. `sql/04b_publisher_seed_data.sql` (tùy chọn, chỉ để demo/test)

Lưu ý:
- Replication/Subscription/Linked Server làm trên UI theo tài liệu thiết lập phân tán.
- Không chạy script hạ tầng auto tạo replication nếu đang làm theo SSMS 21 UI.

## 2) Cách mở và chạy script trong SSMS 21

1. Mở SSMS 21 và kết nối Publisher.
2. Vào `File` -> `Open` -> `File...`
3. Chọn script SQL cần chạy.
4. Kiểm tra dropdown database trên thanh công cụ:
- Ưu tiên chọn đúng DB mục tiêu (`NGANHANG_PUB`) nếu script không tự `USE`.
5. Nhấn `Execute` (hoặc phím `F5`).
6. Kiểm tra tab `Messages`:
- Không có lỗi là đạt.
- Có lỗi thì sửa đúng lỗi rồi chạy lại.

## 3) Thứ tự chạy khuyến nghị

1. `02_publisher_schema.sql` (tạo bảng/ràng buộc)
2. `03_publisher_sp_views.sql` (tạo SP nghiệp vụ/báo cáo)
3. `04_publisher_security.sql` (role + grant/deny + SP auth account)
4. `04b_publisher_seed_data.sql` (nếu cần dữ liệu mẫu)

## 4) Cách kiểm tra SP đã tạo thành công

Chạy trên Publisher:

```sql
USE NGANHANG_PUB;
GO
SELECT p.name
FROM sys.procedures p
ORDER BY p.name;
```

Kiểm tra nhanh một số SP chính:

```sql
USE NGANHANG_PUB;
GO
EXEC dbo.sp_DangNhap;
GO
EXEC dbo.SP_GetBranches;
GO
EXEC dbo.SP_GetAllCustomers;
GO
```

## 5) Đẩy SP xuống Subscriber bằng UI (SSMS 21)

Nếu đã tạo publication, thực hiện:

1. Vào `Replication`, chọn publication.
2. Chuột phải -> `Properties`.
3. Mở `Articles`, bỏ tick `Show only checked articles in the list`.
4. Tick các SP cần replicate.
5. Nhấn `OK`.
6. Chuột phải publication -> `View Snapshot Agent Status` -> `Start`.

## 6) Kiểm tra SP trên subscriber

Trên từng subscriber (BT/TD/TRACUU):

```sql
SELECT p.name
FROM sys.procedures p
WHERE p.name IN (
    N'sp_DangNhap',
    N'SP_GetCustomersByBranch',
    N'SP_Deposit',
    N'SP_Withdraw',
    N'SP_CrossBranchTransfer',
    N'SP_GetAccountStatement'
)
ORDER BY p.name;
```

Nếu thiếu SP:
1. Kiểm tra article đã tick đúng chưa.
2. Chạy lại Snapshot Agent.
3. Kiểm tra Merge Agent status trong Replication Monitor.

## 7) Checklist lỗi thường gặp

1. `Could not find stored procedure ...`
- SP chưa được tạo ở Publisher hoặc chưa replicate xuống Subscriber.

2. `EXECUTE permission denied ...`
- Chưa grant quyền cho role tương ứng trong `04_publisher_security.sql` hoặc role mapping sai.

3. `Linked Server ... not configured`
- Chưa cấu hình LINK theo UI hoặc thiếu `RPC Out`.

4. Lỗi MSDTC khi chuyển khoản liên chi nhánh
- Dịch vụ Distributed Transaction Coordinator chưa bật trên máy liên quan.
