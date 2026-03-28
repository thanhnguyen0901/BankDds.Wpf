# Huong dan reset linked server bang SQL script

Tai lieu nay dung cho truong hop muon xoa toan bo linked server cu va tao lai dung mapping cho toan he thong, khong fix rieng tung account.

## Muc tieu

- Xoa sach linked server cu.
- Tao lai dung mapping `LINK0/LINK1/LINK2`.
- Cau hinh lai login mapping dung chung cho toan he thong.
- Bat day du `Data Access`, `RPC`, `RPC Out`.
- Test lai ca read path va RPC path.

## Luu y truoc khi chay

1. Chay tung script trong query window dang connect dung instance.
2. Tat ca script linked server nen chay trong `master`.
3. Thay cac placeholder mat khau:
   - `<SA_PASSWORD_SQLSERVER2>`
   - `<SA_PASSWORD_SQLSERVER3>`
   - `<SA_PASSWORD_SQLSERVER4>`
4. Neu 3 instance dich dung cung 1 mat khau `sa`, van thay day du cho tung link de de kiem soat.
5. Sau moi script recreate, chay ngay script test cua instance do.

## Mapping chuan

Tren `DESKTOP-JBB41QU` (Publisher):

- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`
- `LINK2` -> `DESKTOP-JBB41QU\SQLSERVER3`

Tren `DESKTOP-JBB41QU\SQLSERVER2`:

- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER3`

Tren `DESKTOP-JBB41QU\SQLSERVER3`:

- `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`
- `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`

## Script 1: Publisher `DESKTOP-JBB41QU`

Mo query window moi, connect vao `DESKTOP-JBB41QU`, roi chay:

```sql
USE master;
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK2')
    EXEC sp_dropserver @server = N'LINK2', @droplogins = 'droplogins';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK0',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK0',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER4>';
GO

EXEC sp_serveroption N'LINK0', N'data access', N'true';
EXEC sp_serveroption N'LINK0', N'rpc',         N'true';
EXEC sp_serveroption N'LINK0', N'rpc out',     N'true';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK1',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER2>';
GO

EXEC sp_serveroption N'LINK1', N'data access', N'true';
EXEC sp_serveroption N'LINK1', N'rpc',         N'true';
EXEC sp_serveroption N'LINK1', N'rpc out',     N'true';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK2',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK2',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER3>';
GO

EXEC sp_serveroption N'LINK2', N'data access', N'true';
EXEC sp_serveroption N'LINK2', N'rpc',         N'true';
EXEC sp_serveroption N'LINK2', N'rpc out',     N'true';
GO

SELECT
    name,
    data_source,
    provider,
    is_data_access_enabled,
    is_rpc_out_enabled
FROM sys.servers
WHERE is_linked = 1
  AND name IN (N'LINK0', N'LINK1', N'LINK2')
ORDER BY name;
GO
```

## Script 2: `DESKTOP-JBB41QU\SQLSERVER2`

Mo query window moi, connect vao `DESKTOP-JBB41QU\SQLSERVER2`, roi chay:

```sql
USE master;
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK0',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK0',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER4>';
GO

EXEC sp_serveroption N'LINK0', N'data access', N'true';
EXEC sp_serveroption N'LINK0', N'rpc',         N'true';
EXEC sp_serveroption N'LINK0', N'rpc out',     N'true';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK1',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER3>';
GO

EXEC sp_serveroption N'LINK1', N'data access', N'true';
EXEC sp_serveroption N'LINK1', N'rpc',         N'true';
EXEC sp_serveroption N'LINK1', N'rpc out',     N'true';
GO

SELECT
    name,
    data_source,
    provider,
    is_data_access_enabled,
    is_rpc_out_enabled
FROM sys.servers
WHERE is_linked = 1
  AND name IN (N'LINK0', N'LINK1')
ORDER BY name;
GO
```

## Script 3: `DESKTOP-JBB41QU\SQLSERVER3`

Mo query window moi, connect vao `DESKTOP-JBB41QU\SQLSERVER3`, roi chay:

