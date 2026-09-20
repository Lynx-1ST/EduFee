using K26_DotNet.Data;
using K26_DotNet.Models;
using K26_DotNet.Services;
using K26_DotNet.Reports;
using K26_DotNet.RegressionTests;
using _26K1_DotNet;
using System.Reflection;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "EduFee-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(root);
        int passed = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            Console.WriteLine("PASS: " + name);
            passed++;
        }
        bool Throws(Action action)
        {
            try { action(); return false; } catch { return true; }
        }
        try
        {
    var day = new DateTime(2026, 9, 18);
    var fee = new TuitionFee(1, 1, 1, 2, 500_000m, dueDate: day, discountAmount: 100_000m);
    Check(fee.OriginalAmount == 1_000_000m, "Original amount respects custom credit rate");
    fee.UpdateStatus(today: day.AddHours(23));
    Check(fee.Status == PaymentStatus.Unpaid, "Due today is not overdue");
    fee.UpdateStatus(today: day.AddDays(1));
    Check(fee.Status == PaymentStatus.Overdue, "Overdue starts the following day");
    fee.PaidAmount = fee.TotalAmount;
    fee.UpdateStatus(today: day.AddDays(1));
    Check(fee.Status == PaymentStatus.Paid, "Paid takes precedence over overdue");
    fee.PaidDate = day.AddDays(1);
    fee.UpdateStatus(today: day.AddDays(1));
    Check(fee.Status == PaymentStatus.LatePaid, "A full payment after the due date is marked as late paid");

    string atomic = Path.Combine(root, "atomic.json");
    AtomicFile.WriteAllText(atomic, "old");
    AtomicFile.WriteAllText(atomic, "new");
    Check(File.ReadAllText(atomic) == "new" && File.ReadAllText(atomic + ".bak") == "old", "Atomic replacement retains previous contents");
    using (var locked = new FileStream(atomic, FileMode.Open, FileAccess.Read, FileShare.Read))
        Check(Throws(() => AtomicFile.WriteAllText(atomic, "lost")), "Locked destination rejects save");
    Check(File.ReadAllText(atomic) == "new" && Directory.GetFiles(root, "*.tmp").Length == 0, "Failed save preserves original and cleans temporary file");

    string receipts = Path.Combine(root, "receipts.json");
    File.WriteAllText(receipts, "broken json");
    Check(Throws(() => new ReceiptService(receipts)) && File.ReadAllText(receipts) == "broken json", "Corrupt receipts are never replaced with empty history");
    File.WriteAllText(receipts, "[]");
    var receiptService = new ReceiptService(receipts);
    using (var locked = new FileStream(receipts, FileMode.Open, FileAccess.Read, FileShare.Read))
        Check(Throws(() => receiptService.CreateReceipt(1, 1, 1, 100, "cash", "Test")), "Receipt write failure is reported");
    Check(receiptService.GetAll().Count == 0, "Failed receipt save rolls back memory");

    var students = new StudentService(Path.Combine(root, "students.json"));
    Student ValidStudent(int id) => new(id, $"Sinh viên {id}", $"sv{id}@example.com", "0900000000", new DateTime(2005, 1, 1), "K26");
    Check(Throws(() => students.AddStudents([ValidStudent(1), ValidStudent(1)])) && students.GetAllStudents().Count == 0,
        "Duplicate batch is rejected without partial import");
    students.AddStudents([ValidStudent(1), ValidStudent(2)]);
    Check(new StudentService(Path.Combine(root, "students.json")).GetAllStudents().Count == 2, "Student batch persists together");
    Check(Throws(() => students.AddStudent(new Student(3, "   ", "Thiếu mã", "", "", new DateTime(2005, 1, 1), "K26"))),
        "StudentCode is required instead of being inferred from the internal ID");
    students.AddStudent(new Student(3, "  2121050003  ", "Sinh viên 3", "", "", new DateTime(2005, 1, 1), "K26"));
    Check(students.GetStudentById(3)?.StudentCode == "2121050003" &&
          Throws(() => students.AddStudent(new Student(4, "2121050003", "Trùng mã", "", "", new DateTime(2005, 1, 1), "K26"))),
        "StudentCode is trimmed and unique regardless of letter case");

    string csv = Path.Combine(root, "students.csv");
    File.WriteAllText(csv, "MaSV,HoTen,Lop,NgaySinh,DienThoai,Email\n00123,\"Nguyễn, An\",K1,15/08/2004,0912345678,a@example.com\nSV-2,B,K1,invalid,,\nSV-3,too few\n");
    var rows = StudentCsvImporter.Read(csv, Array.Empty<string>(), 100);
    Check(rows.Count == 3 && rows[0].IsValid && rows[0].Id == 100 && rows[0].StudentCode == "00123" && rows[0].FullName == "Nguyễn, An",
        "CSV preserves StudentCode text and assigns an independent internal ID");
    Check(!rows[1].IsValid && rows[1].Email == "" && rows[1].PhoneNumber == "", "Missing details are rejected, never fabricated");
    Check(!rows[2].IsValid, "Short CSV rows are shown as errors");
    File.WriteAllText(csv, "MaSV;HoTen;Lop;NgaySinh;DienThoai;Email\nSV-4;\"An\nBình\";K1;2004-08-15;0912345678;a@example.com\n");
    rows = StudentCsvImporter.Read(csv, Array.Empty<string>(), 200);
    Check(rows.Count == 1 && rows[0].IsValid && rows[0].FullName.Contains('\n'), "Semicolon CSV and multiline quoted fields parse");
    File.WriteAllText(csv, "wrong,header\n1,A\n");
    Check(Throws(() => StudentCsvImporter.Read(csv, Array.Empty<string>(), 1)), "Wrong CSV headers are rejected");
    var roundTripStudent = new Student(17, "2121050123", "Nguyễn Văn A", "a@example.com", "0912345678", new DateTime(2004, 8, 15), "K26");
    CsvWriter.WriteCsv([roundTripStudent],
        [
            ("MaSV", s => (object)s.StudentCode), ("HoTen", s => s.FullName), ("Lop", s => s.ClassName),
            ("NgaySinh", s => s.DateOfBirth.ToString("dd/MM/yyyy")), ("DienThoai", s => s.PhoneNumber), ("Email", s => s.Email)
        ], csv);
    rows = StudentCsvImporter.Read(csv, Array.Empty<string>(), 900);
    Check(rows.Single().StudentCode == "2121050123" && rows.Single().Id == 900,
        "Student CSV round-trip keeps StudentCode separate from the internal ID");
    CsvWriter.WriteCsv(new[] { "=1+1", "  @SUM(A1)", "Nguyễn, An" }, [("Name", x => x)], csv);
    var exported = File.ReadAllText(csv);
    Check(exported.Contains("\"'=1+1\"") && exported.Contains("\"'  @SUM(A1)\"") && exported.Contains("\"Nguyễn, An\""), "CSV exports neutralize formulas and retain Unicode");

    string feesPath = Path.Combine(root, "fees.json");
    var tuition = new TuitionService(feesPath);
    tuition.Add(new TuitionFee(0, 1, 1, 1));
    var savedFee = tuition.GetAll()[0];
    using (var locked = new FileStream(feesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        Check(Throws(() => tuition.RecordPayment(savedFee.Id, 100)), "Payment write failure is reported");
    Check(tuition.GetAll()[0].PaidAmount == 0 && tuition.GetAll()[0].PaidDate == null, "Failed payment save rolls back memory");
    tuition.RefreshStatuses([new Semester(1, "Test", day, day.AddMonths(4), DateTime.Today.AddDays(-1))]);
    Check(tuition.GetAll()[0].Status == PaymentStatus.Overdue, "Status refresh uses semester due date fallback");

    string semesters = Path.Combine(root, "semesters.json");
    File.WriteAllText(semesters, "[]");
    Check(new SemesterService(semesters).GetAll().Count == 0, "An intentionally empty semester list stays empty");
    File.WriteAllText(feesPath, "");
    Check(Throws(() => new TuitionService(feesPath)), "Empty data files report corruption");
    string settingsPath = Path.Combine(root, "email-settings.json");
    var email = new EmailService(settingsPath);
    email.Settings.SenderPassword = "test-only-password-123";
    email.SaveSettings();
    Check(!File.ReadAllText(settingsPath).Contains("test-only-password-123"), "SMTP password is encrypted on disk");
    Check(new EmailService(settingsPath).Settings.SenderPassword == "test-only-password-123", "Encrypted SMTP password round trips for current Windows user");
    File.WriteAllText(settingsPath, "{\"SenderPassword\":\"legacy-test-password\"}");
    email = new EmailService(settingsPath);
    email.SaveSettings();
    Check(new EmailService(settingsPath).Settings.SenderPassword == "legacy-test-password" && File.ReadAllText(settingsPath).Contains("dpapi:"), "Legacy email settings upgrade on save");
            VerifyStatisticsSemesterRefresh(root, Check);
            VerifyTuitionPanelFilters(root, Check);
            VerifyPanelRendering(root, Check);
            VerifySqlitePersistence(root, Check, Throws);
            VerifyPdfExports(root, Check);
            FinanceAcceptance.Run(root, Check);
            PdfAcceptance.Run(root, Check);
            StartupAcceptance.Run(root, Check);
            DatabaseAcceptance.Run(root, Check);
            MigrationAcceptance.Run(root, Check);
            ReportAcceptance.Run(root, Check);
            QrPaymentAcceptance.Run(Check);

            Console.WriteLine($"All {passed} regression checks passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private static void VerifyTuitionPanelFilters(string root, Action<bool, string> check)
    {
        var uiRoot = Path.Combine(root, "tuition-panel");
        Directory.CreateDirectory(uiRoot);
        var students = new StudentService(Path.Combine(uiRoot, "students.json"));
        students.AddStudents([
            new Student(101, "CODE-A", "Nguyễn Trùng Tên", "one@example.com", "0900000001", new DateTime(2004, 1, 1), "K26A"),
            new Student(202, "CODE-B", "Nguyễn Trùng Tên", "two@example.com", "0900000002", new DateTime(2004, 1, 2), "K26B")
        ]);

        var semesters = new SemesterService(Path.Combine(uiRoot, "semesters.json"));
        semesters.Add(new Semester(0, "Học kỳ cũ", new DateTime(2025, 1, 1), new DateTime(2025, 5, 1), new DateTime(2025, 2, 1)));
        semesters.Add(new Semester(0, "Học kỳ hiện tại", new DateTime(2026, 1, 1), new DateTime(2026, 5, 1), new DateTime(2026, 2, 1), true));
        var tuition = new TuitionService(Path.Combine(uiRoot, "tuition.json"));
        var receiptsPath = Path.Combine(uiRoot, "receipts.json");
        File.WriteAllText(receiptsPath, "[]");

        using var mainForm = new Form1();
        using var host = new Form { ClientSize = new Size(1080, 720), StartPosition = FormStartPosition.Manual };
        using var panel = new PanelTuition(students, semesters, tuition, new ReceiptService(receiptsPath), mainForm) { Dock = DockStyle.Fill };
        host.Controls.Add(panel);
        host.CreateControl();
        panel.CreateControl();
        Application.DoEvents();
        tuition.Add(new TuitionFee(0, 101, semesters.GetAll().Single(s => s.Name == "Học kỳ cũ").Id, 1));
        tuition.Add(new TuitionFee(0, 202, semesters.GetAll().Single(s => s.Name == "Học kỳ cũ").Id, 1));
        tuition.Add(new TuitionFee(0, 101, semesters.GetActive()!.Id, 1));
        tuition.Add(new TuitionFee(0, 202, semesters.GetActive()!.Id, 1));
        panel.RefreshData();

        var semesterCombo = GetPrivateField<ComboBox>(panel, "cmbSem");
        var grid = GetPrivateField<DataGridView>(panel, "dgv");
        panel.FilterByStudent(101);
        var displayedStudentCodes = grid.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => Convert.ToString(row.Cells["MaSV"].Value))
            .ToArray();
        check(displayedStudentCodes.SequenceEqual(["CODE-A"]),
            "Tuition filters by internal relation while displaying the selected StudentCode when names collide");

        var oldSemester = semesters.GetAll().Single(s => s.Name == "Học kỳ cũ");
        panel.RefreshData(oldSemester.Id);
        check(semesterCombo.SelectedItem?.ToString() == "Học kỳ cũ", "Tuition follows the semester selected from the global badge");

        var activeSemester = semesters.GetActive()!;
        panel.RefreshData(activeSemester.Id);
        check(semesterCombo.SelectedItem?.ToString() == activeSemester.Name, "Tuition returns to the active global semester");
    }

    private static void VerifySqlitePersistence(string root, Action<bool, string> check, Func<Action, bool> throws)
    {
        var path = Path.Combine(root, "runtime-sqlite.db");
        var database = new SqlDatabaseContext(path);
        var repository = new SqliteRepository(database);
        repository.AddStudents([new Student(101, "2121050101", "Nguyễn Văn An", "an@example.com", "0901", new DateTime(2005, 1, 2), "K26")]);
        repository.AddSemester(new Semester(201, "HK kiểm thử", new DateTime(2026, 9, 1), new DateTime(2027, 1, 15), new DateTime(2026, 9, 30), true));
        repository.AddTuitionFees([new TuitionFee(301, 101, 201, 2, 500_000m, dueDate: new DateTime(2026, 9, 30))]);

        var before = repository.LoadAll();
        check(before.Students.Count == 1 && before.Semesters.Count == 1 && before.Fees.Single().TotalAmount == 1_000_000m,
            "SQLite persists student, semester and integer VND tuition");

        var receipt = repository.RecordPayment(301, 400_000m, "Tiền mặt", "Nguyễn Văn An", "Lần 1", new DateTime(2026, 9, 30));
        var after = repository.LoadAll();
        check(after.Fees.Single().PaidAmount == 400_000m && after.Receipts.Single().Id == receipt.Id && after.Receipts.Single().Amount == 400_000m,
            "SQLite commits payment and receipt together");
        check(after.Receipts.Single().StudentNameSnapshot == "Nguyễn Văn An" &&
              after.Receipts.Single().StudentCodeSnapshot == "2121050101" &&
              after.Receipts.Single().TotalPaidAfterSnapshot == 400_000m && after.Receipts.Single().RemainingAfterSnapshot == 600_000m,
            "Receipt stores the actual student code and balance snapshot at payment time");

        repository.UpdateStudent(new Student(101, "2121050199", "Tên đã thay đổi", "new@example.com", "0909", new DateTime(2005, 1, 2), "K27"));
        var sqliteTuition = new TuitionService(database);
        var tuitionDraft = sqliteTuition.GetById(301) ?? throw new InvalidOperationException("Missing tuition fixture.");
        tuitionDraft.TotalAmount = 1_500_000m;
        check(sqliteTuition.GetById(301)?.TotalAmount == 1_000_000m,
            "Tuition query returns a safe copy instead of mutable service state");
        sqliteTuition.Update(tuitionDraft);
        var unchangedReceipt = repository.LoadAll().Receipts.Single();
        check(unchangedReceipt.StudentNameSnapshot == "Nguyễn Văn An" &&
              unchangedReceipt.StudentCodeSnapshot == "2121050101" &&
              unchangedReceipt.TotalTuitionSnapshot == 1_000_000m,
            "Historical receipt snapshot is unchanged after student code, profile and tuition edits");

        check(throws(() => repository.AddStudents([
                new Student(102, "abc-01", "Mã thứ nhất", "", "", new DateTime(2005, 1, 1), "K26"),
                new Student(103, "ABC-01", "Mã trùng", "", "", new DateTime(2005, 1, 1), "K26")
            ])),
            "SQLite rejects duplicate StudentCode case-insensitively");

        check(throws(() => repository.RecordPayment(301, 1_200_000m, "Tiền mặt", "Nguyễn Văn An", "Vượt nợ")),
            "SQLite rejects payment above remaining balance");
        var afterRejectedPayment = repository.LoadAll();
        check(afterRejectedPayment.Fees.Single().PaidAmount == 400_000m && afterRejectedPayment.Receipts.Count == 1,
            "Rejected payment leaves balance and receipt ledger unchanged");
        check(throws(() => repository.DeleteStudent(101)) && throws(() => repository.DeleteTuitionFee(301)),
            "SQLite restricts deletion of financial history");

        string backupPath = Path.Combine(root, "runtime-backup.db");
        database.BackupTo(backupPath);
        repository.AddStudents([new Student(102, "Sinh viên tạm", "temp@example.com", "0902", new DateTime(2005, 2, 1), "K26")]);
        string safetyBackup = database.RestoreFrom(backupPath);
        var restored = repository.LoadAll();
        check(restored.Students.Count == 1 && restored.Students.Single().Id == 101 && File.Exists(safetyBackup),
            "SQLite backup and verified restore recover the previous data");

        string invalidBackup = Path.Combine(root, "invalid-schema.db");
        using (var invalid = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={invalidBackup}"))
        {
            invalid.Open();
            using var command = invalid.CreateCommand();
            command.CommandText = "CREATE TABLE Students(Id INTEGER); CREATE TABLE Semesters(Id INTEGER); CREATE TABLE TuitionFees(Id INTEGER); CREATE TABLE PaymentReceipts(Id INTEGER); PRAGMA user_version=2;";
            command.ExecuteNonQuery();
        }
        check(throws(() => database.RestoreFrom(invalidBackup)) && repository.LoadAll().Students.Count == 1,
            "Restore rejects a database that only imitates EduFee table names");

        using var connection = database.CreateConnection();
        using (var untrimmedCode = connection.CreateCommand())
        {
            untrimmedCode.CommandText = @"
                INSERT INTO Students(Id, StudentCode, FullName, DateOfBirth, ClassName)
                VALUES(999, 'TRAILING-CODE ', 'Mã có khoảng trắng', '2005-01-01', 'K26');";
            check(throws(() => untrimmedCode.ExecuteNonQuery()),
                "SQLite rejects an untrimmed StudentCode at the storage boundary");
        }
        using var fk = connection.CreateCommand();
        fk.CommandText = "PRAGMA foreign_keys;";
        check(Convert.ToInt32(fk.ExecuteScalar()) == 1, "SQLite enables foreign keys on every connection");

        string badRoot = Path.Combine(root, "bad-ledger");
        Directory.CreateDirectory(badRoot);
        var badStudents = new StudentService(Path.Combine(badRoot, "students.json"));
        badStudents.AddStudent(new Student(1, "Sinh viên lệch sổ", "bad@example.com", "0900", new DateTime(2005, 1, 1), "K26"));
        var badSemesters = new SemesterService(Path.Combine(badRoot, "semesters.json"));
        var activeSemester = badSemesters.GetActive() ?? throw new InvalidOperationException("Missing seeded semester.");
        var badFees = new TuitionService(Path.Combine(badRoot, "fees.json"));
        badFees.Add(new TuitionFee(0, 1, activeSemester.Id, 1));
        badFees.RecordPayment(badFees.GetAll().Single().Id, 100_000m, activeSemester.DueDate);
        var badReceipts = new ReceiptService(Path.Combine(badRoot, "receipts.json"));
        var badDatabase = new SqlDatabaseContext(Path.Combine(badRoot, "database.db"));
        check(throws(() => new SqlDataMigrator(badDatabase).MigrateFromJson(badStudents, badSemesters, badFees, badReceipts)) &&
              badDatabase.GetRecordCounts() == (0, 0, 0, 0),
            "JSON migration rejects an unreconciled payment ledger without partial writes");
    }

    private static void VerifyPdfExports(string root, Action<bool, string> check)
    {
        string outputDirectory = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR") ?? Path.Combine(root, "pdf");
        Directory.CreateDirectory(outputDirectory);
        string receiptPath = Path.Combine(outputDirectory, "bien-lai-mau.pdf");
        ReceiptPdfRenderer.Export(receiptPath, new ReceiptPdfData(
            "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT", "BL-2026-0001", new DateTime(2026, 9, 19, 9, 30, 0),
            "Nguyễn Thị Ánh", "SV0101", "K26-CNTT", "HK1 2026-2027", "Trần Văn Bình",
            "Chuyển khoản ngân hàng", 1_200_000m, 3_000_000m, 1_200_000m, 1_800_000m,
            new DateTime(2026, 9, 30), "Thanh toán học phí lần 1"));

        var rows = Enumerable.Range(1, 80).Select(index => new DebtReportRow(
            $"SV{index:D4}", $"Sinh viên Nguyễn Văn {index}", $"K26-{(index % 4) + 1}",
            3_000_000m, index % 3 * 500_000m, 3_000_000m - index % 3 * 500_000m,
            new DateTime(2026, 9, 30), index % 2 == 0 ? "Quá hạn" : "Nộp 1 phần")).ToList();
        string debtPath = Path.Combine(outputDirectory, "bao-cao-cong-no-mau.pdf");
        DebtReportPdfRenderer.Export(debtPath, new DebtReportPdfData(
            "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT", "HK1 2026-2027", "Sinh viên còn nợ",
            new DateTime(2026, 9, 19, 10, 0, 0), rows));

        bool IsPdf(string path) => File.Exists(path) && new FileInfo(path).Length > 1_000 &&
            File.ReadAllBytes(path).Take(5).SequenceEqual("%PDF-"u8.ToArray());
        check(IsPdf(receiptPath), "Receipt PDF is generated without a printer driver");
        check(IsPdf(debtPath), "Multi-page debt report PDF is generated without a printer driver");

        string fullLastPagePath = Path.Combine(outputDirectory, "bao-cao-25-dong.pdf");
        DebtReportPdfRenderer.Export(fullLastPagePath, new DebtReportPdfData(
            "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT", "HK1 2026-2027", "Kiểm tra phân trang",
            new DateTime(2026, 9, 19), rows.Take(25).ToList()));
        string pdfAscii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(fullLastPagePath));
        check(IsPdf(fullLastPagePath) && pdfAscii.Split("/Type /Page ", StringSplitOptions.None).Length - 1 == 2,
            "Debt report reserves a separate total row when the last page is full");
    }

    private static void VerifyStatisticsSemesterRefresh(string root, Action<bool, string> check)
    {
        var uiRoot = Path.Combine(root, "statistics-semester");
        Directory.CreateDirectory(uiRoot);
        var students = new StudentService(Path.Combine(uiRoot, "students.json"));
        students.AddStudent(new Student(1, "Kiểm thử", "test@example.com", "0900000000", new DateTime(2004, 1, 1), "K26"));
        var semestersPath = Path.Combine(uiRoot, "semesters.json");
        File.WriteAllText(semestersPath, "[]");
        var semesters = new SemesterService(semestersPath);
        semesters.Add(new Semester(0, "Học kỳ cũ", new DateTime(2025, 1, 1), new DateTime(2025, 5, 1), new DateTime(2025, 2, 1)));
        semesters.Add(new Semester(0, "Học kỳ hiện tại", new DateTime(2026, 1, 1), new DateTime(2026, 5, 1), new DateTime(2026, 2, 1), true));
        var tuitionPath = Path.Combine(uiRoot, "tuition.json");
        File.WriteAllText(tuitionPath, "[]");
        var tuition = new TuitionService(tuitionPath);
        var receiptsPath = Path.Combine(uiRoot, "receipts.json");
        File.WriteAllText(receiptsPath, "[]");

        using var mainForm = new Form1();
        using var panel = new PanelStatistics(students, semesters, tuition, new ReceiptService(receiptsPath), mainForm);
        var semesterCombo = GetPrivateField<ComboBox>(panel, "cmbSem");
        SelectSemester(semesterCombo, "Học kỳ cũ");
        panel.RefreshData();
        check(semesterCombo.SelectedItem?.ToString() == "Học kỳ cũ", "Statistics refresh preserves a selected older semester");

        SelectSemester(semesterCombo, "Học kỳ hiện tại");
        panel.RefreshData();
        check(semesterCombo.SelectedItem?.ToString() == "Học kỳ hiện tại", "Statistics refresh preserves the current semester selection");
    }

    private static void VerifyPanelRendering(string root, Action<bool, string> check)
    {
        var uiRoot = Path.Combine(root, "render-fixtures");
        Directory.CreateDirectory(uiRoot);
        var students = new StudentService(Path.Combine(uiRoot, "students.json"));
        students.AddStudent(new Student(1, "Nguyễn Văn A", "a@example.com", "0900000001", new DateTime(2004, 1, 1), "K26A"));
        var semesters = new SemesterService(Path.Combine(uiRoot, "semesters.json"));
        semesters.Add(new Semester(0, "HK kiểm thử", new DateTime(2026, 1, 1), new DateTime(2026, 5, 1), new DateTime(2026, 2, 1), true));
        var tuition = new TuitionService(Path.Combine(uiRoot, "tuition.json"));
        var receiptsPath = Path.Combine(uiRoot, "receipts.json");
        File.WriteAllText(receiptsPath, "[]");
        var receipts = new ReceiptService(receiptsPath);

        using var mainForm = new Form1();
        RenderPanel(new PanelTuition(students, semesters, tuition, receipts, mainForm), "tuition", root, check, () =>
        {
            tuition.Add(new TuitionFee(0, 1, semesters.GetActive()?.Id ?? throw new InvalidOperationException("Test semester is missing."), 3));
        });
        RenderPanel(new PanelStudents(students, mainForm), "students", root, check);
        RenderPanel(new PanelStatistics(students, semesters, tuition, receipts, mainForm), "statistics", root, check);
    }

    private static void RenderPanel(UserControl panel, string panelName, string root, Action<bool, string> check, Action? afterAttached = null)
    {
        using var host = new Form { ClientSize = new Size(900, 720), StartPosition = FormStartPosition.Manual };
        using (panel)
        {
            panel.Dock = DockStyle.Fill;
            host.Controls.Add(panel);
            host.CreateControl();
            panel.CreateControl();
            Application.DoEvents();
            afterAttached?.Invoke();
            if (panel is PanelTuition tuitionPanel) tuitionPanel.RefreshData();

            foreach (var width in new[] { 900, 1080 })
            {
                host.ClientSize = new Size(width, 720);
                host.PerformLayout();
                Application.DoEvents();
                using var bitmap = new Bitmap(width, host.ClientSize.Height);
                panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                var screenshot = Path.Combine(root, $"{panelName}-{width}.png");
                bitmap.Save(screenshot);
                string? artifactDirectory = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
                if (!string.IsNullOrWhiteSpace(artifactDirectory))
                {
                    Directory.CreateDirectory(artifactDirectory);
                    bitmap.Save(Path.Combine(artifactDirectory, $"{panelName}-{width}.png"));
                }
                check(new FileInfo(screenshot).Length > 0, $"{panelName} renders at {width}px content width");

                var clippedButtons = Descendants(panel)
                    .OfType<Button>()
                    .Where(button => button.Visible && !button.IsDisposed)
                    .Where(button => GetBoundsRelativeTo(button, host).Left < 0 || GetBoundsRelativeTo(button, host).Right > host.ClientSize.Width)
                    .Select(button => button.Text)
                    .ToArray();
                check(clippedButtons.Length == 0, $"{panelName} keeps visible actions inside {width}px content width");
            }
        }
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static Rectangle GetBoundsRelativeTo(Control control, Control ancestor)
    {
        var bounds = control.Bounds;
        for (var parent = control.Parent; parent != null && parent != ancestor; parent = parent.Parent)
            bounds.Offset(parent.Location);
        return bounds;
    }

    private static T GetPrivateField<T>(object instance, string name) where T : class
    {
        return (instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) as T)
            ?? throw new InvalidOperationException($"Could not find {name} on {instance.GetType().Name}.");
    }

    private static void SelectSemester(ComboBox comboBox, string name)
    {
        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i]?.ToString() == name)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }

        throw new InvalidOperationException($"Semester '{name}' was not available.");
    }
}
