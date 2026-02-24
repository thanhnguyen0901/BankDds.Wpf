# DE3 – NGÂN HÀNG: Trạng Thái Triển Khai Hiện Tại

> **Ngày tạo:** 24/02/2026  
> **Mục đích:** Báo cáo tổng hợp dự án đồ án CSDL Phân Tán – Đề 3 (Ngân Hàng)  
> **Phạm vi quét:** Toàn bộ mã nguồn C#/XAML, 10 script SQL, tất cả tài liệu Markdown

---

## 1. Repository Map

### 1.1 Cấu trúc thư mục chính

| Thư mục | Vai trò |
|---|---|
| `BankDds.Core/` | Domain layer — Models, Interfaces (IRepository + IService), Validators (FluentValidation) |
| `BankDds.Infrastructure/` | Data access layer — SQL repositories (Dapper/ADO.NET), InMemory repositories, Services, Security, Configuration |
| `BankDds.Wpf/` | Presentation layer — WPF Views/ViewModels (Caliburn.Micro + Autofac), appsettings, Converters, Export services |
| `sql/` | 10 script SQL cho toàn bộ pipeline phân tán (schema → SP → security → replication → linked server → subscriber) |
| `docs/requirements/` | Đề bài gốc DE3 (`DE3-NGANHANG.md`) |
| `docs/sql/` | Hướng dẫn setup SSMS (`SETUP_MS_SQL_DISTRIBUTED_GUIDE.md`) |

### 1.2 Entrypoint chính

- **WPF startup:** `BankDds.Wpf/App.xaml` → `App.xaml.cs` → `new AppBootstrapper()` → `DisplayRootViewForAsync<MainShellViewModel>()`
- **DI container:** `BankDds.Wpf/AppBootstrapper.cs` — Autofac, đăng ký toàn bộ service/repository/viewmodel
- **DataMode switch:** `appsettings.json` → `"DataMode": "Sql"` hoặc `"InMemory"` — quyết định dùng SQL hay InMemory repository

### 1.3 SQL script — thứ tự chạy

Xem `sql/00_readme_execution_order.md`:

| # | Script | Chạy trên | Mô tả |
|---|---|---|---|
| 1 | `01_publisher_create_db.sql` | Publisher | Tạo DB `NGANHANG_PUB`, FULL recovery, enable merge publish |
| 2 | `02_publisher_schema.sql` | Publisher | Tạo bảng (CHINHANH, KHACHHANG, NHANVIEN, TAIKHOAN, GD_GOIRUT, GD_CHUYENTIEN), NGUOIDUNG, SEQ_MANV, FK, index |
| 3 | `03_publisher_sp_views.sql` | Publisher | 1 view + ~50 SP (Customer/Employee/Account/Transaction/Report/Auth/Branch) |
| 4 | `04_publisher_security.sql` | Publisher | 3 role (NGANHANG/CHINHANH/KHACHHANG), DENY table, GRANT SP, sp_DangNhap, sp_TaoTaiKhoan, seed login |
| 5 | `05_replication_setup_merge.sql` | Publisher | Distributor + 3 Publication + Articles + Row/Join filter + 3 Push Subscription + Snapshot Agent |
| 6 | `06_linked_servers.sql` | **Mỗi instance** | LINK0/LINK1/LINK2 — tự phát hiện @@SERVERNAME |
| 7 | `07_subscribers_create_db.sql` | **Mỗi subscriber** | Tạo shell DB (NGANHANG_BT / NGANHANG_TD / NGANHANG_TRACUU) |
| 8 | `08_subscribers_post_replication_fixups.sql` | **Mỗi subscriber** | Role, DENY/GRANT, SP bảo mật cục bộ, seed login, xóa view _ALL, TraCuu read-only |
| – | `99_run_all.sql` | Publisher | SQLCMD `:r` tổng hợp script 01–06 (Phase 1) |

---

## 2. Distributed Database Topology (As Implemented)

### 2.1 Sơ đồ 4 instance

```
┌─────────────────────────────────────────────────────────────┐
│  DESKTOP-JBB41QU (default instance)                         │
│  Vai trò: Publisher + Distributor                           │
│  Database: NGANHANG_PUB                                     │
│  Chứa: TẤT CẢ bảng, TẤT CẢ SP/view, NGUOIDUNG (hub-only)│
│  SQL Agent: ✅ có (bắt buộc cho Snapshot Agent)              │
└────────┬─────────────┬─────────────┬────────────────────────┘
         │             │             │
    Push Sub      Push Sub      Push Sub
         │             │             │
    ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
    │SQLSERVER2│   │SQLSERVER3│   │SQLSERVER4│
    │CN1-BT   │   │CN2-TD   │   │TraCuu   │
    │NGANHANG │   │NGANHANG │   │NGANHANG │
    │_BT      │   │_TD      │   │_TRACUU  │
    └─────────┘   └─────────┘   └─────────┘
```

