# AUDIT Login/AuthZ/UI/SQL Consistency

Date: 2026-03-14

Scope:
1. Login workflow + authorization workflow in app.
2. UI behavior by role (show/hide and CRUD).
3. App/SQL consistency check.
4. Mark which previously reported issues are already fixed.

Reference docs:
- `docs/requirements/DE3-NGANHANG.md`
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md`

---

## Executive Summary

Overall status: **CONSISTENT**

- Login + session + role mapping flow: **CONSISTENT**
- Main UI role behavior: **CONSISTENT**
- SQL role/SP policy vs app policy: **CONSISTENT**

Fix tracker for previously agreed 3 issues:

1. Employee transfer blocked logic:
- **FIXED**
- UI transfer button guard has been aligned with edit-state transfer panel.

2. Cross-branch transfer blocked in app:
- **FIXED**

3. NGANHANG SQL over-permission:
- **FIXED**

---

## 1) Current Login/AuthZ Workflow (App)

### 1.1 Login input and authentication

- Login screen currently uses only `UserName` + `Password` (no branch picker on login view):
  - `BankDds.Wpf/Views/LoginView.xaml:200`
  - `BankDds.Wpf/Views/LoginView.xaml:223`
  - `BankDds.Wpf/Views/LoginView.xaml:253`
- `LoginViewModel.Login()` calls `IAuthService.LoginAsync(user, pass)`:
  - `BankDds.Wpf/ViewModels/LoginViewModel.cs:125`
  - `BankDds.Wpf/ViewModels/LoginViewModel.cs:130`
- `AuthService` authenticates by SQL login, then calls `sp_DangNhap`, reads `TENNHOM`, `MACN`, `CustomerCMND`, `EmployeeId`, and maps to app role:
  - `BankDds.Infrastructure/Security/AuthService.cs:38`
  - `BankDds.Infrastructure/Security/AuthService.cs:56`
  - `BankDds.Infrastructure/Security/AuthService.cs:57`
  - `BankDds.Infrastructure/Security/AuthService.cs:58`
  - `BankDds.Infrastructure/Security/AuthService.cs:59`
  - `BankDds.Infrastructure/Security/AuthService.cs:62`

### 1.2 Session bootstrap after login

- Login success builds runtime session by role/scope:
  - `BankDds.Wpf/ViewModels/LoginViewModel.cs:181`
- Session stores:
  - `Username`, `UserGroup`, `SelectedBranch`, `PermittedBranches`, `CustomerCMND`, `EmployeeId`
  - `BankDds.Infrastructure/Data/UserSession.cs`
- Runtime SQL credential is persisted in `ConnectionStringProvider` and used by repositories:
  - `BankDds.Infrastructure/Configuration/ConnectionStringProvider.cs:25`
  - `BankDds.Infrastructure/Configuration/ConnectionStringProvider.cs:37`
  - `BankDds.Infrastructure/Configuration/ConnectionStringProvider.cs:60`

### 1.3 Authorization policy center

- Core policy is centralized in `AuthorizationService`:
  - create-user matrix: `CanCreateUser(...)`
    - `BankDds.Infrastructure/Security/AuthorizationService.cs:29`
  - branch read scope: `CanAccessBranch(...)`
    - `BankDds.Infrastructure/Security/AuthorizationService.cs:45`
  - branch write scope: `CanModifyBranch(...)`
    - `BankDds.Infrastructure/Security/AuthorizationService.cs:65`
  - transaction permission: `CanPerformTransactions(...)`
    - `BankDds.Infrastructure/Security/AuthorizationService.cs:98`
  - report scope: `CanAccessReports(...)`
    - `BankDds.Infrastructure/Security/AuthorizationService.cs:114`

---

## 2) Current UI Role Behavior

### 2.1 Main menu visibility by role

Source:
- `BankDds.Wpf/ViewModels/HomeViewModel.cs`

Observed behavior:

1. `NganHang`
- Can switch branch: `CanSwitchBranch == true`
  - `HomeViewModel.cs:26`
- Can access reports/admin/branches/customer-lookup
- Cannot access operational tabs (customers/accounts/employees/transactions)

2. `ChiNhanh`
- No branch switch
- Can access operational tabs + reports + admin

3. `KhachHang`
- Only reports area is visible at menu level

### 2.2 CRUD behavior by screen

1. Customers screen
- Modify actions only for `ChiNhanh`
  - `BankDds.Wpf/ViewModels/CustomersViewModel.cs:177`

2. Accounts screen
- Modify actions only for `ChiNhanh`
  - `BankDds.Wpf/ViewModels/AccountsViewModel.cs:129`

3. Employees screen
- Modify actions only for `ChiNhanh`
  - `BankDds.Wpf/ViewModels/EmployeesViewModel.cs:114`

4. Transactions screen
- Deposit/Withdraw/Transfer only for `ChiNhanh`
  - `BankDds.Wpf/ViewModels/TransactionsViewModel.cs:120`

5. Reports screen
- `KhachHang` mode hides management reports and keeps statement usage
  - `BankDds.Wpf/ViewModels/ReportsViewModel.cs:165`
  - `BankDds.Wpf/Views/ReportsView.xaml:194`
  - `BankDds.Wpf/Views/ReportsView.xaml:291`
  - `BankDds.Wpf/Views/ReportsView.xaml:372`

6. Admin screen
- Screen open for `NganHang` and `ChiNhanh`:
  - `BankDds.Wpf/ViewModels/AdminViewModel.cs:209`
- But edit/delete login is only for `NganHang`:
  - `BankDds.Wpf/ViewModels/AdminViewModel.cs:161`
  - `BankDds.Wpf/ViewModels/AdminViewModel.cs:162`
- Create target group options:
  - `NganHang` user sees only `NganHang`
    - `AdminViewModel.cs:215`
  - `ChiNhanh` user sees `ChiNhanh` + `KhachHang`
    - `AdminViewModel.cs:221`
    - `AdminViewModel.cs:222`

---

## 3) Current SQL AuthZ Workflow

Source scripts:
- `sql/03_publisher_sp_views.sql`
- `sql/04_publisher_security.sql`

### 3.1 Login and role mapping

- `sp_DangNhap` exists and returns `TENNHOM`, `MACN`, `CustomerCMND`, `EmployeeId`:
  - `sql/04_publisher_security.sql:127`
  - `sql/04_publisher_security.sql:218`
  - `sql/04_publisher_security.sql:219`
  - `sql/04_publisher_security.sql:220`
  - `sql/04_publisher_security.sql:221`
- Public execute grant for login handshake:
  - `sql/04_publisher_security.sql:225`

### 3.2 Create-account matrix in SQL

- `sp_TaoTaiKhoan` permission matrix:
  - `NGANHANG -> NGANHANG`
  - `CHINHANH -> CHINHANH/KHACHHANG`
  - `KHACHHANG -> denied`
  - `sql/04_publisher_security.sql:230`
  - `sql/04_publisher_security.sql:267`
  - `sql/04_publisher_security.sql:273`

- `USP_AddUser` also enforces equivalent group matrix:
  - `sql/03_publisher_sp_views.sql:1074`
  - `sql/03_publisher_sp_views.sql:1143`

### 3.3 KHACHHANG own-data guard in SQL

- Own-account guard for account list:
  - `SP_GetAccountsByCustomer`
  - `sql/03_publisher_sp_views.sql:303`
  - `sql/03_publisher_sp_views.sql:311`
  - `sql/03_publisher_sp_views.sql:331`

- Own-account guard for transactions:
  - `SP_GetTransactionsByAccount`
  - `sql/03_publisher_sp_views.sql:454`
  - `sql/03_publisher_sp_views.sql:459`
  - `sql/03_publisher_sp_views.sql:486`

- Own-account guard for statement:
  - `SP_GetAccountStatement`
  - `sql/03_publisher_sp_views.sql:835`
  - `sql/03_publisher_sp_views.sql:842`
  - `sql/03_publisher_sp_views.sql:869`

---

## 4) Consistency Check (App vs SQL)

## 4.1 Consistent areas

1. Login flow is aligned:
- `user/pass` -> SQL auth -> `sp_DangNhap` -> session creation.

2. Role mapping is aligned:
- SQL groups (`NGANHANG`, `CHINHANH`, `KHACHHANG`) map into app groups (`NganHang`, `ChiNhanh`, `KhachHang`).

3. KHACHHANG read scope is aligned:
- App checks + SQL guards both enforce own-data access.

4. Create-user matrix (including extension that `ChiNhanh` can create `KhachHang`) is aligned:
- App policy + SQL SP policy both follow same matrix.

## 4.2 Previously open inconsistencies and fixes

### A) Employee transfer UI guard mismatch

Status: **FIXED**

What is fixed:
- Service now authorizes by source branch ownership only:
  - `BankDds.Infrastructure/Data/EmployeeService.cs:103`
  - `BankDds.Infrastructure/Data/EmployeeService.cs:120`
- SQL `SP_TransferEmployee` validates CHINHANH caller branch against employee current branch:
  - `sql/03_publisher_sp_views.sql:196`
  - `sql/03_publisher_sp_views.sql:234`
  - `sql/03_publisher_sp_views.sql:248`
  - `sql/03_publisher_sp_views.sql:254`

- Transfer panel in XAML is shown when `IsEditing = true`:
  - `BankDds.Wpf/Views/EmployeesView.xaml:95`
- Transfer command guard is now also `IsEditing`:
  - `BankDds.Wpf/ViewModels/EmployeesViewModel.cs:121`
- Result: transfer action is reachable in the same UI state where transfer inputs are shown.

### B) SQL least-privilege mismatch for CHINHANH user maintenance

Status: **FIXED**

- App policy limits CHINHANH to create-only (no edit/delete login):
  - `BankDds.Wpf/ViewModels/AdminViewModel.cs:161`
  - `BankDds.Wpf/ViewModels/AdminViewModel.cs:162`
  - `BankDds.Infrastructure/Data/UserService.cs:53`
  - `BankDds.Infrastructure/Data/UserService.cs:67`
- CHINHANH execute grants for update/delete/restore user mapping have been removed and explicitly revoked:
  - `sql/04_publisher_security.sql`
- `USP_AddUser` is now insert-only for `NGUOIDUNG` mapping (no silent upsert on existing username):
  - `sql/03_publisher_sp_views.sql`
- `sp_TaoTaiKhoan` now rejects duplicate login creation to prevent create-flow overwrite:
  - `sql/04_publisher_security.sql`

---

## 5) Status of Previously Agreed 3 Fixes

1. Employee transfer blocked logic:
- **FIXED**
- `CanExecuteTransferBranch` has been aligned with transfer UI visibility state.

2. Cross-branch transfer blocked in app:
- **FIXED**
- App service no longer blocks by destination branch:
  - `BankDds.Infrastructure/Data/TransactionService.cs:113`
  - `BankDds.Infrastructure/Data/TransactionService.cs:131`
  - `BankDds.Infrastructure/Data/TransactionService.cs:133`
- SQL path exists and CHINHANH is granted execute:
  - `sql/03_publisher_sp_views.sql:654`
  - `sql/04_publisher_security.sql:101`

3. NGANHANG SQL over-permission:
- **FIXED**
- Current NGANHANG grants are report/admin/branch oriented, no broad operational CRUD grants.
  - `sql/04_publisher_security.sql:44`
  - `sql/04_publisher_security.sql:63`

---

## 6) Recommended Next Actions

1. Apply updated scripts to database environments:
- Re-run `sql/03_publisher_sp_views.sql`
- Re-run `sql/04_publisher_security.sql`
2. Smoke test end-to-end:
- CHINHANH transfer employee to another branch
- CHINHANH create login (ChiNhanh/KhachHang)
- CHINHANH cannot update/delete/reset other user logins
