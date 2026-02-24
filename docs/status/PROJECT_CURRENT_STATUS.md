# DE3 – NGÂN HÀNG: Trạng Thái Triển Khai Hiện Tại

> **Ngày tạo:** 2025-02-24  
> **Build status:** ✅ 0 errors, 0 warnings (`dotnet build BankDds.Wpf.sln`)  
> **DataMode hiện tại trong `appsettings.json`:** `Sql`

---

## 1. Bản Đồ Repository

### 1.1 Cấu trúc thư mục chính

| Thư mục | Mục đích |
|---|---|
| `BankDds.Core/` | Domain layer — interfaces, models, validators. Không phụ thuộc ngoài FluentValidation. |
| `BankDds.Infrastructure/` | Data access layer — SQL repositories (ADO.NET), InMemory repositories, services, auth, config. |
| `BankDds.Wpf/` | WPF UI layer — ViewModels, Views, Shell, Services, Converters. Caliburn.Micro + Autofac DI. |
| `sql/` | **Tất cả SQL scripts** — schema, SP, security, replication, linked server, seed data. |
| `docs/requirements/` | Đề bài gốc (`DE3-NGANHANG.md`). |
| `docs/sql/` | Hướng dẫn setup MS SQL phân tán (`SETUP_MS_SQL_DISTRIBUTED_GUIDE.md`). |
| `docs/demo/` | Hướng dẫn demo tính năng (`customer_lookup_demo.md`). |
| `docs/tests/` | Script kiểm tra (`sp_dangnhap_test.sql`, `seed_data_verification.sql`, `login_branch_resolution.md`). |
| `docs/status/` | Báo cáo này. |

### 1.2 Entrypoint chính

| File | Vai trò |
|---|---|
| `BankDds.Wpf/App.xaml` | WPF Application — bootstraps `AppBootstrapper`. |
| `BankDds.Wpf/AppBootstrapper.cs` | Autofac DI container, Caliburn.Micro config, DataMode routing (`InMemory` / `Sql`). |
| `BankDds.Wpf/Shell/MainShellView.xaml` | Root shell window — hosts `LoginViewModel` → `HomeViewModel`. |
| `BankDds.Wpf/appsettings.json` | Config: connection strings, DataMode, transaction limits. |

### 1.3 SQL folder — thứ tự chạy

| # | Script | Chạy trên | Mô tả |
|---|---|---|---|
| 01 | `01_publisher_create_db.sql` | Publisher | Tạo DB `NGANHANG_PUB`, FULL recovery, enable merge publish |
| 02 | `02_publisher_schema.sql` | Publisher | 7 bảng + sequence + rowguid cho merge replication |
| 03 | `03_publisher_sp_views.sql` | Publisher | 1 view + 50 SP (tất cả nghiệp vụ) |
| 04 | `04_publisher_security.sql` | Publisher | 3 roles, DENY/GRANT, sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau, sp_DanhSachNhanVien, seed logins |
| 04b | `04b_publisher_seed_data.sql` | Publisher | Data mẫu: 2 chi nhánh, 4 nhân viên, 6 khách hàng, 8 tài khoản, 7+ giao dịch, NGUOIDUNG |
| 05 | `05_replication_setup_merge.sql` | Publisher | Distributor + 3 publications + articles + row/join filters + 3 push subscriptions + snapshot |
| 06 | `06_linked_servers.sql` | **Mỗi instance** | LINK0/LINK1/LINK2 — auto-detect `@@SERVERNAME` |
| 07 | `07_subscribers_create_db.sql` | **Mỗi subscriber** | Shell DB rỗng trên CN1/CN2/TraCuu |
| 08 | `08_subscribers_post_replication_fixups.sql` | **Mỗi subscriber** | Roles, DENY/GRANT, security SPs, login sync, TraCuu hardening |
| 99 | `99_run_all.sql` | Publisher | SQLCMD orchestrator — runs 01–06, hướng dẫn 07–08 |

---

## 2. Kiến Trúc CSDL Phân Tán (Đã Triển Khai)

### 2.1 Topology tổng quan

