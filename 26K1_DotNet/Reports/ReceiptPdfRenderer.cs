using System.Drawing;
using System.Drawing.Drawing2D;

namespace K26_DotNet.Reports;

public sealed record ReceiptPdfData(string OrganizationName, string ReceiptCode, DateTime PaymentDate, string StudentName, string StudentCode, string ClassName, string SemesterName, string PayerName, string PaymentMethod, decimal Amount, decimal TotalTuition, decimal TotalPaid, decimal RemainingAmount, DateTime? DueDate, string Note);

public static class ReceiptPdfRenderer
{
    private const int Left = 110, Right = PdfRasterDocument.A4PortraitWidth - 110, ContentY = 330, ContentBottom = 1190, FooterY = PdfRasterDocument.A4PortraitHeight - 110;

    public static void Export(string filePath, ReceiptPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var pages = RenderPages(data);
        try { PdfRasterDocument.Write(filePath, pages, landscape: false); }
        finally { foreach (var page in pages) page.Dispose(); }
    }

    // The caller owns the returned bitmaps and must dispose them after printing or exporting.
    // Keeping this rendering path shared prevents the on-screen panel from clipping printed data.
    public static List<Bitmap> RenderPages(ReceiptPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var measuringBitmap = new Bitmap(1, 1); using var measuringGraphics = Graphics.FromImage(measuringBitmap); using var bodyFont = new Font("Segoe UI", 13); using var boldFont = new Font("Segoe UI", 13, FontStyle.Bold);
        var rows = SplitOversizedRows(CreateRows(data, measuringGraphics, bodyFont, boldFont), measuringGraphics, bodyFont);
        var pageRows = Paginate(rows);
        var pages = new List<Bitmap>(pageRows.Count);
        try
        {
            for (int index = 0; index < pageRows.Count; index++)
            {
                var page = new Bitmap(PdfRasterDocument.A4PortraitWidth, PdfRasterDocument.A4PortraitHeight); pages.Add(page);
                using var graphics = Graphics.FromImage(page); graphics.SmoothingMode = SmoothingMode.AntiAlias; graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; graphics.Clear(Color.White);
                DrawPage(graphics, data, pageRows[index], index + 1, pageRows.Count);
            }
            return pages;
        }
        catch
        {
            foreach (var page in pages) page.Dispose();
            throw;
        }
    }

    private sealed record ReceiptRow(string Label, string Value, int Height, bool IsAmount = false);
    private static List<ReceiptRow> CreateRows(ReceiptPdfData data, Graphics graphics, Font body, Font bold)
    {
        var raw = new (string Label, string Value, bool Amount)[]
        {
            ("Họ và tên sinh viên:", data.StudentName, false), ("Mã sinh viên:", data.StudentCode, false), ("Lớp học:", data.ClassName, false), ("Học kỳ:", data.SemesterName, false),
            ("Người nộp tiền:", data.PayerName, false), ("Hình thức thanh toán:", data.PaymentMethod, false), ("SỐ TIỀN THU:", $"{data.Amount:N0} VNĐ", true),
            ("Tổng học phí:", $"{data.TotalTuition:N0} VNĐ", false), ("Đã nộp tổng cộng:", $"{data.TotalPaid:N0} VNĐ", false), ("Còn lại phải nộp:", $"{data.RemainingAmount:N0} VNĐ", false), ("Hạn nộp học phí:", data.DueDate?.ToString("dd/MM/yyyy") ?? "Không áp dụng", false)
        };
        var rows = new List<ReceiptRow>(raw.Length + 1);
        foreach (var row in raw)
        {
            string value = string.IsNullOrWhiteSpace(row.Value) ? "-" : row.Value;
            int height = row.Amount ? 90 : MeasureRow(graphics, value, body);
            rows.Add(new ReceiptRow(row.Label, value, height, row.Amount));
        }
        if (!string.IsNullOrWhiteSpace(data.Note)) rows.Add(new ReceiptRow("Ghi chú:", data.Note, MeasureRow(graphics, data.Note, body), false));
        return rows;
    }

    private static int MeasureRow(Graphics graphics, string value, Font font) => Math.Max(48, (int)Math.Ceiling(graphics.MeasureString(value, font, Right - Left - 330, StringFormat.GenericDefault).Height + 14));

    // A note (or imported student name) can be much taller than a page. Split it before
    // pagination so every fragment has a drawable rectangle and no text is silently clipped.
    private static List<ReceiptRow> SplitOversizedRows(IReadOnlyList<ReceiptRow> rows, Graphics graphics, Font font)
    {
        int maximumHeight = ContentBottom - ContentY;
        var result = new List<ReceiptRow>();
        foreach (var row in rows)
        {
            if (row.IsAmount || row.Height <= maximumHeight) { result.Add(row); continue; }

            string remaining = row.Value;
            bool first = true;
            while (remaining.Length > 0)
            {
                int count = LargestFittingPrefix(remaining, graphics, font, maximumHeight);
                // A single glyph always fits; this guard prevents a malformed font measurement
                // from causing an infinite loop while preserving the original value.
                count = Math.Max(1, count);
                string fragment = remaining[..count];
                remaining = remaining[count..];
                int height = MeasureRow(graphics, fragment, font);
                result.Add(new ReceiptRow(first ? row.Label : $"{row.Label} (tiếp):", fragment, height));
                first = false;
            }
        }
        return result;
    }

