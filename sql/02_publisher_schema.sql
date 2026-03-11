USE NGANHANG_PUB;
GO
-- Tạo hoặc bổ sung schema Publisher theo hướng idempotent.
IF OBJECT_ID('dbo.CHINHANH', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CHINHANH (
        MACN      nChar(10)         NOT NULL,
        TENCN     nvarchar(100)     NOT NULL,
        DIACHI    nvarchar(100)     NULL,
        SODT      nvarchar(15)      NULL,
        rowguid   uniqueidentifier  NOT NULL ROWGUIDCOL
                  CONSTRAINT DF_CN_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_CHINHANH  PRIMARY KEY (MACN),
        CONSTRAINT UQ_CN_TENCN  UNIQUE      (TENCN),
        CONSTRAINT UQ_CN_rowguid UNIQUE     (rowguid)
    );
    PRINT N'>>> Đã tạo bảng CHINHANH.';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.CHINHANH', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.CHINHANH
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_CN_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.CHINHANH
            ADD CONSTRAINT UQ_CN_rowguid UNIQUE (rowguid);
        PRINT N'>>> CHINHANH: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> CHINHANH đã có rowguid, bỏ qua.';
END
GO
-- Bảng tài khoản đăng nhập ứng dụng.
IF OBJECT_ID('dbo.NGUOIDUNG', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NGUOIDUNG (
        Username      nvarchar(50)  NOT NULL,
        PasswordHash  nvarchar(255) NOT NULL,
        UserGroup     int           NOT NULL,
        DefaultBranch nvarchar(20)  NOT NULL,
        CustomerCMND  nChar(10)     NULL,
        EmployeeId    nChar(10)     NULL,
        TrangThaiXoa  tinyint       NOT NULL DEFAULT 0,
        CONSTRAINT PK_NGUOIDUNG       PRIMARY KEY (Username),
        CONSTRAINT CK_ND_UserGroup    CHECK (UserGroup    IN (0, 1, 2)),
        CONSTRAINT CK_ND_TTX          CHECK (TrangThaiXoa IN (0, 1))
    );
    PRINT N'>>> Đã tạo bảng NGUOIDUNG.';
END
ELSE
    PRINT N'>>> NGUOIDUNG đã tồn tại, bỏ qua.';
GO
-- Sequence sinh mã nhân viên.
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE object_id = OBJECT_ID('dbo.SEQ_MANV'))
BEGIN
    CREATE SEQUENCE dbo.SEQ_MANV AS int START WITH 5 INCREMENT BY 1 NO CYCLE;
    PRINT N'>>> Đã tạo sequence SEQ_MANV (bắt đầu từ 5).';
END
ELSE
    PRINT N'>>> Sequence SEQ_MANV đã tồn tại, bỏ qua.';
GO
-- Bảng khách hàng và cột rowguid phục vụ replication.
IF OBJECT_ID('dbo.KHACHHANG', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KHACHHANG (
        CMND         nChar(10)         NOT NULL,
        HO           nvarchar(50)      NOT NULL,
        TEN          nvarchar(10)      NOT NULL,
        NGAYSINH     date              NULL,
        DIACHI       nvarchar(100)     NOT NULL,
        NGAYCAP      date              NOT NULL,
        SODT         nvarchar(15)      NULL,
        PHAI         nChar(3)          NOT NULL,
        MACN         nChar(10)         NOT NULL,
        TrangThaiXoa tinyint           NOT NULL DEFAULT 0,
        rowguid      uniqueidentifier  NOT NULL ROWGUIDCOL
                     CONSTRAINT DF_KH_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_KHACHHANG    PRIMARY KEY (CMND),
        CONSTRAINT CK_KH_PHAI     CHECK (PHAI IN (N'Nam', N'Nữ')),
        CONSTRAINT CK_KH_TTX      CHECK (TrangThaiXoa IN (0, 1)),
        CONSTRAINT UQ_KH_rowguid  UNIQUE (rowguid)
    );
    PRINT N'>>> Đã tạo bảng KHACHHANG.';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.KHACHHANG', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.KHACHHANG
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_KH_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.KHACHHANG
            ADD CONSTRAINT UQ_KH_rowguid UNIQUE (rowguid);
        PRINT N'>>> KHACHHANG: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> KHACHHANG đã có rowguid, bỏ qua.';
END
GO
-- Bảng nhân viên và cột rowguid phục vụ replication.
IF OBJECT_ID('dbo.NHANVIEN', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NHANVIEN (
        MANV         nChar(10)         NOT NULL,
        HO           nvarchar(50)      NOT NULL,
        TEN          nvarchar(10)      NOT NULL,
        DIACHI       nvarchar(100)     NOT NULL,
        CMND         nChar(10)         NOT NULL,
        PHAI         nChar(3)          NOT NULL,
        SODT         nvarchar(15)      NULL,
        MACN         nChar(10)         NOT NULL,
        TrangThaiXoa tinyint           NOT NULL DEFAULT 0,
        rowguid      uniqueidentifier  NOT NULL ROWGUIDCOL
                     CONSTRAINT DF_NV_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_NHANVIEN    PRIMARY KEY (MANV),
        CONSTRAINT UQ_NV_CMND    UNIQUE      (CMND),
        CONSTRAINT CK_NV_PHAI    CHECK (PHAI IN (N'Nam', N'Nữ')),
        CONSTRAINT CK_NV_TTX     CHECK (TrangThaiXoa IN (0, 1)),
        CONSTRAINT UQ_NV_rowguid UNIQUE (rowguid)
    );
    PRINT N'>>> Đã tạo bảng NHANVIEN.';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.NHANVIEN', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.NHANVIEN
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_NV_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.NHANVIEN
            ADD CONSTRAINT UQ_NV_rowguid UNIQUE (rowguid);
        PRINT N'>>> NHANVIEN: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> NHANVIEN đã có rowguid, bỏ qua.';
END
GO
-- Bảng tài khoản ngân hàng.
IF OBJECT_ID('dbo.TAIKHOAN', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TAIKHOAN (
        SOTK      nChar(9)          NOT NULL,
        CMND      nChar(10)         NOT NULL,
        SODU      money             NOT NULL DEFAULT 0,
        MACN      nChar(10)         NOT NULL,
        NGAYMOTK  datetime          NOT NULL DEFAULT GETDATE(),
        Status    nvarchar(10)      NOT NULL DEFAULT 'Active',
        rowguid   uniqueidentifier  NOT NULL ROWGUIDCOL
                  CONSTRAINT DF_TK_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_TAIKHOAN      PRIMARY KEY (SOTK),
        CONSTRAINT CK_TK_SODU       CHECK (SODU >= 0),
        CONSTRAINT CK_TK_STATUS     CHECK (Status IN ('Active', 'Closed')),
        CONSTRAINT FK_TK_KHACHHANG  FOREIGN KEY (CMND) REFERENCES dbo.KHACHHANG(CMND),
        CONSTRAINT UQ_TK_rowguid    UNIQUE (rowguid)
    );
    PRINT N'>>> Đã tạo bảng TAIKHOAN.';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.TAIKHOAN', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.TAIKHOAN
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_TK_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.TAIKHOAN
            ADD CONSTRAINT UQ_TK_rowguid UNIQUE (rowguid);
        PRINT N'>>> TAIKHOAN: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> TAIKHOAN đã có rowguid, bỏ qua.';
END
GO
-- Bảng giao dịch gửi/rút.
IF OBJECT_ID('dbo.GD_GOIRUT', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GD_GOIRUT (
        MAGD         int               NOT NULL IDENTITY(1,1),
        SOTK         nChar(9)          NOT NULL,
        LOAIGD       nChar(2)          NOT NULL,
        NGAYGD       datetime          NOT NULL DEFAULT GETDATE(),
        SOTIEN       money             NOT NULL DEFAULT 100000,
        MANV         nChar(10)         NOT NULL,
        MACN         nChar(10)         NOT NULL,
        Status       nvarchar(10)      NOT NULL DEFAULT 'Completed',
        ErrorMessage nvarchar(500)     NULL,
        rowguid      uniqueidentifier  NOT NULL ROWGUIDCOL
                     CONSTRAINT DF_GR_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_GD_GOIRUT    PRIMARY KEY (MAGD),
        CONSTRAINT CK_GR_LOAIGD   CHECK (LOAIGD IN ('GT', 'RT')),
        CONSTRAINT CK_GR_SOTIEN   CHECK (SOTIEN >= 100000),
        CONSTRAINT FK_GR_TAIKHOAN FOREIGN KEY (SOTK) REFERENCES dbo.TAIKHOAN(SOTK),
        CONSTRAINT FK_GR_MANV     FOREIGN KEY (MANV) REFERENCES dbo.NHANVIEN(MANV),
        CONSTRAINT UQ_GR_rowguid  UNIQUE (rowguid)
    );
    PRINT N'>>> Đã tạo bảng GD_GOIRUT (gồm MACN và rowguid).';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.GD_GOIRUT', 'MACN') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_GOIRUT ADD MACN nChar(10) NULL;
        UPDATE gr SET gr.MACN = tk.MACN
        FROM   dbo.GD_GOIRUT gr
        JOIN   dbo.TAIKHOAN  tk ON gr.SOTK = tk.SOTK
        WHERE  gr.MACN IS NULL;
        ALTER TABLE dbo.GD_GOIRUT ALTER COLUMN MACN nChar(10) NOT NULL;
        PRINT N'>>> GD_GOIRUT: đã thêm và điền dữ liệu cột MACN.';
    END
    IF COL_LENGTH('dbo.GD_GOIRUT', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_GOIRUT
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_GR_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.GD_GOIRUT
            ADD CONSTRAINT UQ_GR_rowguid UNIQUE (rowguid);
        PRINT N'>>> GD_GOIRUT: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> GD_GOIRUT đã đầy đủ cấu trúc, bỏ qua.';
END
GO
-- Bảng giao dịch chuyển tiền.
IF OBJECT_ID('dbo.GD_CHUYENTIEN', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GD_CHUYENTIEN (
        MAGD         int               NOT NULL IDENTITY(1,1),
        SOTK_CHUYEN  nChar(9)          NOT NULL,
        SOTK_NHAN    nChar(9)          NOT NULL,
        LOAIGD       nChar(2)          NOT NULL DEFAULT 'CT',
        NGAYGD       datetime          NOT NULL DEFAULT GETDATE(),
        SOTIEN       money             NOT NULL,
        MANV         nChar(10)         NOT NULL,
        MACN         nChar(10)         NOT NULL,
        Status       nvarchar(10)      NOT NULL DEFAULT 'Completed',
        ErrorMessage nvarchar(500)     NULL,
        rowguid      uniqueidentifier  NOT NULL ROWGUIDCOL
                     CONSTRAINT DF_CT_rowguid DEFAULT (NEWSEQUENTIALID()),
        CONSTRAINT PK_GD_CHUYENTIEN   PRIMARY KEY (MAGD),
        CONSTRAINT CK_CT_LOAIGD       CHECK (LOAIGD = 'CT'),
        CONSTRAINT CK_CT_SOTIEN       CHECK (SOTIEN > 0),
        CONSTRAINT FK_CT_SOTK_CHUYEN  FOREIGN KEY (SOTK_CHUYEN) REFERENCES dbo.TAIKHOAN(SOTK),
        CONSTRAINT FK_CT_MANV         FOREIGN KEY (MANV)        REFERENCES dbo.NHANVIEN(MANV),
        CONSTRAINT UQ_CT_rowguid      UNIQUE (rowguid)
    );
    PRINT N'>>> Đã tạo bảng GD_CHUYENTIEN (gồm MACN và rowguid).';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.GD_CHUYENTIEN', 'MACN') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_CHUYENTIEN ADD MACN nChar(10) NULL;
        UPDATE ct SET ct.MACN = tk.MACN
        FROM   dbo.GD_CHUYENTIEN ct
        JOIN   dbo.TAIKHOAN      tk ON ct.SOTK_CHUYEN = tk.SOTK
        WHERE  ct.MACN IS NULL;
        ALTER TABLE dbo.GD_CHUYENTIEN ALTER COLUMN MACN nChar(10) NOT NULL;
        PRINT N'>>> GD_CHUYENTIEN: đã thêm và điền dữ liệu cột MACN.';
    END
    IF COL_LENGTH('dbo.GD_CHUYENTIEN', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_CHUYENTIEN
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_CT_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.GD_CHUYENTIEN
            ADD CONSTRAINT UQ_CT_rowguid UNIQUE (rowguid);
        PRINT N'>>> GD_CHUYENTIEN: đã thêm cột rowguid.';
    END
    ELSE
        PRINT N'>>> GD_CHUYENTIEN đã đầy đủ cấu trúc, bỏ qua.';
END
GO
-- Bổ sung khóa ngoại theo chi nhánh nếu chưa có.
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KH_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.KHACHHANG
        ADD CONSTRAINT FK_KH_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT N'>>> Đã tạo FK_KH_CHINHANH.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NV_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.NHANVIEN
        ADD CONSTRAINT FK_NV_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT N'>>> Đã tạo FK_NV_CHINHANH.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TK_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.TAIKHOAN
        ADD CONSTRAINT FK_TK_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT N'>>> Đã tạo FK_TK_CHINHANH.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GR_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.GD_GOIRUT
        ADD CONSTRAINT FK_GR_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT N'>>> Đã tạo FK_GR_CHINHANH.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CT_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.GD_CHUYENTIEN
        ADD CONSTRAINT FK_CT_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT N'>>> Đã tạo FK_CT_CHINHANH.';
END
GO
-- Tạo các index hỗ trợ truy vấn theo MACN và khóa liên quan.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.KHACHHANG') AND name = 'IX_KH_MACN')
    CREATE NONCLUSTERED INDEX IX_KH_MACN ON dbo.KHACHHANG(MACN);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.NHANVIEN') AND name = 'IX_NV_MACN')
    CREATE NONCLUSTERED INDEX IX_NV_MACN ON dbo.NHANVIEN(MACN);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TAIKHOAN') AND name = 'IX_TK_MACN')
    CREATE NONCLUSTERED INDEX IX_TK_MACN ON dbo.TAIKHOAN(MACN);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_GOIRUT') AND name = 'IX_GR_MACN')
    CREATE NONCLUSTERED INDEX IX_GR_MACN ON dbo.GD_GOIRUT(MACN);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_CHUYENTIEN') AND name = 'IX_CT_MACN')
    CREATE NONCLUSTERED INDEX IX_CT_MACN ON dbo.GD_CHUYENTIEN(MACN);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TAIKHOAN') AND name = 'IX_TK_CMND')
    CREATE NONCLUSTERED INDEX IX_TK_CMND ON dbo.TAIKHOAN(CMND);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_GOIRUT') AND name = 'IX_GR_SOTK')
    CREATE NONCLUSTERED INDEX IX_GR_SOTK ON dbo.GD_GOIRUT(SOTK);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_CHUYENTIEN') AND name = 'IX_CT_SOTK_CHUYEN')
    CREATE NONCLUSTERED INDEX IX_CT_SOTK_CHUYEN ON dbo.GD_CHUYENTIEN(SOTK_CHUYEN);
GO
PRINT N'>>> Đã tạo/kiểm tra xong nhóm index hỗ trợ.';
GO
-- Xóa các view _ALL cũ (không còn sử dụng).
IF OBJECT_ID('dbo.KHACHHANG_ALL',     'V') IS NOT NULL DROP VIEW dbo.KHACHHANG_ALL;
IF OBJECT_ID('dbo.NHANVIEN_ALL',      'V') IS NOT NULL DROP VIEW dbo.NHANVIEN_ALL;
IF OBJECT_ID('dbo.TAIKHOAN_ALL',      'V') IS NOT NULL DROP VIEW dbo.TAIKHOAN_ALL;
IF OBJECT_ID('dbo.GD_GOIRUT_ALL',     'V') IS NOT NULL DROP VIEW dbo.GD_GOIRUT_ALL;
IF OBJECT_ID('dbo.GD_CHUYENTIEN_ALL', 'V') IS NOT NULL DROP VIEW dbo.GD_CHUYENTIEN_ALL;
GO
PRINT N'>>> Đã xóa các view _ALL cũ.';
GO
-- Kiểm tra nhanh đối tượng đã có cột rowguid.
SELECT
    o.type_desc                         AS ObjectType,
    SCHEMA_NAME(o.schema_id) + '.' + o.name AS ObjectName,
    CASE WHEN c.name = 'rowguid' THEN 'YES' ELSE 'n/a' END AS HasRowGuid
FROM sys.objects o
LEFT JOIN sys.columns c
    ON c.object_id = o.object_id AND c.name = 'rowguid'
WHERE o.is_ms_shipped = 0
  AND o.type IN ('U', 'V', 'SO')   
ORDER BY o.type_desc, o.name;
GO
PRINT N'=== Hoàn tất 02_publisher_schema.sql ===';
GO