### 2.2 Chi tiết từng node

#### Publisher/Distributor — `DESKTOP-JBB41QU` / `NGANHANG_PUB`

- **Bảng hub-only (KHÔNG replicate):** `NGUOIDUNG` (login/user metadata), `SEQ_MANV` (sequence)
- **Bảng replicate:** `CHINHANH`, `KHACHHANG`, `NHANVIEN`, `TAIKHOAN`, `GD_GOIRUT`, `GD_CHUYENTIEN`
- **View:** `view_DanhSachPhanManh` (dùng cho login dropdown)
- **SP:** ~50 SP nghiệp vụ + 5 SP bảo mật (sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau, sp_DanhSachNhanVien)
- **Evidence:** `sql/02_publisher_schema.sql`, `sql/03_publisher_sp_views.sql`, `sql/04_publisher_security.sql`

#### Subscriber CN1 — `SQLSERVER2` / `NGANHANG_BT`

- **Nhận qua Merge Replication (PUB_NGANHANG_BT):** 6 bảng + 50 SP (proc schema only) + 1 view
- **Row filter:** `MACN = N'BENTHANH'` trên KHACHHANG, NHANVIEN, TAIKHOAN; join filter trên GD_GOIRUT, GD_CHUYENTIEN qua TAIKHOAN
- **Bảo mật cục bộ (script 08):** 3 role + DENY table + GRANT SP + SP bảo mật cục bộ + seed login
- **Evidence:** `sql/05_replication_setup_merge.sql` (Part C — PUB_NGANHANG_BT)

#### Subscriber CN2 — `SQLSERVER3` / `NGANHANG_TD`

- **Giống CN1** nhưng filter `MACN = N'TANDINH'` (PUB_NGANHANG_TD)
- **Evidence:** `sql/05_replication_setup_merge.sql` (Part C — PUB_NGANHANG_TD)

#### Subscriber TraCuu — `SQLSERVER4` / `NGANHANG_TRACUU`

- **Nhận qua Merge Replication (PUB_TRACUU):** chỉ 2 bảng — `CHINHANH` (download-only) + `KHACHHANG` (active only, download-only)
- **KHÔNG nhận SP, view, hay bảng giao dịch**
- **Bảo mật (script 08 Section 7):** DENY INSERT/UPDATE/DELETE, GRANT SELECT trên KHACHHANG + CHINHANH
- **Evidence:** `sql/05_replication_setup_merge.sql` (Part C — PUB_TRACUU), `sql/08_subscribers_post_replication_fixups.sql` (Section 7)

---

## 3. Replication Workflow (QLVT-style)

### 3.1 Replication Components Status

| Component | Trạng thái | Chi tiết | File tham chiếu |
|---|---|---|---|
| **Distributor configuration** | ✅ Implemented | Publisher tự làm Distributor (`sp_adddistributor`, `sp_adddistributiondb`, `sp_adddistpublisher`) | `sql/05_replication_setup_merge.sql` Part A |
| **Publication creation** | ✅ Implemented | 3 publication: `PUB_NGANHANG_BT`, `PUB_NGANHANG_TD`, `PUB_TRACUU` | Part C |
| **Articles: tables** | ✅ Implemented | 6 bảng cho BT/TD, 2 bảng cho TRACUU (CHINHANH + KHACHHANG) | Part C |
| **Articles: stored procedures** | ✅ Implemented | 50 SP (proc schema only) cho BT/TD; KHÔNG replicate SP cho TRACUU | Part C |
| **Articles: views** | ✅ Implemented | `view_DanhSachPhanManh` cho BT/TD | Part C |
| **Row filters (CN1/CN2)** | ✅ Implemented | `MACN = N'BENTHANH'` / `MACN = N'TANDINH'` trên KHACHHANG, NHANVIEN, TAIKHOAN; join filter trên GD_ tables | Part C |
| **Column filters (TraCuu)** | ✅ Implemented | Column filter không dùng — thay vào đó dùng subset filter `TrangThaiXoa = 0` cho KHACHHANG; download-only cả 2 bảng | Part C |
| **Push subscriptions (CN1/CN2/TraCuu)** | ✅ Implemented | 3 push subscription (frequency_type=64 = autostart/continuous) | Part D |
| **Snapshot Agent run steps** | ✅ Implemented | `sp_startpublication_snapshot` cho cả 3 publication | Part E |
| **SQL Server Agent requirements** | ✅ Documented | Agent phải chạy trên Publisher default instance | `sql/00_readme_execution_order.md` |

### 3.2 "SP Authoring Rule" Compliance

