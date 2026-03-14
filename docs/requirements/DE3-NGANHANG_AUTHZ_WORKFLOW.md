# DE3 NGANHANG - Chot Rule Phan Quyen va Workflow Dang Nhap

Ngay chot: 2026-03-13  
Pham vi: Rule phan quyen, tao account, scope du lieu, workflow login/session.

## 1. Muc tieu

Tai lieu nay la moc tham chieu de dong bo SQL + app flow.  
Moi thay doi code/schema lien quan phan quyen phai bam tai lieu nay.

## 2. 3 nhom nguoi dung

1. `NganHang`
2. `ChiNhanh`
3. `KhachHang`

Moi account chi thuoc **1 nhom duy nhat**.

## 3. Scope du lieu theo role

1. `NganHang`
- Duoc chon bat ky chi nhanh nao de xem bao cao.
- Truy van du lieu tren phan manh tuong ung chi nhanh duoc chon.
- Khong la nhom tac nghiep CRUD hang ngay.

2. `ChiNhanh`
- Toan quyen tac nghiep tren chi nhanh dang dang nhap.
- Khong duoc thao tac du lieu tac nghiep cua chi nhanh khac.

3. `KhachHang`
- Chi duoc xem sao ke tai khoan cua chinh minh.
- Khong duoc tao account.

## 4. Rule tao account (chot trien khai)

1. `NganHang` duoc tao account nhom `NganHang`.
2. `ChiNhanh` duoc tao:
- account nhom `ChiNhanh` (cung nhom)
- account nhom `KhachHang` cho khach thuoc chi nhanh cua minh.
3. `KhachHang` khong duoc tao account.

Ghi chu:
- Rule "ChiNhanh tao duoc KhachHang" la bo sung nghiep vu de he thong van hanh duoc onboarding khach hang.

## 5. Quy tac business bat buoc

1. Mot khach hang chi dang ky thuoc 1 chi nhanh duy nhat (`KHACHHANG.MACN`).
2. Mot khach hang co the mo nhieu tai khoan o nhieu chi nhanh (`TAIKHOAN.MACN` co the khac nhau).
3. `KhachHang` khi login duoc xem tat ca tai khoan thuoc chinh CMND cua minh (ke ca tai khoan o chi nhanh khac).
4. `ChiNhanh` chi xem/thao tac tai khoan cua khach ma `TAIKHOAN.MACN` thuoc chi nhanh dang dang nhap.

## 6. NGUOIDUNG va y nghia mapping

Bang `NGUOIDUNG` dung de map context nghiep vu cho app:

1. `Username`: dinh danh account dang nhap.
2. `UserGroup`: 0/1/2 tuong ung `NganHang/ChiNhanh/KhachHang`.
3. `DefaultBranch`: chi nhanh mac dinh/session scope.
4. `EmployeeId`: bat buoc voi `ChiNhanh`.
5. `CustomerCMND`: bat buoc voi `KhachHang`.
6. `TrangThaiXoa`: trang thai hoat dong.

## 7. Workflow login/session (chot)

1. User nhap `username/password`.
2. SQL auth thanh cong.
3. Goi `sp_DangNhap` de lay `TENNHOM`, `MACN`, `EmployeeId`, `CustomerCMND`.
4. App dung session tu ket qua SQL.
5. App an/hien va chan chuc nang theo role.
6. Moi truy van/ghi du lieu dua tren role + scope session, khong dua vao input tu do.

## 8. Checklist dung/sai nhanh

1. Login `KhachHang` khong vao duoc man hinh CRUD.
2. Login `KhachHang` chi xem duoc sao ke tai khoan cua chinh minh.
3. Login `ChiNhanh` khong thao tac du lieu tac nghiep chi nhanh khac.
4. Login `NganHang` chon duoc chi nhanh de xem bao cao.
5. Tao account `KhachHang` chi duoc phep boi `ChiNhanh` (theo bo sung nghiep vu da chot).
