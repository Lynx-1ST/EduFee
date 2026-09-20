using K26_DotNet.Data;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

/// <summary>Small, dependency-free acceptance checks for the tuition ledger.</summary>
public static class FinanceAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(check);
        Directory.CreateDirectory(root);

        CheckThrows(() => new TuitionFee(1, 1, 1, 3, 100m, discountAmount: 301m),
            "constructor rejects a discount above the original tuition", check);

        var jsonPath = Path.Combine(root, "finance-acceptance.json");
        var jsonService = new TuitionService(jsonPath);
        CheckThrows(() => jsonService.Add(new TuitionFee { StudentId = 1, SemesterId = 1, Credits = 1, TotalAmount = 1.5m }),
            "Add rejects fractional VND", check);
        check(jsonService.GetAll().Count == 0, "failed Add leaves the JSON cache unchanged");

        var valid = new TuitionFee { StudentId = 1, SemesterId = 1, Credits = 1, TotalAmount = 100m };
        var invalid = new TuitionFee { StudentId = 2, SemesterId = 1, Credits = 1, TotalAmount = 10m, DiscountAmount = -1m };
        CheckThrows(() => jsonService.AddRange([valid, invalid]),
            "AddRange validates every item before changing the cache", check);
        check(jsonService.GetAll().Count == 0, "failed AddRange leaves no partial additions");

        var db = new SqlDatabaseContext(Path.Combine(root, "finance-acceptance.db"));
        var students = new StudentService(db);
        students.AddStudent(new Student(1, "Nguyen Van A", "a@example.edu", "", new DateTime(2000, 1, 1), "K26A"));
        var semesters = new SemesterService(db);
        var today = DateTime.Today;
        semesters.Add(new Semester(1, "HK1", today.AddDays(-10), today.AddMonths(4), today.AddDays(30), true));
        var fees = new TuitionService(db);
        var receipts = new ReceiptService(db);
        fees.Add(new TuitionFee { StudentId = 1, SemesterId = 1, Credits = 3, TotalAmount = 300m });

        var detached = fees.GetById(1)!;
        detached.TotalAmount = 999m;
        check(fees.GetById(1)!.TotalAmount == 300m, "query results are detached clones");

        var receipt = fees.RecordPaymentWithReceipt(1, 100m, null, receipts, "Tiền mặt", "Nguyen Van A");
        check(receipt.TotalPaidAfterSnapshot == 100m && fees.GetById(1)!.PaidAmount == 100m,
            "payment commits one matching receipt and updates the cache from its snapshot");

        var edited = fees.GetById(1)!;
        edited.PaidAmount = 0m;
        CheckThrows(() => fees.Update(edited), "Update cannot alter the receipt-backed paid amount", check);
        var persisted = new TuitionService(db).GetById(1)!;
        check(persisted.PaidAmount == 100m, "rejected ledger edit leaves SQLite unchanged");
        using (var connection = db.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE Semesters SET DueDate=@due WHERE Id=1";
            command.Parameters.AddWithValue("@due", today.AddDays(-1).ToString("o"));
            command.ExecuteNonQuery();
        }
        edited = fees.GetById(1)!;
        edited.Note = "Sửa ghi chú sau hạn học kỳ";
        fees.Update(edited);
        check(fees.GetById(1)!.Status == PaymentStatus.Overdue &&
            new TuitionService(db).GetById(1)!.Status == PaymentStatus.Overdue,
            "Editing tuition keeps cached overdue status consistent with the semester due date in SQL");
        using (var connection = db.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TRIGGER RejectReceipt BEFORE INSERT ON PaymentReceipts BEGIN SELECT RAISE(ABORT, 'injected failure'); END;";
            command.ExecuteNonQuery();
        }
        bool rejected = false;
        try { fees.RecordPaymentWithReceipt(1, 50m, null, receipts, "Tiền mặt", "Nguyen Van A"); }
        catch (Microsoft.Data.Sqlite.SqliteException) { rejected = true; }
        check(rejected && fees.GetById(1)!.PaidAmount == 100m &&
            new TuitionService(db).GetById(1)!.PaidAmount == 100m &&
            new ReceiptService(db).GetAll().Count == 1 && receipts.GetAll().Count == 1,
            "Receipt insert failure rolls back SQLite payment and leaves both service caches unchanged");
    }

    private static void CheckThrows(Action action, string message, Action<bool, string> check)
    {
        try
        {
            action();
            check(false, message);
        }
        catch (ArgumentException) { check(true, message); }
        catch (InvalidOperationException) { check(true, message); }
    }
}