    private static int LargestFittingPrefix(string text, Graphics graphics, Font font, int maximumHeight)
    {
        int low = 1, high = text.Length, best = 0;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (MeasureRow(graphics, text[..middle], font) <= maximumHeight) { best = middle; low = middle + 1; }
            else high = middle - 1;
        }
        // Prefer ending at whitespace when possible so continuation pages remain readable.
        int wordBreak = best;
        while (wordBreak > 1 && !char.IsWhiteSpace(text[wordBreak - 1])) wordBreak--;
        return wordBreak > 1 ? wordBreak : best;
    }
    private static List<IReadOnlyList<ReceiptRow>> Paginate(IReadOnlyList<ReceiptRow> rows)
    {
        var pages = new List<IReadOnlyList<ReceiptRow>>(); var current = new List<ReceiptRow>(); int used = ContentY;
        foreach (var row in rows)
        {
            if (current.Count > 0 && used + row.Height > ContentBottom) { pages.Add(current); current = new List<ReceiptRow>(); used = ContentY; }
            current.Add(row); used += row.Height;
        }
        pages.Add(current); return pages;
    }

    private static void DrawPage(Graphics graphics, ReceiptPdfData data, IReadOnlyList<ReceiptRow> rows, int pageNumber, int pageCount)
    {
        using var titleFont = new Font("Segoe UI", 25, FontStyle.Bold); using var headingFont = new Font("Segoe UI", 14, FontStyle.Bold); using var bodyFont = new Font("Segoe UI", 13); using var boldFont = new Font("Segoe UI", 13, FontStyle.Bold); using var smallFont = new Font("Segoe UI", 11);
        using var muted = new SolidBrush(Color.FromArgb(80, 90, 105)); using var navy = new SolidBrush(Color.FromArgb(26, 78, 137)); using var dark = new SolidBrush(Color.FromArgb(35, 40, 48)); using var green = new SolidBrush(Color.FromArgb(22, 124, 80)); using var greenLight = new SolidBrush(Color.FromArgb(231, 248, 239)); using var border = new Pen(Color.FromArgb(211, 218, 227), 2);
        int headerLeft = Left;
        DrawText(graphics, data.OrganizationName, headingFont, muted, headerLeft, 108, Right - headerLeft, 35); DrawText(graphics, "BIÊN LAI THU TIỀN HỌC PHÍ", titleFont, navy, headerLeft, 153, Right - headerLeft, 48);
        DrawText(graphics, $"Mã biên lai: {data.ReceiptCode}    |    Ngày lập: {data.PaymentDate:dd/MM/yyyy HH:mm}", smallFont, muted, headerLeft, 215, Right - headerLeft, 28); graphics.DrawLine(border, Left, 285, Right, 285);
        int y = ContentY;
        foreach (var row in rows)
        {
            if (row.IsAmount) { graphics.FillRectangle(greenLight, Left, y, Right - Left, row.Height); graphics.DrawRectangle(new Pen(Color.FromArgb(115, 190, 147), 2), Left, y, Right - Left, row.Height); DrawText(graphics, row.Label, boldFont, green, Left + 24, y + 26, 270, 34); DrawTextRight(graphics, row.Value, titleFont, green, Right - 24, y + 19, 420, 45); }
            else { DrawText(graphics, row.Label, boldFont, dark, Left, y + 6, 305, row.Height - 8); DrawText(graphics, row.Value, bodyFont, dark, Left + 315, y + 6, Right - Left - 315, row.Height - 8); }
            y += row.Height;
        }
        if (pageNumber == pageCount) { int signatureY = Math.Max(y + 42, 1280); DrawCentered(graphics, "NGƯỜI NỘP TIỀN\n(Ký và ghi rõ họ tên)", smallFont, muted, Left + 20, signatureY, 310, 65); DrawCentered(graphics, "NGƯỜI THU TIỀN\n(Ký, đóng dấu)", smallFont, muted, Right - 330, signatureY, 310, 65); }
        DrawText(graphics, "Biên lai được xuất trực tiếp từ EduFee.", smallFont, muted, Left, FooterY, Right - Left - 100, 25); DrawTextRight(graphics, $"Trang {pageNumber}/{pageCount}", smallFont, muted, Right, FooterY, 100, 25);
    }
    private static void DrawText(Graphics g, string text, Font font, Brush brush, int x, int y, int width, int height) => g.DrawString(text ?? string.Empty, font, brush, new RectangleF(x, y, width, height), StringFormat.GenericDefault);
    private static void DrawTextRight(Graphics g, string text, Font font, Brush brush, int right, int y, int width, int height) { using var format = new StringFormat { Alignment = StringAlignment.Far }; g.DrawString(text ?? string.Empty, font, brush, new RectangleF(right - width, y, width, height), format); }
    private static void DrawCentered(Graphics g, string text, Font font, Brush brush, int x, int y, int width, int height) { using var format = new StringFormat { Alignment = StringAlignment.Center }; g.DrawString(text, font, brush, new RectangleF(x, y, width, height), format); }
}