```sql
USE master;
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK0',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK0',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER4>';
GO

EXEC sp_serveroption N'LINK0', N'data access', N'true';
EXEC sp_serveroption N'LINK0', N'rpc',         N'true';
EXEC sp_serveroption N'LINK0', N'rpc out',     N'true';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK1',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD_SQLSERVER2>';
GO

EXEC sp_serveroption N'LINK1', N'data access', N'true';
EXEC sp_serveroption N'LINK1', N'rpc',         N'true';
EXEC sp_serveroption N'LINK1', N'rpc out',     N'true';
GO

SELECT
    name,
    data_source,
    provider,
    is_data_access_enabled,
    is_rpc_out_enabled
FROM sys.servers
WHERE is_linked = 1
  AND name IN (N'LINK0', N'LINK1')
ORDER BY name;
GO
```

## Script 4: Test tren Publisher voi account admin

Connect vao `DESKTOP-JBB41QU`, chay:

```sql
EXEC master.dbo.sp_testlinkedserver N'LINK0';
GO
EXEC master.dbo.sp_testlinkedserver N'LINK1';
GO
EXEC master.dbo.sp_testlinkedserver N'LINK2';
GO

SELECT TOP 3 * FROM [LINK0].[NGANHANG].dbo.CHINHANH;
GO
SELECT TOP 3 * FROM [LINK1].[NGANHANG].dbo.CHINHANH;
GO
SELECT TOP 3 * FROM [LINK2].[NGANHANG].dbo.CHINHANH;
GO
```

Neu 6 cau lenh tren pass, moi chuyen sang test RPC path.

## Script 5: Test direct RPC tren Publisher bang account app

Dang nhap SSMS vao `DESKTOP-JBB41QU` bang chinh SQL login dung trong app, vi du `NV_BT`, roi chay:

```sql
SELECT SYSTEM_USER AS CurrentLogin, USER_NAME() AS CurrentDbUser;
GO

EXEC [LINK0].master.dbo.sp_executesql
    N'SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;';
GO

EXEC [LINK1].master.dbo.sp_executesql
    N'SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;';
GO

EXEC [LINK2].master.dbo.sp_executesql
    N'SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;';
GO

EXEC ('SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;') AT LINK0;
GO

EXEC ('SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;') AT LINK1;
GO

EXEC ('SELECT SYSTEM_USER AS RemoteLogin, USER_NAME() AS RemoteDbUser;') AT LINK2;
GO
```

Luu y:

- direct RPC test nay khong nhat thiet phai pass voi account nghiep vu
- no duoc dung de xac nhan account app co direct linked-server RPC hay khong
- flow tao user cua app se di qua stored procedure, khong phai direct RPC query

Neu direct RPC test van fail:

- dieu do chua du de ket luan linked server recreate sai
- can test tiep proc path ben duoi

## Script 5b: Bat trust cho proc path `EXECUTE AS OWNER`

Neu khi goi `sp_TaoTaiKhoan` bang account app ma loi:

- `Access to the remote server is denied because the current security context is not trusted`

thi tren Publisher `DESKTOP-JBB41QU`, dang nhap bang `sa` va chay:

```sql
USE master;
GO

ALTER AUTHORIZATION ON DATABASE::NGANHANG TO sa;
GO

ALTER DATABASE NGANHANG SET TRUSTWORTHY ON;
GO

SELECT
    name,
    SUSER_SNAME(owner_sid) AS DbOwner,
    is_trustworthy_on
FROM sys.databases
WHERE name = N'NGANHANG';
GO
```

Ky vong:

- `DbOwner = sa`
- `is_trustworthy_on = 1`

Sau do dang nhap lai bang account app va test proc path:

```sql
USE NGANHANG;
GO

EXEC dbo.sp_TaoTaiKhoan
    @LOGIN   = N'KH_TEST_EXECOWNER_01',
    @PASS    = N'Test@123',
    @TENNHOM = N'KHACHHANG';
GO
```

## Script 5c: Rerun cac proc security sau khi cap nhat SQL trong repo

Sau khi pull code moi co patch security, tren Publisher `DESKTOP-JBB41QU` dang nhap bang `sa` va rerun lai cac block trong file [sql/04_publisher_security.sql](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/sql/04_publisher_security.sql):

