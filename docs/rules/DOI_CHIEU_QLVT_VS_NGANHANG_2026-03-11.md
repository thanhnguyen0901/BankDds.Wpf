# Đối chiếu QLVT vs NGANHANG (2026-03-11)

## 1) Phạm vi đối chiếu
Tài liệu này đối chiếu project NGANHANG theo 2 tài liệu chuẩn:
- `docs/rules/CHECKLIST_REFACTOR_NGANHANG_PASS_FAIL.md`
- `docs/rules/PHAN_TICH_QLVT_DE_CAP_NHAT_NGANHANG.md`

Mục tiêu: xác nhận hướng triển khai hiện tại có bám đúng bản chất QLVT hay chưa.

---

## 2) Kết luận tổng quan
- Trạng thái chung: **Đúng phần lớn, chưa đạt hoàn toàn chuẩn QLVT**.
- Ước lượng mức bám chuẩn: **~70-80%**.
- Điểm đúng: tách được runtime app khỏi luồng tự dựng hạ tầng trong C#, login theo `sp_DangNhap`, role-based UI, nghiệp vụ chính đi qua SP.
- Điểm lệch chính: luồng tạo account chưa chuẩn `sp_TaoTaiKhoan`, một số quyền UI/SQL chưa khớp, vẫn còn query trực tiếp ở vài chỗ.

---

## 3) Bảng đối chiếu theo nhóm checklist

### 3.1 Nhóm ARC (Kiến trúc)
| ID | Trạng thái | Nhận xét | Bằng chứng |
|---|---|---|---|
| ARC-01 | PASS | Runtime app không tự tạo publication/subscription trong C# | `BankDds.Wpf/AppBootstrapper.cs`, scan toàn bộ `.cs` không có `sp_addmergepublication` |
| ARC-02 | PASS | Runtime chỉ tiêu thụ linked server đã setup; không dựng linked server trong app | `BankDds.Infrastructure/Data/Repositories/TransactionRepository.cs` |
| ARC-03 | PASS | Không có startup logic C# tự cấp toàn bộ quyền hạ tầng | `BankDds.Wpf/AppBootstrapper.cs` |
| ARC-04 | PASS | Có tài liệu setup phân tán tương đối đầy đủ | `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md`, `sql/00_readme_execution_order.md` |
| ARC-05 | PASS | SP runtime tập trung nghiệp vụ (customer/account/employee/transaction/report/auth) | `sql/03_publisher_sp_views.sql`, `sql/04_publisher_security.sql` |

Ghi chú ARC:
- Theo tinh thần QLVT, hạ tầng thường làm qua SSMS UI.
- Hiện NGANHANG đang dùng script T-SQL để setup replication/linked server (vẫn chấp nhận được về kỹ thuật, nhưng lệch tinh thần "UI-first" của QLVT).

---

### 3.2 Nhóm AUTH (Đăng nhập/phân quyền UI)
| ID | Trạng thái | Nhận xét | Bằng chứng |
|---|---|---|---|
| AUTH-01/03/05 | PASS (code-level) | Đăng nhập đi qua `sp_DangNhap`, map role về `NganHang/ChiNhanh/KhachHang` | `BankDds.Infrastructure/Security/AuthService.cs`, `BankDds.Wpf/ViewModels/LoginViewModel.cs` |
| AUTH-02/04/06 | PARTIAL | Có role-based UI và service authorization, nhưng có điểm chưa khớp SQL permission | `BankDds.Wpf/ViewModels/HomeViewModel.cs`, `BankDds.Infrastructure/Security/AuthorizationService.cs` |
| AUTH-07/08 | PASS (code-level) | Có xử lý lỗi login sai/password sai và fallback branch | `BankDds.Infrastructure/Security/AuthService.cs`, `BankDds.Wpf/ViewModels/LoginViewModel.cs` |

Lệch quan trọng:
- UI cho phép `CHINHANH` vào Admin và thực hiện một số thao tác, nhưng SQL không cấp `SP_RestoreUser` cho `CHINHANH`.
  - UI: `BankDds.Wpf/ViewModels/HomeViewModel.cs`, `BankDds.Wpf/ViewModels/AdminViewModel.cs`
  - SQL: `sql/04_publisher_security.sql` (có `GRANT SP_RestoreUser TO NGANHANG`, không có cho `CHINHANH`)
  - SQL subscriber fixup (legacy): `sql/archive/08_subscribers_post_replication_fixups.sql`

---

