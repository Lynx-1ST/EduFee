using System.Globalization;
using K26_DotNet.Models;
using Microsoft.Data.Sqlite;

namespace K26_DotNet.Data;

/// <summary>
/// Persistence operations used by the desktop application after the one-time JSON import.
/// Each method owns its transaction so the in-memory services can roll back cleanly on failure.
/// </summary>
public sealed class SqliteRepository
{
    private readonly SqlDatabaseContext _db;

    public SqliteRepository(SqlDatabaseContext db) => _db = db;
    public string DatabasePath => _db.DbPath;

    public (List<Student> Students, List<Semester> Semesters, List<TuitionFee> Fees, List<PaymentReceipt> Receipts) LoadAll() =>
        new SqlDataMigrator(_db).LoadAllFromSql();

    public void AddStudents(IEnumerable<Student> students)
    {
        using var connection = _db.CreateConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var student in students)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Students (Id, StudentCode, FullName, Email, PhoneNumber, DateOfBirth, ClassName)
                VALUES (@id, @code, @name, @email, @phone, @dob, @class);
                """;
            AddStudentParameters(command, student);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void UpdateStudent(Student student)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Students SET StudentCode=@code, FullName=@name, Email=@email, PhoneNumber=@phone,
                DateOfBirth=@dob, ClassName=@class WHERE Id=@id;
            """;
        AddStudentParameters(command, student);
        RequireOne(command.ExecuteNonQuery(), "Không tìm thấy sinh viên cần cập nhật.");
    }

    public void DeleteStudent(int id) => DeleteRestricted("Students", id,
        "Không thể xóa sinh viên đã có dữ liệu học phí. Hãy giữ hồ sơ để bảo toàn lịch sử tài chính.");

    public void AddSemester(Semester semester)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Semesters (Id, Name, StartDate, EndDate, DueDate, IsActive)
            VALUES (@id, @name, @start, @end, @due, @active);
            """;
        AddSemesterParameters(command, semester);
        command.ExecuteNonQuery();
    }

    public void UpdateSemester(Semester semester)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Semesters SET Name=@name, StartDate=@start, EndDate=@end,
                DueDate=@due, IsActive=@active WHERE Id=@id;
            """;
        AddSemesterParameters(command, semester);
        RequireOne(command.ExecuteNonQuery(), "Không tìm thấy học kỳ cần cập nhật.");
    }

    public void SetActiveSemester(int id)
    {
        using var connection = _db.CreateConnection();
        using var transaction = connection.BeginTransaction();
        using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = "UPDATE Semesters SET IsActive=0;";
            clear.ExecuteNonQuery();
        }
        using (var activate = connection.CreateCommand())
        {
            activate.Transaction = transaction;
            activate.CommandText = "UPDATE Semesters SET IsActive=1 WHERE Id=@id;";
            activate.Parameters.AddWithValue("@id", id);
            RequireOne(activate.ExecuteNonQuery(), "Không tìm thấy học kỳ cần kích hoạt.");
        }
        transaction.Commit();
    }

    public void DeleteSemester(int id) => DeleteRestricted("Semesters", id,
        "Không thể xóa học kỳ đã có dữ liệu học phí. Hãy giữ học kỳ để bảo toàn lịch sử tài chính.");

    public void AddTuitionFees(IEnumerable<TuitionFee> fees)
    {
        using var connection = _db.CreateConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var fee in fees)
        {
            ValidateFee(fee);
            if (fee.PaidAmount != 0 || fee.PaidDate.HasValue)
                throw new InvalidOperationException("Phiếu học phí mới không được có số đã thu; hãy ghi nhận bằng biên lai.");
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO TuitionFees
                    (Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason,
                     PaidAmount, PaidDate, DueDate, Status, Note)
                VALUES (@id,@student,@semester,@credits,@total,@discount,@reason,@paid,@paidDate,@dueDate,@status,@note);
                """;
            AddFeeParameters(command, fee);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public PaymentStatus UpdateTuitionFee(TuitionFee fee)
    {
        ValidateFee(fee);
        using var connection = _db.CreateConnection();
        using (var ledger = connection.CreateCommand())
        {
            ledger.CommandText = "SELECT PaidAmount, PaidDate FROM TuitionFees WHERE Id=@id;";
            ledger.Parameters.AddWithValue("@id", fee.Id);
            using var reader = ledger.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("Không tìm thấy phiếu học phí cần cập nhật.");
            long paidAmount = reader.GetInt64(0);
            DateTime? paidDate = reader.IsDBNull(1) ? null : ParseDate(reader.GetString(1));
            if (paidAmount != ToVnd(fee.PaidAmount) || paidDate != fee.PaidDate)
                throw new InvalidOperationException("Không thể sửa số tiền hoặc ngày thu trực tiếp. Hãy lập biên lai thu tiền.");
        }
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE TuitionFees SET Credits=@credits, TotalAmount=@total, DiscountAmount=@discount,
                DiscountReason=@reason, DueDate=@dueDate,
                Status=CASE
                    WHEN PaidAmount >= @total
                         AND PaidDate IS NOT NULL
                         AND COALESCE(@dueDate, (SELECT DueDate FROM Semesters WHERE Id=TuitionFees.SemesterId)) IS NOT NULL
                         AND date(PaidDate) > date(COALESCE(@dueDate, (SELECT DueDate FROM Semesters WHERE Id=TuitionFees.SemesterId))) THEN 4
                    WHEN PaidAmount >= @total THEN 2
                    WHEN COALESCE(@dueDate, (SELECT DueDate FROM Semesters WHERE Id=TuitionFees.SemesterId)) IS NOT NULL
                         AND @today > date(COALESCE(@dueDate, (SELECT DueDate FROM Semesters WHERE Id=TuitionFees.SemesterId))) THEN 3
                    WHEN PaidAmount > 0 THEN 1
                    ELSE 0
                END,
                Note=@note WHERE Id=@id RETURNING Status;
            """;
        AddFeeParameters(command, fee);
        command.Parameters.AddWithValue("@today", DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var status = command.ExecuteScalar();
        if (status is null) throw new InvalidOperationException("Không tìm thấy phiếu học phí cần cập nhật.");
        return (PaymentStatus)Convert.ToInt32(status);
    }

    public void DeleteTuitionFee(int id) => DeleteRestricted("TuitionFees", id,
        "Không thể xóa phiếu học phí đã có biên lai. Hãy giữ phiếu để bảo toàn lịch sử thu.");

    public PaymentReceipt RecordPayment(int feeId, decimal amount, string paymentMethod, string payerName,
        string note, DateTime? semesterDueDate = null)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền thanh toán phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(payerName)) throw new ArgumentException("Thiếu người nộp tiền.", nameof(payerName));
        long amountVnd = ToVnd(amount);

        using var connection = _db.CreateConnection();
        using var transaction = connection.BeginTransaction();

        int studentId;
        int semesterId;
        long total;
        long paid;
        DateTime? feeDueDate;
        string studentName;
        string studentCode;
        string className;
        string semesterName;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = @"
                SELECT f.StudentId, f.SemesterId, f.TotalAmount, f.PaidAmount, COALESCE(f.DueDate, sem.DueDate),
                       st.FullName, st.StudentCode, st.ClassName, sem.Name
                FROM TuitionFees f
                JOIN Students st ON st.Id=f.StudentId
                JOIN Semesters sem ON sem.Id=f.SemesterId
                WHERE f.Id=@id;";
            query.Parameters.AddWithValue("@id", feeId);
            using var reader = query.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException($"Không tìm thấy học phí ID {feeId}.");
            studentId = reader.GetInt32(0);
            semesterId = reader.GetInt32(1);
            total = reader.GetInt64(2);
            paid = reader.GetInt64(3);
            feeDueDate = reader.IsDBNull(4) ? null : ParseDate(reader.GetString(4));
            studentName = reader.GetString(5);
            studentCode = reader.GetString(6);
            className = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
            semesterName = reader.GetString(8);
        }

        if (paid + amountVnd > total)
            throw new InvalidOperationException($"Số tiền vượt quá số còn lại ({total - paid:N0} VNĐ).");

        var paymentDate = DateTime.Now;
        long newPaid = paid + amountVnd;
        var effectiveDueDate = feeDueDate ?? semesterDueDate;
        var status = newPaid >= total
            ? effectiveDueDate.HasValue && paymentDate.Date > effectiveDueDate.Value.Date
                ? PaymentStatus.LatePaid
                : PaymentStatus.Paid
            : effectiveDueDate.HasValue && DateTime.Today > effectiveDueDate.Value.Date ? PaymentStatus.Overdue
            : PaymentStatus.PartiallyPaid;

        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = "UPDATE TuitionFees SET PaidAmount=@paid, PaidDate=@date, Status=@status WHERE Id=@id;";
            update.Parameters.AddWithValue("@paid", newPaid);
            update.Parameters.AddWithValue("@date", FormatDate(paymentDate));
            update.Parameters.AddWithValue("@status", (int)status);
            update.Parameters.AddWithValue("@id", feeId);
            RequireOne(update.ExecuteNonQuery(), "Không thể cập nhật khoản học phí.");
        }

        int nextId;
        using (var next = connection.CreateCommand())
        {
            next.Transaction = transaction;
            next.CommandText = "SELECT COALESCE(MAX(Id), 0) + 1 FROM PaymentReceipts;";
            nextId = Convert.ToInt32(next.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        string receiptCode = $"BL-{paymentDate.Year}-{nextId:D4}";
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO PaymentReceipts
                    (Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note,
                     StudentNameSnapshot, StudentCodeSnapshot, ClassNameSnapshot, SemesterNameSnapshot,
                     TotalTuitionSnapshot, TotalPaidAfterSnapshot, RemainingAfterSnapshot, DueDateSnapshot)
                VALUES (@id,@fee,@student,@semester,@code,@amount,@date,@method,@payer,@note,
                        @studentName,@studentCode,@className,@semesterName,@total,@paidAfter,@remaining,@due);
                """;
            insert.Parameters.AddWithValue("@id", nextId);
            insert.Parameters.AddWithValue("@fee", feeId);
            insert.Parameters.AddWithValue("@student", studentId);
            insert.Parameters.AddWithValue("@semester", semesterId);
            insert.Parameters.AddWithValue("@code", receiptCode);
            insert.Parameters.AddWithValue("@amount", amountVnd);
            insert.Parameters.AddWithValue("@date", FormatDate(paymentDate));
            insert.Parameters.AddWithValue("@method", paymentMethod?.Trim() ?? string.Empty);
            insert.Parameters.AddWithValue("@payer", payerName.Trim());
            insert.Parameters.AddWithValue("@note", note?.Trim() ?? string.Empty);
            insert.Parameters.AddWithValue("@studentName", studentName);
            insert.Parameters.AddWithValue("@studentCode", studentCode);
            insert.Parameters.AddWithValue("@className", className);
            insert.Parameters.AddWithValue("@semesterName", semesterName);
            insert.Parameters.AddWithValue("@total", total);
            insert.Parameters.AddWithValue("@paidAfter", newPaid);
            insert.Parameters.AddWithValue("@remaining", total - newPaid);
            insert.Parameters.AddWithValue("@due", effectiveDueDate.HasValue ? FormatDate(effectiveDueDate.Value) : DBNull.Value);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
        return new PaymentReceipt(nextId, receiptCode, feeId, studentId, semesterId, amountVnd,
            paymentMethod ?? string.Empty, payerName, note ?? string.Empty)
        {
            PaymentDate = paymentDate,
            StudentNameSnapshot = studentName,
            StudentCodeSnapshot = studentCode,
            ClassNameSnapshot = className,
            SemesterNameSnapshot = semesterName,
            TotalTuitionSnapshot = total,
            TotalPaidAfterSnapshot = newPaid,
            RemainingAfterSnapshot = total - newPaid,
            DueDateSnapshot = effectiveDueDate
        };
    }

    private void DeleteRestricted(string table, int id, string relationshipMessage)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {table} WHERE Id=@id;";
        command.Parameters.AddWithValue("@id", id);
        try { RequireOne(command.ExecuteNonQuery(), "Không tìm thấy dữ liệu cần xóa."); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(relationshipMessage, ex);
        }
    }

    private static void AddStudentParameters(SqliteCommand command, Student student)
    {
        command.Parameters.AddWithValue("@id", student.Id);
        command.Parameters.AddWithValue("@code", student.StudentCode.Trim());
        command.Parameters.AddWithValue("@name", student.FullName.Trim());
        command.Parameters.AddWithValue("@email", student.Email?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@phone", student.PhoneNumber?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@dob", FormatDate(student.DateOfBirth));
        command.Parameters.AddWithValue("@class", student.ClassName.Trim());
    }

    private static void AddSemesterParameters(SqliteCommand command, Semester semester)
    {
        command.Parameters.AddWithValue("@id", semester.Id);
        command.Parameters.AddWithValue("@name", semester.Name.Trim());
        command.Parameters.AddWithValue("@start", FormatDate(semester.StartDate));
        command.Parameters.AddWithValue("@end", FormatDate(semester.EndDate));
        command.Parameters.AddWithValue("@due", FormatDate(semester.DueDate));
        command.Parameters.AddWithValue("@active", semester.IsActive ? 1 : 0);
    }

    private static void AddFeeParameters(SqliteCommand command, TuitionFee fee)
    {
        command.Parameters.AddWithValue("@id", fee.Id);
        command.Parameters.AddWithValue("@student", fee.StudentId);
        command.Parameters.AddWithValue("@semester", fee.SemesterId);
        command.Parameters.AddWithValue("@credits", fee.Credits);
        command.Parameters.AddWithValue("@total", ToVnd(fee.TotalAmount));
        command.Parameters.AddWithValue("@discount", ToVnd(fee.DiscountAmount));
        command.Parameters.AddWithValue("@reason", fee.DiscountReason ?? string.Empty);
        command.Parameters.AddWithValue("@paid", ToVnd(fee.PaidAmount));
        command.Parameters.AddWithValue("@paidDate", fee.PaidDate.HasValue ? FormatDate(fee.PaidDate.Value) : DBNull.Value);
        command.Parameters.AddWithValue("@dueDate", fee.DueDate.HasValue ? FormatDate(fee.DueDate.Value) : DBNull.Value);
        command.Parameters.AddWithValue("@status", (int)fee.Status);
        command.Parameters.AddWithValue("@note", fee.Note ?? string.Empty);
    }

    private static void ValidateFee(TuitionFee fee)
    {
        ArgumentNullException.ThrowIfNull(fee);
        if (fee.Id <= 0 || fee.StudentId <= 0 || fee.SemesterId <= 0)
            throw new ArgumentOutOfRangeException(nameof(fee), "Mã phiếu, sinh viên và học kỳ phải lớn hơn 0.");
        if (fee.Credits <= 0) throw new ArgumentOutOfRangeException(nameof(fee.Credits), "Số tín chỉ phải lớn hơn 0.");
        if (fee.TotalAmount < 0 || fee.DiscountAmount < 0 || fee.PaidAmount < 0 || fee.PaidAmount > fee.TotalAmount)
            throw new ArgumentException("Số tiền học phí, giảm trừ hoặc đã thu không hợp lệ.");
        _ = ToVnd(fee.TotalAmount);
        _ = ToVnd(fee.DiscountAmount);
        _ = ToVnd(fee.PaidAmount);
    }

    private static long ToVnd(decimal amount)
    {
        if (amount != decimal.Truncate(amount))
            throw new ArgumentException("Số tiền VND phải là số nguyên đồng.");
        return checked((long)amount);
    }

    private static string FormatDate(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static DateTime? ParseDate(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;

    private static void RequireOne(int affected, string message)
    {
        if (affected != 1) throw new InvalidOperationException(message);
    }
}
