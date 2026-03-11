# Kế Hoạch Chuyển Đổi NGANHANG Theo Hướng QLVT (UI-First trên SSMS)

Ngày lập: 2026-03-11  
Mục tiêu: chuyển hiện trạng NGANHANG sang mô hình triển khai giống QLVT:
- Hạ tầng phân tán (Replication/Subscription/Linked Server) thao tác trên SSMS UI.
- Thiết lập group role + login/user nền trên SSMS UI.
- Chỉ giữ SP runtime cho nghiệp vụ và phân quyền lúc chạy app.
- Phân mảnh trong suốt đối với ứng dụng.

Trạng thái triển khai:
- `Giai đoạn A`: **DONE** (2026-03-11)
  - Baseline tag: `baseline-qlvt-ui-first-20260311`
  - Nhánh migration: `migration/qlvt-ui-first`
  - Role mapping: `docs/rules/ROLE_MAPPING_DE3_VS_QLVT.md`
- `Giai đoạn B`: **READY FOR EXECUTION** (2026-03-11)
  - Runbook UI-first: `docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md`
  - Checklist thực thi: `docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md`
  - `sql/00_readme_execution_order.md` đã chuyển sang UI-first, script infra đánh dấu legacy
- `Giai đoạn C`: **DONE (repo scope)** (2026-03-11)
  - Runtime SP package tách riêng: `sql/runtime/`
  - Audit tham số SP vs code: `docs/rules/GIAI_DOAN_C_RASOAT_THAMSO_SP_VS_CODE.md`
  - Transitional SP cũ được tách riêng để chờ remove ở Phase D
- `Giai đoạn D`: **DONE (repo scope)** (2026-03-11)
  - `UserRepository` chuyển sang `sp_TaoTaiKhoan/sp_XoaTaiKhoan/sp_DoiMatKhau/sp_DanhSachNhanVien`
  - `AdminViewModel`/UI chuyển flow soft-delete -> SQL login lifecycle
  - Còn lại: test tích hợp SQL thật + remove transitional SP ở phase cleanup

---

## 1) Nguyên tắc bắt buộc

- Không dùng script để tự động tạo Publication/Subscription/Distributor.
- Không dùng script để tự động dựng Linked Server hàng loạt.
- Không dùng script runtime để thay cho bước setup hạ tầng.
- App chỉ gọi SP nghiệp vụ/runtime và đọc dữ liệu theo role + chi nhánh.
- Topology LINK giữ theo ý tưởng QLVT:
  - `LINK0`: tới TraCuu.
  - `LINK1`: tới chi nhánh còn lại.
  - `LINK2`: dùng ở Publisher; có thể bổ sung ở CN nếu thật sự cần nghiệp vụ.

---

## 2) Hiện trạng cần đổi

1. Repo đang có script setup hạ tầng tự động:
- `sql/05_replication_setup_merge.sql`
- `sql/06_linked_servers.sql`
- `sql/08_subscribers_post_replication_fixups.sql` (phần helper grant/fixup)

2. Module Admin hiện đi theo CRUD bảng `NGUOIDUNG` (`USP_AddUser`, `SP_UpdateUser`, `SP_SoftDeleteUser`, `SP_RestoreUser`) thay vì luồng QLVT `sp_TaoTaiKhoan`/`sp_XoaTaiKhoan`/`sp_DoiMatKhau`.

3. Quyền UI và quyền SQL chưa khớp hoàn toàn (đặc biệt thao tác restore tài khoản).

---

## 3) Checklist triển khai theo giai đoạn

## Giai đoạn A - Khóa baseline và tách phạm vi
- [x] A1. Tag/backup trạng thái hiện tại để rollback an toàn.
- [x] A2. Tạo nhánh migration riêng (không làm trực tiếp trên nhánh demo đang chạy).
- [x] A3. Chốt role theo đề DE3-NGANHANG: `NGANHANG`, `CHINHANH`, `KHACHHANG` (không dùng role `USER` như QLVT vì khác đề).

Kết quả cần có:
- Nhánh migration rõ ràng.
- Biên bản role mapping DE3 vs QLVT.

## Giai đoạn B - Chuyển setup hạ tầng sang SSMS UI (không chạy script infra)
- [x] B1. Viết tài liệu thao tác UI cho Distributor/Publication/Subscription (step-by-step, có ảnh).
- [ ] B2. Thực hiện replication bằng SSMS Wizard:
  - [ ] Tạo Distributor.
  - [ ] Tạo publication cho Publisher.
  - [ ] Chọn article table/view/SP theo phạm vi runtime.
  - [ ] Tạo push subscription tới CN1/CN2/TraCuu.
  - [ ] Chạy Snapshot Agent từ Replication Monitor.
