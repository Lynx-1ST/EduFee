using System;
using System.Collections.Generic;
using System.Globalization;
using K26_DotNet.Models;
using K26_DotNet.Services;
using Microsoft.Data.Sqlite;

namespace K26_DotNet.Data
{
    public class SqlDataMigrator
    {
        private readonly SqlDatabaseContext _db;

        public SqlDataMigrator(SqlDatabaseContext db)
        {
            _db = db;
        }

        public (int Students, int Semesters, int Fees, int Receipts, string Message) MigrateFromJson(
            StudentService svSvc, SemesterService semSvc, TuitionService tuiSvc, ReceiptService receiptSvc)
        {
            using var conn = _db.CreateConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                var students = svSvc.GetAllStudents();
                var semesters = semSvc.GetAll();
                var fees = tuiSvc.GetAll();
                var receipts = receiptSvc.GetAll();
                ValidateFinancialLedger(fees, receipts);
                var studentById = students.ToDictionary(student => student.Id);
                var semesterById = semesters.ToDictionary(semester => semester.Id);
                var feeById = fees.ToDictionary(fee => fee.Id);
                var paidAfterByReceiptId = new Dictionary<int, decimal>();
                foreach (var group in receipts.GroupBy(receipt => receipt.TuitionFeeId))
                {
                    decimal cumulative = 0;
                    foreach (var receipt in group.OrderBy(receipt => receipt.PaymentDate).ThenBy(receipt => receipt.Id))
                    {
                        cumulative += receipt.Amount;
                        paidAfterByReceiptId[receipt.Id] = cumulative;
                    }
                }

                // A migration is a complete snapshot replacement. This removes rows that were
                // deleted from JSON and avoids reviving stale data on a later SQL restore.
                using (var clear = conn.CreateCommand())
                {
                    clear.Transaction = tx;
                    clear.CommandText = "DELETE FROM PaymentReceipts; DELETE FROM TuitionFees; DELETE FROM Semesters; DELETE FROM Students;";
                    clear.ExecuteNonQuery();
                }

                // 1. Migrate Students
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO Students (Id, StudentCode, FullName, Email, PhoneNumber, DateOfBirth, ClassName)
                        VALUES (@id, @code, @name, @email, @phone, @dob, @class);";

                    var pId = cmd.Parameters.Add("@id", SqliteType.Integer);
                    var pCode = cmd.Parameters.Add("@code", SqliteType.Text);
                    var pName = cmd.Parameters.Add("@name", SqliteType.Text);
                    var pEmail = cmd.Parameters.Add("@email", SqliteType.Text);
                    var pPhone = cmd.Parameters.Add("@phone", SqliteType.Text);
                    var pDob = cmd.Parameters.Add("@dob", SqliteType.Text);
                    var pClass = cmd.Parameters.Add("@class", SqliteType.Text);

                    foreach (var s in students)
                    {
                        pId.Value = s.Id;
                        pCode.Value = s.StudentCode;
                        pName.Value = s.FullName ?? "";
                        pEmail.Value = s.Email ?? "";
                        pPhone.Value = s.PhoneNumber ?? "";
                        pDob.Value = s.DateOfBirth.ToString("o", CultureInfo.InvariantCulture);
                        pClass.Value = s.ClassName ?? "";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Migrate Semesters
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO Semesters (Id, Name, StartDate, EndDate, DueDate, IsActive, TuitionPerCredit)
                        VALUES (@id, @name, @start, @end, @due, @active, @tuitionPerCredit);";

                    var pId = cmd.Parameters.Add("@id", SqliteType.Integer);
                    var pName = cmd.Parameters.Add("@name", SqliteType.Text);
                    var pStart = cmd.Parameters.Add("@start", SqliteType.Text);
                    var pEnd = cmd.Parameters.Add("@end", SqliteType.Text);
                    var pDue = cmd.Parameters.Add("@due", SqliteType.Text);
                    var pActive = cmd.Parameters.Add("@active", SqliteType.Integer);
                    var pTuitionPerCredit = cmd.Parameters.Add("@tuitionPerCredit", SqliteType.Integer);

                    foreach (var s in semesters)
                    {
                        pId.Value = s.Id;
                        pName.Value = s.Name ?? "";
                        pStart.Value = s.StartDate.ToString("o", CultureInfo.InvariantCulture);
                        pEnd.Value = s.EndDate.ToString("o", CultureInfo.InvariantCulture);
                        pDue.Value = s.DueDate.ToString("o", CultureInfo.InvariantCulture);
                        pActive.Value = s.IsActive ? 1 : 0;
                        pTuitionPerCredit.Value = checked((long)s.TuitionPerCredit);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. Migrate TuitionFees
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO TuitionFees (Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note)
                        VALUES (@id, @svId, @semId, @credits, @tot, @disc, @discReason, @paid, @paidDate, @dueDate, @status, @note);";

                    var pId = cmd.Parameters.Add("@id", SqliteType.Integer);
                    var pSvId = cmd.Parameters.Add("@svId", SqliteType.Integer);
                    var pSemId = cmd.Parameters.Add("@semId", SqliteType.Integer);
                    var pCredits = cmd.Parameters.Add("@credits", SqliteType.Integer);
                    var pTot = cmd.Parameters.Add("@tot", SqliteType.Integer);
                    var pDisc = cmd.Parameters.Add("@disc", SqliteType.Integer);
                    var pDiscReason = cmd.Parameters.Add("@discReason", SqliteType.Text);
                    var pPaid = cmd.Parameters.Add("@paid", SqliteType.Integer);
                    var pPaidDate = cmd.Parameters.Add("@paidDate", SqliteType.Text);
                    var pDueDate = cmd.Parameters.Add("@dueDate", SqliteType.Text);
                    var pStatus = cmd.Parameters.Add("@status", SqliteType.Integer);
                    var pNote = cmd.Parameters.Add("@note", SqliteType.Text);

                    foreach (var f in fees)
                    {
                        pId.Value = f.Id;
                        pSvId.Value = f.StudentId;
                        pSemId.Value = f.SemesterId;
                        pCredits.Value = f.Credits;
                        pTot.Value = ToVnd(f.TotalAmount);
                        pDisc.Value = ToVnd(f.DiscountAmount);
                        pDiscReason.Value = f.DiscountReason ?? "";
                        pPaid.Value = ToVnd(f.PaidAmount);
                        pPaidDate.Value = f.PaidDate.HasValue ? f.PaidDate.Value.ToString("o", CultureInfo.InvariantCulture) : (object)DBNull.Value;
                        pDueDate.Value = f.DueDate.HasValue ? f.DueDate.Value.ToString("o", CultureInfo.InvariantCulture) : (object)DBNull.Value;
                        pStatus.Value = (int)f.Status;
                        pNote.Value = f.Note ?? "";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 4. Migrate PaymentReceipts
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO PaymentReceipts
                            (Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note,
                             StudentNameSnapshot, StudentCodeSnapshot, ClassNameSnapshot, SemesterNameSnapshot,
                             TotalTuitionSnapshot, TotalPaidAfterSnapshot, RemainingAfterSnapshot, DueDateSnapshot)
                        VALUES (@id, @feeId, @svId, @semId, @code, @amount, @date, @method, @payer, @note,
                                @studentName, @studentCode, @className, @semesterName, @total, @paidAfter, @remaining, @dueSnapshot);";

                    var pId = cmd.Parameters.Add("@id", SqliteType.Integer);
                    var pFeeId = cmd.Parameters.Add("@feeId", SqliteType.Integer);
                    var pSvId = cmd.Parameters.Add("@svId", SqliteType.Integer);
                    var pSemId = cmd.Parameters.Add("@semId", SqliteType.Integer);
                    var pCode = cmd.Parameters.Add("@code", SqliteType.Text);
                    var pAmount = cmd.Parameters.Add("@amount", SqliteType.Integer);
                    var pDate = cmd.Parameters.Add("@date", SqliteType.Text);
                    var pMethod = cmd.Parameters.Add("@method", SqliteType.Text);
                    var pPayer = cmd.Parameters.Add("@payer", SqliteType.Text);
                    var pNote = cmd.Parameters.Add("@note", SqliteType.Text);
                    var pStudentName = cmd.Parameters.Add("@studentName", SqliteType.Text);
                    var pStudentCode = cmd.Parameters.Add("@studentCode", SqliteType.Text);
                    var pClassName = cmd.Parameters.Add("@className", SqliteType.Text);
                    var pSemesterName = cmd.Parameters.Add("@semesterName", SqliteType.Text);
                    var pTotalSnapshot = cmd.Parameters.Add("@total", SqliteType.Integer);
                    var pPaidAfter = cmd.Parameters.Add("@paidAfter", SqliteType.Integer);
                    var pRemaining = cmd.Parameters.Add("@remaining", SqliteType.Integer);
                    var pDueSnapshot = cmd.Parameters.Add("@dueSnapshot", SqliteType.Text);

                    foreach (var r in receipts)
                    {
                        pId.Value = r.Id;
                        pFeeId.Value = r.TuitionFeeId;
                        pSvId.Value = r.StudentId;
                        pSemId.Value = r.SemesterId;
                        pCode.Value = r.ReceiptCode ?? "";
                        pAmount.Value = ToVnd(r.Amount);
                        pDate.Value = r.PaymentDate.ToString("o", CultureInfo.InvariantCulture);
                        pMethod.Value = r.PaymentMethod ?? "";
                        pPayer.Value = r.PayerName ?? "";
                        pNote.Value = r.Note ?? "";
                        var fee = feeById[r.TuitionFeeId];
                        var student = studentById[r.StudentId];
                        var semester = semesterById[r.SemesterId];
                        decimal paidAfter = paidAfterByReceiptId[r.Id];
                        bool hasSnapshot = !string.IsNullOrWhiteSpace(r.StudentCodeSnapshot);
                        if (hasSnapshot && (r.TotalPaidAfterSnapshot < r.Amount || r.RemainingAfterSnapshot < 0 ||
                            r.TotalTuitionSnapshot != r.TotalPaidAfterSnapshot + r.RemainingAfterSnapshot))
                            throw new InvalidOperationException($"Biên lai {r.ReceiptCode} có snapshot số tiền không hợp lệ.");
                        pStudentName.Value = hasSnapshot ? r.StudentNameSnapshot : student.FullName;
                        pStudentCode.Value = hasSnapshot ? r.StudentCodeSnapshot : student.StudentCode;
                        pClassName.Value = hasSnapshot ? r.ClassNameSnapshot : student.ClassName;
                        pSemesterName.Value = hasSnapshot ? r.SemesterNameSnapshot : semester.Name;
                        pTotalSnapshot.Value = ToVnd(hasSnapshot ? r.TotalTuitionSnapshot : fee.TotalAmount);
                        pPaidAfter.Value = ToVnd(hasSnapshot ? r.TotalPaidAfterSnapshot : paidAfter);
                        pRemaining.Value = ToVnd(hasSnapshot ? r.RemainingAfterSnapshot : fee.TotalAmount - paidAfter);
                        pDueSnapshot.Value = hasSnapshot
                            ? (object?)r.DueDateSnapshot?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value
                            : (fee.DueDate ?? semester.DueDate).ToString("o", CultureInfo.InvariantCulture);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                return (students.Count, semesters.Count, fees.Count, receipts.Count, "Chuyển đổi dữ liệu sang SQL thành công!");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw new InvalidOperationException($"Lỗi chuyển đổi dữ liệu sang SQL: {ex.Message}", ex);
            }
        }

        public (List<Student> Students, List<Semester> Semesters, List<TuitionFee> Fees, List<PaymentReceipt> Receipts) LoadAllFromSql()
        {
            var students = new List<Student>();
            var semesters = new List<Semester>();
            var fees = new List<TuitionFee>();
            var receipts = new List<PaymentReceipt>();

            using var conn = _db.CreateConnection();

            // 1. Read Students
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, StudentCode, FullName, Email, PhoneNumber, DateOfBirth, ClassName FROM Students ORDER BY Id;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime dob = DateTime.TryParse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : DateTime.MinValue;
                    students.Add(new Student
                    {
                        Id = reader.GetInt32(0),
                        StudentCode = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                        FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        PhoneNumber = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        DateOfBirth = dob,
                        ClassName = reader.IsDBNull(6) ? "" : reader.GetString(6)
                    });
                }
            }

            // 2. Read Semesters
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, StartDate, EndDate, DueDate, IsActive, TuitionPerCredit FROM Semesters ORDER BY Id;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime start = DateTime.TryParse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var s) ? s : DateTime.MinValue;
                    DateTime end = DateTime.TryParse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var e) ? e : DateTime.MinValue;
                    DateTime due = DateTime.TryParse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var du) ? du : DateTime.MinValue;

