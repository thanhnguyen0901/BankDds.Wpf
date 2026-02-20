/*=============================================================================
  16-linked-servers.sql — Linked Server Configuration Scripts
  Generated: 2026-02-18 (DE03 compliance patch)
  Updated:   2026-02-20 — topology aligned to distributed lab setup guide

  PURPOSE
  -------
  Executable scripts to create Linked Server entries required for:
    • Cross-branch views on Coordinator (SECTION D of 01-schema.sql)
    • TraCuu views on SERVER3           (SECTION E of 01-schema.sql)
    • SP_CrossBranchTransfer            (MSDTC distributed transactions)
    • SP_GetAllEmployees / SP_GetAllCustomers / SP_GetAllAccounts on Coordinator

  TOPOLOGY
  --------
    Coordinator (default instance) : DESKTOP-JBB41QU            → DB NGANHANG         (Bank_Main)
    SERVER1  (linked server name)  : DESKTOP-JBB41QU\SQLSERVER2 → DB NGANHANG_BT      (BENTHANH)
    SERVER2  (linked server name)  : DESKTOP-JBB41QU\SQLSERVER3 → DB NGANHANG_TD      (TANDINH)
    SERVER3  (linked server name)  : DESKTOP-JBB41QU\SQLSERVER4 → DB NGANHANG_TRACUU  (TraCuu)

  EXECUTION ORDER
  ---------------
    Section A  → run on Coordinator (DESKTOP-JBB41QU)
                 creates SERVER1, SERVER2, SERVER3
    Section B  → run on SERVER1     (DESKTOP-JBB41QU\SQLSERVER2)
                 creates SERVER2  (for cross-branch transfer)
    Section C  → run on SERVER2     (DESKTOP-JBB41QU\SQLSERVER3)
                 creates SERVER1  (for cross-branch transfer)
    Section D  → run on SERVER3     (DESKTOP-JBB41QU\SQLSERVER4 — TraCuu)
                 creates SERVER1, SERVER2  (for tra cứu KH union views)

  AFTER RUNNING THIS SCRIPT
  -------------------------
  1. Verify linked servers on each instance:
       EXEC sp_linkedservers;
       SELECT name, data_source FROM sys.servers ORDER BY name;
  2. Test connectivity:
       -- from Coordinator:
       SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].KHACHHANG;
       SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].KHACHHANG;
       SELECT TOP 1 * FROM [SERVER3].[NGANHANG_TRACUU].[dbo].V_KHACHHANG_ALL;
       -- from SERVER1:
       SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].NHANVIEN;
       -- from SERVER2:
       SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].NHANVIEN;
       -- from SERVER3 (TraCuu):
       SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].KHACHHANG;
       SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].KHACHHANG;
  3. Enable MSDTC (required for SP_CrossBranchTransfer):
       Component Services → My Computer → Distributed Transaction Coordinator
       → Local DTC → Security → enable "Network DTC Access",
       "Allow Remote Clients", "Allow Inbound", "Allow Outbound".
       No Authentication Required (lab).
       Repeat on all machines / instances.
  4. Run 01-schema.sql SECTION D & E (cross-branch + TraCuu views) after linked
     servers exist.

  SECURITY NOTE
  -------------
  Current credentials: sa / Password!123
  Replace with dedicated service accounts before deploying beyond local dev.

=============================================================================*/


/* =========================================================================
   SECTION A — Run on Coordinator (default instance: DESKTOP-JBB41QU)
   Creates linked servers to all 3 remote instances:
     SERVER1 → DESKTOP-JBB41QU\SQLSERVER2 (BENTHANH  / NGANHANG_BT)
     SERVER2 → DESKTOP-JBB41QU\SQLSERVER3 (TANDINH   / NGANHANG_TD)
     SERVER3 → DESKTOP-JBB41QU\SQLSERVER4 (TraCuu    / NGANHANG_TRACUU)
   Required for cross-branch views and Bank_Main SPs.
   ========================================================================= */

USE NGANHANG;
GO

-- ── Coordinator → SERVER1 (BENTHANH) ─────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER1')
BEGIN
    EXEC sp_dropserver @server = N'SERVER1', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER1 linked server.';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER1',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER1', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'Coordinator → SERVER1 linked server created and configured.';
GO

-- ── Coordinator → SERVER2 (TANDINH) ──────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER2')
BEGIN
    EXEC sp_dropserver @server = N'SERVER2', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER2 linked server.';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER2',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER2',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER2', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'Coordinator → SERVER2 linked server created and configured.';