- [ ] B3. Cấu hình Linked Server bằng UI:
  - [ ] CN1: `LINK0 -> TraCuu`, `LINK1 -> CN2`.
  - [ ] CN2: `LINK0 -> TraCuu`, `LINK1 -> CN1`.
  - [ ] Publisher: `LINK0/1/2` theo thiết kế tổng hợp.
- [ ] B4. Setup role/login/user nền bằng SSMS UI (Security -> Logins, DB Security -> Users/Roles).

Kết quả cần có:
- Runbook UI đầy đủ trong docs.
- Không phụ thuộc script `05/06` để dựng infra.

Trạng thái:
- Đã hoàn tất phần tài liệu/chuyển hướng quy trình trong repo.
- Chờ thực thi thủ công trên SSMS môi trường thật để đóng B2/B3/B4.

## Giai đoạn C - Chuẩn hóa SP runtime
- [x] C1. Tách file SP runtime khỏi file setup hạ tầng.
- [x] C2. Chuẩn hóa SP auth/account theo hướng QLVT:
  - [x] `sp_DangNhap`
  - [x] `sp_TaoTaiKhoan`
  - [x] `sp_XoaTaiKhoan`
  - [x] `sp_DoiMatKhau`
  - [x] `sp_DanhSachNhanVien` (nếu UI cấp account theo nhân viên)
- [x] C3. Giữ nhóm SP nghiệp vụ ngân hàng (khách hàng/tài khoản/giao dịch/báo cáo/chuyển liên CN).
- [x] C4. Rà soát tham số SP để khớp code (tránh lệch tên tham số giữa repo và SQL).

Kết quả cần có:
- Bộ script SQL runtime-only rõ ràng, không chứa lệnh tạo replication/linked server.

Artifact:
- `sql/runtime/00_readme_runtime_execution_order.md`
- `sql/runtime/01_runtime_business_report_branch_sp.sql`
- `sql/runtime/02_runtime_auth_account_sp.sql`
- `sql/runtime/03_transitional_user_crud_sp.sql`
- `sql/runtime/90_cleanup_unused_nonruntime_sp.sql`

## Giai đoạn D - Refactor ứng dụng theo runtime SP mới
- [x] D1. Refactor `UserRepository`:
  - [x] Bỏ luồng `USP_AddUser`/`SP_UpdateUser` kiểu CRUD bảng.
  - [x] Chuyển sang gọi `sp_TaoTaiKhoan`/`sp_XoaTaiKhoan`/`sp_DoiMatKhau`.
- [x] D2. Refactor `AdminViewModel` theo luồng tài khoản SQL login/runtime SP.
- [x] D3. Đồng bộ matrix quyền UI với quyền SQL thực tế.
- [x] D4. Chuẩn hóa hành vi theo đề DE3:
  - `NGANHANG`: xem + báo cáo + tạo tài khoản đúng quyền.
  - `CHINHANH`: CRUD trong CN mình + tạo tài khoản đúng quyền.
  - `KHACHHANG`: chỉ xem sao kê của chính mình, không tạo account.

Kết quả cần có:
- UI và SQL permissions không mâu thuẫn.
- Không còn lỗi kiểu "EXECUTE permission denied" do thiết kế lệch.

Artifact:
- `BankDds.Infrastructure/Data/Repositories/UserRepository.cs`
- `BankDds.Infrastructure/Data/UserService.cs`
- `BankDds.Wpf/ViewModels/AdminViewModel.cs`
- `BankDds.Wpf/Views/AdminView.xaml`
- `BankDds.Core/Validators/UserValidator.cs`
- `docs/rules/GIAI_DOAN_D_REFACTOR_ADMIN_AUTH.md`

## Giai đoạn E - Cleanup script/document cũ
- [ ] E1. Chuyển `sql/05_replication_setup_merge.sql`, `sql/06_linked_servers.sql` sang thư mục `sql/archive` hoặc đánh dấu deprecated.
- [ ] E2. Cập nhật `sql/00_readme_execution_order.md` thành luồng UI-first.
- [ ] E3. Cập nhật `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md` để dùng Wizard/UI làm đường chính.
- [ ] E4. Cập nhật docs test để phản ánh quy trình mới.

Kết quả cần có:
- Người mới clone repo làm theo docs mà không cần chạy script infra tự động.

