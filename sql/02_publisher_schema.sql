/*=============================================================================
  02_publisher_schema.sql
  Vai trò   : Máy chủ phát hành / Điều phối viên (server gốc)
  Chạy trên : DESKTOP-JBB41QU / NGANHANG_PUB
  Mục đích: Tạo lược đồ HOÀN CHỈNH của Máy chủ phát hành cho Sao chép hợp nhất (Merge Replication):
             • Bảng chỉ ở Trung tâm:     CHINHANH, NGUOIDUNG, SEQ_MANV
             • Bảng được sao chép:        KHACHHANG, NHANVIEN, TAIKHOAN,
                                           GD_GOIRUT, GD_CHUYENTIEN
             • Mỗi bảng được sao chép chứa:
                 – MACN  nChar(10)  hỗ trợ bộ lọc hàng có tham số
                 – rowguid  uniqueidentifier ROWGUIDCOL  (Sao chép hợp nhất)
             • Dọn dẹp view _ALL (không dùng nữa — xem Phần 5)
             • Chỉ mục hỗ trợ trên MACN cho snapshot lọc hiệu quả

  Trong Sao chép hợp nhất (Merge Replication), Máy chủ phát hành sở hữu bản chính
  của mọi hàng. Máy chủ đăng ký nhận nhận lược đồ + dữ liệu qua Tác vụ snapshot;
  chúng KHÔNG chạy script này.

  Bất biến lũy đẳng: CÓ — tất cả đối tượng được bảo vệ bởi IF OBJECT_ID / IF NOT EXISTS.
  An toàn khi chạy lại. KHÔNG xóa dữ liệu hiện có.

  THỨ TỰ THỰC THI: Bước 2/8 (Chỉ Máy chủ phát hành, sau 01_publisher_create_db).

  Nguồn : Hợp nhất và thay thế sql/01-schema.sql (tất cả các phần).
=============================================================================*/

USE NGANHANG_PUB;
GO

/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 1 — Đối tượng chỉ ở Trung tâm / Điều phối viên
   (KHÔNG được sao chép đến máy chủ đăng ký nhận chi nhánh)
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── CHINHANH — Bảng tham chiếu Chi nhánh ─────────────────────────────────────
-- Được sao chép đến TẤT CẢ máy chủ đăng ký nhận (CN1, CN2, TraCuu) KHÔNG có bộ lọc hàng
-- để mọi site có thể phân giải tên chi nhánh. Bao gồm rowguid để theo dõi sao chép hợp nhất.
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
    PRINT '>>> Table CHINHANH created.';
END
ELSE
BEGIN
    -- Đảm bảo cột rowguid tồn tại (thêm bất biến lũy đẳng cho bảng đã có sẵn)
    IF COL_LENGTH('dbo.CHINHANH', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.CHINHANH
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_CN_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.CHINHANH
            ADD CONSTRAINT UQ_CN_rowguid UNIQUE (rowguid);
        PRINT '>>> CHINHANH: added rowguid column.';
    END
    ELSE
        PRINT '>>> CHINHANH already exists with rowguid — skipped.';
END
GO

-- ── NGUOIDUNG — Tài khoản người dùng / đăng nhập (Chỉ Điều phối viên, KHÔNG được sao chép) ──
-- UserGroup: 0 = NganHang | 1 = ChiNhanh | 2 = KhachHang
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
    PRINT '>>> Table NGUOIDUNG created.';
END
ELSE
    PRINT '>>> Table NGUOIDUNG already exists — skipped.';
GO

-- ── SEQ_MANV — Chuỗi tự tăng mã nhân viên không trùng lặp ───────────────────
-- Seed sử dụng NV00000001..NV00000004; giá trị đầu tiên được tạo = 5.
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE object_id = OBJECT_ID('dbo.SEQ_MANV'))
BEGIN
    CREATE SEQUENCE dbo.SEQ_MANV AS int START WITH 5 INCREMENT BY 1 NO CYCLE;
    PRINT '>>> Sequence SEQ_MANV created (start=5).';