- `CREATE OR ALTER PROCEDURE dbo.sp_SyncSecurityToSubscribers`
- `CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoan`
- `CREATE OR ALTER PROCEDURE dbo.sp_XoaTaiKhoan`
- `CREATE OR ALTER PROCEDURE dbo.sp_DoiMatKhau`
- `CREATE OR ALTER PROCEDURE dbo.USP_AddUser`

Ly do:

- cac proc nay da duoc doi sang mo hinh `EXECUTE AS OWNER`
- phan kiem tra quyen nay dua tren `ORIGINAL_LOGIN()`, khong con phu thuoc `USER_NAME()` cua owner context
- `sp_TaoTaiKhoan` co cleanup local `LOGIN/USER` neu sync remote fail, tranh partial-create
- `USP_AddUser` cung can rerun de nhin thay SQL principal/role vua tao va giu dung phan quyen caller theo `ORIGINAL_LOGIN()`

Neu proc path pass, cleanup account test:

```sql
USE NGANHANG;
GO
EXEC dbo.sp_XoaTaiKhoan @LOGIN = N'KH_TEST_EXECOWNER_01';
GO
```

Neu proc path van fail sau buoc nay:

- gui lai exact error message moi
- luc do moi tiep tuc khoanh vung o layer SQL security/module execution context

Neu loi moi la:

- `Sync security to LINK0 failed: Login failed for user 'sa'.`
- hoac tuong tu voi `LINK1` / `LINK2`

thi nghia la linked server da qua buoc mapping/trust, nhung credential `sa` dang luu trong linked server khong dang nhap duoc vao instance dich. Khi do phai kiem tra lai tren instance dich:

- login `sa` co duoc enable khong
- mat khau `sa` da dung chua
- linked server `@rmtpassword` da nhap dung chua

Day la loi ha tang SQL/credential tren may dich, khong phai loi app.

## Script 6: Cleanup user tao do neu bi partial state

Sau khi linked server da on dinh, neu username tung tao loi can cleanup truoc khi tao lai, connect vao Publisher `DESKTOP-JBB41QU` va chay:

```sql
USE NGANHANG;
GO

SELECT name, type_desc
FROM sys.server_principals
WHERE name = N'thanhnguyen0901';
GO

SELECT name, type_desc
FROM sys.database_principals
WHERE name = N'thanhnguyen0901';
GO

SELECT rp.name AS RoleName
FROM sys.database_role_members drm
JOIN sys.database_principals dp ON dp.principal_id = drm.member_principal_id
JOIN sys.database_principals rp ON rp.principal_id = drm.role_principal_id
WHERE dp.name = N'thanhnguyen0901';
GO

SELECT Username, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa
FROM dbo.NGUOIDUNG
WHERE Username = N'thanhnguyen0901';
GO
```

Neu con partial state, cleanup:

```sql
USE NGANHANG;
GO
EXEC dbo.sp_XoaTaiKhoan @LOGIN = N'thanhnguyen0901';
GO
```

Roi kiem tra lai 4 query tren den khi sach.

## Thu tu thuc hien de nghi

1. Chay Script 1 tren Publisher.
2. Chay Script 2 tren `SQLSERVER2`.
3. Chay Script 3 tren `SQLSERVER3`.
4. Chay Script 4 tren Publisher.
5. Chay Script 5 tren Publisher bang login app, vi du `NV_BT`.
6. Neu proc path bao `current security context is not trusted`, chay them Script 5b tren Publisher bang `sa`.
7. Rerun Script 5c neu vua cap nhat SQL patch trong repo.
8. Neu da on, chay Script 6 neu can cleanup username tao do.
9. Quay lai app, tao user lai.

## Ket qua mong doi

Khi xong:

- linked server duoc tao moi dung mapping tren toan he thong
- read path pass
- proc path `sp_TaoTaiKhoan` / `sp_SyncSecurityToSubscribers` pass voi account app
- flow `sp_SyncSecurityToSubscribers` khong con fail vi login mapping
- man `Tao nguoi dung` hoat dong on dinh cho moi account hop le, khong phai chi rieng `NV_BT`
