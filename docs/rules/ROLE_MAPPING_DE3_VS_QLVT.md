# Role Mapping: DE3-NGANHANG vs QLVT

Ngày cập nhật: 2026-03-11

## 1) Mục đích
Chốt mapping vai trò để migration theo hướng QLVT nhưng vẫn đúng đề DE3-NGANHANG.

---

## 2) So sánh role

| Nguồn | Role | Quyền cốt lõi |
|---|---|---|
| QLVT | `CongTy` | Xem dữ liệu theo chi nhánh chọn, xem báo cáo, tạo tài khoản cùng role |
| QLVT | `ChiNhanh` | Full CRUD trong chi nhánh đăng nhập, tạo tài khoản `ChiNhanh` + `User` |
| QLVT | `User` | Full CRUD trong chi nhánh đăng nhập, không tạo tài khoản |
| DE3-NGANHANG | `NGANHANG` | Chọn bất kỳ chi nhánh để xem báo cáo/dữ liệu; tạo login cùng nhóm |
| DE3-NGANHANG | `CHINHANH` | Toàn quyền trong chi nhánh đăng nhập; tạo login cùng nhóm |
| DE3-NGANHANG | `KHACHHANG` | Chỉ xem sao kê tài khoản của chính mình; không tạo login |

---

## 3) Mapping áp dụng cho project NGANHANG

| Mapping tư tưởng từ QLVT | Áp dụng thực tế ở DE3-NGANHANG |
|---|---|
| `CongTy` | `NGANHANG` |
| `ChiNhanh` | `CHINHANH` |
| `User` | **Không dùng** (thay bằng `KHACHHANG` theo đề DE3) |

Kết luận bắt buộc:
- Không thêm role `USER` vào NGANHANG.
- Không sao chép nguyên matrix quyền của `User` từ QLVT.
- Kiến trúc hạ tầng + runtime theo QLVT, nhưng quyền nghiệp vụ phải theo DE3.

---

## 4) Rule phân quyền mục tiêu (để dùng cho refactor)

1. `NGANHANG`
- Đăng nhập ở Publisher, có thể chọn chi nhánh để xem dữ liệu/báo cáo.
- Không CRUD dữ liệu nghiệp vụ chi nhánh (theo rule đang áp dụng của project).
- Được tạo tài khoản theo policy DE3 (cùng nhóm; các ngoại lệ phải ghi rõ trong UI/SQL).

2. `CHINHANH`
- Không được switch sang chi nhánh khác.
- Full CRUD trong chi nhánh đăng nhập.
- Được tạo tài khoản theo policy DE3 (cùng nhóm).

3. `KHACHHANG`
- Chỉ xem sao kê tài khoản của chính mình.
- Không tạo tài khoản.

---

## 5) Kiểm tra chấp nhận

- Login mỗi role trả về đúng `TENNHOM` và `MACN` (khi áp dụng).
- UI hiển thị đúng chức năng theo role.
- GRANT EXECUTE SQL khớp với nút/flow trên UI.