```
┌─────────────────────────────────────────────────────────────────────┐
│  Publisher / Distributor (server gốc)                               │
│  DESKTOP-JBB41QU (default instance)                                 │
│  DB: NGANHANG_PUB                                                   │
│  Chứa: TẤT CẢ hàng từ mọi chi nhánh + NGUOIDUNG + SEQ_MANV       │
│  Merge Replication: 3 publications → 3 push subscriptions           │
├─────────┬──────────────────┬──────────────────┬─────────────────────┤
│         │ PUB_NGANHANG_BT  │ PUB_NGANHANG_TD  │ PUB_TRACUU         │
│         ▼                  ▼                  ▼                    │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐        │
│  │ CN1/SQLSERVER2│  │ CN2/SQLSERVER3│  │ TraCuu/SQLSERVER4  │        │
│  │ NGANHANG_BT  │  │ NGANHANG_TD  │  │ NGANHANG_TRACUU    │        │
│  │ MACN=BENTHANH│  │ MACN=TANDINH │  │ CHINHANH+KHACHHANG │        │
│  │ 6 bảng+50 SP │  │ 6 bảng+50 SP │  │ 2 bảng (chỉ đọc)  │        │
│  └──────────────┘  └──────────────┘  └────────────────────┘        │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Chi tiết từng instance

#### Publisher (DESKTOP-JBB41QU / NGANHANG_PUB)

| Đối tượng | Chi tiết |
|---|---|
| **Bảng** | CHINHANH, NGUOIDUNG, KHACHHANG, NHANVIEN, TAIKHOAN, GD_GOIRUT, GD_CHUYENTIEN |
| **Sequence** | SEQ_MANV (auto-increment mã nhân viên) |
| **View** | view_DanhSachPhanManh (TOP 2 chi nhánh — dropdown login) |
| **SP nghiệp vụ** | 50 SP (Customer 7 + Employee 10 + Account 11 + Transaction 8 + Report 3 + Auth/Branch 11) |
| **SP bảo mật** | sp_DangNhap, sp_TaoTaiKhoan, sp_XoaTaiKhoan, sp_DoiMatKhau, sp_DanhSachNhanVien |
| **Roles** | NGANHANG, CHINHANH, KHACHHANG |
| **Seed logins** | ADMIN_NH (NGANHANG), NV_BT (CHINHANH), KH_DEMO (KHACHHANG) |

> Bằng chứng: `sql/03_publisher_sp_views.sql` dòng 1–40, `sql/04_publisher_security.sql` dòng 1–20.

#### Subscriber CN1 (SQLSERVER2 / NGANHANG_BT)

| Đối tượng | Chi tiết |
|---|---|
| **Bảng** | CHINHANH (tất cả), KHACHHANG/NHANVIEN/TAIKHOAN/GD_GOIRUT/GD_CHUYENTIEN (MACN=BENTHANH) |
| **SP** | 50 SP (replicated via proc schema only) + sp_DangNhap, sp_TaoTaiKhoan cục bộ |
| **View** | view_DanhSachPhanManh (replicated) |
| **Roles** | NGANHANG, CHINHANH, KHACHHANG (tạo bởi script 08) |

#### Subscriber CN2 (SQLSERVER3 / NGANHANG_TD)

| Đối tượng | Chi tiết |
|---|---|
| Giống CN1 nhưng dữ liệu lọc theo MACN=TANDINH |

#### Subscriber TraCuu (SQLSERVER4 / NGANHANG_TRACUU)

| Đối tượng | Chi tiết |
|---|---|
| **Bảng** | CHINHANH (tất cả), KHACHHANG (tất cả, không lọc) |
| **SP** | Không có SP nghiệp vụ nào được replicate (chỉ đọc qua SELECT trực tiếp) |
| **Bảo mật** | GRANT SELECT ON KHACHHANG, CHINHANH. DENY INSERT/UPDATE/DELETE |
| **Mục đích** | Tra cứu khách hàng toàn hệ thống (NGANHANG role only) |

> Bằng chứng: `sql/05_replication_setup_merge.sql` dòng 54–70 (bảng article summary).

---

## 3. Replication Workflow (QLVT-style)

### 3.1 Trạng Thái Các Thành Phần Replication

| Thành phần | Trạng thái | File tham chiếu |
|---|---|---|
| Distributor configuration | ✅ Implemented | `sql/05_replication_setup_merge.sql` Part A |
| Publication creation (3 publications) | ✅ Implemented | `sql/05_replication_setup_merge.sql` Part C |
| Articles: tables — CN1/CN2 (6 bảng mỗi pub) | ✅ Implemented | Part C1c/C2c (BT), Part C2 (TD) |
| Articles: tables — TraCuu (2 bảng) | ✅ Implemented | Part C3 |
| Articles: stored procedures (50 SP, proc schema only) | ✅ Implemented | Part C1e/C2e |
| Articles: view (view_DanhSachPhanManh, view schema only) | ✅ Implemented | Part C1f/C2f |
| Row filters (MACN = N'BENTHANH' / N'TANDINH') | ✅ Implemented | `subset_filterclause` trên KHACHHANG, NHANVIEN, TAIKHOAN |
| Join filters (GD_GOIRUT → TAIKHOAN, GD_CHUYENTIEN → TAIKHOAN) | ✅ Implemented | Part C1d/C2d |
| Column filters (TraCuu) | ⬜ Không áp dụng | TraCuu nhận đầy đủ cột KHACHHANG, không cần lọc cột |
| Push subscriptions (CN1, CN2, TraCuu) | ✅ Implemented | Part D (3 `sp_addmergesubscription` + `sp_addmergepushsubscription_agent`) |
| Snapshot Agent run steps | ✅ Implemented | Part E — `sp_startpublication_snapshot` cho 3 publications |
| SQL Server Agent requirement | ✅ Documented | Header comment + `99_run_all.sql` TODO-RUN02 |

### 3.2 "SP Authoring Rule" Compliance

| Quy tắc | Tuân thủ? | Bằng chứng |
|---|---|---|
| SP chỉ được định nghĩa trên Publisher | ✅ Đúng | `sql/03_publisher_sp_views.sql` — tất cả 50 SP dùng `CREATE OR ALTER` trên `USE NGANHANG_PUB` |
| Subscriber KHÔNG tạo SP/view thủ công | ✅ Đúng | `sql/07_subscribers_create_db.sql` — chỉ tạo shell DB rỗng, comment rõ "KHÔNG tạo bảng, SP hoặc view" |
| SP bảo mật (sp_DangNhap, sp_TaoTaiKhoan) tạo cục bộ trên subscriber | ✅ Đúng | `sql/08_subscribers_post_replication_fixups.sql` Part 4 — `CREATE OR ALTER PROCEDURE dbo.sp_DangNhap` với logic `DB_NAME()` thay vì NGUOIDUNG |
| Luồng cập nhật SP: edit Publisher → update article → generate snapshot → run agent → verify | ✅ Documented | `sql/00_readme_execution_order.md` cuối file: "SP chỉ được tạo trên Máy chủ phát hành" |

**Ngoại lệ hợp lệ:** `sp_DangNhap`, `sp_TaoTaiKhoan`, `sp_XoaTaiKhoan`, `sp_DoiMatKhau` phải tồn tại cục bộ trên mỗi subscriber vì chúng sử dụng `sp_addlogin` (server-level) và `DB_NAME()` mapping. Đây là thiết kế có chủ đích, không phải vi phạm.

---

## 4. Chiến Lược Linked Server

### 4.1 Bảng ánh xạ linked server

| Chạy trên | LINK0 → | LINK1 → | LINK2 → |
|---|---|---|---|
| **Publisher** (default) | SQLSERVER4 (TraCuu) | SQLSERVER2 (CN1–BT) | SQLSERVER3 (CN2–TD) |
| **CN1** (SQLSERVER2) | SQLSERVER4 (TraCuu) | SQLSERVER3 (CN2–TD) | — |
| **CN2** (SQLSERVER3) | SQLSERVER4 (TraCuu) | SQLSERVER2 (CN1–BT) | — |

> File: `sql/06_linked_servers.sql` — 3 phần (A/B/C) auto-detect `@@SERVERNAME`.

### 4.2 Mục đích sử dụng

| Linked Server | Dùng bởi | Mục đích |
|---|---|---|
| **LINK1** (trên CN1/CN2) | `SP_CrossBranchTransfer` — ĐƯỜNG B | Giao dịch phân tán chuyển khoản liên chi nhánh (`BEGIN DISTRIBUTED TRANSACTION` + 4-part name `[LINK1].[DB].dbo.TAIKHOAN`) |
| **LINK0** (ở mọi nơi) | Dự phòng / giám sát | Truy cập TraCuu từ chi nhánh hoặc Publisher |
| **LINK1, LINK2** (trên Publisher) | Giám sát / báo cáo tổng hợp | Publisher kiểm tra dữ liệu subscriber |

### 4.3 Tính nhất quán đặt tên

**Quy tắc ngân hàng tuân thủ:** Trên CN1, LINK1 → "chi nhánh kia" (CN2). Trên CN2, LINK1 → "chi nhánh kia" (CN1). Cùng tên, đích đối xứng → cùng SP code chạy trên cả hai.

### 4.4 Bảo mật linked server

- Hiện tại dùng `sa / Password!123` (mặc định lab).
- Script có `TODO-LS-SEC` comment nhắc thay bằng `svc_linkedserver` chuyên dụng.
- Mỗi linked server bật: `rpc`, `rpc out`, `data access` = `true`.

---

## 5. SQL Security & Roles (Yêu Cầu Môn Học)

### 5.1 Roles

| Role SQL | C# UserGroup | Quyền |
|---|---|---|
| **NGANHANG** | `UserGroup.NganHang` (0) | Xem tất cả chi nhánh, quản trị đầy đủ, tạo login bất kỳ role |
| **CHINHANH** | `UserGroup.ChiNhanh` (1) | CRUD dữ liệu chi nhánh mình, tạo login CHINHANH/KHACHHANG |
| **KHACHHANG** | `UserGroup.KhachHang` (2) | Chỉ đọc: sao kê tài khoản cá nhân |

### 5.2 Cách tạo login

| SP | Mô tả |
|---|---|
| `sp_TaoTaiKhoan(@LOGIN, @PASS, @TENNHOM)` | Tạo SQL login + DB user + assign role. Có kiểm tra phân quyền: CHINHANH không thể tạo NGANHANG login. |
| `sp_XoaTaiKhoan(@LOGIN)` | Xóa login (chỉ NGANHANG). |
| `sp_DoiMatKhau(@LOGIN, @PASSCU, @PASSMOI)` | Đổi mật khẩu (tự đổi hoặc NGANHANG reset). |

> File: `sql/04_publisher_security.sql` Part 5–7.

### 5.3 Giải quyết identity tại login (sp_DangNhap)

**Luồng:**
1. App connect vào Publisher bằng SQL login + password đã nhập.
2. Gọi `sp_DangNhap` (không tham số).
3. SP đọc `sys.database_role_members` để xác định role (ưu tiên: NGANHANG > CHINHANH > KHACHHANG).
4. Giải quyết DefaultBranch (MACN):
   - Thử `NGUOIDUNG.DefaultBranch` (nếu bảng tồn tại).
   - Thử `NHANVIEN.MACN` (WHERE MANV = SYSTEM_USER).
   - Thử `KHACHHANG.MACN` (WHERE CMND = SYSTEM_USER).
5. Trả về: `MANV`, `HOTEN`, `TENNHOM`, `MACN`.
6. C# AuthService lưu credentials vào `ConnectionStringProvider` cho mọi lệnh DB tiếp theo.

> File: `sql/04_publisher_security.sql` Part 4, `BankDds.Infrastructure/Security/AuthService.cs`.

### 5.4 GRANT/DENY enforcement

| Bảo vệ | Trạng thái |
|---|---|
| **DENY SELECT/INSERT/UPDATE/DELETE** trên tất cả bảng cho cả 3 roles | ✅ Implemented (Publisher + Subscriber) |
| **GRANT EXECUTE** trên SP theo ma trận phân quyền chi tiết | ✅ Implemented (52 SP × 3 roles, bảng trong script 04 Part 3) |
| **sp_DangNhap** GRANT TO PUBLIC (pre-auth resolver) | ✅ Implemented |
| **Ownership chaining** bypass DENY cho dbo SP → dbo table | ✅ Hoạt động (SP chạy dưới quyền dbo) |

### 5.5 Authorization còn lại ở app-level

| Kiểm tra | Nơi thực hiện | Lý do |
|---|---|---|
| `CanModifyBranch()` — NGANHANG là view-only, không CRUD | `AuthorizationService.cs` | Yêu cầu DE3: NGANHANG chỉ xem báo cáo |
| `CanViewCustomerLookup()` — chỉ NGANHANG | `HomeViewModel.cs` | TraCuu là tính năng ngân hàng cấp cao |
| `CanCreateUser()` phân quyền tạo login | `AuthorizationService.cs` | Bổ sung sp_TaoTaiKhoan server-side |
| Transaction limits (min/max/daily) | `TransactionValidator.cs` | Business rule ở app, SQL chỉ CHECK >= 100000 |

---

## 6. Luồng Runtime Ứng Dụng (WPF)

### 6.1 Khởi động app / DI container / Configuration

```
App.xaml → AppBootstrapper.Configure()
  ├─ Load appsettings.json + appsettings.Development.json + ENV vars
  ├─ Đọc DataMode ("Sql" hoặc "InMemory")
  ├─ Đăng ký repositories (Sql* hoặc InMemory*)
  ├─ Đăng ký services (CustomerService, AccountService, ...)
  ├─ Đăng ký AuthService (SQL login → sp_DangNhap)
  ├─ Đăng ký ViewModels + Views (Caliburn.Micro convention)
  └─ OnStartup → DisplayRootViewForAsync<MainShellViewModel>()
      └─ MainShellViewModel → ShowLoginAsync() → LoginViewModel