END
ELSE
    PRINT '>>> Sequence SEQ_MANV already exists — skipped.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 2 — Bảng chi nhánh được sao chép
   Chỉ được định nghĩa trên Máy chủ phát hành. Tác vụ snapshot đẩy lược đồ + dữ liệu
   ban đầu đến CN1 (NGANHANG_BT), CN2 (NGANHANG_TD), và TraCuu (NGANHANG_TRACUU).

   Mỗi bảng trong phần này chứa:
     • MACN  nChar(10)  — mã chi nhánh dùng làm vị từ bộ lọc hàng
     • rowguid  uniqueidentifier ROWGUIDCOL  — bắt buộc bởi Sao chép hợp nhất (Merge Replication)
       cho phát hiện xung đột và theo dõi thay đổi.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- ── KHACHHANG (Khách hàng) ────────────────────────────────────────────────────
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
    PRINT '>>> Table KHACHHANG created.';
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
        PRINT '>>> KHACHHANG: added rowguid column.';
    END
    ELSE
        PRINT '>>> KHACHHANG already exists with rowguid — skipped.';
END
GO

-- ── NHANVIEN (Nhân viên) ──────────────────────────────────────────────────────
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
    PRINT '>>> Table NHANVIEN created.';
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
        PRINT '>>> NHANVIEN: added rowguid column.';
    END
    ELSE
        PRINT '>>> NHANVIEN already exists with rowguid — skipped.';
END
GO

-- ── TAIKHOAN (Tài khoản ngân hàng) ───────────────────────────────────────────
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
    PRINT '>>> Table TAIKHOAN created.';
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
        PRINT '>>> TAIKHOAN: added rowguid column.';
    END
    ELSE
        PRINT '>>> TAIKHOAN already exists with rowguid — skipped.';
END
GO

-- ── GD_GOIRUT (Gửi / Rút tiền) ──────────────────────────────────────────────
-- MACN được thêm ở đây (không có trong lược đồ gốc) để bộ lọc hàng có thể phân vùng
-- giao dịch theo chi nhánh mà không cần JOIN đến TAIKHOAN.
-- IDENTITY seed 1,1 — MAGD được gán tự động.
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
    PRINT '>>> Table GD_GOIRUT created (with MACN + rowguid).';
END
ELSE
BEGIN
    -- Thêm MACN nếu thiếu (nâng cấp từ lược đồ cũ)
    IF COL_LENGTH('dbo.GD_GOIRUT', 'MACN') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_GOIRUT ADD MACN nChar(10) NULL;
        -- Bổ sung ngược MACN từ chi nhánh của tài khoản
        UPDATE gr SET gr.MACN = tk.MACN
        FROM   dbo.GD_GOIRUT gr
        JOIN   dbo.TAIKHOAN  tk ON gr.SOTK = tk.SOTK
        WHERE  gr.MACN IS NULL;
        -- Bây giờ đặt NOT NULL
        ALTER TABLE dbo.GD_GOIRUT ALTER COLUMN MACN nChar(10) NOT NULL;
        PRINT '>>> GD_GOIRUT: added + back-filled MACN column.';
    END

    IF COL_LENGTH('dbo.GD_GOIRUT', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_GOIRUT
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_GR_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.GD_GOIRUT
            ADD CONSTRAINT UQ_GR_rowguid UNIQUE (rowguid);
        PRINT '>>> GD_GOIRUT: added rowguid column.';
    END
    ELSE
        PRINT '>>> GD_GOIRUT already up-to-date — skipped.';
END
GO

-- ── GD_CHUYENTIEN (Chuyển tiền) ──────────────────────────────────────────────
-- MACN = chi nhánh của tài khoản NGUỒN (SOTK_CHUYEN).
-- Tài khoản đích (SOTK_NHAN) có thể thuộc chi nhánh khác.
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
        -- Không có khóa ngoại trên SOTK_NHAN: đích có thể là chi nhánh khác.
    );
    PRINT '>>> Table GD_CHUYENTIEN created (with MACN + rowguid).';
