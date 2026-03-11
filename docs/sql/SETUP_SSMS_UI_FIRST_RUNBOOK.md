# Runbook SSMS UI-First (Giai đoạn B)

Mục tiêu: triển khai hạ tầng phân tán NGANHANG theo đúng hướng QLVT bằng **SSMS UI**, không dùng script auto-setup replication/linked-server/security nền.

Phạm vi runbook này:
- B2: Publication + Subscription bằng Wizard/Replication Monitor.
- B3: Linked Server bằng UI.
- B4: Role/Login/User mapping bằng UI.

Không thuộc runbook này:
- Refactor SP/runtime app (Giai đoạn C/D).
- Cleanup script/archive (Giai đoạn E).

---

## 1) Điều kiện trước khi thao tác

- 4 instance online:
  - Publisher: `DESKTOP-JBB41QU`
  - CN1: `DESKTOP-JBB41QU\SQLSERVER2`
  - CN2: `DESKTOP-JBB41QU\SQLSERVER3`
  - TraCuu: `DESKTOP-JBB41QU\SQLSERVER4`
- SQL Server Agent chạy trên Publisher.
- SQL Server Browser đang chạy.
- Mixed Mode Authentication bật trên tất cả instance.
- DB đã có schema + SP runtime cơ bản trên Publisher (giai đoạn SQL runtime sẽ chuẩn hóa tiếp).

---

## 2) B2 - Replication bằng SSMS UI

## 2.1 Cấu hình Distributor
1. Mở SSMS, connect Publisher.
2. Mở node `Replication` -> chuột phải `Configure Distribution...`.
3. Chọn Publisher hiện tại làm Distributor.
4. Chọn snapshot folder (ví dụ `C:\ReplData`) có quyền Agent đọc/ghi.
5. Hoàn tất wizard.

Kết quả mong đợi:
- Xuất hiện distribution database.
- Không lỗi permission/wizard.

## 2.2 Tạo 3 publication (Merge)
Tạo lần lượt:
- `PUB_NGANHANG_BT`
- `PUB_NGANHANG_TD`
- `PUB_TRACUU`

Menu: `Replication` -> `Local Publications` -> `New Publication...`

Thiết lập chung:
1. Chọn DB nguồn: `NGANHANG_PUB`.
2. Publication type: `Merge publication`.
3. Chọn articles:
   - Table bắt buộc: `CHINHANH`, `KHACHHANG`, `NHANVIEN`, `TAIKHOAN`, `GD_GOIRUT`, `GD_CHUYENTIEN` cho BT/TD.
   - TraCuu: chọn `CHINHANH`, `KHACHHANG`.
   - Bỏ `sysdiagrams`.
4. Với publication BT/TD:
   - Bật row filter theo `MACN`.
   - BT: `MACN = N'BENTHANH'`.
   - TD: `MACN = N'TANDINH'`.
5. Articles cho SP/view runtime:
   - Trong mục Articles, bỏ tick `Show only checked articles`.
   - Chọn nhóm Stored Procedures/View cần replicate (runtime objects).
6. Đặt publication name tương ứng, finish.

## 2.3 Tạo Push Subscription
Tạo subscription từ Publisher:
- `PUB_NGANHANG_BT` -> Subscriber `SQLSERVER2` DB `NGANHANG_BT`
- `PUB_NGANHANG_TD` -> Subscriber `SQLSERVER3` DB `NGANHANG_TD`
- `PUB_TRACUU` -> Subscriber `SQLSERVER4` DB `NGANHANG_TRACUU`

Menu: Publication -> chuột phải -> `New Subscriptions...`

Thiết lập chung:
1. Chọn `Push subscriptions`.
2. Chọn subscriber instance và database đích đúng mapping.
3. Agent security dùng login có quyền tương ứng.
4. Schedule: chạy liên tục (dev/demo có thể schedule phù hợp).

## 2.4 Chạy Snapshot + kiểm tra sync
1. Mở `Replication Monitor`.
2. Chạy Snapshot Agent cho từng publication.
3. Theo dõi Merge Agent tới khi trạng thái success.

Kết quả mong đợi:
- 3 publication active.
- Subscriber có data theo đúng phân mảnh.

---

## 3) B3 - Linked Server bằng SSMS UI

Thao tác tại: `Server Objects` -> `Linked Servers` -> `New Linked Server...`

Thiết lập cho từng node:

1. Trên Publisher:
- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4` (TraCuu)
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2` (CN1)
- `LINK2` -> `DESKTOP-JBB41QU\SQLSERVER3` (CN2)

2. Trên CN1:
- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER3`

3. Trên CN2:
- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`

Tab Security:
- Chọn cơ chế mapping login phù hợp môi trường (khuyến nghị explicit mapping).

Tab Server Options:
- `Data Access` = `True`
- `RPC` = `True`
- `RPC Out` = `True`

Kiểm tra nhanh:
- `Test Connection` hoặc chạy query kiểm tra qua linked server.

---

## 4) B4 - Role/Login/User bằng SSMS UI

Nguyên tắc:
- Role mục tiêu theo DE3: `NGANHANG`, `CHINHANH`, `KHACHHANG`.
- Tạo Login ở mức server.
- Map User vào DB tương ứng và add role membership.

## 4.1 Tạo role (database level)
1. Mở DB (`NGANHANG_PUB`, subscriber DB khi cần).
2. `Security` -> `Roles` -> `Database Roles` -> `New Database Role...`
3. Tạo 3 role: `NGANHANG`, `CHINHANH`, `KHACHHANG`.

## 4.2 Tạo login (server level)
1. `Security` -> `Logins` -> `New Login...`
2. Tạo login theo tài khoản demo/vận hành (ví dụ `ADMIN_NH`, `NV_BT`, `KH_DEMO`).
3. Chọn SQL Server authentication và password policy theo lab.

## 4.3 User mapping + role membership
1. Mở properties của login.
2. Tab `User Mapping`:
   - Tick database đích.
   - Chọn default schema `dbo`.
   - Tick role membership phù hợp (`NGANHANG`/`CHINHANH`/`KHACHHANG`).

## 4.4 Cấp quyền đối tượng bằng UI
1. Mở role -> `Properties` -> `Securables`.
2. Add các object SP runtime cần `EXECUTE`.
3. Với bảng dữ liệu trực tiếp, áp dụng deny/grant theo matrix bảo mật.

Lưu ý:
- Matrix quyền cuối sẽ chốt ở Giai đoạn C/D khi refactor xong tuyến SP auth/account.

---

## 5) Bằng chứng cần lưu sau khi làm xong

1. Ảnh Distributor configuration hoàn tất.
2. Ảnh list publication + subscription active trong Replication Monitor.
3. Ảnh Linked Server node trên Publisher/CN1/CN2.
4. Ảnh login mapping và role membership cho 3 role.
5. Log/truy vấn xác minh:
   - Publication status
   - Query qua LINK
   - `sp_DangNhap` trả role hợp lệ

---

## 6) Liên kết checklist thực thi

Sau khi thao tác UI, điền trạng thái vào:
- `docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md`
