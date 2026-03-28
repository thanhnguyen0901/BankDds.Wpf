# Runbook fix loi `ChiNhanh` tao user

Tai lieu nay dung cho may khac trong team khi gap loi role `ChiNhanh` tao account `KhachHang` bi fail o buoc sync security qua linked server.

## Ket luan root cause

Bug nay khong phai loi UI. Root cause dung la tong hop 3 nhom van de:

1. Linked server `LINK0/LINK1/LINK2` luu sai remote SQL credential, dan den `Login failed for user 'sa'`.
2. SQL security procedures tren Publisher truoc day vua check quyen vua thuc thi thao tac dac quyen theo caller context, dan den fail khi account `ChiNhanh` tao login/user/role.
3. `USP_AddUser` chua duoc doi sang mo hinh `EXECUTE AS OWNER`, nen sau khi `sp_TaoTaiKhoan` tao xong SQL principal thi proc mapping `NGUOIDUNG` van co the bao `Username ... chua duoc tao SQL login/user trong database`.

## File can dung

1. Recreate linked server:
   - [HUONG_DAN_RESET_LINKED_SERVER_SQL_ONLY_2026-03-28.md](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/sql/HUONG_DAN_RESET_LINKED_SERVER_SQL_ONLY_2026-03-28.md)
2. Rerun patch SQL proc:
   - [PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/sql/PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql)

## Chay file nao o dau

### 1. Tren `DESKTOP-JBB41QU\\SQLSERVER4`

Khong chay file patch proc o day.

Can kiem tra:
- SQL Authentication da bat
- login `sa` dang `Enabled`
- dang nhap truc tiep vao `SQLSERVER4` bang `sa` va dung password that su thanh cong

Neu dang nhap bang `sa` khong duoc:
- reset lai password `sa`
- enable `sa`

### 2. Tren `DESKTOP-JBB41QU\\SQLSERVER2`

Khong chay file patch proc o day.

Can dam bao:
- dang nhap duoc bang `sa`
- linked server duoc recreate dung theo huong dan mapping

### 3. Tren `DESKTOP-JBB41QU\\SQLSERVER3`

Khong chay file patch proc o day.

Can dam bao:
- dang nhap duoc bang `sa`
- linked server duoc recreate dung theo huong dan mapping

### 4. Tren Publisher `DESKTOP-JBB41QU`

Can lam day du:

1. Recreate `LINK0`, `LINK1`, `LINK2` bang dung password `sa` cua tung instance dich.
2. Dam bao database `NGANHANG`:
   - owner = `sa`
   - `TRUSTWORTHY ON`
3. Chay toan bo file [PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/sql/PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql) bang `sa` hoac `sysadmin`.

## Thu tu thuc hien khuyen nghi tren may moi

1. Xac nhan tren `SQLSERVER4` dang login `sa` duoc bang password du kien.
2. Xac nhan tren `SQLSERVER2` dang login `sa` duoc bang password du kien.
3. Xac nhan tren `SQLSERVER3` dang login `sa` duoc bang password du kien.
4. Tren Publisher, recreate toan bo `LINK0/LINK1/LINK2`.
5. Tren `SQLSERVER2`, recreate `LINK0/LINK1`.
6. Tren `SQLSERVER3`, recreate `LINK0/LINK1`.
7. Tren Publisher, test:
   - `sp_testlinkedserver LINK0`
   - `sp_testlinkedserver LINK1`
   - `sp_testlinkedserver LINK2`
   - `SELECT TOP 1 * FROM [LINK0].[NGANHANG].dbo.CHINHANH`
   - `SELECT TOP 1 * FROM [LINK1].[NGANHANG].dbo.CHINHANH`
   - `SELECT TOP 1 * FROM [LINK2].[NGANHANG].dbo.CHINHANH`
8. Tren Publisher, bat:
   - `ALTER AUTHORIZATION ON DATABASE::NGANHANG TO sa`
   - `ALTER DATABASE NGANHANG SET TRUSTWORTHY ON`
9. Tren Publisher, chay toan bo file [PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/sql/PATCH_RERUN_SECURITY_PROCS_2026-03-28.sql).
10. Neu username da tao do o cac lan fail truoc, cleanup partial state roi moi test lai.
11. Vao app, dang nhap role `ChiNhanh`, tao lai user.

## Cleanup neu da co partial state

Chay tren Publisher `DESKTOP-JBB41QU` bang `sa`:

```sql
USE NGANHANG;
GO

IF EXISTS (SELECT 1 FROM dbo.NGUOIDUNG WHERE Username = N'<username>')
BEGIN
    DELETE FROM dbo.NGUOIDUNG
    WHERE Username = N'<username>';
END
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'<username>' AND type IN ('S', 'U'))
BEGIN
    DROP USER [<username>];
END
GO

USE master;
GO

IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'<username>')
BEGIN
    DROP LOGIN [<username>];
END
GO
```

## Dau hieu thanh cong

Sau khi fix dung:

1. `sp_testlinkedserver` pass cho `LINK0`, `LINK1`, `LINK2`.
2. Role `ChiNhanh` tao duoc account `KhachHang` trong app.
3. Account moi dang nhap duoc vao app.
4. `NGUOIDUNG` co dong mapping tuong ung.

## Loi gap lai va cach doc

- `Login failed for user 'sa'`
  - linked server dang luu sai password `sa` cua instance dich
- `Username ... chua duoc tao SQL login/user trong database`
  - Publisher chua rerun patch moi co `USP_AddUser`
- `Login ... da ton tai`
  - do partial state tu lan fail truoc, can cleanup
