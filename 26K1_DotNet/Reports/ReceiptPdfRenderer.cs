using System.Drawing;
using System.Drawing.Drawing2D;

namespace K26_DotNet.Reports;

public sealed record ReceiptPdfData(
    string OrganizationName,
    string ReceiptCode,
    DateTime PaymentDate,
    string StudentName,
    string StudentCode,
    string ClassName,
    string SemesterName,
    string PayerName,
    string PaymentMethod,
    decimal Amount,
    decimal TotalTuition,
    decimal TotalPaid,
    decimal RemainingAmount,
    DateTime? DueDate,
    string Note);

public static class ReceiptPdfRenderer
{
    public static void Export(string filePath, ReceiptPdfData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var page = new Bitmap(PdfRasterDocument.A4PortraitWidth, PdfRasterDocument.A4PortraitHeight);
        using var graphics = Graphics.FromImage(page);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.White);

        const int left = 110;
        const int right = PdfRasterDocument.A4PortraitWidth - 110;
        int y = 115;
        using var titleFont = new Font("Segoe UI", 25, FontStyle.Bold);
        using var headingFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 13);
        using var boldFont = new Font("Segoe UI", 13, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 11);
        using var muted = new SolidBrush(Color.FromArgb(80, 90, 105));
        using var navy = new SolidBrush(Color.FromArgb(26, 78, 137));
        using var dark = new SolidBrush(Color.FromArgb(35, 40, 48));
        using var green = new SolidBrush(Color.FromArgb(22, 124, 80));
        using var greenLight = new SolidBrush(Color.FromArgb(231, 248, 239));
        using var border = new Pen(Color.FromArgb(211, 218, 227), 2);

        DrawText(graphics, data.OrganizationName, headingFont, muted, left, y, right - left, 35); y += 50;
        DrawText(graphics, "BIÊN LAI THU TIỀN HỌC PHÍ", titleFont, navy, left, y, right - left, 48); y += 62;
        DrawText(graphics, $"Mã biên lai: {data.ReceiptCode}    |    Ngày lập: {data.PaymentDate:dd/MM/yyyy HH:mm}", smallFont, muted, left, y, right - left, 28); y += 45;
        graphics.DrawLine(border, left, y, right, y); y += 32;

        y = DrawRow(graphics, "Họ và tên sinh viên:", data.StudentName, y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Mã sinh viên:", data.StudentCode, y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Lớp học:", data.ClassName, y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Học kỳ:", data.SemesterName, y, left, right, boldFont, bodyFont, dark);
        y += 14;
        graphics.DrawLine(border, left, y, right, y); y += 32;

        y = DrawRow(graphics, "Người nộp tiền:", data.PayerName, y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Hình thức thanh toán:", data.PaymentMethod, y, left, right, boldFont, bodyFont, dark);
        y += 16;
        const int amountHeight = 105;
        graphics.FillRectangle(greenLight, left, y, right - left, amountHeight);
        graphics.DrawRectangle(new Pen(Color.FromArgb(115, 190, 147), 2), left, y, right - left, amountHeight);
        DrawText(graphics, "SỐ TIỀN THU", boldFont, green, left + 28, y + 35, 260, 34);
        DrawTextRight(graphics, $"{data.Amount:N0} VNĐ", titleFont, green, right - 28, y + 25, 430, 45);
        y += amountHeight + 35;

        y = DrawRow(graphics, "Tổng học phí:", $"{data.TotalTuition:N0} VNĐ", y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Đã nộp tổng cộng:", $"{data.TotalPaid:N0} VNĐ", y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Còn lại phải nộp:", $"{data.RemainingAmount:N0} VNĐ", y, left, right, boldFont, bodyFont, dark);
        y = DrawRow(graphics, "Hạn nộp học phí:", data.DueDate?.ToString("dd/MM/yyyy") ?? "Không áp dụng", y, left, right, boldFont, bodyFont, dark);
        if (!string.IsNullOrWhiteSpace(data.Note))
            y = DrawRow(graphics, "Ghi chú:", data.Note, y, left, right, boldFont, bodyFont, dark);

        int signatureY = Math.Max(y + 75, 1300);
        DrawCentered(graphics, "NGƯỜI NỘP TIỀN\n(Ký và ghi rõ họ tên)", smallFont, muted, left + 20, signatureY, 310, 65);
        DrawCentered(graphics, "NGƯỜI THU TIỀN\n(Ký, đóng dấu)", smallFont, muted, right - 330, signatureY, 310, 65);
        DrawText(graphics, "Biên lai được xuất trực tiếp từ EduFee.", smallFont, muted, left, PdfRasterDocument.A4PortraitHeight - 110, right - left, 25);

        PdfRasterDocument.Write(filePath, new[] { page }, landscape: false);
    }

    private static int DrawRow(Graphics graphics, string label, string value, int y, int left, int right, Font labelFont, Font valueFont, Brush color)
    {
        DrawText(graphics, label, labelFont, color, left, y, 305, 55);
        DrawText(graphics, string.IsNullOrWhiteSpace(value) ? "-" : value, valueFont, color, left + 315, y, right - left - 315, 55);
        return y + 48;
    }

    private static void DrawText(Graphics g, string text, Font font, Brush brush, int x, int y, int width, int height) =>
        g.DrawString(text ?? string.Empty, font, brush, new RectangleF(x, y, width, height), StringFormat.GenericDefault);

    private static void DrawTextRight(Graphics g, string text, Font font, Brush brush, int right, int y, int width, int height) =>
        g.DrawString(text, font, brush, new RectangleF(right - width, y, width, height), new StringFormat { Alignment = StringAlignment.Far });

    private static void DrawCentered(Graphics g, string text, Font font, Brush brush, int x, int y, int width, int height) =>
        g.DrawString(text, font, brush, new RectangleF(x, y, width, height), new StringFormat { Alignment = StringAlignment.Center });
}