```

> File: `BankDds.Wpf/AppBootstrapper.cs` (302 dòng).

### 6.2 Login flow

```
LoginViewModel.OnActivateAsync()
  │
  ├─ 1. LoadBranchesFromPublisherAsync()
  │     Connect to Publisher (sa/123 pre-auth)
  │     SELECT MACN, TENCN FROM view_DanhSachPhanManh
  │     → Populate branch dropdown: [BENTHANH, TANDINH]
  │     Fallback: hardcoded nếu Publisher unreachable
  │
  └─ 2. Login() — user nhấn nút
        │
        ├─ AuthService.LoginAsync(userName, password)
        │   Connect to Publisher bằng SQL login/password đã nhập
        │   EXEC sp_DangNhap → {MANV, HOTEN, TENNHOM, MACN}
        │   Map TENNHOM → UserGroup (NganHang/ChiNhanh/KhachHang)
        │   Lưu credentials vào ConnectionStringProvider
        │
        ├─ Xác định permitted branches:
        │   NganHang → [BENTHANH, TANDINH] (cả hai)
        │   ChiNhanh → [DefaultBranch từ sp_DangNhap]
        │   KhachHang → [DefaultBranch từ sp_DangNhap]
        │
        └─ UserSession.SetSession() → MainShell.ShowHomeAsync()