GO

-- ── Coordinator → SERVER3 (TRACUU) ───────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER3')
BEGIN
    EXEC sp_dropserver @server = N'SERVER3', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER3 linked server.';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER3',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER3',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER3', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER3', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER3', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'Coordinator → SERVER3 linked server created and configured.';
GO


/* =========================================================================
   SECTION B — Run on SERVER1 (DESKTOP-JBB41QU\SQLSERVER2 / NGANHANG_BT)
   Creates linked server to SERVER2 (TANDINH) for cross-branch transfers.
   ========================================================================= */

-- ── SERVER1 → SERVER2 (TANDINH) ──────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER2')
BEGIN
    EXEC sp_dropserver @server = N'SERVER2', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER2 linked server on SERVER1.';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER2',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER2',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER2', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'SERVER1 → SERVER2 linked server created and configured.';
GO


/* =========================================================================
   SECTION C — Run on SERVER2 (DESKTOP-JBB41QU\SQLSERVER3 / NGANHANG_TD)
   Creates linked server to SERVER1 (BENTHANH) for cross-branch transfers.
   ========================================================================= */

-- ── SERVER2 → SERVER1 (BENTHANH) ─────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER1')
BEGIN
    EXEC sp_dropserver @server = N'SERVER1', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER1 linked server on SERVER2.';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER1',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER1', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'SERVER2 → SERVER1 linked server created and configured.';
GO


/* =========================================================================
   SECTION D — Run on SERVER3 / TraCuu (DESKTOP-JBB41QU\SQLSERVER4 / NGANHANG_TRACUU)
   Creates linked servers to SERVER1 (BENTHANH) and SERVER2 (TANDINH) so that
   TraCuu can build union views across both branches.
   ========================================================================= */

-- ── SERVER3 → SERVER1 (BENTHANH) ─────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER1')
BEGIN
    EXEC sp_dropserver @server = N'SERVER1', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER1 linked server on SERVER3 (TraCuu).';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER1',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER2';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER1',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER1', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER1', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'SERVER3 (TraCuu) → SERVER1 linked server created and configured.';
GO

-- ── SERVER3 → SERVER2 (TANDINH) ──────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER2')
BEGIN
    EXEC sp_dropserver @server = N'SERVER2', @droplogins = 'droplogins';
    PRINT 'Dropped existing SERVER2 linked server on SERVER3 (TraCuu).';
END
GO
EXEC sp_addlinkedserver
    @server     = N'SERVER2',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER3';
GO
EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'SERVER2',
    @useself     = 'false',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'Password!123';
GO
EXEC sp_serveroption @server = N'SERVER2', @optname = 'data access',    @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc',            @optvalue = 'true';
EXEC sp_serveroption @server = N'SERVER2', @optname = 'rpc out',        @optvalue = 'true';
GO
PRINT 'SERVER3 (TraCuu) → SERVER2 linked server created and configured.';
GO


/* =========================================================================
   VERIFICATION QUERIES — Run after setup to confirm connectivity
   ========================================================================= */

-- From Coordinator (DESKTOP-JBB41QU):
-- SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].KHACHHANG;
-- SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].KHACHHANG;

-- From SERVER1 (DESKTOP-JBB41QU\SQLSERVER2):
-- SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].NHANVIEN;

-- From SERVER2 (DESKTOP-JBB41QU\SQLSERVER3):
-- SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].NHANVIEN;

-- From SERVER3 / TraCuu (DESKTOP-JBB41QU\SQLSERVER4):
-- SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].KHACHHANG;
-- SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].KHACHHANG;

-- Test cross-branch views (run on Coordinator after SECTION D of 01-schema.sql)
-- SELECT COUNT(*) AS TotalCustomers FROM [NGANHANG].dbo.KHACHHANG_ALL;
-- SELECT COUNT(*) AS TotalEmployees FROM [NGANHANG].dbo.NHANVIEN_ALL;
-- SELECT COUNT(*) AS TotalAccounts  FROM [NGANHANG].dbo.TAIKHOAN_ALL;

-- Test TraCuu view (run on SERVER3 after SECTION E of 01-schema.sql)
-- SELECT COUNT(*) AS TotalCustomers FROM [NGANHANG_TRACUU].dbo.V_KHACHHANG_ALL;
