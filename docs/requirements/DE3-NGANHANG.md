```md
# ĐỀ TÀI MÔN CƠ SỞ DỮ LIỆU PHÂN TÁN

## ĐỀ TÀI SỐ 3 – NGÂN HÀNG

### Nội dung
Quản lý các tài khoản và giao dịch của khách hàng.

---

## I. CƠ SỞ DỮ LIỆU NGANHANG

### 1. Bảng CHINHANH

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| MACN | nChar(10) | Primary key – mã chi nhánh |
| TENCN | nvarchar(100) | Unique, Not Null |
| DIACHI | nvarchar(100) | |
| SoDT | nvarchar(15) | |

---

### 2. Bảng KHACHHANG

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| HO | nvarchar(50) | Not Null |
| TEN | nvarchar(10) | Not Null |
| DIACHI | nvarchar(100) | Not Null |
| CMND | nChar(10) | Primary key |
| NGAYCAP | Date | Not Null |
| SODT | nvarchar(15) | |
| PHAI | nvarchar(3) | 'Nam' hoặc 'Nữ' |
| MACN | nChar(10) | Foreign Key |

---

### 3. Bảng NHANVIEN

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| MANV | nChar(10) | Primary key |
| HO | nvarchar(50) | Not Null |
| TEN | nvarchar(10) | Not Null |
| DIACHI | nvarchar(100) | Not Null |
| CMND | nChar(10) | Unique, Not Null |
| PHAI | nvarchar(3) | 'Nam' hoặc 'Nữ' |
| SODT | nvarchar(15) | |
| MACN | nChar(10) | Foreign Key, Not Null |
| TrangThaiXoa | Int | Default = 0 |

---

### 4. Bảng TAIKHOAN

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| SOTK | nChar(9) | Primary key |
| CMND | nChar(10) | Foreign Key (KHACHHANG), Not Null |
| SODU | Money | Not Null, >= 0 |
| MACN | nChar(10) | Foreign Key |
| NGAYMOTK | DateTime | Ngày mở tài khoản |

---

### 5. Bảng GD_GOIRUT (Gửi tiền / Rút tiền)

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| MAGD | Int | Primary key, Identity |
| SOTK | nChar(9) | Foreign Key, Not Null |
| LOAIGD | nChar(2) | 'GT' (Gửi tiền), 'RT' (Rút tiền) |
| NGAYGD | DateTime | Default = GetDate(), Not Null |
| SOTIEN | Money | Default = 100000, Check >= 100000 |
| MANV | nChar(10) | Foreign Key – NV lập giao dịch |

---

### 6. Bảng GD_CHUYENTIEN

| Field Name | Data Type | Constraint |
|-----------|-----------|------------|
| MAGD | Int | Primary key, Identity |
| SOTK_CHUYEN | nChar(9) | Foreign Key, Not Null |
| NGAYGD | DateTime | Default = GetDate(), Not Null |
| SOTIEN | Money | Check > 0 |
| SOTK_NHAN | nChar(9) | Foreign Key, Not Null |
| MANV | nChar(10) | Foreign Key – NV lập giao dịch |

---

## II. YÊU CẦU PHÂN TÁN CƠ SỞ DỮ LIỆU

Giả sử ngân hàng có **2 chi nhánh**:

- **BENTHANH**
- **TANDINH**

Cơ sở dữ liệu **NGANHANG** được phân tán thành **3 phân mảnh**:

- **Server1**:  
  - Lưu thông tin khách hàng đăng ký tại **BENTHANH**
  - Các giao dịch thực hiện tại **BENTHANH**

- **Server2**:  
  - Lưu thông tin khách hàng đăng ký tại **TANDINH**
  - Các giao dịch thực hiện tại **TANDINH**

- **Server3 (TraCuu)**:  
  - Chứa thông tin **khách hàng của cả 2 chi nhánh**

### Ghi chú nghiệp vụ
- Mỗi khách hàng **chỉ đăng ký tại 1 chi nhánh**
- Một khách hàng **có thể mở nhiều tài khoản tại các chi nhánh khác nhau**

---

## III. CHỨC NĂNG CHƯƠNG TRÌNH

### A. Cập nhật

1. Cập nhật Khách hàng  
2. Mở tài khoản cho khách hàng (thiết kế theo **SubForm**)  
3. Cập nhật Nhân viên:
   - Thêm
   - Xóa
   - Ghi
   - Chuyển nhân viên qua chi nhánh khác
4. Cập nhật giao dịch:
   - Gửi tiền
   - Rút tiền
   - Chuyển tiền

#### Ghi chú
- Sinh viên tự thiết kế giao diện
- Tất cả các Form phải có:
  - Thêm
  - Xóa
  - Phục hồi
  - Ghi
  - Thoát

---

### B. Liệt kê – Thống kê

1. **Sao kê giao dịch của 1 tài khoản trong khoảng thời gian**
   - Tham số: `@TuNgay`, `@DenNgay`
   - Yêu cầu hiển thị:
     - Số dư đầu kỳ (`@TuNgay - 1`)
     - Danh sách giao dịch
     - Số dư cuối kỳ

**Ví dụ:**

```

Số dư đến ngày @TuNgay - 1: 10.000.000

Số dư đầu | Ngày | Loại GD | Số tiền | Số dư sau
10.000.000 | 01/03/22 | GT | 5.000.000 | 15.000.000
15.000.000 | 07/03/22 | CT | 7.000.000 | 8.000.000

Số dư tới ngày @DenNgay: 8.000.000

```

2. Liệt kê các tài khoản mở trong khoảng thời gian:
   - Theo chi nhánh
   - Tất cả chi nhánh

3. Liệt kê khách hàng:
   - Theo từng chi nhánh
   - Sắp xếp tăng dần theo **Họ + Tên**

---

## IV. QUẢN TRỊ – PHÂN QUYỀN

Chương trình có **3 nhóm người dùng**:

### 1. Nhóm NganHang
- Được chọn **bất kỳ chi nhánh nào** để xem báo cáo
- Truy vấn dữ liệu trên phân mảnh tương ứng
- Được tạo login mới **cùng nhóm**

### 2. Nhóm ChiNhanh
- Toàn quyền trên **chi nhánh đã đăng nhập**
- Được tạo login mới **cùng nhóm**

### 3. Nhóm KhachHang
- Chỉ được xem **sao kê tài khoản của chính mình**
- Không được tạo login

### Quản lý đăng nhập
- Chương trình cho phép:
  - Tạo login
  - Gán password
  - Phân quyền cho login
- Căn cứ quyền đăng nhập để xác định:
  - Được làm việc trên phân mảnh nào
  - Hay được làm việc trên tất cả các phân mảnh

---

## V. LƯU Ý

- Sinh viên tự kiểm tra các ràng buộc dữ liệu
- **Bắt buộc thực hiện truy vấn trên SQL Server**
```