| Yêu cầu | Trạng thái | Evidence |
|---|---|---|
| SP được viết **CHỈ trên Publisher** | ✅ Tuân thủ | Tất cả SP nằm trong `sql/03_publisher_sp_views.sql`, chạy `USE NGANHANG_PUB` duy nhất |
| Subscriber **KHÔNG tự tạo SP/view nghiệp vụ** | ✅ Tuân thủ | Script 07 chỉ tạo shell DB trống; SP đến qua Snapshot Agent; `00_readme` ghi rõ: "KHÔNG chạy 03 trực tiếp trên CN1, CN2 hoặc TraCuu" |
| SP bảo mật cục bộ được tạo **riêng trên subscriber** (script 08) | ✅ Hợp lý | `sp_DangNhap`, `sp_TaoTaiKhoan`, v.v. dùng `sp_addlogin` (server-level) nên không thể replicate — tạo cục bộ bởi script 08 |
| Workflow cập nhật SP: edit Publisher → update article → snapshot → verify | ✅ Documented | `sql/00_readme_execution_order.md` ghi: "SP chỉ tạo trên Publisher. Chúng được truyền đến CN1/CN2 qua Merge Replication (proc schema only)" |

---

## 4. Linked Server Strategy

### 4.1 Các linked server

| Instance | LINK1 trỏ đến | LINK0 trỏ đến | LINK2 trỏ đến |
|---|---|---|---|
| **Publisher** (default) | SQLSERVER2 (CN1) | SQLSERVER4 (TraCuu) | SQLSERVER3 (CN2) |
| **CN1** (SQLSERVER2) | SQLSERVER3 (CN2) | SQLSERVER4 (TraCuu) | — |
| **CN2** (SQLSERVER3) | SQLSERVER2 (CN1) | SQLSERVER4 (TraCuu) | — |

- **File:** `sql/06_linked_servers.sql`
- **Tự phát hiện** `@@SERVERNAME` → chỉ tạo link phù hợp instance hiện tại
- **Credential:** `sa` / `Password!123` (lab default)
- **Options bật:** `rpc`, `rpc out`, `data access`

### 4.2 Lý do dùng Linked Server

**LINK1 = "chi nhánh kia" (symmetric naming):**  
- `SP_CrossBranchTransfer` Path B trên CN1 dùng `LINK1.NGANHANG_TD.dbo.SP_AddToAccount` → gọi sang CN2
- `SP_CrossBranchTransfer` Path B trên CN2 dùng `LINK1.NGANHANG_BT.dbo.SP_AddToAccount` → gọi sang CN1
- Cùng tên `LINK1` trên cả 2 CN → **SP code không cần sửa**, chạy giống nhau cả 2 nơi

**LINK0 = TraCuu:** Dùng cho tra cứu tổng hợp (nếu cần query từ subscriber)

**LINK2:** Chỉ trên Publisher, trỏ sang CN2 (hỗ trợ report cross-branch từ coordinator)

### 4.3 Naming consistency

Script `06_linked_servers.sql` dùng naming convention nhất quán:
- `LINK0` = luôn là TraCuu (server tra cứu)
- `LINK1` = luôn là "chi nhánh khác" (đối xứng trên CN1/CN2)
- `LINK2` = chỉ trên Publisher (CN2)

> **Lưu ý:** `docs/sql/SETUP_MS_SQL_DISTRIBUTED_GUIDE.md` (tài liệu cũ hơn) dùng tên `SERVER1/SERVER2/SERVER3`. Script hiện tại đã chuyển sang `LINK0/LINK1/LINK2` để tránh nhầm với tên instance. Đây là **sự khác biệt giữa doc cũ và script mới** — doc cũ nên được cập nhật.

---

## 5. SQL Security & Roles (Course Requirement)

### 5.1 Ba nhóm vai trò

| Role DB | UserGroup (app) | Mô tả | File |
|---|---|---|---|
| `NGANHANG` | NganHang (0) | Quản trị toàn hệ thống — xem tất cả branch, tạo mọi loại login | `sql/04_publisher_security.sql` Section 1 |
| `CHINHANH` | ChiNhanh (1) | Nhân viên chi nhánh — full CRUD trên branch mình, tạo login cùng nhóm | Section 1 |
| `KHACHHANG` | KhachHang (2) | Khách hàng — chỉ xem sao kê tài khoản của mình | Section 1 |

### 5.2 Tạo login

- **SP:** `sp_TaoTaiKhoan` (`sql/04_publisher_security.sql` Section 5)
  - Tạo SQL login (`CREATE LOGIN`)
  - Tạo DB user (`CREATE USER`)
  - Gán vào role phù hợp (`ALTER ROLE ... ADD MEMBER`)
  - Kiểm tra quyền: CHINHANH không thể tạo NGANHANG login
- **Trên Subscriber:** `sp_TaoTaiKhoan` được tạo cục bộ bởi `sql/08_subscribers_post_replication_fixups.sql` Section 4 (vì dùng `sp_addlogin` ở server-level)

### 5.3 Login resolver

