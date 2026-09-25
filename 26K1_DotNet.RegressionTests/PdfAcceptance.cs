using System.Text;
using K26_DotNet.Reports;

/// <summary>Focused acceptance checks for the PDF layouts. Program.cs owns when these checks run.</summary>
public static class PdfAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root); ArgumentNullException.ThrowIfNull(check);
        string directory = Path.Combine(Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR") ?? root, "pdf-acceptance"); Directory.CreateDirectory(directory);
        var longText = string.Join(" ", Enumerable.Repeat("Nguyễn Thị Minh Anh có tên dài để kiểm tra xuống dòng trong báo cáo", 8));
        var rows = Enumerable.Range(1, 101).Select(i => new DebtReportRow($"SV{i:D4}", i % 5 == 0 ? longText : $"Sinh viên {i}", i % 7 == 0 ? "Lớp có tên dài K26 Công nghệ thông tin chất lượng cao" : "K26-CNTT", 3_000_000m, i * 10_000m, 3_000_000m - i * 10_000m, new DateTime(2026, 9, 30), i % 2 == 0 ? "Quá hạn cần liên hệ phụ huynh để đối soát khoản nợ" : "Nộp một phần")).ToList();
        ExportDebt(directory, "debt-empty.pdf", Array.Empty<DebtReportRow>(), 1, check, "Debt PDF supports no rows");
        ExportDebt(directory, "debt-one.pdf", rows.Take(1).ToList(), 1, check, "Debt PDF supports one row");
        ExportDebt(directory, "debt-25.pdf", rows.Take(25).ToList(), 2, check, "Debt PDF puts a full final table on a new page before totals");
        ExportDebt(directory, "debt-101-long.pdf", rows, 2, check, "Debt PDF paginates 100+ rows and wrapped names");

        string receipt = Path.Combine(directory, "receipt-long.pdf");
        ReceiptPdfRenderer.Export(receipt, new ReceiptPdfData("EduFee · Student Tuition Management", "BL-LONG-0001", new DateTime(2026, 9, 19, 9, 30, 0), longText, "SV-LONG-0001", "Lớp rất dài kiểm tra trình bày biên lai học phí", "Học kỳ có tên dài", longText, "Chuyển khoản ngân hàng", 1_200_000m, 3_000_000m, 1_200_000m, 1_800_000m, new DateTime(2026, 9, 30), string.Join(" ", Enumerable.Repeat("Ghi chú dài phải được giữ nguyên và tiếp tục ở trang kế tiếp.", 120))));
        // This note needs more than one content page. Requiring three pages catches a renderer
        // that merely moves one oversized row to a second page and clips the remainder.
        check(IsPdf(receipt) && CountPages(receipt) >= 3, "Receipt PDF splits oversized notes across continuation pages");
    }

    private static void ExportDebt(string directory, string name, IReadOnlyList<DebtReportRow> rows, int minimumPages, Action<bool, string> check, string description)
    {
        string path = Path.Combine(directory, name);
        DebtReportPdfRenderer.Export(path, new DebtReportPdfData("EduFee · Student Tuition Management", "HK1 2026-2027", "Kiểm thử PDF", new DateTime(2026, 9, 19), rows));
        check(IsPdf(path) && CountPages(path) >= minimumPages, description);
    }

    private static bool IsPdf(string path) => File.Exists(path) && new FileInfo(path).Length > 1_000 && File.ReadAllBytes(path).Take(5).SequenceEqual("%PDF-"u8.ToArray());
    private static int CountPages(string path) => Encoding.ASCII.GetString(File.ReadAllBytes(path)).Split("/Type /Page ", StringSplitOptions.None).Length - 1;
}
