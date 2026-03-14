using BankDds.Core.Models;
using ClosedXML.Excel;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;
namespace BankDds.Wpf.Services;

/// <summary>
/// Exports reports to PDF (iText7) and Excel (ClosedXML).
/// All methods are thread-safe and run on a background thread via Task.Run.
/// </summary>
public class ReportExportService : IReportExportService
{
    // ------------------------- shared iText helpers -------------------------

    /// <summary>
    /// Creates a PdfFont that supports Vietnamese glyphs.
    /// Falls back to Helvetica if the system font is not available.
    /// </summary>
    private static PdfFont CreateVietnameseFont()
    {
        try
        {
            // Try Windows system font path for a font with good Vietnamese coverage
            var arialPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

            if (File.Exists(arialPath))
                return PdfFontFactory.CreateFont(arialPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var segoeUiPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");

            if (File.Exists(segoeUiPath))
                return PdfFontFactory.CreateFont(segoeUiPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
        }
        catch
        {
            // Swallow – fall through to Helvetica
        }

        return PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
    }

    private static PdfFont CreateVietnameseFontBold()
    {
        try
        {
            var arialBoldPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arialbd.ttf");

            if (File.Exists(arialBoldPath))
                return PdfFontFactory.CreateFont(arialBoldPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var segoeBoldPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeuib.ttf");

            if (File.Exists(segoeBoldPath))
                return PdfFontFactory.CreateFont(segoeBoldPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
        }
        catch
        {
            // Swallow – fall through to Helvetica-Bold
        }

        return PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>Adds a standard report header paragraph to the document.</summary>
    private static void AddPdfHeader(Document doc, PdfFont boldFont, PdfFont normalFont, string title, string? subtitle = null)
    {
        doc.Add(new Paragraph(title)
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));

        if (!string.IsNullOrEmpty(subtitle))
        {
            doc.Add(new Paragraph(subtitle)
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(4));
        }

        doc.Add(new Paragraph($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .SetFont(normalFont)
            .SetFontSize(9)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontColor(ColorConstants.GRAY)
            .SetMarginBottom(12));
    }

    private static Cell HeaderCell(string text, PdfFont boldFont)
    {
        return new Cell()
            .Add(new Paragraph(text).SetFont(boldFont).SetFontSize(9))
            .SetBackgroundColor(new DeviceRgb(0, 120, 215))
            .SetFontColor(ColorConstants.WHITE)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetPadding(5);
    }

    private static Cell DataCell(string text, PdfFont font, TextAlignment align = TextAlignment.LEFT)
    {
        return new Cell()
            .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
            .SetTextAlignment(align)
            .SetPadding(4);
    }

    // ------------------------------ STATEMENT ------------------------------

    public Task ExportStatementToPdfAsync(AccountStatement statement, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            var font = CreateVietnameseFont();
            var boldFont = CreateVietnameseFontBold();

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            AddPdfHeader(doc, boldFont, font,
                "SAO KÊ TÀI KHOẢN",
                $"Tài khoản: {statement.SOTK}  |  Từ {statement.FromDate:dd/MM/yyyy} đến {statement.ToDate:dd/MM/yyyy}");

            // Summary row
            var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(10);
            summaryTable.AddCell(new Cell().Add(new Paragraph($"Số dư đầu kỳ: {statement.OpeningBalance:N0} VND").SetFont(boldFont).SetFontSize(10)).SetBorder(Border.NO_BORDER));
            summaryTable.AddCell(new Cell().Add(new Paragraph($"Số dư cuối kỳ: {statement.ClosingBalance:N0} VND").SetFont(boldFont).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
            doc.Add(summaryTable);

            // Table
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 15, 12, 13, 15, 15, 10, 20 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("Số dư đầu", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày", boldFont));
            table.AddHeaderCell(HeaderCell("Loại GD", boldFont));
            table.AddHeaderCell(HeaderCell("Số tiền", boldFont));
            table.AddHeaderCell(HeaderCell("Số dư sau", boldFont));
            table.AddHeaderCell(HeaderCell("Mã GD", boldFont));
            table.AddHeaderCell(HeaderCell("Diễn giải", boldFont));

            foreach (var line in statement.Lines)
            {
                table.AddCell(DataCell($"{line.OpeningBalance:N0}", font, TextAlignment.RIGHT));
                table.AddCell(DataCell(line.Date.ToString("dd/MM/yyyy"), font, TextAlignment.CENTER));
                table.AddCell(DataCell(line.TypeDisplay, font, TextAlignment.CENTER));
                table.AddCell(DataCell(line.AmountDisplay, font, TextAlignment.RIGHT));
                table.AddCell(DataCell($"{line.RunningBalance:N0}", font, TextAlignment.RIGHT));
                table.AddCell(DataCell(line.TransactionId, font, TextAlignment.CENTER));
                table.AddCell(DataCell(line.Description, font));
            }

            doc.Add(table);

            doc.Add(new Paragraph($"\nTổng số giao dịch: {statement.Lines.Count}")
                .SetFont(font).SetFontSize(9).SetFontColor(ColorConstants.GRAY));
        });
    }

    public Task ExportStatementToExcelAsync(AccountStatement statement, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sao kê");

            // Header info
            ws.Cell(1, 1).Value = "SAO KÊ TÀI KHOẢN";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            ws.Cell(2, 1).Value = $"Tài khoản: {statement.SOTK}";
            ws.Cell(3, 1).Value = $"Từ: {statement.FromDate:dd/MM/yyyy}  —  đến: {statement.ToDate:dd/MM/yyyy}";
            ws.Cell(4, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            ws.Cell(5, 1).Value = "Số dư đầu kỳ:";
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 2).Value = statement.OpeningBalance;
            ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0";

            ws.Cell(5, 3).Value = "Số dư cuối kỳ:";
            ws.Cell(5, 3).Style.Font.Bold = true;
            ws.Cell(5, 4).Value = statement.ClosingBalance;
            ws.Cell(5, 4).Style.NumberFormat.Format = "#,##0";

            // Column headers at row 7
            int headerRow = 7;
            string[] headers = ["Số dư đầu", "Ngày", "Loại GD", "Số tiền", "Số dư sau", "Mã GD", "Diễn giải"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = headerRow + 1;
            foreach (var line in statement.Lines)
            {
                ws.Cell(row, 1).Value = line.OpeningBalance;
                ws.Cell(row, 1).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 2).Value = line.Date;
                ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";

                ws.Cell(row, 3).Value = line.TypeDisplay;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 4).Value = line.IsDebit ? -line.Amount : line.Amount;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 5).Value = line.RunningBalance;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 6).Value = line.TransactionId;
                ws.Cell(row, 7).Value = line.Description;
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        });
    }

    // ------------------------------- ACCOUNTS -------------------------------

    public Task ExportAccountsToPdfAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            var font = CreateVietnameseFont();
            var boldFont = CreateVietnameseFontBold();

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

            AddPdfHeader(doc, boldFont, font,
                "BÁO CÁO TÀI KHOẢN ĐÃ MỞ",
                $"Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}  |  Tổng: {accounts.Count} tài khoản");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 20, 18, 22, 15, 25 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("Số TK", boldFont));
            table.AddHeaderCell(HeaderCell("CMND", boldFont));
            table.AddHeaderCell(HeaderCell("Số dư", boldFont));
            table.AddHeaderCell(HeaderCell("Chi nhánh", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày mở", boldFont));

            foreach (var a in accounts)
            {
                table.AddCell(DataCell(a.SOTK, font));
                table.AddCell(DataCell(a.CMND, font));
                table.AddCell(DataCell($"{a.SODU:N0}", font, TextAlignment.RIGHT));
                table.AddCell(DataCell(a.MACN, font, TextAlignment.CENTER));
                table.AddCell(DataCell(a.NGAYMOTK.ToString("dd/MM/yyyy"), font, TextAlignment.CENTER));
            }

            doc.Add(table);
        });
    }