- **SP:** `sp_DangNhap` (`sql/04_publisher_security.sql` Section 4)
  - Trả về: `MANV` (SYSTEM_USER), `HOTEN` (USER_NAME()), `TENNHOM` (tên role ưu tiên cao nhất: NGANHANG > CHINHANH > KHACHHANG)
  - `GRANT EXECUTE ON sp_DangNhap TO PUBLIC` — mọi login đều gọi được
  - App gọi SP này NGAY SAU khi `SqlConnection.OpenAsync()` thành công → xác định role

### 5.4 GRANT/DENY enforcement

- **DENY SELECT/INSERT/UPDATE/DELETE** trên TẤT CẢ bảng gốc cho cả 3 role → buộc truy cập qua SP (ownership chaining)
  - File: `sql/04_publisher_security.sql` Section 2
- **GRANT EXECUTE** chi tiết theo ma trận 52 SP × 3 role
  - File: `sql/04_publisher_security.sql` Section 3
  - Ví dụ: `NGANHANG` được EXECUTE tất cả; `CHINHANH` được branch-level ops; `KHACHHANG` chỉ SP_GetAccountStatement, SP_GetBranches, v.v.
- **Trên Subscriber:** DENY/GRANT được áp dụng cục bộ bởi `sql/08_subscribers_post_replication_fixups.sql` Section 2–3

### 5.5 Seed logins demo

| Login | Password | Role | File |
|---|---|---|---|
| `ADMIN_NH` | `Admin@123` | NGANHANG | `sql/04_publisher_security.sql` Section 9 |
| `NV_BT` | `NhanVien@123` | CHINHANH | Section 9 |
| `KH_DEMO` | `KhachHang@123` | KHACHHANG | Section 9 |

> Đồng bộ sang subscriber bởi `sql/08_subscribers_post_replication_fixups.sql` Section 5.

### 5.6 App-only authorization (bổ sung)

Ngoài SQL GRANT/DENY, app còn có `AuthorizationService` (`BankDds.Infrastructure/Security/AuthorizationService.cs`) kiểm tra:
- `CanAccessBranch()`, `CanModifyBranch()` — NGANHANG view-only, CHINHANH CRUD branch mình
- `CanCreateUser()` — CHINHANH chỉ tạo cùng nhóm
- `CanPerformTransactions()` — KhachHang không được thao tác
- Đây là **lớp phòng vệ UI** — SQL Server vẫn enforce GRANT/DENY ở tầng DB

---

## 6. Application Runtime Workflow (WPF)

### 6.1 Khởi động

1. `App.xaml.cs` → `new AppBootstrapper()`
2. `AppBootstrapper.Configure()`:
   - Đọc `appsettings.json` + `appsettings.Development.json` + Environment Variables
   - Đọc `"DataMode"` → `"Sql"` (production) hoặc `"InMemory"` (dev/test)
   - Đăng ký Autofac: IConnectionStringProvider, IUserSession, IAuthorizationService, Validators, Repositories (InMemory hoặc Sql), Services, AuthService, ViewModels
3. `OnStartup` → `DisplayRootViewForAsync<MainShellViewModel>()` → hiện `MainShellView.xaml` (Shell)
4. `MainShellViewModel.OnInitializeAsync()` → `ShowLoginAsync()` → hiện `LoginView`

### 6.2 Luồng Login

```
LoginView
    │
    ├── OnActivate: LoadBranchesFromPublisherAsync()
    │   └── Connect Publisher (sa/123) → SELECT MACN, TENCN FROM view_DanhSachPhanManh
    │       → Populate branch dropdown (BENTHANH, TANDINH)
    │       → Fallback hardcoded nếu Publisher unreachable
    │
    └── User click Login:
        ├── AuthService.LoginAsync(username, password)
        │   ├── GetPublisherConnectionForLogin(username, password)
        │   ├── SqlConnection.OpenAsync() → SQL Server verify credential
        │   ├── EXEC sp_DangNhap → MANV, HOTEN, TENNHOM
        │   ├── Map TENNHOM → UserGroup (NganHang/ChiNhanh/KhachHang)
        │   └── SetSqlLoginCredentials(username, password) → inject vào tất cả connection string sau này
        │
        ├── Xác định PermittedBranches theo role:
        │   ├── NganHang → tất cả branch codes (từ view_DanhSachPhanManh)
        │   ├── ChiNhanh → [result.DefaultBranch] (branch mình)
        │   └── KhachHang → [result.DefaultBranch]
        │
        ├── UserSession.SetSession(username, displayName, userGroup, selectedBranch, permittedBranches, ...)
        │
        └── Navigate → HomeViewModel
```

### 6.3 Sau khi Login — Connection Routing

