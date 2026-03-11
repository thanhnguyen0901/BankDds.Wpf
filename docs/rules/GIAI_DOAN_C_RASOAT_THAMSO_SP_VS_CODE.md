# Giai đoạn C - Rà soát tham số SP vs code (C4)

Ngày: 2026-03-11

Mục tiêu:
- Xác nhận các SP runtime trong package mới khớp tham số mà code C# đang gửi.
- Ghi nhận các SP transitional để xử lý ở Phase D.

---

## 1) Kết quả tổng quan

- Nhóm SP nghiệp vụ chính: **khớp tham số** với code hiện tại.
- Nhóm auth/account SP chuẩn QLVT: **đã có đầy đủ** trong runtime package.
- Nhóm user CRUD cũ: giữ tạm trong script transitional để không làm gãy app trước Phase D.

---

## 2) Đối chiếu quan trọng

1. `SP_GetCustomersByBranch`
- Code gọi:
  - `CustomerRepository` gửi `@MACN`
  - `ReportRepository` gửi `@BranchCode`
- SP runtime:
  - định nghĩa đồng thời `@MACN`, `@BranchCode`, gộp bằng `COALESCE`
- Kết luận: **PASS**

2. `SP_GetAccountsOpenedInPeriod`
- Code gửi `@FromDate`, `@ToDate`, `@BranchCode`
- SP runtime cùng tên tham số
- Kết luận: **PASS**

3. `SP_GetTransactionSummary`
- Code gửi `@FromDate`, `@ToDate`, `@BranchCode`
- SP runtime cùng tên tham số
- Kết luận: **PASS**

4. `sp_DangNhap`
- Code yêu cầu trả về cột: `MANV`, `HOTEN`, `TENNHOM`, `MACN`
- SP runtime auth đang trả đúng 4 cột
- Kết luận: **PASS**

5. `USP_AddUser` / `SP_UpdateUser` / `SP_SoftDeleteUser` / `SP_RestoreUser`
- Code `UserRepository` còn gọi các SP này
- Đã tách riêng vào `03_transitional_user_crud_sp.sql`
- Kết luận: **TRANSITIONAL** (sẽ loại bỏ ở Phase D)

---

## 3) File liên quan

- `sql/runtime/01_runtime_business_report_branch_sp.sql`
- `sql/runtime/02_runtime_auth_account_sp.sql`
- `sql/runtime/03_transitional_user_crud_sp.sql`
- `sql/runtime/90_cleanup_unused_nonruntime_sp.sql`

---

## 4) Hành động tiếp theo

Phase D sẽ refactor code Admin/UserRepository sang:
- `sp_TaoTaiKhoan`
- `sp_XoaTaiKhoan`
- `sp_DoiMatKhau`

Khi hoàn tất, loại bỏ script transitional user CRUD.
