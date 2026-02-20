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
    // ????????????????????????? shared iText helpers ?????????????????????????

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

        doc.Add(new Paragraph($"Ngày xu?t: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
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

    // ??????????????????????? STATEMENT ???????????????????????

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
                "SAO KÊ TÀI KHO?N",
                $"Tài kho?n: {statement.SOTK}  |  T? {statement.FromDate:dd/MM/yyyy} ??n {statement.ToDate:dd/MM/yyyy}");

            // Summary row
            var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(10);
            summaryTable.AddCell(new Cell().Add(new Paragraph($"S? d? ??u k?: {statement.OpeningBalance:N0} VND").SetFont(boldFont).SetFontSize(10)).SetBorder(Border.NO_BORDER));
            summaryTable.AddCell(new Cell().Add(new Paragraph($"S? d? cu?i k?: {statement.ClosingBalance:N0} VND").SetFont(boldFont).SetFontSize(10)).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
            doc.Add(summaryTable);

            // Table
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 15, 12, 13, 15, 15, 10, 20 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("S? d? ??u", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày", boldFont));
            table.AddHeaderCell(HeaderCell("Lo?i GD", boldFont));
            table.AddHeaderCell(HeaderCell("S? ti?n", boldFont));
            table.AddHeaderCell(HeaderCell("S? d? sau", boldFont));
            table.AddHeaderCell(HeaderCell("Mã GD", boldFont));
            table.AddHeaderCell(HeaderCell("Di?n gi?i", boldFont));

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

            doc.Add(new Paragraph($"\nT?ng s? giao d?ch: {statement.Lines.Count}")
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
            ws.Cell(1, 1).Value = "SAO KÊ TÀI KHO?N";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            ws.Cell(2, 1).Value = $"Tài kho?n: {statement.SOTK}";
            ws.Cell(3, 1).Value = $"T?: {statement.FromDate:dd/MM/yyyy}  —  ??n: {statement.ToDate:dd/MM/yyyy}";
            ws.Cell(4, 1).Value = $"Ngày xu?t: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            ws.Cell(5, 1).Value = "S? d? ??u k?:";
            ws.Cell(5, 1).Style.Font.Bold = true;
            ws.Cell(5, 2).Value = statement.OpeningBalance;
            ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0";

            ws.Cell(5, 3).Value = "S? d? cu?i k?:";
            ws.Cell(5, 3).Style.Font.Bold = true;
            ws.Cell(5, 4).Value = statement.ClosingBalance;
            ws.Cell(5, 4).Style.NumberFormat.Format = "#,##0";

            // Column headers at row 7
            int headerRow = 7;
            string[] headers = ["S? d? ??u", "Ngày", "Lo?i GD", "S? ti?n", "S? d? sau", "Mã GD", "Di?n gi?i"];
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

    // ??????????????????????? ACCOUNTS ???????????????????????

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
                "BÁO CÁO TÀI KHO?N ?Ã M?",
                $"T? {fromDate:dd/MM/yyyy} ??n {toDate:dd/MM/yyyy}  |  T?ng: {accounts.Count} tài kho?n");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 20, 18, 22, 15, 25 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("S? TK", boldFont));
            table.AddHeaderCell(HeaderCell("CMND", boldFont));
            table.AddHeaderCell(HeaderCell("S? d?", boldFont));
            table.AddHeaderCell(HeaderCell("Chi nhánh", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày m?", boldFont));

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
            var ws = wb.Worksheets.Add("Tài kho?n");

            ws.Cell(1, 1).Value = "BÁO CÁO TÀI KHO?N ?Ã M?";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"T?: {fromDate:dd/MM/yyyy}  —  ??n: {toDate:dd/MM/yyyy}";
            ws.Cell(3, 1).Value = $"Ngày xu?t: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            int headerRow = 5;
            string[] headers = ["S? TK", "CMND", "S? d?", "Chi nhánh", "Ngày m?"];
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

    // ??????????????????????? CUSTOMERS ???????????????????????

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
                $"Chi nhánh: {branchCode ?? "T?T C?"}  |  T?ng: {customers.Count} khách hàng");

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 12, 20, 10, 12, 20, 10, 8, 8 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("CMND", boldFont));
            table.AddHeaderCell(HeaderCell("H? tên", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày sinh", boldFont));
            table.AddHeaderCell(HeaderCell("Ngày c?p", boldFont));
            table.AddHeaderCell(HeaderCell("??a ch?", boldFont));
            table.AddHeaderCell(HeaderCell("S?T", boldFont));
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
            ws.Cell(2, 1).Value = $"Chi nhánh: {branchCode ?? "T?T C?"}";
            ws.Cell(3, 1).Value = $"Ngày xu?t: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            int headerRow = 5;
            string[] headers = ["CMND", "H? tên", "Ngày sinh", "Ngày c?p", "??a ch?", "S?T", "Phái", "Chi nhánh"];
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

    // ??????????????????????? TRANSACTION SUMMARY ???????????????????????

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
                "BÁO CÁO T?NG H?P GIAO D?CH",
                $"T? {summary.FromDate:dd/MM/yyyy} ??n {summary.ToDate:dd/MM/yyyy}  |  Chi nhánh: {summary.BranchDisplay}");

            // Summary statistics box
            var statsTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            statsTable.AddCell(new Cell().Add(new Paragraph("T?ng giao d?ch").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.TotalTransactionCount}").SetFont(font).SetFontSize(14).SetFontColor(new DeviceRgb(0, 120, 215)))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("G?i ti?n (GT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.DepositCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(40, 167, 69)))
                .Add(new Paragraph($"{summary.TotalDepositAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("Rút ti?n (RT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.WithdrawalCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(220, 53, 69)))
                .Add(new Paragraph($"{summary.TotalWithdrawalAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            statsTable.AddCell(new Cell().Add(new Paragraph("Chuy?n ti?n (CT)").SetFont(boldFont).SetFontSize(9))
                .Add(new Paragraph($"{summary.TransferCount} GD").SetFont(font).SetFontSize(11).SetFontColor(new DeviceRgb(23, 162, 184)))
                .Add(new Paragraph($"{summary.TotalTransferAmount:N0} VND").SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER));

            doc.Add(statsTable);

            // Total amount
            doc.Add(new Paragraph($"T?ng s? ti?n giao d?ch: {summary.TotalAmount:N0} VND")
                .SetFont(boldFont).SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(new DeviceRgb(0, 120, 215))
                .SetMarginBottom(15));

            // Transaction detail table
            if (summary.Transactions.Count > 0)
            {
                doc.Add(new Paragraph("Chi ti?t giao d?ch:")
                    .SetFont(boldFont).SetFontSize(11).SetMarginBottom(6));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 10, 18, 10, 15, 20, 15, 12 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell(HeaderCell("Mã GD", boldFont));
                table.AddHeaderCell(HeaderCell("S? TK", boldFont));
                table.AddHeaderCell(HeaderCell("Lo?i", boldFont));
                table.AddHeaderCell(HeaderCell("Ngày", boldFont));
                table.AddHeaderCell(HeaderCell("S? ti?n", boldFont));
                table.AddHeaderCell(HeaderCell("TK ??n", boldFont));
                table.AddHeaderCell(HeaderCell("Tr?ng thái", boldFont));

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
            var wsSummary = wb.Worksheets.Add("T?ng h?p");

            wsSummary.Cell(1, 1).Value = "BÁO CÁO T?NG H?P GIAO D?CH";
            wsSummary.Cell(1, 1).Style.Font.Bold = true;
            wsSummary.Cell(1, 1).Style.Font.FontSize = 14;
            wsSummary.Cell(2, 1).Value = $"T?: {summary.FromDate:dd/MM/yyyy}  —  ??n: {summary.ToDate:dd/MM/yyyy}";
            wsSummary.Cell(3, 1).Value = $"Chi nhánh: {summary.BranchDisplay}";
            wsSummary.Cell(4, 1).Value = $"Ngày xu?t: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

            // Stats
            int r = 6;
            wsSummary.Cell(r, 1).Value = "Lo?i"; wsSummary.Cell(r, 1).Style.Font.Bold = true;
            wsSummary.Cell(r, 2).Value = "S? l??ng"; wsSummary.Cell(r, 2).Style.Font.Bold = true;
            wsSummary.Cell(r, 3).Value = "T?ng ti?n (VND)"; wsSummary.Cell(r, 3).Style.Font.Bold = true;
            foreach (var cell in wsSummary.Range(r, 1, r, 3).Cells())
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D7");
                cell.Style.Font.FontColor = XLColor.White;
            }

            r++;
            wsSummary.Cell(r, 1).Value = "G?i ti?n (GT)";
            wsSummary.Cell(r, 2).Value = summary.DepositCount;
            wsSummary.Cell(r, 3).Value = summary.TotalDepositAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "Rút ti?n (RT)";
            wsSummary.Cell(r, 2).Value = summary.WithdrawalCount;
            wsSummary.Cell(r, 3).Value = summary.TotalWithdrawalAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "Chuy?n ti?n (CT)";
            wsSummary.Cell(r, 2).Value = summary.TransferCount;
            wsSummary.Cell(r, 3).Value = summary.TotalTransferAmount;
            wsSummary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";

            r++;
            wsSummary.Cell(r, 1).Value = "T?NG C?NG";
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
                var wsDetail = wb.Worksheets.Add("Chi ti?t");

                int headerRow = 1;
                string[] headers = ["Mã GD", "S? TK", "Lo?i", "Ngày", "S? ti?n", "TK ??n", "Tr?ng thái"];
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
