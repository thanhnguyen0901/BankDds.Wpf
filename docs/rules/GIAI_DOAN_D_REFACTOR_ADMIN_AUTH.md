# Giai đoạn D - Refactor Admin/Auth sang SP Runtime Mới

Ngày: 2026-03-11

## 1) Phạm vi đã làm

1. Chuyển `UserRepository` sang SP auth/account:
- `sp_TaoTaiKhoan`
- `sp_XoaTaiKhoan`
- `sp_DoiMatKhau`
- `sp_DanhSachNhanVien`

2. Bỏ flow CRUD `NGUOIDUNG` trong repository:
- không còn gọi `USP_AddUser`, `SP_UpdateUser`, `SP_SoftDeleteUser`, `SP_RestoreUser`, `SP_GetAllUsers`.

3. Cập nhật `AdminViewModel`:
- Create login mới qua flow `AddUserAsync` (SP `sp_TaoTaiKhoan`).
- Edit = reset password (NGANHANG-only).
- Delete = xóa login cứng (NGANHANG-only).
- Restore soft-delete bị vô hiệu hóa.

4. Cập nhật validator/model/interface để phù hợp SQL-login mode.

---

## 2) File đã đổi

- `BankDds.Infrastructure/Data/Repositories/UserRepository.cs`
- `BankDds.Infrastructure/Data/UserService.cs`
- `BankDds.Wpf/ViewModels/AdminViewModel.cs`
- `BankDds.Wpf/Views/AdminView.xaml`
- `BankDds.Core/Validators/UserValidator.cs`
- `BankDds.Core/Models/User.cs`
- `BankDds.Core/Interfaces/IUserRepository.cs`
- `BankDds.Core/Interfaces/IUserService.cs`

---

## 3) Thay đổi hành vi chính

1. Không còn soft-delete account trong admin:
- Trước: soft-delete/restore theo `TrangThaiXoa`.
- Nay: delete login cứng qua `sp_XoaTaiKhoan`.

2. Danh sách user:
- Trước: lấy từ `NGUOIDUNG`.
- Nay: lấy từ `sp_DanhSachNhanVien` (database role members).

3. Reset password:
- Theo `sp_DoiMatKhau`, admin flow chỉ hỗ trợ reset kiểu quản trị.

---

## 4) Điểm còn lại

1. Transitional SP vẫn còn trong SQL package:
- `sql/runtime/03_transitional_user_crud_sp.sql`
- Sẽ xóa ở phase cleanup khi xác nhận không còn dependency.

2. Chưa có test tích hợp SQL live trong phiên này:
- Cần test trực tiếp bằng account `NGANHANG`, `CHINHANH`, `KHACHHANG`.