END
ELSE
BEGIN
    -- Thêm MACN nếu thiếu (nâng cấp từ lược đồ cũ)
    IF COL_LENGTH('dbo.GD_CHUYENTIEN', 'MACN') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_CHUYENTIEN ADD MACN nChar(10) NULL;
        -- Bổ sung ngược MACN từ chi nhánh của tài khoản nguồn
        UPDATE ct SET ct.MACN = tk.MACN
        FROM   dbo.GD_CHUYENTIEN ct
        JOIN   dbo.TAIKHOAN      tk ON ct.SOTK_CHUYEN = tk.SOTK
        WHERE  ct.MACN IS NULL;
        ALTER TABLE dbo.GD_CHUYENTIEN ALTER COLUMN MACN nChar(10) NOT NULL;
        PRINT '>>> GD_CHUYENTIEN: added + back-filled MACN column.';
    END

    IF COL_LENGTH('dbo.GD_CHUYENTIEN', 'rowguid') IS NULL
    BEGIN
        ALTER TABLE dbo.GD_CHUYENTIEN
            ADD rowguid uniqueidentifier NOT NULL
                CONSTRAINT DF_CT_rowguid DEFAULT (NEWSEQUENTIALID()) ROWGUIDCOL;
        ALTER TABLE dbo.GD_CHUYENTIEN
            ADD CONSTRAINT UQ_CT_rowguid UNIQUE (rowguid);
        PRINT '>>> GD_CHUYENTIEN: added rowguid column.';
    END
    ELSE
        PRINT '>>> GD_CHUYENTIEN already up-to-date — skipped.';
END
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 3 — Ràng buộc khóa ngoại giữa các bảng được sao chép
   Được tạo SAU KHI tất cả các bảng tồn tại để các tham chiếu thuận được thỏa mãn.
   ═══════════════════════════════════════════════════════════════════════════════ */

-- Khóa ngoại: KHACHHANG.MACN → CHINHANH.MACN  (hiện trên cùng DB Máy chủ phát hành)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KH_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.KHACHHANG
        ADD CONSTRAINT FK_KH_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT '>>> FK_KH_CHINHANH created.';
END
GO

-- Khóa ngoại: NHANVIEN.MACN → CHINHANH.MACN
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NV_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.NHANVIEN
        ADD CONSTRAINT FK_NV_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT '>>> FK_NV_CHINHANH created.';
END
GO

-- Khóa ngoại: TAIKHOAN.MACN → CHINHANH.MACN
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TK_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.TAIKHOAN
        ADD CONSTRAINT FK_TK_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT '>>> FK_TK_CHINHANH created.';
END
GO

-- Khóa ngoại: GD_GOIRUT.MACN → CHINHANH.MACN
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GR_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.GD_GOIRUT
        ADD CONSTRAINT FK_GR_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT '>>> FK_GR_CHINHANH created.';
END
GO

-- Khóa ngoại: GD_CHUYENTIEN.MACN → CHINHANH.MACN
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CT_CHINHANH'
)
BEGIN
    ALTER TABLE dbo.GD_CHUYENTIEN
        ADD CONSTRAINT FK_CT_CHINHANH FOREIGN KEY (MACN) REFERENCES dbo.CHINHANH(MACN);
    PRINT '>>> FK_CT_CHINHANH created.';
END
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 4 — Chỉ mục trên MACN
   Bộ lọc hàng của Sao chép hợp nhất (Merge Replication) sử dụng WHERE MACN = '<chi_nhánh>'
   trong mọi truy vấn snapshot có tham số. Chỉ mục non-clustered trên MACN giúp
   việc tạo snapshot lọc và liệt kê thay đổi hiệu quả.
   ═══════════════════════════════════════════════════════════════════════════════ */

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

