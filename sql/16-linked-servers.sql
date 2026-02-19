/*=============================================================================
  16-linked-servers.sql — Linked Server Configuration Scripts
  Generated: 2026-02-18 (DE03 compliance patch)

  PURPOSE
  -------
  Executable scripts to create Linked Server entries required for:
    • Cross-branch views (SECTION D of 01-schema.sql)
    • SP_CrossBranchTransfer (MSDTC distributed transactions)
    • SP_GetAllEmployees / SP_GetAllCustomers / SP_GetAllAccounts on Bank_Main

  TOPOLOGY
  --------
    SERVER1  / NGANHANG_BT  : Chi nhánh Bến Thành  (branch code BENTHANH)
    SERVER2  / NGANHANG_TD  : Chi nhánh Tân Định    (branch code TANDINH)
    SERVER3  / NGANHANG     : Bank_Main (central tables + views + auth)

  EXECUTION ORDER
  ---------------
    Section A  → run on SERVER1 (creates outbound links SERVER1 → SERVER2, SERVER3)
    Section B  → run on SERVER2 (creates outbound links SERVER2 → SERVER1, SERVER3)
    Section C  → run on SERVER3 (creates outbound links SERVER3 → SERVER1, SERVER2)

  AFTER RUNNING THIS SCRIPT
  -------------------------
  1. Verify linked servers with:
       SELECT * FROM sys.servers;
  2. Test connectivity:
       SELECT * FROM [SERVER2].[NGANHANG_TD].[dbo].NHANVIEN;  -- from SERVER1
       SELECT * FROM [SERVER1].[NGANHANG_BT].[dbo].NHANVIEN;  -- from SERVER2
       SELECT * FROM [SERVER1].[NGANHANG_BT].[dbo].TAIKHOAN;  -- from SERVER3
  3. Enable MSDTC (required for SP_CrossBranchTransfer):
       Component Services → My Computer → Distributed Transaction Coordinator
       → Local DTC → Security → enable "Network DTC Access",
       "Allow Remote Clients", "Allow Remote Administration", "Allow Inbound".
       Repeat on all three servers.
  4. Run 01-schema.sql SECTION D (cross-branch views) after linked servers exist.

  SECURITY NOTE
  -------------
  Replace 'sa' / '123' with dedicated service-account credentials before
  deploying to any environment beyond local development.

=============================================================================*/


/* =========================================================================
   SECTION A — Run on SERVER1 / NGANHANG_BT
   Add outbound links to SERVER2 (TANDINH) and SERVER3 (Bank_Main)
   ========================================================================= */

-- ── SERVER1 → SERVER2 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER2')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER2',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER2',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER1 → SERVER2 linked server created.';
END
ELSE
    PRINT 'SERVER1 → SERVER2 already exists.';
GO

-- ── SERVER1 → SERVER3 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER3')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER3',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER3',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER1 → SERVER3 linked server created.';
END
ELSE
    PRINT 'SERVER1 → SERVER3 already exists.';
GO


/* =========================================================================
   SECTION B — Run on SERVER2 / NGANHANG_TD
   Add outbound links to SERVER1 (BENTHANH) and SERVER3 (Bank_Main)
   ========================================================================= */

-- ── SERVER2 → SERVER1 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER1')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER1',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER1',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER2 → SERVER1 linked server created.';
END
ELSE
    PRINT 'SERVER2 → SERVER1 already exists.';
GO

-- ── SERVER2 → SERVER3 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER3')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER3',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER3',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER2 → SERVER3 linked server created.';
END
ELSE
    PRINT 'SERVER2 → SERVER3 already exists.';
GO


/* =========================================================================
   SECTION C — Run on SERVER3 / NGANHANG (Bank_Main)
   Add outbound links to SERVER1 (BENTHANH) and SERVER2 (TANDINH)
   Required for cross-branch views in SECTION D of 01-schema.sql
   ========================================================================= */

USE NGANHANG;
GO

-- ── SERVER3 → SERVER1 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER1')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER1',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER1',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER3 → SERVER1 linked server created.';
END
ELSE
    PRINT 'SERVER3 → SERVER1 already exists.';
GO

-- ── SERVER3 → SERVER2 ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'SERVER2')
BEGIN
    EXEC sp_addlinkedserver
        @server     = N'SERVER2',
        @srvproduct = N'SQL Server';

    EXEC sp_addlinkedsrvlogin
        @rmtsrvname  = N'SERVER2',
        @useself     = 'false',
        @locallogin  = NULL,
        @rmtuser     = N'sa',
        @rmtpassword = N'123';

    PRINT 'SERVER3 → SERVER2 linked server created.';
END
ELSE
    PRINT 'SERVER3 → SERVER2 already exists.';
GO


/* =========================================================================
   VERIFICATION QUERIES — Run after setup to confirm connectivity
   These SELECT statements should return data from the remote servers.
   ========================================================================= */

-- From SERVER1: peek into SERVER2 and SERVER3
-- SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].NHANVIEN;
-- SELECT TOP 1 * FROM [SERVER3].[NGANHANG].[dbo].CHINHANH;

-- From SERVER2: peek into SERVER1 and SERVER3
-- SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].NHANVIEN;
-- SELECT TOP 1 * FROM [SERVER3].[NGANHANG].[dbo].CHINHANH;

-- From SERVER3: peek into SERVER1 and SERVER2
-- SELECT TOP 1 * FROM [SERVER1].[NGANHANG_BT].[dbo].KHACHHANG;
-- SELECT TOP 1 * FROM [SERVER2].[NGANHANG_TD].[dbo].KHACHHANG;

-- Test cross-branch view (run on SERVER3 after SECTION D of 01-schema.sql)
-- SELECT COUNT(*) AS TotalCustomers FROM dbo.KHACHHANG_ALL;
-- SELECT COUNT(*) AS TotalEmployees FROM dbo.NHANVIEN_ALL;
-- SELECT COUNT(*) AS TotalAccounts  FROM dbo.TAIKHOAN_ALL;
