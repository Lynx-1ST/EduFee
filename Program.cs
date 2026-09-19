namespace _26K1_DotNet
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[]? args)
        {
            if (args != null && args.Length > 0 && args[0] == "--test")
            {
                RunSelfTest();
                return;
            }

            using var instanceMutex = new Mutex(true, "Local\\EduFee-Desktop-SingleInstance", out bool isFirstInstance);
            if (!isFirstInstance)
            {
                MessageBox.Show("EduFee đang được mở. Vui lòng sử dụng cửa sổ hiện tại để tránh ghi dữ liệu đồng thời.",
                    "EduFee", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (args != null && args.Length > 0 && args[0] == "--migrate")
                {
                    RunMigrate();
                    return;
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
            finally
            {
                instanceMutex.ReleaseMutex();
            }
        }

        static void RunMigrate()
        {
            var dbContext = new K26_DotNet.Data.SqlDatabaseContext("edufee.db");
            var studentSvc = new K26_DotNet.Services.StudentService("students.json");
            var semSvc = new K26_DotNet.Services.SemesterService("semesters.json");
            var tuitionSvc = new K26_DotNet.Services.TuitionService("tuitionfees.json");
            var receiptSvc = new K26_DotNet.Services.ReceiptService("receipts.json");

            var migrator = new K26_DotNet.Data.SqlDataMigrator(dbContext);
            var (sCount, semCount, feeCount, recCount, msg) = migrator.MigrateFromJson(studentSvc, semSvc, tuitionSvc, receiptSvc);
            Console.WriteLine($"[Migrate] Đã chuyển đổi thành công sang edufee.db: {sCount} SV, {semCount} HK, {feeCount} phiếu HP, {recCount} biên lai.");
        }

        static void RunSelfTest()
        {
            var previousDirectory = Environment.CurrentDirectory;
            var testDirectory = Path.Combine(Path.GetTempPath(), "EduFee-selftest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            try
            {
                Environment.CurrentDirectory = testDirectory;
                RunIsolatedSelfTest();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Environment.ExitCode = 1;
            }
            finally
            {
                Environment.CurrentDirectory = previousDirectory;
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        static void RunIsolatedSelfTest()
        {
            Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ HỆ THỐNG EMAIL & SQL ===");

            // 1. Kiểm thử SQLite Context
            var dbContext = new K26_DotNet.Data.SqlDatabaseContext("test_edufee.db");
            Console.WriteLine($"[1] Database SQLite: {dbContext.DbPath} - Đã tạo bảng thành công!");

            // 2. Kiểm thử Migration từ JSON sang SQL
            var studentSvc = new K26_DotNet.Services.StudentService();
            var semSvc = new K26_DotNet.Services.SemesterService();
            var tuitionSvc = new K26_DotNet.Services.TuitionService();
            var receiptSvc = new K26_DotNet.Services.ReceiptService();

            var migrator = new K26_DotNet.Data.SqlDataMigrator(dbContext);
            var (sCount, semCount, feeCount, recCount, msg) = migrator.MigrateFromJson(studentSvc, semSvc, tuitionSvc, receiptSvc);
            Console.WriteLine($"[2] Migrate JSON -> SQL: {msg} (SV: {sCount}, HK: {semCount}, Học phí: {feeCount}, Biên lai: {recCount})");

            // 3. Kiểm thử Đọc từ SQL
            var (sqlStudents, sqlSemesters, sqlFees, sqlReceipts) = migrator.LoadAllFromSql();
            if (sqlStudents.Count != sCount || sqlSemesters.Count != semCount || sqlFees.Count != feeCount || sqlReceipts.Count != recCount)
                throw new InvalidOperationException("SQL migration counts do not match.");
            Console.WriteLine($"[3] Load SQL: Đọc thành công {sqlStudents.Count} SV, {sqlSemesters.Count} HK, {sqlFees.Count} học phí, {sqlReceipts.Count} biên lai");

            // 4. Kiểm thử Email Service (Simulation Mode)
            var emailSvc = new K26_DotNet.Services.EmailService();
            emailSvc.Settings.IsSimulationMode = true;

            var testStudent = sqlStudents.Count > 0 ? sqlStudents[0] : new K26_DotNet.Models.Student { FullName = "Test SV", Email = "test@edu.vn" };
            var testSemester = sqlSemesters.Count > 0 ? sqlSemesters[0] : new K26_DotNet.Models.Semester(1, "HK1 2025-2026", DateTime.Now, DateTime.Now.AddMonths(4), DateTime.Now.AddDays(30));
            var testReceipt = new K26_DotNet.Models.PaymentReceipt(9999, "BL-TEST-0001", 1, testStudent.Id, 1, 3500000m, "Chuyển khoản (VietQR)", testStudent.FullName, "Test email confirmation");
            var testFee = sqlFees.Count > 0 ? sqlFees[0] : new K26_DotNet.Models.TuitionFee { TotalAmount = 3500000m, PaidAmount = 3500000m };

            var emailTask = emailSvc.SendReceiptEmailAsync(testStudent, testSemester, testFee, testReceipt);
            emailTask.Wait();
            var (emailSuccess, emailMsg) = emailTask.Result;
            if (!emailSuccess) throw new InvalidOperationException(emailMsg);
            Console.WriteLine($"[4] Gửi Email Biên Lai (Simulation): Thành công = {emailSuccess}, Thông báo: {emailMsg}");

            // Kiểm tra log file
            if (File.Exists("simulated_emails.log"))
            {
                var logContent = File.ReadAllText("simulated_emails.log");
                bool hasReceipt = logContent.Contains("BL-TEST-0001");
                if (!hasReceipt) throw new InvalidOperationException("Receipt missing from simulation log.");
                Console.WriteLine($"[5] Kiểm tra file simulated_emails.log: {(hasReceipt ? "Đã ghi nhận biên lai chính xác!" : "Chưa tìm thấy biên lai trong log")}");
            }
            else throw new InvalidOperationException("Simulation log was not created.");

            Console.WriteLine("=== HOÀN TẤT TẤT CẢ KIỂM THỬ THÀNH CÔNG (100% PASS) ===");
        }
    }
}