- **Branch CRUD (Customer/Employee/Account/Transaction):**
  - `SqlXxxRepository.GetConnectionString()` → `IConnectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch)`
  - Ví dụ: nếu SelectedBranch = `BENTHANH` → connect `SQLSERVER2/NGANHANG_BT`
  - Mọi SP `EXEC` trên subscriber DB tương ứng

- **GetAll operations (NGANHANG role):**
  - `SqlAccountRepository.GetAllAccountsAsync()` → `IConnectionStringProvider.GetPublisherConnection()` → connect Publisher NGANHANG_PUB
  - Publisher có đầy đủ dữ liệu nhờ Merge Replication hai chiều

- **Reports:**
  - `SqlReportRepository` → `GetPublisherConnection()` cho báo cáo cross-branch
  - Account statement: chạy trên branch subscriber (GetConnectionStringForBranch)

- **Branch switching (NGANHANG role):**
  - NGANHANG user chọn branch khác → `HomeViewModel` ghi nhận `SelectedBranch`
  - Tất cả repository call tự động route sang connection string mới
  - **Lưu ý quan trọng:** Hiện tại app **CHƯA có UI chuyển branch sau login** — SelectedBranch được set lúc login và giữ nguyên. Đây là một GAP (xem Section 7).

### 6.4 Các SP được gọi cho key operations

| Operation | SP | Chạy trên |
|---|---|---|
| Danh sách KH theo CN | `SP_GetCustomersByBranch` | Branch subscriber |
| Tất cả KH | `SP_GetAllCustomers` | Publisher |
| Thêm KH | `SP_AddCustomer` | Branch subscriber |
| Sửa KH | `SP_UpdateCustomer` | Branch subscriber |
| Xóa KH | `SP_DeleteCustomer` | Branch subscriber |
| Phục hồi KH | `SP_RestoreCustomer` | Branch subscriber |
| Danh sách TK theo CN | `SP_GetAccountsByBranch` | Branch subscriber |
| Thêm TK | `SP_AddAccount` | Branch subscriber |
| Đóng/Mở TK | `SP_CloseAccount` / `SP_ReopenAccount` | Branch subscriber |
| Gửi tiền | `SP_Deposit` | Branch subscriber |
| Rút tiền | `SP_Withdraw` | Branch subscriber |
| Chuyển tiền cùng CN | `SP_CrossBranchTransfer` Path A | Branch subscriber |
| Chuyển tiền liên CN | `SP_CrossBranchTransfer` Path B | Branch subscriber → LINK1 (distributed) |
| Sao kê TK | `SP_GetAccountStatement` | Branch subscriber |
| TK mở trong kỳ | `SP_GetAccountsOpenedInPeriod` | Publisher (cross-branch) hoặc Branch |
| Tổng hợp GD | `SP_GetTransactionSummary` | Publisher hoặc Branch |
| Login | `sp_DangNhap` | Publisher |
| Tạo login | `sp_TaoTaiKhoan` | Publisher hoặc Subscriber |
| Đổi mật khẩu | `sp_DoiMatKhau` | Publisher hoặc Subscriber |

---

## 7. What's Done vs TODO

### ✅ Đã hoàn thành

**SQL/Database:**
- [x] Schema đầy đủ 6 bảng + NGUOIDUNG + SEQ_MANV (script 02)
- [x] ~50 SP + 1 view trên Publisher (script 03)
- [x] 3 role + DENY table + GRANT SP matrix đầy đủ (script 04)
- [x] sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau (script 04)
- [x] Merge Replication: Distributor + 3 Publication + Articles (bảng + SP + view) + Row/Join filter + Push Subscription + Snapshot Agent (script 05)
- [x] Linked Server LINK0/LINK1/LINK2 symmetric naming (script 06)
- [x] Subscriber shell DB (script 07)
- [x] Post-replication security fixup subscriber (script 08)
- [x] Master orchestrator 99_run_all.sql (SQLCMD mode)
- [x] SP chuyển tiền liên CN qua distributed transaction (SP_CrossBranchTransfer Path B)
- [x] Seed login demo (ADMIN_NH, NV_BT, KH_DEMO)