```

> File: `BankDds.Wpf/ViewModels/LoginViewModel.cs`, `BankDds.Infrastructure/Security/AuthService.cs`.

### 6.3 Post-login navigation

```
HomeViewModel
  ├─ Branch dropdown (NGANHANG only, ComboBox + CanSwitchBranch)
  │   OnBranchChanged → UserSession.SetSelectedBranch()
  │   → Close + re-open active child VM (fresh data load)
  │
  ├─ ShowCustomers()     → CustomersViewModel    (NGANHANG, CHINHANH)
  ├─ ShowAccounts()      → AccountsViewModel     (NGANHANG, CHINHANH)
  ├─ ShowEmployees()     → EmployeesViewModel    (NGANHANG, CHINHANH)
  ├─ ShowTransactions()  → TransactionsViewModel  (NGANHANG, CHINHANH)
  ├─ ShowReports()       → ReportsViewModel       (tất cả roles)
  ├─ ShowAdmin()         → AdminViewModel         (NGANHANG, CHINHANH)
  ├─ ShowBranches()      → BranchesViewModel      (NGANHANG only)
  ├─ ShowCustomerLookup()→ CustomerLookupViewModel(NGANHANG only)
  └─ Logout()            → ClearSession → ShowLoginAsync()
```

### 6.4 Connection routing

| Tình huống | Connection string | Chi tiết |
|---|---|---|
| Login / branch list | `Publisher` template + entered credentials | Publisher (NGANHANG_PUB) |
| CRUD dữ liệu chi nhánh | `Branch_{MACN}` template + session credentials | CN1 (NGANHANG_BT) hoặc CN2 (NGANHANG_TD) |
| Cross-branch reads (GetAll*) | `Publisher` template + session credentials | Publisher (NGANHANG_PUB) — all rows |
| Customer Lookup | `LookupDatabase` template + session credentials | SQLSERVER4 (NGANHANG_TRACUU), fallback Publisher |

> File: `BankDds.Infrastructure/Configuration/ConnectionStringProvider.cs`.

### 6.5 SP mapping cho các thao tác chính

| Thao tác | SP | Connection | C# caller |
|---|---|---|---|
| **Customer CRUD** | SP_GetCustomersByBranch, SP_GetCustomerByCMND, SP_AddCustomer, SP_UpdateCustomer, SP_DeleteCustomer, SP_RestoreCustomer | Branch subscriber | SqlCustomerRepository |
| **Customer xem tất cả** | SP_GetAllCustomers | Publisher | SqlCustomerRepository |
| **Account CRUD** | SP_GetAccountsByBranch, SP_GetAccount, SP_AddAccount, SP_UpdateAccount, SP_CloseAccount, SP_ReopenAccount, SP_DeleteAccount | Branch subscriber | SqlAccountRepository |
| **Gửi tiền** | SP_Deposit | Branch subscriber | SqlTransactionRepository |
| **Rút tiền** | SP_Withdraw | Branch subscriber | SqlTransactionRepository |
| **Chuyển tiền (cùng CN + liên CN)** | SP_CrossBranchTransfer | Branch subscriber | SqlTransactionRepository.TransferAsync() |
| **Sao kê tài khoản** | SP_GetAccountStatement | Branch subscriber | SqlReportRepository |
| **TK mở trong kỳ** | SP_GetAccountsOpenedInPeriod | Branch/Publisher | SqlReportRepository |
| **Tổng hợp giao dịch** | SP_GetTransactionSummary | Branch/Publisher | SqlReportRepository |
| **Tra cứu KH toàn hệ thống** | Direct SELECT on KHACHHANG | NGANHANG_TRACUU (hoặc Publisher fallback) | SqlCustomerLookupRepository |
| **Tạo login** | sp_TaoTaiKhoan | Publisher/Subscriber | AdminViewModel → SqlUserRepository |
| **Đăng nhập** | sp_DangNhap | Publisher | AuthService |

---

## 7. Hoàn Thành vs TODO

### 7.1 Đã hoàn thành ✅

- ✅ **Schema đầy đủ** — 7 bảng + rowguid + MACN indexes (merge replication ready)
- ✅ **50 SP nghiệp vụ** + 1 view + 5 SP bảo mật = 56 đối tượng trên Publisher
- ✅ **Merge Replication script** — Distributor + 3 Publications + Articles (bảng + 50 SP + 1 view) + Row/Join filters + 3 Push subscriptions + Snapshot agent
- ✅ **Linked Server** — LINK0/LINK1/LINK2 với đặt tên đối xứng, auto-detect instance
- ✅ **SQL Security** — 3 roles (NGANHANG/CHINHANH/KHACHHANG), DENY table access, GRANT EXECUTE per role, ownership chaining
- ✅ **sp_DangNhap** — resolve role + DefaultBranch (NGUOIDUNG → NHANVIEN → KHACHHANG), nhất quán Publisher + Subscriber
- ✅ **sp_TaoTaiKhoan** — tạo SQL login + DB user + role membership, phân quyền caller
- ✅ **sp_XoaTaiKhoan + sp_DoiMatKhau + sp_DanhSachNhanVien**
- ✅ **Subscriber post-replication fixups** — roles, DENY/GRANT, security SPs, login sync, TraCuu hardening
- ✅ **Seed data** — 2 chi nhánh, 4 NV, 6 KH, 8 TK, giao dịch mẫu, NGUOIDUNG, 3 seed logins
- ✅ **WPF 3-layer architecture** — Core (interfaces + models + validators) / Infrastructure (SQL + InMemory repos + services) / WPF (ViewModels + Views)
- ✅ **Autofac DI + Caliburn.Micro** — DataMode routing (InMemory / Sql), InstancePerDependency cho SQL repos
- ✅ **Login flow** — branch dropdown từ view_DanhSachPhanManh, SQL credential authentication, sp_DangNhap role resolution
- ✅ **Branch switching** — NGANHANG role chọn chi nhánh, reload active child VM
- ✅ **Connection routing** — Publisher / Branch_{MACN} / LookupDatabase templates, credential injection
- ✅ **Customer Lookup** — NGANHANG-only tra cứu KH toàn hệ thống qua NGANHANG_TRACUU (direct SELECT)
- ✅ **Chuyển khoản liên chi nhánh** — SP_CrossBranchTransfer: Path A (local TXN) + Path B (DISTRIBUTED TRANSACTION via LINK1)
- ✅ **Báo cáo** — sao kê tài khoản (2 result sets: opening balance + transactions), TK mở trong kỳ, tổng hợp GD
- ✅ **Export** — ReportExportService (ClosedXML cho Excel, itext7 cho PDF)
- ✅ **Validators** — FluentValidation cho Customer, Account, Employee, Transaction, User, Branch
- ✅ **Tài liệu** — SETUP guide, execution order, demo guide, test scripts
- ✅ **Idempotent scripts** — tất cả SQL scripts an toàn chạy lại
- ✅ **Build clean** — 0 errors, 0 warnings

### 7.2 Thiếu / Cần bổ sung ❌

| # | Mục | Rủi ro | Ghi chú |
|---|---|---|---|
| 1 | **NGUOIDUNG chưa sync đến subscriber** | MEDIUM | NGUOIDUNG chỉ ở Publisher. Khi app connect vào CN1 (CHINHANH login), sp_DangNhap subscriber version dùng `DB_NAME()` thay vì lookup NGUOIDUNG. Tuy nhiên, nếu cần tạo login trên subscriber, phải dùng sp_TaoTaiKhoan cục bộ. Script 08 đã đồng bộ seed logins. |
| 2 | **MSDTC chưa test end-to-end** | HIGH | SP_CrossBranchTransfer Path B cần MSDTC chạy trên cả 2 subscriber. Nếu MSDTC chưa cấu hình, chuyển khoản liên chi nhánh từ subscriber sẽ thất bại. Demo nên test trước. |
| 3 | **SQL Server Agent phải chạy** | HIGH | Replication phụ thuộc Agent cho Snapshot + Merge jobs. Nếu Agent tắt, subscriber không nhận data. Script có document nhưng dễ bị quên. |
| 4 | **Chưa có UI thay đổi mật khẩu** | LOW | sp_DoiMatKhau tồn tại nhưng chưa có ViewModel/View tương ứng. Có thể demo qua SSMS. |
| 5 | **KhachHang flow chưa đầy đủ** | MEDIUM | KhachHang login hiện chỉ xem reports/sao kê. Chưa có UI riêng cho customer self-service (xem tài khoản cá nhân filtered by CMND). ReportsView có thể dùng nhưng UX chưa tối ưu. |
| 6 | **Subscriber sp_DangNhap dùng DB_NAME() hardcode** | LOW | Mapping `NGANHANG_BT → BENTHANH`, `NGANHANG_TD → TANDINH` hardcode trong sp_DangNhap subscriber version. Nếu thêm chi nhánh mới, phải update SP thủ công. Chấp nhận được cho DE3 (chỉ 2 CN). |
| 7 | **99_run_all.sql chưa chạy Phase 2 tự động** | LOW | Phase 2 (subscriber) phải chạy thủ công trên từng instance. Documented nhưng không auto. |
| 8 | **Linked server dùng sa password** | LOW | Lab environment acceptable. Có TODO comment nhắc thay. |
| 9 | **Chưa có unit tests** | LOW | Không bắt buộc trong đề bài DE3. InMemory mode có thể dùng để test thủ công. |

### 7.3 Rủi ro chấm điểm

| Rủi ro | Mức độ | Mô tả | Biện pháp |
|---|---|---|---|
| **MSDTC không hoạt động khi demo** | 🔴 HIGH | Chuyển khoản liên CN từ subscriber thất bại → mất điểm phần giao dịch phân tán | Test MSDTC trước demo: `SELECT * FROM sys.dm_exec_connections WHERE net_transport = 'TCP'`. Cấu hình Component Services → MSDTC → Security → cho phép Network DTC Access. |
| **SQL Server Agent tắt** | 🔴 HIGH | Replication không chạy → subscriber rỗng → không demo được | Kiểm tra: `Get-Service SQLSERVERAGENT \| Select Status`. Nếu tắt: `Start-Service SQLSERVERAGENT`. |
| **Snapshot chưa hoàn thành** | 🔴 HIGH | Subscriber có shell DB nhưng không có data/SP | Kiểm tra: `USE distribution; SELECT * FROM dbo.MSmerge_history ORDER BY start_time DESC`. Chạy lại snapshot nếu cần. |
| **CHINHANH login chọn sai branch** | 🟡 MEDIUM | NV_BT login connect vào CN2 thay vì CN1 → data rỗng | App đã xử lý: sp_DangNhap trả về MACN, LoginViewModel lock branch cho CHINHANH. |
| **Export PDF/Excel lỗi font** | 🟡 MEDIUM | itext7 có thể thiếu font Unicode tiếng Việt | Kiểm tra trước demo; fallback xuất Excel (ClosedXML ổn định hơn). |

---

## 8. Cách Chạy (Setup & Verification)

### 8.1 Prerequisites

| Yêu cầu | Chi tiết |
|---|---|
| **SQL Server 2019+** | 4 instances trên cùng máy: default + SQLSERVER2 + SQLSERVER3 + SQLSERVER4 |
| **SQL Server Replication feature** | Phải cài qua SQL Server Installation Center |
| **SQL Server Agent** | Phải RUNNING trên Publisher instance |
| **MSDTC** | Cần cho chuyển khoản liên CN trên subscriber. Component Services → MSDTC → Security → Enable Network DTC Access, Allow Inbound/Outbound |
| **.NET 8 SDK** | Cho build/run WPF app |
| **Windows OS** | WPF yêu cầu Windows |

### 8.2 Thứ Tự Thực Thi

#### Phase 1 — Publisher (DESKTOP-JBB41QU)

```powershell
# Bước 1: Chạy tất cả script Publisher bằng master runner
sqlcmd -S "DESKTOP-JBB41QU" -E -i "sql\99_run_all.sql"
# Hoặc chạy từng script 01 → 06 riêng lẻ.
```

#### Phase 1b — Linked Server trên Subscribers

```powershell
# RUN ON: CN1
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\06_linked_servers.sql"
# RUN ON: CN2
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\06_linked_servers.sql"
```

#### Phase 2 — Subscriber Shell DBs

```powershell
# RUN ON: CN1
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\07_subscribers_create_db.sql"
# RUN ON: CN2
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\07_subscribers_create_db.sql"
# RUN ON: TraCuu
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\07_subscribers_create_db.sql"
```

#### Phase 2b — Chờ Snapshot Agent (~1–5 phút)

```sql
-- Kiểm tra trên Publisher:
USE distribution;
SELECT a.name AS AgentName, h.start_time, h.runstatus, h.comments
FROM dbo.MSmerge_agents a
JOIN dbo.MSmerge_history h ON a.id = h.agent_id
ORDER BY h.start_time DESC;
-- runstatus = 2 → Success
```

#### Phase 2c — Post-Replication Fixups

```powershell
# RUN ON: CN1 (sau khi snapshot PUB_NGANHANG_BT hoàn thành)
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER2" -E -i "sql\08_subscribers_post_replication_fixups.sql"
# RUN ON: CN2 (sau khi snapshot PUB_NGANHANG_TD hoàn thành)
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER3" -E -i "sql\08_subscribers_post_replication_fixups.sql"
# RUN ON: TraCuu (sau khi snapshot PUB_TRACUU hoàn thành)
sqlcmd -S "DESKTOP-JBB41QU\SQLSERVER4" -E -i "sql\08_subscribers_post_replication_fixups.sql"
```

#### Phase 3 — Chạy App

```powershell
cd BankDds.Wpf
dotnet run
```

**Đăng nhập mẫu:**

| Login | Password | Role | Kết quả |
|---|---|---|---|
| `ADMIN_NH` | `Admin@123` | NGANHANG | Xem tất cả chi nhánh, quản trị đầy đủ |
| `NV_BT` | `NhanVien@123` | CHINHANH | CRUD chi nhánh Bến Thành |
| `KH_DEMO` | `KhachHang@123` | KHACHHANG | Xem sao kê TK cá nhân |
| `sa` | `123` | sysadmin → NGANHANG | Full access (dùng khi debug) |

### 8.3 Verification Checklist

#### ✅ Replication tồn tại và healthy

```sql
-- RUN ON: Publisher
USE NGANHANG_PUB;
SELECT name FROM sysmergepublications;
-- Kỳ vọng: PUB_NGANHANG_BT, PUB_NGANHANG_TD, PUB_TRACUU