    public Task ExportAccountsToExcelAsync(List<Account> accounts, DateTime fromDate, DateTime toDate, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Tài khoản");

            ws.Cell(1, 1).Value = "BÁO CÁO TÀI KHOẢN ĐÃ MỞ";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Từ: {fromDate:dd/MM/yyyy}  —  đến: {toDate:dd/MM/yyyy}";
            ws.Cell(3, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            int headerRow = 5;
            string[] headers = ["Số TK", "CMND", "Số dư", "Chi nhánh", "Ngày mở"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = headerRow + 1;
            foreach (var a in accounts)
            {
                ws.Cell(row, 1).Value = a.SOTK;
                ws.Cell(row, 2).Value = a.CMND;
                ws.Cell(row, 3).Value = a.SODU;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 4).Value = a.MACN;
                ws.Cell(row, 5).Value = a.NGAYMOTK;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        });
    }

    // ------------------------------ CUSTOMERS ------------------------------

    public Task ExportCustomersToPdfAsync(List<Customer> customers, string? branchCode, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            var font = CreateVietnameseFont();
            var boldFont = CreateVietnameseFontBold();

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            AddPdfHeader(doc, boldFont, font,
                "BÁO CÁO KHÁCH HÀNG THEO CHI NHÁNH",
                $"Chi nhánh: {branchCode ?? "TẤT CẢ"}  |  Tổng: {customers.Count} khách hàng");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 12, 20, 10, 12, 20, 10, 8, 8 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("CMND", boldFont));
            table.AddHeaderCell(HeaderCell("Họ tên", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày sinh", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày cấp", boldFont));
            table.AddHeaderCell(HeaderCell("Địa chỉ", boldFont));
            table.AddHeaderCell(HeaderCell("SĐT", boldFont));
            table.AddHeaderCell(HeaderCell("Phái", boldFont));
            table.AddHeaderCell(HeaderCell("Chi nhánh", boldFont));

