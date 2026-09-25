using System.Drawing;
using System.Drawing.Drawing2D;

namespace K26_DotNet.Reports;

public sealed record DebtReportRow(string StudentCode, string StudentName, string ClassName, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, DateTime? DueDate, string Status);
public sealed record DebtReportPdfData(string OrganizationName, string SemesterName, string FilterDescription, DateTime ExportedAt, IReadOnlyList<DebtReportRow> Rows);

public static class DebtReportPdfRenderer
{
    private const int Left = 90, Right = PdfRasterDocument.A4LandscapeWidth - 90, HeaderY = 290, MinimumRowHeight = 34, FooterY = PdfRasterDocument.A4LandscapeHeight - 62, TableBottom = FooterY - 34;
    // Widths add up exactly to the drawable table area (Right - Left).
    private static readonly int[] ColumnWidths = [120, 340, 180, 200, 195, 200, 150, 189];
    private static readonly string[] ColumnTitles = ["Mã SV", "Họ và tên", "Lớp", "Phải nộp", "Đã nộp", "Còn nợ", "Hạn nộp", "Trạng thái"];
    private static readonly StringFormat WrapFormat = new() { LineAlignment = StringAlignment.Center };
    private static readonly StringFormat RightFormat = new() { Alignment = StringAlignment.Far };