## Giai đoạn F - Kiểm thử chấp nhận
- [ ] F1. Test login theo 3 role.
- [ ] F2. Test chuyển chi nhánh (NGANHANG chỉ xem, không CRUD).
- [ ] F3. Test CRUD đúng phạm vi role.
- [ ] F4. Test tạo/xóa/đổi mật khẩu tài khoản qua SP auth.
- [ ] F5. Test chuyển tiền liên chi nhánh qua LINK + MSDTC.
- [ ] F6. Test báo cáo theo khoảng thời gian + phạm vi branch.
- [ ] F7. Test tình huống lỗi network/link/site down (không bẩn dữ liệu).

Kết quả cần có:
- Biên bản PASS/FAIL theo checklist.

---

## 4) Danh sách SP hiện tại: giữ/xóa/đổi hướng

## 4.1 SP đề xuất xóa ngay (không dùng runtime app hiện tại)
Các SP dưới đây có trong SQL nhưng app không gọi trực tiếp:
- `SP_CreateTransferTransaction` (không thấy app/SP khác gọi)
- `sp_SafeAddMergeProcArticle` (helper setup replication)
- `sp_SafeAddMergeViewArticle` (helper setup replication)
- `sp_SafeGrantExec` (helper setup grant hậu replication)

Hành động:
- [x] Xóa khỏi runtime package.
- [ ] Nếu cần lưu lịch sử, chuyển vào `sql/archive`.

Ghi chú:
- Đã tách runtime package mới ở `sql/runtime/`.
- Đã thêm script cleanup: `sql/runtime/90_cleanup_unused_nonruntime_sp.sql`.

## 4.2 SP đang có nhưng sẽ chuyển thành tuyến chính (hiện chưa được app gọi)
- `sp_TaoTaiKhoan`
- `sp_XoaTaiKhoan`
- `sp_DoiMatKhau`
- `sp_DanhSachNhanVien`

Hành động:
- [x] Refactor code Admin/UserRepository để gọi các SP này.

## 4.3 SP đang được app gọi nhưng dự kiến xóa sau khi refactor Admin
- `USP_AddUser`
- `SP_UpdateUser`
- `SP_SoftDeleteUser`
- `SP_RestoreUser`
- `SP_GetUser`
- `SP_GetAllUsers`

Điều kiện xóa:
- [ ] Hoàn tất chuyển module Admin sang luồng `sp_TaoTaiKhoan`/`sp_XoaTaiKhoan`/`sp_DoiMatKhau`.
- [ ] Có SP/list thay thế phục vụ danh sách tài khoản nếu UI cần.

## 4.4 SP nghiệp vụ giữ lại
Giữ các nhóm SP nghiệp vụ cốt lõi:
- Khách hàng: `SP_Get/Add/Update/Delete/Restore...`
- Nhân viên: `SP_Get/Add/Update/Delete/Restore/Transfer...`
- Tài khoản: `SP_Get/Add/Update/Delete/Close/Reopen...`
- Giao dịch: `SP_Deposit`, `SP_Withdraw`, `SP_CrossBranchTransfer`, các SP thống kê ngày
- Báo cáo: `SP_GetAccountStatement`, `SP_GetAccountsOpenedInPeriod`, `SP_GetTransactionSummary`
- Đăng nhập: `sp_DangNhap`

---

## 5) Định nghĩa "phân mảnh trong suốt" cho NGANHANG

Đạt khi đồng thời thỏa:
- App không biết chi tiết server vật lý khi thao tác nghiệp vụ thường ngày.
- Chọn chi nhánh trên UI chỉ làm thay đổi ngữ cảnh kết nối/branch filter.
- Nghiệp vụ liên site (ví dụ chuyển tiền liên CN) do SP xử lý nội bộ qua LINK/MSDTC.
- Không có logic app tự union dữ liệu thủ công từ nhiều site cho nghiệp vụ chuẩn.

---

## 6) Tiêu chí Done của đợt chuyển đổi

- [ ] Toàn bộ bước hạ tầng phân tán được mô tả và thực hiện bằng SSMS UI.
- [ ] Không còn dùng script infra auto-create như đường chính.
- [ ] Module Admin chạy đúng hướng SP auth/account kiểu QLVT.
- [ ] SP không dùng đã được loại khỏi runtime package (hoặc archive có chú thích).
- [ ] Bộ test PASS cho role, giao dịch liên CN, báo cáo, và xử lý lỗi.

---

## 7) Thứ tự triển khai đề xuất (để review dễ)

1. Giai đoạn B (UI infra)  
2. Giai đoạn C (SQL runtime cleanup)  
3. Giai đoạn D (refactor app Admin/Auth)  
4. Giai đoạn E (cleanup docs/scripts)  
5. Giai đoạn F (test acceptance + chốt PASS/FAIL)
