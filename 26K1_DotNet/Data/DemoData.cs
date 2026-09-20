using K26_DotNet.Models;
using K26_DotNet.Services;

namespace K26_DotNet.Data;

/// <summary>Small, deterministic fixture for a presentation without touching real data.</summary>
public static class DemoData
{
    public static void EnsureSeeded(SqlDatabaseContext database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var counts = database.GetRecordCounts();
        if (counts.Students != 0 || counts.Semesters != 0 || counts.Fees != 0 || counts.Receipts != 0) return;

        var students = new StudentService(database);
        var semesters = new SemesterService(database);
        var fees = new TuitionService(database);
        var receipts = new ReceiptService(database);
        students.AddStudents([
            new Student(1001, "Nguyễn Minh Anh", "anh.nguyen@example.edu.vn", "0901000001", new DateTime(2005, 3, 12), "K26-CNTT-01"),
            new Student(1002, "Trần Quốc Bảo", "bao.tran@example.edu.vn", "0901000002", new DateTime(2005, 7, 21), "K26-CNTT-01"),
            new Student(1003, "Lê Thu Hà", "ha.le@example.edu.vn", "0901000003", new DateTime(2005, 11, 8), "K26-KT-02")
        ]);
        semesters.Add(new Semester(0, "HK1 2026-2027 (Demo)",
            new DateTime(2026, 9, 1), new DateTime(2027, 1, 20), new DateTime(2026, 9, 30), true));
        var semester = semesters.GetActive() ?? throw new InvalidOperationException("Không thể tạo học kỳ demo.");
        fees.AddRange([
            new TuitionFee(0, 1001, semester.Id, 15, dueDate: semester.DueDate, note: "Dữ liệu trình diễn"),
            new TuitionFee(0, 1002, semester.Id, 12, dueDate: semester.DueDate, note: "Dữ liệu trình diễn"),
            new TuitionFee(0, 1003, semester.Id, 10, dueDate: semester.DueDate, discountAmount: 500_000m,
                discountReason: "Học bổng demo", note: "Dữ liệu trình diễn")
        ]);
        var firstFee = fees.GetAll().Single(fee => fee.StudentId == 1001);
        fees.RecordPaymentWithReceipt(firstFee.Id, 3_000_000m, semester.DueDate, receipts,
            "Chuyển khoản", "Nguyễn Minh Anh", "Thanh toán demo lần 1");
    }
}
