# Checklist Thực Thi SSMS UI-First - Giai đoạn B

Ngày thực thi: ..........  
Người thực thi: ..........  
Môi trường: ..........

---

## B2 - Publication/Subscription (UI)

| ID | Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|---|
| B2-01 | Configure Distributor bằng Wizard thành công | TODO | |
| B2-02 | Tạo publication `PUB_NGANHANG_BT` (Merge) | TODO | |
| B2-03 | Tạo publication `PUB_NGANHANG_TD` (Merge) | TODO | |
| B2-04 | Tạo publication `PUB_TRACUU` (Merge) | TODO | |
| B2-05 | Áp row filter đúng cho BT/TD theo `MACN` | TODO | |
| B2-06 | Chọn articles table/view/SP theo phạm vi runtime | TODO | |
| B2-07 | Tạo push subscription BT -> SQLSERVER2/NGANHANG_BT | TODO | |
| B2-08 | Tạo push subscription TD -> SQLSERVER3/NGANHANG_TD | TODO | |
| B2-09 | Tạo push subscription TRACUU -> SQLSERVER4/NGANHANG_TRACUU | TODO | |
| B2-10 | Snapshot Agent + Merge Agent chạy thành công | TODO | |

---

## B3 - Linked Server (UI)

| ID | Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|---|
| B3-01 | Publisher có LINK0/LINK1/LINK2 đúng datasource | TODO | |
| B3-02 | CN1 có LINK0/LINK1 đúng datasource | TODO | |
| B3-03 | CN2 có LINK0/LINK1 đúng datasource | TODO | |
| B3-04 | Server options: Data Access/RPC/RPC Out = True | TODO | |
| B3-05 | Query test qua LINK không lỗi permission/login | TODO | |

---

## B4 - Role/Login/User (UI)

| ID | Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|---|
| B4-01 | Tạo role DB: NGANHANG, CHINHANH, KHACHHANG | TODO | |
| B4-02 | Tạo login mẫu server-level (ADMIN_NH/NV_BT/KH_DEMO...) | TODO | |
| B4-03 | User mapping vào đúng DB | TODO | |
| B4-04 | Role membership đúng theo DE3 | TODO | |
| B4-05 | Cấp quyền đối tượng runtime theo matrix | TODO | |

---

## Kiểm tra chấp nhận tối thiểu

| ID | Kiểm tra | Trạng thái | Bằng chứng |
|---|---|---|---|
| AC-01 | Login role NGANHANG vào app thành công | TODO | |
| AC-02 | Login role CHINHANH vào app thành công | TODO | |
| AC-03 | Login role KHACHHANG vào app thành công | TODO | |
| AC-04 | NGANHANG xem được dữ liệu liên chi nhánh theo branch chọn | TODO | |
| AC-05 | CHINHANH chỉ thao tác được chi nhánh mình | TODO | |
| AC-06 | KHACHHANG chỉ xem sao kê của chính mình | TODO | |

---

## Ghi chú lỗi/phát hiện

- ..................................................................
- ..................................................................
- ..................................................................