                    semesters.Add(new Semester
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        StartDate = start,
                        EndDate = end,
                        DueDate = due,
                        IsActive = reader.GetInt32(5) == 1,
                        TuitionPerCredit = reader.GetInt64(6)
                    });
                }
            }

            // 3. Read TuitionFees
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note FROM TuitionFees ORDER BY Id;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime? paidDate = reader.IsDBNull(8) ? null : DateTime.TryParse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var pd) ? pd : null;
                    DateTime? dueDate = reader.IsDBNull(9) ? null : DateTime.TryParse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dd) ? dd : null;

                    fees.Add(new TuitionFee
                    {
                        Id = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        SemesterId = reader.GetInt32(2),
                        Credits = reader.GetInt32(3),
                        TotalAmount = reader.GetInt64(4),
                        DiscountAmount = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                        DiscountReason = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        PaidAmount = reader.GetInt64(7),
                        PaidDate = paidDate,
                        DueDate = dueDate,
                        Status = (PaymentStatus)reader.GetInt32(10),
                        Note = reader.IsDBNull(11) ? "" : reader.GetString(11)
                    });
                }
            }

            // 4. Read Receipts
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note,
                    StudentNameSnapshot, StudentCodeSnapshot, ClassNameSnapshot, SemesterNameSnapshot,
                    TotalTuitionSnapshot, TotalPaidAfterSnapshot, RemainingAfterSnapshot, DueDateSnapshot
                    FROM PaymentReceipts ORDER BY Id;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime pDate = DateTime.TryParse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.Now;

                    receipts.Add(new PaymentReceipt
                    {
                        Id = reader.GetInt32(0),
                        TuitionFeeId = reader.GetInt32(1),
                        StudentId = reader.GetInt32(2),
                        SemesterId = reader.GetInt32(3),
                        ReceiptCode = reader.GetString(4),
                        Amount = reader.GetInt64(5),
                        PaymentDate = pDate,
                        PaymentMethod = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        PayerName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Note = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        StudentNameSnapshot = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        StudentCodeSnapshot = reader.IsDBNull(11) ? "" : reader.GetString(11),
                        ClassNameSnapshot = reader.IsDBNull(12) ? "" : reader.GetString(12),
                        SemesterNameSnapshot = reader.IsDBNull(13) ? "" : reader.GetString(13),
                        TotalTuitionSnapshot = reader.IsDBNull(14) ? 0 : reader.GetInt64(14),
                        TotalPaidAfterSnapshot = reader.IsDBNull(15) ? 0 : reader.GetInt64(15),
                        RemainingAfterSnapshot = reader.IsDBNull(16) ? 0 : reader.GetInt64(16),
                        DueDateSnapshot = reader.IsDBNull(17) ? null : DateTime.TryParse(reader.GetString(17), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var snapshotDue) ? snapshotDue : null
                    });
                }
            }

            return (students, semesters, fees, receipts);
        }

        private static long ToVnd(decimal amount)
        {
            if (amount != decimal.Truncate(amount))
                throw new InvalidOperationException("Dữ liệu tiền VND phải là số nguyên đồng trước khi chuyển đổi.");
            return checked((long)amount);
        }

        private static void ValidateFinancialLedger(IReadOnlyList<TuitionFee> fees, IReadOnlyList<PaymentReceipt> receipts)
        {
            var issues = new List<string>();
            var feeById = fees.ToDictionary(fee => fee.Id);
            foreach (var receipt in receipts)
            {
                if (!feeById.TryGetValue(receipt.TuitionFeeId, out var fee))
                {
                    issues.Add($"Biên lai {receipt.ReceiptCode} tham chiếu phiếu học phí không tồn tại #{receipt.TuitionFeeId}.");
                    continue;
                }
                if (receipt.StudentId != fee.StudentId || receipt.SemesterId != fee.SemesterId)
                    issues.Add($"Biên lai {receipt.ReceiptCode} không khớp sinh viên/học kỳ của phiếu #{fee.Id}.");
            }

            var receiptTotals = receipts.GroupBy(receipt => receipt.TuitionFeeId)
                .ToDictionary(group => group.Key, group => group.Sum(receipt => receipt.Amount));
            foreach (var fee in fees)
            {
                decimal receiptTotal = receiptTotals.GetValueOrDefault(fee.Id);
                if (receiptTotal != fee.PaidAmount)
                    issues.Add($"Phiếu #{fee.Id}: đã thu {fee.PaidAmount:N0} nhưng tổng biên lai là {receiptTotal:N0} VNĐ.");
            }

            if (issues.Count > 0)
                throw new InvalidOperationException("Không thể nhập dữ liệu chưa đối soát:\n- " + string.Join("\n- ", issues.Take(10)) +
                    (issues.Count > 10 ? $"\n- ... và {issues.Count - 10} lỗi khác." : string.Empty));
        }
    }
}