            foreach (var c in customers)
            {
                table.AddCell(DataCell(c.CMND, font));
                table.AddCell(DataCell(c.FullName, font));
                table.AddCell(DataCell(c.NgaySinh?.ToString("dd/MM/yyyy") ?? "", font, TextAlignment.CENTER));
                table.AddCell(DataCell(c.NgayCap?.ToString("dd/MM/yyyy") ?? "", font, TextAlignment.CENTER));
                table.AddCell(DataCell(c.DiaChi, font));
                table.AddCell(DataCell(c.SODT, font));
                table.AddCell(DataCell(c.Phai, font, TextAlignment.CENTER));
                table.AddCell(DataCell(c.MaCN, font, TextAlignment.CENTER));
            }

            doc.Add(table);
        });
    }

    public Task ExportCustomersToExcelAsync(List<Customer> customers, string? branchCode, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Khách hàng");

            ws.Cell(1, 1).Value = "BÁO CÁO KHÁCH HÀNG THEO CHI NHÁNH";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Chi nhánh: {branchCode ?? "TẤT CẢ"}";
            ws.Cell(3, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            int headerRow = 5;
            string[] headers = ["CMND", "Họ tên", "Ngày sinh", "Ngày cấp", "Địa chỉ", "SĐT", "Phái", "Chi nhánh"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = headerRow + 1;
            foreach (var c in customers)
            {
                ws.Cell(row, 1).Value = c.CMND;
                ws.Cell(row, 2).Value = c.FullName;

                if (c.NgaySinh.HasValue)
                {
                    ws.Cell(row, 3).Value = c.NgaySinh.Value;
                    ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
                }

                if (c.NgayCap.HasValue)
                {
                    ws.Cell(row, 4).Value = c.NgayCap.Value;
                    ws.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                }

                ws.Cell(row, 5).Value = c.DiaChi;
                ws.Cell(row, 6).Value = c.SODT;
                ws.Cell(row, 7).Value = c.Phai;
                ws.Cell(row, 8).Value = c.MaCN;
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        });
    }

    // ------------------------- TRANSACTION SUMMARY -------------------------

    public Task ExportTransactionSummaryToPdfAsync(TransactionSummary summary, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            var font = CreateVietnameseFont();
            var boldFont = CreateVietnameseFontBold();

            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

            AddPdfHeader(doc, boldFont, font,
                "BÁO CÁO TỔNG HỢP GIAO DỊCH",
                $"Từ {summary.FromDate:dd/MM/yyyy} đến {summary.ToDate:dd/MM/yyyy}  |  Chi nhánh: {summary.BranchDisplay}");

            // Summary statistics box
            var statsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            statsTable.AddCell(new Cell().Add(new Paragraph("Tổng giao dịch").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.TotalTransactionCount}").SetFont(font).SetFontSize(14).SetFontColor(new DeviceRgb(0, 120, 215)))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("Gửi tiền (GT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.DepositCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(40, 167, 69)))
                .Add(new Paragraph($"{summary.TotalDepositAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("Rút tiền (RT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.WithdrawalCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(220, 53, 69)))
                .Add(new Paragraph($"{summary.TotalWithdrawalAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("Chuyển tiền (CT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.TransferCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(23, 162, 184)))
                .Add(new Paragraph($"{summary.TotalTransferAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            doc.Add(statsTable);

            // Total amount
            doc.Add(new Paragraph($"Tổng số tiền giao dịch: {summary.TotalAmount:N0} VND")
                .SetFont(boldFont).SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(new DeviceRgb(0, 120, 215))
                .SetMarginBottom(15));

            // Transaction detail table
            if (summary.Transactions.Count > 0)
            {
                doc.Add(new Paragraph("Chi tiết giao dịch:")
                    .SetFont(boldFont).SetFontSize(11).SetMarginBottom(6));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 10, 18, 10, 15, 20, 15, 12 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell(HeaderCell("Mã GD", boldFont));
                table.AddHeaderCell(HeaderCell("Số TK", boldFont));
                table.AddHeaderCell(HeaderCell("Loại", boldFont));
                table.AddHeaderCell(HeaderCell("Ngày", boldFont));
                table.AddHeaderCell(HeaderCell("Số tiền", boldFont));
                table.AddHeaderCell(HeaderCell("TK nhận", boldFont));
                table.AddHeaderCell(HeaderCell("Trạng thái", boldFont));

                foreach (var t in summary.Transactions)
                {
                    table.AddCell(DataCell(t.MAGD.ToString(), font, TextAlignment.CENTER));
                    table.AddCell(DataCell(t.SOTK, font));
                    table.AddCell(DataCell(t.LOAIGD, font, TextAlignment.CENTER));
                    table.AddCell(DataCell(t.NGAYGD.ToString("dd/MM/yyyy"), font, TextAlignment.CENTER));
                    table.AddCell(DataCell($"{t.SOTIEN:N0}", font, TextAlignment.RIGHT));
                    table.AddCell(DataCell(t.SOTK_NHAN ?? "", font));
                    table.AddCell(DataCell(t.StatusDisplay, font, TextAlignment.CENTER));
                }

                doc.Add(table);
            }
        });
    }

    public Task ExportTransactionSummaryToExcelAsync(TransactionSummary summary, string filePath)
    {
        return Task.Run(() =>
        {
            EnsureDirectory(filePath);
            using var wb = new XLWorkbook();

            // --- Sheet 1: Summary ---
            var wsSummary = wb.Worksheets.Add("Tổng hợp");

            wsSummary.Cell(1, 1).Value = "BÁO CÁO TỔNG HỢP GIAO DỊCH";
            wsSummary.Cell(1, 1).Style.Font.Bold = true;
            wsSummary.Cell(1, 1).Style.Font.FontSize = 14;
            wsSummary.Cell(2, 1).Value = $"Từ: {summary.FromDate:dd/MM/yyyy}  —  đến: {summary.ToDate:dd/MM/yyyy}";
            wsSummary.Cell(3, 1).Value = $"Chi nhánh: {summary.BranchDisplay}";
            wsSummary.Cell(4, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            // Stats
            int r = 6;
            wsSummary.Cell(r, 1).Value = "Loại"; wsSummary.Cell(r, 1).Style.Font.Bold = true;
            wsSummary.Cell(r, 2).Value = "Số lượng"; wsSummary.Cell(r, 2).Style.Font.Bold = true;
            wsSummary.Cell(r, 3).Value = "Tổng tiền (VND)"; wsSummary.Cell(r, 3).Style.Font.Bold = true;
            foreach (var cell in wsSummary.Range(r, 1, r, 3).Cells())
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                cell.Style.Font.FontColor = XLColor.White;
            }

            r++;
            wsSummary.Cell(r, 1).Value = "Gửi tiền (GT)";
            wsSummary.Cell(r, 2).Value = summary.DepositCount;
            wsSummary.Cell(r, 3).Value = summary.TotalDepositAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "Rút tiền (RT)";
            wsSummary.Cell(r, 2).Value = summary.WithdrawalCount;
            wsSummary.Cell(r, 3).Value = summary.TotalWithdrawalAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "Chuyển tiền (CT)";
            wsSummary.Cell(r, 2).Value = summary.TransferCount;
            wsSummary.Cell(r, 3).Value = summary.TotalTransferAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "TỔNG CỘNG";
            wsSummary.Cell(r, 1).Style.Font.Bold = true;
            wsSummary.Cell(r, 2).Value = summary.TotalTransactionCount;
            wsSummary.Cell(r, 2).Style.Font.Bold = true;
            wsSummary.Cell(r, 3).Value = summary.TotalAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            wsSummary.Cell(r, 3).Style.Font.Bold = true;

            wsSummary.Columns().AdjustToContents();

            // --- Sheet 2: Transaction details ---
            if (summary.Transactions.Count > 0)
            {
                var wsDetail = wb.Worksheets.Add("Chi tiết");

                int headerRow = 1;
                string[] headers = ["Mã GD", "Số TK", "Loại", "Ngày", "Số tiền", "TK nhận", "Trạng thái"];
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = wsDetail.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 2;
                foreach (var t in summary.Transactions)
                {
                    wsDetail.Cell(row, 1).Value = t.MAGD;
                    wsDetail.Cell(row, 2).Value = t.SOTK;
                    wsDetail.Cell(row, 3).Value = t.LOAIGD;
                    wsDetail.Cell(row, 4).Value = t.NGAYGD;
                    wsDetail.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                    wsDetail.Cell(row, 5).Value = t.SOTIEN;
                    wsDetail.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    wsDetail.Cell(row, 6).Value = t.SOTK_NHAN ?? "";
                    wsDetail.Cell(row, 7).Value = t.StatusDisplay;
                    row++;
                }

                wsDetail.Columns().AdjustToContents();
            }

            wb.SaveAs(filePath);
        });
    }
}