SELECT publication_name, subscriber_server, subscriber_db, status
FROM dbo.sysmergesubscriptions
WHERE publication_name IS NOT NULL;
-- Kỳ vọng: 3 rows (BT→SQLSERVER2, TD→SQLSERVER3, TRACUU→SQLSERVER4)
```

#### ✅ Row filter đúng trên subscriber

```sql
-- RUN ON: CN1 (SQLSERVER2)
USE NGANHANG_BT;
SELECT MACN, COUNT(*) AS Cnt FROM dbo.KHACHHANG GROUP BY MACN;
-- Kỳ vọng: chỉ MACN = 'BENTHANH'

-- RUN ON: CN2 (SQLSERVER3)
USE NGANHANG_TD;
SELECT MACN, COUNT(*) AS Cnt FROM dbo.KHACHHANG GROUP BY MACN;
-- Kỳ vọng: chỉ MACN = 'TANDINH'
```

#### ✅ TraCuu nhận đúng data

```sql
-- RUN ON: TraCuu (SQLSERVER4)
USE NGANHANG_TRACUU;
SELECT MACN, COUNT(*) AS Cnt FROM dbo.KHACHHANG GROUP BY MACN;
-- Kỳ vọng: cả BENTHANH và TANDINH (tất cả khách hàng)