-- Chỉ mục trên TAIKHOAN(CMND) — tăng tốc tra cứu tài khoản theo khách hàng
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TAIKHOAN') AND name = 'IX_TK_CMND')
    CREATE NONCLUSTERED INDEX IX_TK_CMND ON dbo.TAIKHOAN(CMND);
GO

-- Chỉ mục trên GD_GOIRUT(SOTK) — tăng tốc truy vấn sao kê / lịch sử
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_GOIRUT') AND name = 'IX_GR_SOTK')
    CREATE NONCLUSTERED INDEX IX_GR_SOTK ON dbo.GD_GOIRUT(SOTK);
GO

-- Chỉ mục trên GD_CHUYENTIEN(SOTK_CHUYEN) — lịch sử chuyển tiền theo tài khoản nguồn
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.GD_CHUYENTIEN') AND name = 'IX_CT_SOTK_CHUYEN')
    CREATE NONCLUSTERED INDEX IX_CT_SOTK_CHUYEN ON dbo.GD_CHUYENTIEN(SOTK_CHUYEN);
GO

PRINT '>>> Section 4: MACN + helper indexes created/verified.';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 5 — Xóa view UNION ALL cũ (KHÔNG DÙNG NỮA)
   Các view tiện ích _ALL bắt nguồn từ kiến trúc Linked-Server cũ,
   nơi Điều phối viên tổng hợp dữ liệu chi nhánh qua
   các truy vấn UNION ALL dùng tên bốn phần.

   Trong Sao chép hợp nhất (Merge Replication), Máy chủ phát hành đã chứa TẤT CẢ
   các hàng cục bộ trong bảng gốc (KHACHHANG, NHANVIEN, TAIKHOAN, v.v.).
   SP hiện truy vấn trực tiếp bảng gốc — các view _ALL đã lỗi thời.

   Phần này XÓA các view _ALL còn sót lại để dọn dẹp. Không tạo view mới.
   Xem docs/migration/00_migration_plan.md § Deprecation.
   ═══════════════════════════════════════════════════════════════════════════════ */

IF OBJECT_ID('dbo.KHACHHANG_ALL',     'V') IS NOT NULL DROP VIEW dbo.KHACHHANG_ALL;
IF OBJECT_ID('dbo.NHANVIEN_ALL',      'V') IS NOT NULL DROP VIEW dbo.NHANVIEN_ALL;
IF OBJECT_ID('dbo.TAIKHOAN_ALL',      'V') IS NOT NULL DROP VIEW dbo.TAIKHOAN_ALL;
IF OBJECT_ID('dbo.GD_GOIRUT_ALL',     'V') IS NOT NULL DROP VIEW dbo.GD_GOIRUT_ALL;
IF OBJECT_ID('dbo.GD_CHUYENTIEN_ALL', 'V') IS NOT NULL DROP VIEW dbo.GD_CHUYENTIEN_ALL;
GO

PRINT '>>> Section 5: Legacy _ALL views dropped (deprecated).';
GO


/* ═══════════════════════════════════════════════════════════════════════════════
   PHẦN 6 — Xác minh lược đồ
   Truy vấn tóm tắt nhanh để xác nhận tất cả đối tượng mong đợi tồn tại.
   ═══════════════════════════════════════════════════════════════════════════════ */

SELECT
    o.type_desc                         AS ObjectType,
    SCHEMA_NAME(o.schema_id) + '.' + o.name AS ObjectName,
    CASE WHEN c.name = 'rowguid' THEN 'YES' ELSE 'n/a' END AS HasRowGuid
FROM sys.objects o
LEFT JOIN sys.columns c
    ON c.object_id = o.object_id AND c.name = 'rowguid'
WHERE o.is_ms_shipped = 0
  AND o.type IN ('U', 'V', 'SO')    -- Tables, Views, Sequences
ORDER BY o.type_desc, o.name;
GO

PRINT '=== 02_publisher_schema.sql completed successfully ===';
GO