    public static void Export(string filePath, DebtReportPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data); ArgumentNullException.ThrowIfNull(data.Rows);
        using var measuringBitmap = new Bitmap(1, 1); using var measuringGraphics = Graphics.FromImage(measuringBitmap); using var bodyFont = new Font("Segoe UI", 10);
        var pageRows = Paginate(ExpandOversizedRows(data.Rows, measuringGraphics, bodyFont), measuringGraphics, bodyFont);
        var pages = new List<Bitmap>(pageRows.Count);
        try
        {
            for (int index = 0; index < pageRows.Count; index++)
            {
                var page = new Bitmap(PdfRasterDocument.A4LandscapeWidth, PdfRasterDocument.A4LandscapeHeight); pages.Add(page);
                using var graphics = Graphics.FromImage(page); graphics.SmoothingMode = SmoothingMode.AntiAlias; graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; graphics.Clear(Color.White);
                DrawPage(graphics, data, index + 1, pageRows.Count, pageRows[index]);
            }
            PdfRasterDocument.Write(filePath, pages, landscape: true);
        }
        finally { foreach (var page in pages) page.Dispose(); }
    }

    private sealed record PageRow(DebtReportRow Row, int Height);
    private static List<IReadOnlyList<PageRow>> Paginate(IReadOnlyList<DebtReportRow> rows, Graphics graphics, Font font)
    {
        var pages = new List<IReadOnlyList<PageRow>>(); var current = new List<PageRow>();
        int used = HeaderY + MinimumRowHeight, limit = TableBottom - MinimumRowHeight; // reserve total row, including on a full last page
        foreach (var row in rows)
        {
            int height = MeasureRowHeight(graphics, row, font);
            if (current.Count > 0 && used + height > limit) { pages.Add(current); current = new List<PageRow>(); used = HeaderY + MinimumRowHeight; }
            current.Add(new PageRow(row, height)); used += height;
        }
        pages.Add(current); // An empty report remains a useful, valid one-page report.
        return pages;
    }

    private static int MeasureRowHeight(Graphics graphics, DebtReportRow row, Font font)
    {
        string[] values = [row.StudentCode, row.StudentName, row.ClassName, $"{row.TotalAmount:N0}", $"{row.PaidAmount:N0}", $"{row.RemainingAmount:N0}", row.DueDate?.ToString("dd/MM/yyyy") ?? "-", row.Status];
        float required = MinimumRowHeight;
        for (int i = 0; i < values.Length; i++) required = Math.Max(required, graphics.MeasureString(values[i] ?? string.Empty, font, ColumnWidths[i] - 14, WrapFormat).Height + 10);
        return Math.Max(MinimumRowHeight, (int)Math.Ceiling(required));
    }

    private static IReadOnlyList<DebtReportRow> ExpandOversizedRows(IReadOnlyList<DebtReportRow> rows, Graphics graphics, Font font)
    {
        int maximumHeight = TableBottom - MinimumRowHeight - (HeaderY + MinimumRowHeight);
        var expanded = new List<DebtReportRow>();
        foreach (var row in rows)
        {
            if (MeasureRowHeight(graphics, row, font) <= maximumHeight) { expanded.Add(row); continue; }
            expanded.AddRange(SplitOversizedRow(row, graphics, font, maximumHeight));
        }
        return expanded;
    }

    private static IEnumerable<DebtReportRow> SplitOversizedRow(DebtReportRow row, Graphics graphics, Font font, int maximumHeight)
    {
        string[] values = [row.StudentCode, row.StudentName, row.ClassName, $"{row.TotalAmount:N0}", $"{row.PaidAmount:N0}", $"{row.RemainingAmount:N0}", row.DueDate?.ToString("dd/MM/yyyy") ?? "-", row.Status];
        int field = Enumerable.Range(0, values.Length).MaxBy(index => graphics.MeasureString(values[index] ?? string.Empty, font, ColumnWidths[index] - 14, WrapFormat).Height);
        string remaining = values[field] ?? string.Empty;
        bool first = true;
        while (remaining.Length > 0)
        {
            int count = LargestFittingPrefix(row, field, remaining, graphics, font, maximumHeight);
            count = Math.Max(1, count);
            string fragment = remaining[..count]; remaining = remaining[count..];
            var fragmentRow = ReplaceValue(row, field, fragment);
            // Keep identifying data on the first fragment; continuation fragments retain the
            // wrapped field and status so every visible row remains understandable.
            if (!first) fragmentRow = fragmentRow with { StudentCode = string.Empty, TotalAmount = 0, PaidAmount = 0, RemainingAmount = 0, DueDate = null };
            yield return fragmentRow;
            first = false;
        }
    }

    private static int LargestFittingPrefix(DebtReportRow row, int field, string text, Graphics graphics, Font font, int maximumHeight)
    {
        int low = 1, high = text.Length, best = 0;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (MeasureRowHeight(graphics, ReplaceValue(row, field, text[..middle]), font) <= maximumHeight) { best = middle; low = middle + 1; }
            else high = middle - 1;
        }
        int wordBreak = best; while (wordBreak > 1 && !char.IsWhiteSpace(text[wordBreak - 1])) wordBreak--;
        return wordBreak > 1 ? wordBreak : best;
    }

    private static DebtReportRow ReplaceValue(DebtReportRow row, int field, string value) => field switch
    {
        0 => row with { StudentCode = value }, 1 => row with { StudentName = value }, 2 => row with { ClassName = value }, 7 => row with { Status = value }, _ => row
    };

    private static void DrawPage(Graphics graphics, DebtReportPdfData data, int pageNumber, int pageCount, IReadOnlyList<PageRow> rows)
    {
        using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold); using var headingFont = new Font("Segoe UI", 12, FontStyle.Bold); using var bodyFont = new Font("Segoe UI", 10); using var smallFont = new Font("Segoe UI", 9);
        using var navy = new SolidBrush(Color.FromArgb(26, 78, 137)); using var dark = new SolidBrush(Color.FromArgb(35, 40, 48)); using var muted = new SolidBrush(Color.FromArgb(85, 95, 110)); using var headerFill = new SolidBrush(Color.FromArgb(232, 241, 250)); using var totalFill = new SolidBrush(Color.FromArgb(235, 249, 241)); using var grid = new Pen(Color.FromArgb(201, 211, 221));
        int headerLeft = Left;
        graphics.DrawString(data.OrganizationName ?? string.Empty, headingFont, muted, new RectangleF(headerLeft, 70, Right - headerLeft, 30)); graphics.DrawString("BÁO CÁO CÔNG NỢ HỌC PHÍ", titleFont, navy, new RectangleF(headerLeft, 112, Right - headerLeft, 44));
        graphics.DrawString($"Học kỳ: {data.SemesterName}", bodyFont, dark, new RectangleF(Left, 175, 620, 28)); graphics.DrawString($"Bộ lọc: {data.FilterDescription}", bodyFont, dark, new RectangleF(Left, 205, Right - Left, 28)); graphics.DrawString($"Thời điểm xuất: {data.ExportedAt:dd/MM/yyyy HH:mm}", smallFont, muted, new RectangleF(Left, 240, Right - Left, 24));
        DrawTableHeader(graphics, headingFont, dark, headerFill, grid);
        int y = HeaderY + MinimumRowHeight; foreach (var row in rows) { DrawRow(graphics, row.Row, y, row.Height, bodyFont, dark, grid); y += row.Height; }
        if (pageNumber == pageCount)
        {
            decimal total = data.Rows.Sum(r => r.TotalAmount), paid = data.Rows.Sum(r => r.PaidAmount), remaining = data.Rows.Sum(r => r.RemainingAmount);
            graphics.FillRectangle(totalFill, Left, y, Right - Left, MinimumRowHeight); DrawCell(graphics, "TỔNG CỘNG", Left, y, ColumnWidths.Take(3).Sum(), MinimumRowHeight, headingFont, dark, StringAlignment.Near);
            int amountX = Left + ColumnWidths.Take(3).Sum(); DrawCell(graphics, $"{total:N0}", amountX, y, ColumnWidths[3], MinimumRowHeight, headingFont, dark, StringAlignment.Far); amountX += ColumnWidths[3]; DrawCell(graphics, $"{paid:N0}", amountX, y, ColumnWidths[4], MinimumRowHeight, headingFont, dark, StringAlignment.Far); amountX += ColumnWidths[4]; DrawCell(graphics, $"{remaining:N0}", amountX, y, ColumnWidths[5], MinimumRowHeight, headingFont, dark, StringAlignment.Far); graphics.DrawRectangle(grid, Left, y, Right - Left, MinimumRowHeight);
        }
        graphics.DrawLine(grid, Left, FooterY - 16, Right, FooterY - 16); graphics.DrawString($"Trang {pageNumber}/{pageCount}", smallFont, muted, new RectangleF(Left, FooterY, Right - Left, 20), RightFormat); graphics.DrawString("EduFee - Báo cáo được xuất trực tiếp từ hệ thống", smallFont, muted, new RectangleF(Left, FooterY, 620, 20));
    }

    private static void DrawTableHeader(Graphics g, Font font, Brush brush, Brush fill, Pen grid)
    {
        int x = Left; for (int i = 0; i < ColumnTitles.Length; i++) { g.FillRectangle(fill, x, HeaderY, ColumnWidths[i], MinimumRowHeight); DrawCell(g, ColumnTitles[i], x, HeaderY, ColumnWidths[i], MinimumRowHeight, font, brush, i is >= 3 and <= 5 ? StringAlignment.Far : StringAlignment.Near); g.DrawRectangle(grid, x, HeaderY, ColumnWidths[i], MinimumRowHeight); x += ColumnWidths[i]; }
    }
    private static void DrawRow(Graphics g, DebtReportRow row, int y, int height, Font font, Brush brush, Pen grid)
    {
        string[] values = [row.StudentCode, row.StudentName, row.ClassName, $"{row.TotalAmount:N0}", $"{row.PaidAmount:N0}", $"{row.RemainingAmount:N0}", row.DueDate?.ToString("dd/MM/yyyy") ?? "-", row.Status]; int x = Left;
        for (int i = 0; i < values.Length; i++) { DrawCell(g, values[i], x, y, ColumnWidths[i], height, font, brush, i is >= 3 and <= 5 ? StringAlignment.Far : StringAlignment.Near); g.DrawRectangle(grid, x, y, ColumnWidths[i], height); x += ColumnWidths[i]; }
    }
    private static void DrawCell(Graphics g, string text, int x, int y, int width, int height, Font font, Brush brush, StringAlignment align)
    {
        using var format = new StringFormat(WrapFormat) { Alignment = align }; g.DrawString(text ?? string.Empty, font, brush, new RectangleF(x + 7, y + 3, width - 14, height - 6), format);
    }
}