-- Kiểm tra bảng:
SELECT name FROM sys.tables WHERE name NOT LIKE 'MSmerge%' ORDER BY name;
-- Kỳ vọng: CHINHANH, KHACHHANG (chỉ 2 bảng nghiệp vụ)
```

#### ✅ SP update trên Publisher propagate xuống subscriber

```sql
-- Bước 1: Thay đổi SP trên Publisher
USE NGANHANG_PUB;
CREATE OR ALTER PROCEDURE dbo.SP_GetEmployee @MANV nChar(10)
AS BEGIN SET NOCOUNT ON;
    SELECT MANV, HO, TEN, DIACHI, CMND, PHAI, SODT, MACN, TrangThaiXoa
    FROM dbo.NHANVIEN WHERE MANV = @MANV;
    -- Thêm comment test: --REPLICATION_TEST
END
GO

-- Bước 2: Generate snapshot cho PUB_NGANHANG_BT
EXEC sp_startpublication_snapshot @publication = N'PUB_NGANHANG_BT';

-- Bước 3: Chờ snapshot hoàn thành, kiểm tra trên CN1
-- RUN ON: CN1 (SQLSERVER2)
USE NGANHANG_BT;
EXEC sp_helptext 'SP_GetEmployee';
-- Kỳ vọng: thấy comment --REPLICATION_TEST
```

#### ✅ Linked server hoạt động

```sql
-- RUN ON: CN1 (SQLSERVER2)
SELECT TOP 1 SOTK, SODU FROM [LINK1].[NGANHANG_TD].dbo.TAIKHOAN;
-- Kỳ vọng: trả về data từ CN2

-- RUN ON: CN2 (SQLSERVER3)
SELECT TOP 1 SOTK, SODU FROM [LINK1].[NGANHANG_BT].dbo.TAIKHOAN;
-- Kỳ vọng: trả về data từ CN1
```

#### ✅ MSDTC cho giao dịch phân tán

```sql
-- RUN ON: CN1 (SQLSERVER2) — chuyển tiền từ BT account sang TD account
USE NGANHANG_BT;
EXEC SP_CrossBranchTransfer
    @SOTK_CHUYEN = N'BT0000001',
    @SOTK_NHAN   = N'TD0000001',
    @SOTIEN      = 100000,
    @MANV        = N'NV00000001';
-- Kỳ vọng: return code 0 (thành công)
-- Nếu lỗi MSDTC → cấu hình Component Services
```

---

> **Tóm tắt:** Dự án DE3 Ngân Hàng đã triển khai **đầy đủ** kiến trúc CSDL phân tán với Merge Replication, Linked Server, SQL Security 3-role, và WPF client 3-layer. Tất cả script SQL idempotent, build clean, và tài liệu đầy đủ. Rủi ro chính cần kiểm tra trước demo: **MSDTC**, **SQL Server Agent**, và **Snapshot Agent completion**.
