USE master;
GO

-- Tài khoản dùng để đăng nhập linked server từ các instance.
DECLARE @RemoteUser nvarchar(128) = N'sa';
DECLARE @RemotePass nvarchar(128) = N'Password!123';
GO

-- Phần A: cấu hình linked server khi chạy trên Publisher (default instance).
IF CHARINDEX(N'\', @@SERVERNAME) = 0
BEGIN
    DECLARE @HostA nvarchar(128) = CAST(SERVERPROPERTY('MachineName') AS nvarchar(128));
    DECLARE @Cn1A sysname = N'SQLSERVER2';
    DECLARE @Cn2A sysname = N'SQLSERVER3';
    DECLARE @TcA  sysname = N'SQLSERVER4';

    PRINT N'';
    PRINT N'--- Phần A: Linked server trên Publisher ---';
    PRINT N'Instance: ' + @@SERVERNAME;

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK1 cũ.';
    END

    DECLARE @DataSrcA_LINK1 nvarchar(260) = @HostA + N'\' + @Cn1A;
    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcA_LINK1;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT N'  LINK1 -> SQLSERVER2 (NGANHANG_BT) OK';

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK2')
    BEGIN
        EXEC sp_dropserver @server = N'LINK2', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK2 cũ.';
    END

    DECLARE @DataSrcA_LINK2 nvarchar(260) = @HostA + N'\' + @Cn2A;
    EXEC sp_addlinkedserver
        @server     = N'LINK2',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcA_LINK2;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK2',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK2', 'rpc',         'true';
    EXEC sp_serveroption N'LINK2', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK2', 'data access', 'true';
    PRINT N'  LINK2 -> SQLSERVER3 (NGANHANG_TD) OK';

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK0 cũ.';
    END

    DECLARE @DataSrcA_LINK0 nvarchar(260) = @HostA + N'\' + @TcA;
    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcA_LINK0;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT N'  LINK0 -> SQLSERVER4 (NGANHANG_TRACUU) OK';

    PRINT N'>>> Hoàn tất Phần A: Publisher đã sẵn sàng linked server.';
END
ELSE
    PRINT N'Phần A: Bỏ qua (không phải Publisher instance).';
GO

-- Phần B: cấu hình linked server khi chạy trên CN1 (SQLSERVER2).
IF RIGHT(@@SERVERNAME, LEN(N'\SQLSERVER2')) = N'\SQLSERVER2'
BEGIN
    DECLARE @HostB nvarchar(128) = CAST(SERVERPROPERTY('MachineName') AS nvarchar(128));
    DECLARE @Cn2B sysname = N'SQLSERVER3';
    DECLARE @TcB  sysname = N'SQLSERVER4';

    PRINT N'';
    PRINT N'--- Phần B: Linked server trên CN1 ---';
    PRINT N'Instance: ' + @@SERVERNAME;

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK1 cũ.';
    END

    DECLARE @DataSrcB_LINK1 nvarchar(260) = @HostB + N'\' + @Cn2B;
    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcB_LINK1;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT N'  LINK1 -> SQLSERVER3 (NGANHANG_TD - chi nhánh còn lại) OK';

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK0 cũ.';
    END

    DECLARE @DataSrcB_LINK0 nvarchar(260) = @HostB + N'\' + @TcB;
    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcB_LINK0;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT N'  LINK0 -> SQLSERVER4 (NGANHANG_TRACUU) OK';

    PRINT N'>>> Hoàn tất Phần B: CN1 đã sẵn sàng linked server.';
END
ELSE
    PRINT N'Phần B: Bỏ qua (không phải CN1 instance).';
GO

-- Phần C: cấu hình linked server khi chạy trên CN2 (SQLSERVER3).
IF RIGHT(@@SERVERNAME, LEN(N'\SQLSERVER3')) = N'\SQLSERVER3'
BEGIN
    DECLARE @HostC nvarchar(128) = CAST(SERVERPROPERTY('MachineName') AS nvarchar(128));
    DECLARE @Cn1C sysname = N'SQLSERVER2';
    DECLARE @TcC  sysname = N'SQLSERVER4';

    PRINT N'';
    PRINT N'--- Phần C: Linked server trên CN2 ---';
    PRINT N'Instance: ' + @@SERVERNAME;

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK1')
    BEGIN
        EXEC sp_dropserver @server = N'LINK1', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK1 cũ.';
    END

    DECLARE @DataSrcC_LINK1 nvarchar(260) = @HostC + N'\' + @Cn1C;
    EXEC sp_addlinkedserver
        @server     = N'LINK1',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcC_LINK1;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK1',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK1', 'rpc',         'true';
    EXEC sp_serveroption N'LINK1', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK1', 'data access', 'true';
    PRINT N'  LINK1 -> SQLSERVER2 (NGANHANG_BT - chi nhánh còn lại) OK';

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    BEGIN
        EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
        PRINT N'  Đã xóa LINK0 cũ.';
    END

    DECLARE @DataSrcC_LINK0 nvarchar(260) = @HostC + N'\' + @TcC;
    EXEC sp_addlinkedserver
        @server     = N'LINK0',
        @srvproduct = N'',
        @provider   = N'MSOLEDBSQL',
        @datasrc    = @DataSrcC_LINK0;

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'LINK0',
        @useself     = N'false',
        @locallogin  = NULL,
        @rmtuser     = @RemoteUser,
        @rmtpassword = @RemotePass;

    EXEC sp_serveroption N'LINK0', 'rpc',         'true';
    EXEC sp_serveroption N'LINK0', 'rpc out',     'true';
    EXEC sp_serveroption N'LINK0', 'data access', 'true';
    PRINT N'  LINK0 -> SQLSERVER4 (NGANHANG_TRACUU) OK';

    PRINT N'>>> Hoàn tất Phần C: CN2 đã sẵn sàng linked server.';
END
ELSE
    PRINT N'Phần C: Bỏ qua (không phải CN2 instance).';
GO

-- Tổng hợp linked server sau khi cấu hình.
PRINT N'';
PRINT N'--- Danh sách linked server trên ' + @@SERVERNAME + N' ---';
SELECT
    s.name          AS LinkedServerName,
    s.data_source   AS DataSource,
    s.provider      AS Provider,
    s.is_remote_login_enabled  AS RpcEnabled,
    s.is_data_access_enabled   AS DataAccessEnabled
FROM sys.servers s
WHERE s.name IN (N'LINK0', N'LINK1', N'LINK2')
ORDER BY s.name;
GO

PRINT N'';
PRINT N'=== Hoàn tất 06_linked_servers.sql trên ' + @@SERVERNAME + N' ===';
GO
