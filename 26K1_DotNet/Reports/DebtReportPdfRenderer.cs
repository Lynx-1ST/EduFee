using System.Drawing;
using System.Drawing.Drawing2D;

namespace K26_DotNet.Reports;

public sealed record DebtReportRow(
    string StudentCode,
    string StudentName,
    string ClassName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime? DueDate,
    string Status);

public sealed record DebtReportPdfData(
    string OrganizationName,
    string SemesterName,
    string FilterDescription,
    DateTime ExportedAt,
    IReadOnlyList<DebtReportRow> Rows);

public static class DebtReportPdfRenderer
{
    private const int Left = 90;
    private const int Right = PdfRasterDocument.A4LandscapeWidth - 90;
    private const int HeaderY = 290;
    private const int RowHeight = 34;
    private const int FooterY = PdfRasterDocument.A4LandscapeHeight - 62;
    private static readonly int[] ColumnWidths = [115, 280, 145, 185, 175, 185, 145, 150];
    private static readonly string[] ColumnTitles = ["Mã SV", "Họ và tên", "Lớp", "Phải nộp", "Đã nộp", "Còn nợ", "Hạn nộp", "Trạng thái"];

    private const int MaxRowsNormalPage = 24;
    private const int MaxRowsLastPage = 23;

    public static void Export(string filePath, DebtReportPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(data.Rows);
        var pageRows = Paginate(data.Rows);
        int pageCount = pageRows.Count;
        var pages = new List<Bitmap>(pageCount);
        try
        {
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var page = new Bitmap(PdfRasterDocument.A4LandscapeWidth, PdfRasterDocument.A4LandscapeHeight);
                pages.Add(page);
                using var graphics = Graphics.FromImage(page);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.White);
                DrawPage(graphics, data, pageIndex + 1, pageCount, pageRows[pageIndex]);
            }
            PdfRasterDocument.Write(filePath, pages, landscape: true);
        }
        finally
        {
            foreach (var page in pages) page.Dispose();
        }
    }

    private static List<IReadOnlyList<DebtReportRow>> Paginate(IReadOnlyList<DebtReportRow> rows)
    {
        if (rows.Count == 0) return [Array.Empty<DebtReportRow>()];
        var pages = new List<IReadOnlyList<DebtReportRow>>();
        int index = 0;
        while (index < rows.Count)
        {
            int remaining = rows.Count - index;
            if (remaining <= MaxRowsLastPage)
            {
                pages.Add(rows.Skip(index).Take(remaining).ToList());
                break;
            }
            int take = (remaining == MaxRowsNormalPage) ? MaxRowsLastPage : MaxRowsNormalPage;
            pages.Add(rows.Skip(index).Take(take).ToList());
            index += take;
        }
        return pages;
    }

    private static void DrawPage(Graphics graphics, DebtReportPdfData data, int pageNumber, int pageCount, IReadOnlyList<DebtReportRow> rows)
    {
        using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold);
        using var headingFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 10);
        using var smallFont = new Font("Segoe UI", 9);
        using var navy = new SolidBrush(Color.FromArgb(26, 78, 137));
        using var dark = new SolidBrush(Color.FromArgb(35, 40, 48));
        using var muted = new SolidBrush(Color.FromArgb(85, 95, 110));
        using var headerFill = new SolidBrush(Color.FromArgb(232, 241, 250));
        using var totalFill = new SolidBrush(Color.FromArgb(235, 249, 241));
        using var grid = new Pen(Color.FromArgb(201, 211, 221));

        graphics.DrawString(data.OrganizationName, headingFont, muted, new RectangleF(Left, 70, Right - Left, 30));
        graphics.DrawString("BÁO CÁO CÔNG NỢ HỌC PHÍ", titleFont, navy, new RectangleF(Left, 112, Right - Left, 44));
        graphics.DrawString($"Học kỳ: {data.SemesterName}", bodyFont, dark, new RectangleF(Left, 175, 620, 28));
        graphics.DrawString($"Bộ lọc: {data.FilterDescription}", bodyFont, dark, new RectangleF(Left, 205, 860, 28));
        graphics.DrawString($"Thời điểm xuất: {data.ExportedAt:dd/MM/yyyy HH:mm}", smallFont, muted, new RectangleF(Left, 240, Right - Left, 24));

        DrawTableHeader(graphics, HeaderY, headingFont, dark, headerFill, grid);
        int y = HeaderY + RowHeight;
        foreach (var row in rows)
        {
            DrawRow(graphics, row, y, bodyFont, dark, grid);
            y += RowHeight;
        }

        if (pageNumber == pageCount)
        {
            decimal total = data.Rows.Sum(r => r.TotalAmount);
            decimal paid = data.Rows.Sum(r => r.PaidAmount);
            decimal remaining = data.Rows.Sum(r => r.RemainingAmount);
            graphics.FillRectangle(totalFill, Left, y, Right - Left, RowHeight);
            DrawCell(graphics, "TỔNG CỘNG", Left, y, ColumnWidths.Take(3).Sum(), RowHeight, headingFont, dark, StringAlignment.Near);
            int amountX = Left + ColumnWidths.Take(3).Sum();
            DrawCell(graphics, $"{total:N0}", amountX, y, ColumnWidths[3], RowHeight, headingFont, dark, StringAlignment.Far);
            amountX += ColumnWidths[3];
            DrawCell(graphics, $"{paid:N0}", amountX, y, ColumnWidths[4], RowHeight, headingFont, dark, StringAlignment.Far);
            amountX += ColumnWidths[4];
            DrawCell(graphics, $"{remaining:N0}", amountX, y, ColumnWidths[5], RowHeight, headingFont, dark, StringAlignment.Far);
            graphics.DrawRectangle(grid, Left, y, Right - Left, RowHeight);
        }

        graphics.DrawLine(grid, Left, FooterY - 16, Right, FooterY - 16);
        graphics.DrawString($"Trang {pageNumber}/{pageCount}", smallFont, muted, new RectangleF(Left, FooterY, Right - Left, 20), new StringFormat { Alignment = StringAlignment.Far });
        graphics.DrawString("EduFee - Báo cáo được xuất trực tiếp từ hệ thống", smallFont, muted, new RectangleF(Left, FooterY, 620, 20));
    }

    private static void DrawTableHeader(Graphics g, int y, Font font, Brush brush, Brush fill, Pen grid)
    {
        int x = Left;
        for (int i = 0; i < ColumnTitles.Length; i++)
        {
            g.FillRectangle(fill, x, y, ColumnWidths[i], RowHeight);
            DrawCell(g, ColumnTitles[i], x, y, ColumnWidths[i], RowHeight, font, brush, i is >= 3 and <= 5 ? StringAlignment.Far : StringAlignment.Near);
            g.DrawRectangle(grid, x, y, ColumnWidths[i], RowHeight);
            x += ColumnWidths[i];
        }
    }

    private static void DrawRow(Graphics g, DebtReportRow row, int y, Font font, Brush brush, Pen grid)
    {
        string[] values = [row.StudentCode, row.StudentName, row.ClassName, $"{row.TotalAmount:N0}", $"{row.PaidAmount:N0}", $"{row.RemainingAmount:N0}", row.DueDate?.ToString("dd/MM/yyyy") ?? "-", row.Status];
        int x = Left;
        for (int i = 0; i < values.Length; i++)
        {
            DrawCell(g, values[i], x, y, ColumnWidths[i], RowHeight, font, brush, i is >= 3 and <= 5 ? StringAlignment.Far : StringAlignment.Near);
            g.DrawRectangle(grid, x, y, ColumnWidths[i], RowHeight);
            x += ColumnWidths[i];
        }
    }

    private static void DrawCell(Graphics g, string text, int x, int y, int width, int height, Font font, Brush brush, StringAlignment align)
    {
        using var format = new StringFormat { Alignment = align, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text ?? string.Empty, font, brush, new RectangleF(x + 7, y, width - 14, height), format);
    }
}