### 3.3 Nhóm ACC (Luồng tạo tài khoản runtime)
| ID | Trạng thái | Nhận xét | Bằng chứng |
|---|---|---|---|
| ACC-01..05 | FAIL (so với chuẩn QLVT) | App hiện dùng `USP_AddUser/SP_UpdateUser/...` trên bảng `NGUOIDUNG`, chưa dùng chuẩn `sp_TaoTaiKhoan` để tạo SQL login/user/role như QLVT | `BankDds.Infrastructure/Data/Repositories/UserRepository.cs`, `sql/04_publisher_security.sql` |

Chi tiết:
- Code đang gọi:
  - `USP_AddUser`
  - `SP_UpdateUser`
  - `SP_SoftDeleteUser`
  - `SP_RestoreUser`
- Nhưng mô hình QLVT kỳ vọng:
  - `sp_TaoTaiKhoan`
  - `sp_XoaTaiKhoan`
  - `sp_DoiMatKhau`
  - `sp_DanhSachNhanVien`

---

### 3.4 Nhóm DIST/LINK (hạ tầng phân tán + linked server)
| ID | Trạng thái | Nhận xét | Bằng chứng |
|---|---|---|---|
| DIST-01..06 | PARTIAL | Có đầy đủ script setup Distributor/Publication/Subscription/Linked Server; chưa xác nhận PASS runtime vì chưa chạy test DB thực tế trong phiên này | `sql/archive/05_replication_setup_merge.sql`, `sql/archive/06_linked_servers.sql`, `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md` |
| LINK-01..04 | PARTIAL | Có thiết kế LINK0/LINK1/LINK2 và SP dùng linked server; chưa chạy test SQL live để chấm pass/fail cuối | `sql/archive/06_linked_servers.sql`, `sql/03_publisher_sp_views.sql` (`SP_CrossBranchTransfer`) |

---

### 3.5 Nhóm BIZ/REP/SAFE (nghiệp vụ, báo cáo, an toàn dữ liệu)
| ID | Trạng thái | Nhận xét | Bằng chứng |
|---|---|---|---|
| BIZ-01..04 | PARTIAL | Code + SP thể hiện đúng hướng (chuyển liên chi nhánh, validate, transaction), nhưng chưa chạy integration test DB nên chưa thể PASS chính thức | `BankDds.Infrastructure/Data/Repositories/TransactionRepository.cs`, `sql/03_publisher_sp_views.sql` |
| REP-01..03 | PARTIAL | Báo cáo đã đi qua SP và có role filtering; cần test dữ liệu thật để chốt | `BankDds.Infrastructure/Data/ReportRepository.cs`, `BankDds.Infrastructure/Data/ReportService.cs` |
| SAFE-01..03 | PARTIAL | SP có `BEGIN DISTRIBUTED TRANSACTION`, rollback/throw; chưa có test failover/mất kết nối live trong phiên này | `sql/03_publisher_sp_views.sql` (`SP_CrossBranchTransfer`) |

---

## 4) Lệch kiến trúc chính so với hướng QLVT

1. Luồng quản trị tài khoản chưa chuẩn QLVT:
- Chưa dùng `sp_TaoTaiKhoan` làm tuyến chính từ app.
- Đang dùng CRUD trên bảng `NGUOIDUNG` qua `USP_AddUser/SP_UpdateUser/...`.

2. Chưa đồng nhất quyền UI và quyền SQL:
- `CHINHANH` có thể vào Admin trên UI, nhưng SQL không cấp đủ quyền cho một số thao tác (ví dụ restore user).

3. Chưa "SP-only runtime" tuyệt đối:
- Vẫn có query trực tiếp ở:
  - `BankDds.Wpf/ViewModels/LoginViewModel.cs` (load `view_DanhSachPhanManh`)
  - `BankDds.Infrastructure/Data/Repositories/CustomerLookupRepository.cs` (SELECT trực tiếp `KHACHHANG`)

4. Hạ tầng phân tán đang theo script hóa:
- Có script tạo replication + linked server đầy đủ.
- Nếu bám tinh thần QLVT "UI-first", cần bổ sung/nhấn mạnh quy trình thao tác SSMS UI tương đương.

---

## 5) Điểm đúng đã có (nên giữ)

1. `sp_DangNhap` + role-based UI đang đúng hướng và rõ ràng.
2. Phần lớn repository nghiệp vụ đã đi qua stored procedure.
3. Có tài liệu setup tổng thể + execution order tương đối chi tiết.
4. Có thiết kế giao dịch liên chi nhánh qua `SP_CrossBranchTransfer` + linked server + distributed transaction.

---

