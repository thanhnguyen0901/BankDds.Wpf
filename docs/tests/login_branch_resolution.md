# Login Branch Resolution — Verification Guide

> **Created:** 2026-02-24  
> **Scope:** Verify that `sp_DangNhap` returns `MACN` (default branch code) and that
> `AuthService` + `LoginViewModel` correctly resolve `SelectedBranch` for every role.

---

## 1. Problem Statement

`sp_DangNhap` originally returned only 3 columns: `MANV`, `HOTEN`, `TENNHOM`.  
`AuthResult.DefaultBranch` was never populated, leaving `SelectedBranch` empty for
CHINHANH and KHACHHANG logins. This caused connection routing to fail.

## 2. Fix Summary

| Layer | Change |
|---|---|
| **SQL (Publisher)** `04_publisher_security.sql` | `sp_DangNhap` now returns a 4th column `MACN` by querying `NHANVIEN` (employee) or `KHACHHANG` (customer) table to match `SYSTEM_USER` to a branch code. |
| **SQL (Subscriber)** `08_subscribers_post_replication_fixups.sql` | `sp_DangNhap` returns `MACN` by reading `TOP 1 MACN FROM CHINHANH` — on a row-filtered subscriber this reliably returns the single local branch. |
| **C# AuthService** | Reads `MACN` column when present; catches `IndexOutOfRangeException` for backward-compat with old SP. Logs warning when branch is empty. |
| **C# LoginViewModel** | If `result.DefaultBranch` is empty for CHINHANH/KHACHHANG, falls back to the branch the user selected in the login dropdown (loaded from `view_DanhSachPhanManh`). |

## 3. Resolution Flow

```
sp_DangNhap on Publisher
  ├── NGANHANG  → MACN = NULL (not needed; NGANHANG picks from dropdown)
  ├── CHINHANH  → MACN = NHANVIEN.MACN where MANV = SYSTEM_USER
  └── KHACHHANG → MACN = KHACHHANG.MACN where CMND = SYSTEM_USER

AuthService.LoginAsync()
  ├── Reads MACN column → sets AuthResult.DefaultBranch
  └── If column missing or NULL → logs warning, returns empty DefaultBranch

LoginViewModel.Login()
  ├── NGANHANG:   ignores DefaultBranch, uses all branches from dropdown
  ├── CHINHANH:   uses DefaultBranch if present, else dropdown SelectedBranch
  └── KHACHHANG:  uses DefaultBranch if present, else dropdown SelectedBranch
```

## 4. SQL Verification (Run on Publisher)

### 4a. Confirm sp_DangNhap returns MACN column

```sql
USE NGANHANG_PUB;
-- Run as sa (will return NGANHANG role, MACN = NULL — expected)
EXEC sp_DangNhap;
-- Expected columns: MANV, HOTEN, TENNHOM, MACN
```

### 4b. Test with CHINHANH login (requires seed employee in NHANVIEN)

```sql
USE NGANHANG_PUB;

-- First ensure a NHANVIEN row exists for NV_BT
-- (If seed data is not loaded, insert a test row)
IF NOT EXISTS (SELECT 1 FROM NHANVIEN WHERE MANV = N'NV_BT')
BEGIN
    INSERT INTO NHANVIEN (MANV, HO, TEN, DIACHI, NGAYSINH, LUONG, MACN, TrangThaiXoa)
    VALUES (N'NV_BT', N'Nguyen', N'Van A', N'123 Le Loi', '1990-01-01', 10000000, N'BENTHANH', 0);
END

-- Now test sp_DangNhap as NV_BT
EXECUTE AS USER = 'NV_BT';
EXEC sp_DangNhap;
-- Expected: MANV=NV_BT, HOTEN=NV_BT, TENNHOM=CHINHANH, MACN=BENTHANH
REVERT;
```

### 4c. Test with KHACHHANG login

```sql
USE NGANHANG_PUB;

-- Ensure a KHACHHANG row with CMND matching the login name
IF NOT EXISTS (SELECT 1 FROM KHACHHANG WHERE CMND = N'KH_DEMO')
BEGIN
    INSERT INTO KHACHHANG (CMND, HO, TEN, DIACHI, NGAYSINH, SODT, MACN, TrangThaiXoa)
    VALUES (N'KH_DEMO', N'Tran', N'Thi B', N'456 Hai Ba Trung', '1985-06-15', N'0901234567', N'BENTHANH', 0);
END

EXECUTE AS USER = 'KH_DEMO';
EXEC sp_DangNhap;
-- Expected: MANV=KH_DEMO, HOTEN=KH_DEMO, TENNHOM=KHACHHANG, MACN=BENTHANH
REVERT;
```

### 4d. Test on Subscriber (CN1)

```sql
-- Connect to: DESKTOP-JBB41QU\SQLSERVER2
USE NGANHANG_BT;

EXEC sp_DangNhap;
-- Expected for any CHINHANH/KHACHHANG: MACN = BENTHANH
-- (derived from TOP 1 MACN FROM CHINHANH which is row-filtered to BENTHANH)
```

## 5. C# Verification (Manual App Test)

### Scenario A — CHINHANH login with SP-resolved branch

1. Run all SQL scripts (01–08) including updated `sp_DangNhap`.
2. Insert a NHANVIEN row for `NV_BT` with `MACN = 'BENTHANH'`.
3. Start the app: `dotnet run --project BankDds.Wpf`.
4. Login: `NV_BT` / `NhanVien@123`, select any branch in dropdown.
5. **Expected:** After login, `SelectedBranch` = `BENTHANH` regardless of dropdown choice
   (SP returned the authoritative branch).
6. HomeView shows `Chi Nhanh (BENTHANH)` in the role text.

### Scenario B — CHINHANH login without NHANVIEN row (fallback)

1. Ensure `NV_BT` has no matching NHANVIEN row (or use a newly created CHINHANH login).
2. Login: `NV_BT` / `NhanVien@123`, select `BENTHANH` in the dropdown.
3. **Expected:** `sp_DangNhap` returns `MACN = NULL` → AuthService logs warning →
   LoginViewModel uses the dropdown selection `BENTHANH`.
4. Check Debug Output window for warning:
   ```
   sp_DangNhap returned empty MACN for 'NV_BT' (role=ChiNhanh). Branch will be inferred from the login form selection.
   ```

### Scenario C — NGANHANG login ignores DefaultBranch

1. Login: `ADMIN_NH` / `Admin@123`, select `TANDINH` in dropdown.
2. **Expected:** `SelectedBranch` = `TANDINH` (from dropdown, not SP).
3. Branch dropdown visible in HomeView (can switch to `BENTHANH`).

## 6. Edge Cases

| Scenario | Expected Behavior |
|---|---|
| SP returns MACN with trailing spaces (nChar(10)) | `AuthService` calls `.Trim()` on the value |
| SP returns NULL MACN for NGANHANG | Ignored — NGANHANG uses permitted branches from dropdown |
| Old SP without MACN column deployed | `IndexOutOfRangeException` caught; warning logged; falls back to dropdown |
| Login form dropdown is empty (Publisher unreachable) | Hardcoded fallback `BENTHANH`/`TANDINH` used; branch routing still works |
| Both SP and dropdown have empty branch | `SelectedBranch` will be empty → `GetConnectionStringForBranch("")` throws `InvalidOperationException` — user sees error in UI |