**Application (C#/WPF):**
- [x] Clean 3-layer architecture (Core / Infrastructure / Wpf)
- [x] Dual DataMode: InMemory (dev) + Sql (production)
- [x] SQL repositories cho ALL entities (SqlCustomerRepository, SqlAccountRepository, SqlEmployeeRepository, SqlTransactionRepository, SqlUserRepository, SqlReportRepository, SqlBranchRepository)
- [x] ConnectionStringProvider: credential injection, branch routing, publisher connection
- [x] AuthService: SQL login → sp_DangNhap → role resolution
- [x] AuthorizationService: role-based access control (CanAccessBranch, CanModifyBranch, v.v.)
- [x] Full CRUD UI: Customer, Account, Employee, Transaction, Report, Branch, User Admin
- [x] SubForm pattern: Customer → Account (đúng yêu cầu DE3 §III.A.2)
- [x] Employee transfer branch (đúng yêu cầu DE3 §III.A.3)
- [x] Giao dịch: Gửi / Rút / Chuyển tiền + validation (đúng DE3 §III.A.4)
- [x] Sao kê TK đúng format DE3 §III.B.1 (số dư đầu, danh sách GD, số dư cuối)
- [x] Export PDF/Excel cho tất cả báo cáo (iText7 + ClosedXML)
- [x] FluentValidation cho tất cả entity
- [x] Login dropdown load từ Publisher `view_DanhSachPhanManh`
- [x] 3 nhóm user đúng DE3 §IV (NganHang, ChiNhanh, KhachHang)

**Documentation:**
- [x] README.md chi tiết kiến trúc + cách build/run
- [x] DE3-NGANHANG.md (đề bài gốc)
- [x] SETUP_MS_SQL_DISTRIBUTED_GUIDE.md (hướng dẫn SSMS)
- [x] 00_readme_execution_order.md (thứ tự chạy script)

### ❌ Thiếu / Chưa hoàn thành

| # | Mục | Mức rủi ro | Chi tiết |
|---|---|---|---|
| 1 | **Branch switching UI sau login (NGANHANG role)** | 🔴 HIGH | DE3 §IV.1 yêu cầu NGANHANG "được chọn bất kỳ chi nhánh nào để xem báo cáo". Hiện tại `LoginViewModel` set `SelectedBranch` lúc login, nhưng **HomeView KHÔNG có dropdown chuyển branch**. NGANHANG user chỉ xem được branch đã chọn lúc login. Cần thêm ComboBox branch vào HomeView + logic cập nhật `UserSession.SelectedBranch`. |
| 2 | **DefaultBranch resolution cho CHINHANH login** | 🟡 MEDIUM | `AuthService.LoginAsync()` nhận `result.DefaultBranch` từ `sp_DangNhap`, nhưng SP hiện tại trả về `SYSTEM_USER` và `USER_NAME()`, **không trả DefaultBranch**. Backend `LoginViewModel` dùng `result.DefaultBranch` — giá trị này có thể rỗng. Cần kiểm tra sp_DangNhap trả đủ thông tin branch hay không nếu login qua subscriber. |
| 3 | **Seed data (INSERT bảng nghiệp vụ)** | 🟡 MEDIUM | Script 02 tạo bảng nhưng **không tìm thấy seed INSERT** cho CHINHANH, KHACHHANG, NHANVIEN, TAIKHOAN, GD_*. Demo cần có dữ liệu mẫu. Cần tạo script seed hoặc thêm vào cuối script 02. |
| 4 | **sp_DangNhap trên subscriber trả DefaultBranch** | 🟡 MEDIUM | Khi CHINHANH login qua subscriber (NGANHANG_BT), cần biết branch code = "BENTHANH". Script 08 tạo sp_DangNhap cục bộ — cần xác nhận SP trả branch info. |
| 5 | **Tài liệu SETUP_MS_SQL_DISTRIBUTED_GUIDE.md outdated** | 🟡 MEDIUM | Doc cũ dùng tên `SERVER1/SERVER2/SERVER3`, nhưng script hiện tại dùng `LINK0/LINK1/LINK2`. DB name trong doc là `NGANHANG` nhưng script dùng `NGANHANG_PUB`. Cần update cho nhất quán. |
| 6 | **App connection string cho Publisher** | 🟢 LOW | `appsettings.json` ghi `Database=NGANHANG_PUB` nhưng doc SETUP guide ghi `NGANHANG`. Script 01 tạo `NGANHANG_PUB`. → appsettings đúng, doc cần sửa. |
| 7 | **TraCuu không có trong app connection** | 🟡 MEDIUM | `appsettings.json` chỉ có Branch_BENTHANH và Branch_TANDINH. Không có connection cho TraCuu. Nếu demo cần query TraCuu từ app (ví dụ báo cáo khách hàng toàn hệ thống), cần thêm. |
| 8 | **MSDTC verification/doc** | 🟢 LOW | SP_CrossBranchTransfer Path B dùng distributed transaction qua Linked Server. MSDTC phải bật trên Windows. Doc SETUP guide có đề cập (XI) nhưng scripts không tự bật. |
| 9 | **Test script / verification script** | 🟢 LOW | Không có script test riêng (ví dụ: insert test data → run SP → verify output). Mỗi script có section verification nhưng không có automated test. |

### ⚠️ Rủi ro có thể bị trừ điểm

| Rủi ro | Mức | Lý do |
|---|---|---|
| Không có branch switching UI cho NGANHANG | 🔴 HIGH | Yêu cầu rõ ràng DE3 §IV.1: "Được chọn bất kỳ chi nhánh nào để xem báo cáo" |
| DefaultBranch không trả về từ sp_DangNhap subscriber | 🟡 MEDIUM | App cần biết branch code để route connection; nếu thiếu sẽ crash |
| Seed data thiếu | 🟡 MEDIUM | Demo trống sẽ không thuyết phục |
| Naming mismatch doc cũ vs script mới | 🟢 LOW | Gây nhầm lẫn khi giảng viên đọc doc |

---

## 8. How to Run (Setup & Verification)

### 8.1 Prerequisites

| Yêu cầu | Chi tiết |
|---|---|
| SQL Server | 4 instances trên cùng 1 máy: default (`DESKTOP-JBB41QU`), `SQLSERVER2`, `SQLSERVER3`, `SQLSERVER4` |
| SQL Server Agent | **BẮT BUỘC** chạy trên default instance (cho Snapshot Agent) |
| SQL Server Browser | Bật (để named instances SQLSERVER2/3/4 connect được) |
| Mixed Mode Auth | Bật trên tất cả 4 instances |
| MSDTC | Bật Network DTC Access, Allow Inbound/Outbound (cho chuyển tiền liên CN) |
| .NET 8 SDK | Để build app WPF |
| sa password | `Password!123` (lab default — dùng cho linked server + initial connection) |

### 8.2 Execution Order

#### Phase 1 — Publisher (chạy trên `DESKTOP-JBB41QU` default instance)

```powershell
# Option A: Chạy từng script trong SSMS
# Connect: DESKTOP-JBB41QU → mở từng file → F5

# Option B: SQLCMD mode (tự động)
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\99_run_all.sql"
```

Thứ tự chi tiết nếu chạy thủ công:

| Bước | RUN ON | Script | Thao tác |
|---|---|---|---|
| 1 | `DESKTOP-JBB41QU` | `sql/01_publisher_create_db.sql` | Tạo DB NGANHANG_PUB |
| 2 | `DESKTOP-JBB41QU` | `sql/02_publisher_schema.sql` | Tạo bảng + sequence + constraint |
| 3 | `DESKTOP-JBB41QU` | `sql/03_publisher_sp_views.sql` | Tạo SP + view |
| 4 | `DESKTOP-JBB41QU` | `sql/04_publisher_security.sql` | Tạo role + DENY/GRANT + seed login |
| 5 | `DESKTOP-JBB41QU` | `sql/05_replication_setup_merge.sql` | Distributor + 3 Publication + Subscription + Snapshot |
| 6a | `DESKTOP-JBB41QU` | `sql/06_linked_servers.sql` | Linked Server trên Publisher |

#### Phase 1b — Linked Server trên CN1/CN2

| Bước | RUN ON | Script |
|---|---|---|
| 6b | `DESKTOP-JBB41QU\SQLSERVER2` | `sql/06_linked_servers.sql` |
| 6c | `DESKTOP-JBB41QU\SQLSERVER3` | `sql/06_linked_servers.sql` |

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\06_linked_servers.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\06_linked_servers.sql"
```

#### Phase 2 — Subscriber Shells (chạy trên từng subscriber)

| Bước | RUN ON | Script |
|---|---|---|
| 7a | `SQLSERVER2` | `sql/07_subscribers_create_db.sql` |
| 7b | `SQLSERVER3` | `sql/07_subscribers_create_db.sql` |
| 7c | `SQLSERVER4` | `sql/07_subscribers_create_db.sql` |

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

#### ⏳ Chờ Snapshot Agent (~1–5 phút)

Kiểm tra trạng thái trên Publisher:

```sql
USE distribution;
SELECT publication, status, start_time, duration
FROM dbo.MSsnapshot_history
ORDER BY start_time DESC;
```

Hoặc xem trong SSMS → Replication Monitor → chờ **"Completed"** cho cả 3 publication.

#### Phase 3 — Post-Replication Security (SAU KHI Snapshot xong)

| Bước | RUN ON | Script |
|---|---|---|
| 8a | `SQLSERVER2` | `sql/08_subscribers_post_replication_fixups.sql` |
| 8b | `SQLSERVER3` | `sql/08_subscribers_post_replication_fixups.sql` |
| 8c | `SQLSERVER4` | `sql/08_subscribers_post_replication_fixups.sql` |

```powershell
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\08_subscribers_post_replication_fixups.sql"
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\08_subscribers_post_replication_fixups.sql"
```

#### Phase 4 — Chạy App

```powershell
cd d:\Projects\SV\CSDL-PT\BankDds.Wpf
dotnet build
dotnet run --project BankDds.Wpf\BankDds.Wpf.csproj
```

Kiểm tra `appsettings.json`:
```json
{
  "DataMode": "Sql",
  "ConnectionStrings": {
    "Publisher": "Server=DESKTOP-JBB41QU;Database=NGANHANG_PUB;TrustServerCertificate=True;",
    "Branch_BENTHANH": "Server=DESKTOP-JBB41QU\\SQLSERVER2;Database=NGANHANG_BT;TrustServerCertificate=True;",
    "Branch_TANDINH": "Server=DESKTOP-JBB41QU\\SQLSERVER3;Database=NGANHANG_TD;TrustServerCertificate=True;"
  }
}
```

Login thử: `ADMIN_NH` / `Admin@123` → NganHang role.

### 8.3 Verification Checklist

#### A. Replication tồn tại và healthy

```sql
-- RUN ON: Publisher (DESKTOP-JBB41QU)
USE NGANHANG_PUB;

-- Kiểm tra publication
EXEC sp_helppublication;
-- Expected: PUB_NGANHANG_BT, PUB_NGANHANG_TD, PUB_TRACUU

-- Kiểm tra subscription
EXEC sp_helpsubscription;
-- Expected: 3 push subscription (BT → SQLSERVER2, TD → SQLSERVER3, TRACUU → SQLSERVER4)

-- Kiểm tra articles
EXEC sp_helparticle @publication = 'PUB_NGANHANG_BT';
-- Expected: 6 bảng + 50 SP + 1 view
```

#### B. Row filter đúng

```sql
-- RUN ON: SQLSERVER2 (CN1)
USE NGANHANG_BT;
SELECT DISTINCT MACN FROM KHACHHANG;
-- Expected: chỉ 'BENTHANH'

SELECT DISTINCT MACN FROM NHANVIEN;
-- Expected: chỉ 'BENTHANH'

-- RUN ON: SQLSERVER3 (CN2)
USE NGANHANG_TD;
SELECT DISTINCT MACN FROM KHACHHANG;
-- Expected: chỉ 'TANDINH'
```

#### C. TraCuu nhận đúng dữ liệu

```sql
-- RUN ON: SQLSERVER4
USE NGANHANG_TRACUU;

-- Kiểm tra bảng tồn tại
SELECT name FROM sys.tables ORDER BY name;
-- Expected: CHINHANH, KHACHHANG (+ MSmerge_* metadata)

-- Kiểm tra KHACHHANG có cả 2 CN
SELECT MACN, COUNT(*) AS cnt FROM KHACHHANG GROUP BY MACN;
-- Expected: BENTHANH (n rows), TANDINH (m rows)

-- Kiểm tra chỉ active customer
SELECT DISTINCT TrangThaiXoa FROM KHACHHANG;
-- Expected: chỉ 0

-- SP KHÔNG tồn tại trên TraCuu
SELECT name FROM sys.procedures WHERE name LIKE 'SP_%';
-- Expected: không có SP nghiệp vụ (chỉ MSmerge_* internal)
```

#### D. SP update trên Publisher replicate xuống

```sql
-- RUN ON: Publisher
USE NGANHANG_PUB;
-- 1. Sửa SP (ví dụ thêm comment)
CREATE OR ALTER PROCEDURE SP_GetCustomersByBranch @MACN nChar(10)
AS
BEGIN
    -- Updated version test
    SET NOCOUNT ON;
    SELECT * FROM KHACHHANG WHERE MACN = @MACN AND TrangThaiXoa = 0;
END;
GO

-- 2. Chạy lại Snapshot Agent
EXEC sp_startpublication_snapshot @publication = 'PUB_NGANHANG_BT';
-- Chờ snapshot hoàn thành

-- 3. Verify trên subscriber
-- RUN ON: SQLSERVER2
USE NGANHANG_BT;
EXEC sp_helptext 'SP_GetCustomersByBranch';
-- Expected: thấy dòng "-- Updated version test"
```

#### E. Linked Server hoạt động

```sql
-- RUN ON: SQLSERVER2 (CN1)
EXEC sp_linkedservers;
-- Expected: LINK0 (TraCuu), LINK1 (CN2)

-- Test distributed query
SELECT TOP 3 * FROM LINK1.NGANHANG_TD.dbo.TAIKHOAN;
-- Expected: trả về tài khoản của TANDINH
```

#### F. Security enforcement

```sql
-- RUN ON: SQLSERVER2 (CN1)
USE NGANHANG_BT;

-- Test SP access
EXECUTE AS USER = 'NV_BT';
EXEC SP_GetCustomersByBranch @MACN = N'BENTHANH';
-- Expected: thành công

-- Test direct table access bị chặn
SELECT TOP 1 * FROM dbo.KHACHHANG;
-- Expected: FAIL ("The SELECT permission was denied")

REVERT;
```

---

> **Kết luận:** Dự án đã hoàn thành **phần lớn** yêu cầu DE3 bao gồm schema, SP, replication, security, và app WPF. Rủi ro lớn nhất là thiếu **branch switching UI** cho NGANHANG role và cần **seed data** để demo. Các script SQL được cấu trúc tốt, idempotent, và tuân thủ quy tắc "SP chỉ tạo trên Publisher".