## 6) Kết luận cuối
- Nếu câu hỏi là "hướng triển khai hiện tại có đúng đề và gần QLVT chưa?": **Có, nhưng chưa đạt chuẩn hoàn toàn**.
- Mức cần ưu tiên chỉnh để "đúng chuẩn QLVT" khi bảo vệ:
  1. Chuẩn hóa module Admin theo luồng `sp_TaoTaiKhoan` (thay vì `USP_AddUser` làm tuyến chính).
  2. Đồng bộ lại matrix quyền UI với GRANT thực tế trong SQL.
  3. Quyết định rõ chuẩn runtime: giữ direct query ở lookup/login hay chuyển hết qua SP để nhất quán.
  4. Bổ sung bằng chứng test live cho DIST/LINK/BIZ/REP/SAFE để chuyển từ PARTIAL sang PASS.

---

## 7) Hạn chế của phiên đối chiếu này
- Chưa thực thi smoke/integration test trực tiếp trên các SQL instance trong phiên này.
- `dotnet build` trong môi trường hiện tại trả về FAILED nhưng không phát sinh dòng compile error cụ thể (nhiều khả năng liên quan môi trường SDK/workload).

---

## 8) Bổ sung từ README QLVT (user cung cấp) và tác động đến đánh giá

### 8.1 Link Server pattern QLVT
Thông tin QLVT bổ sung:
- LINK0: phân mảnh hiện tại -> server tra cứu (mảnh 3)
- LINK1: phân mảnh hiện tại -> phân mảnh còn lại
- LINK2: phân mảnh hiện tại -> server gốc
- Server tra cứu không cần LINK vì không tham gia cập nhật dữ liệu.

Đối chiếu NGANHANG:
- **Khớp một phần**:
  - Trên CN1/CN2: dùng LINK0 (-> TraCuu) + LINK1 (-> chi nhánh còn lại), phù hợp mục tiêu giao dịch liên chi nhánh và tra cứu.
  - Trên Publisher: có LINK0/LINK1/LINK2 đầy đủ.
  - TraCuu không phải node cập nhật nghiệp vụ runtime.
- **Khác QLVT ở chi tiết topology**:
  - CN1/CN2 của NGANHANG không tạo LINK2 về Publisher như mô tả QLVT "3 LINK cho mỗi phân mảnh 1&2".
  - Việc này không sai về kỹ thuật nếu nghiệp vụ runtime không cần truy vấn ngược từ CN về Publisher qua LINK2.

Bằng chứng:
- `sql/archive/06_linked_servers.sql`
- `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md`

### 8.2 Cách đẩy Stored Procedure qua Replication
Thông tin QLVT bổ sung:
- Quy trình chuẩn qua SSMS UI: chọn Articles, tick SP cần đẩy, chạy Snapshot Agent.

Đối chiếu NGANHANG:
- **Tương đương về bản chất**, nhưng cách thực hiện đang script hóa:
  - Dùng `sp_addmergearticle` / helper trong script để add SP/view/table vào publication.
  - Sau đó chạy snapshot/sync theo quy trình đã tài liệu hóa.

Bằng chứng:
- `sql/archive/05_replication_setup_merge.sql`
- `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md`

### 8.3 Authorization matrix QLVT vs NGANHANG
Thông tin QLVT bổ sung:
- `CongTy`: xem dữ liệu + báo cáo + tạo login cùng vai trò.
- `ChiNhanh`: full CRUD chi nhánh mình + tạo login `ChiNhanh`, `User`.
- `User`: full CRUD chi nhánh mình, không tạo login.

Đối chiếu NGANHANG:
- Cần phân biệt:
  - **Theo đề DE3-NGANHANG**: nhóm thứ 3 là `KhachHang` (chỉ xem sao kê của chính mình), không phải `User` full CRUD.
  - Vì vậy không thể bê nguyên quyền `User` của QLVT sang NGANHANG.
- Phần cần bám QLVT là **kiến trúc phân tán + luồng login/role-based runtime**, không phải sao chép nguyên ma trận quyền nghiệp vụ khi đề bài khác nhau.

Bằng chứng:
- `docs/requirements/DE3-NGANHANG.md`
- `BankDds.Wpf/ViewModels/HomeViewModel.cs`
- `BankDds.Infrastructure/Security/AuthorizationService.cs`

---

## 9) Kết luận cập nhật sau thông tin README QLVT
- Kết luận trước đó **không thay đổi**:
  - NGANHANG đã đi đúng hướng kiến trúc phân tán/runtime theo QLVT ở mức cao.
  - Nhưng vẫn còn lệch cần sửa để "đúng chuẩn bảo vệ":
    1. Chuẩn hóa luồng quản trị account theo `sp_TaoTaiKhoan` (thay vì chỉ CRUD `NGUOIDUNG`).
    2. Đồng bộ quyền UI và GRANT SQL.
    3. Chốt rõ chuẩn LINK topology (có cần LINK2 trên CN1/CN2 hay không) và ghi rõ lý do kiến trúc.
