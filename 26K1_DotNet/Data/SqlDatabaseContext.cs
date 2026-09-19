using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace K26_DotNet.Data
{
    public class SqlDatabaseContext
    {
        private const int CurrentSchemaVersion = 2;
        private static readonly string[] ApplicationTables =
        {
            "Students", "Semesters", "TuitionFees", "PaymentReceipts"
        };

        private readonly string _connectionString;
        public string DbPath { get; }

        public SqlDatabaseContext(string dbFileName = "edufee.db")
        {
            DbPath = Path.GetFullPath(dbFileName);
            _connectionString = new SqliteConnectionStringBuilder { DataSource = DbPath }.ToString();
            InitializeDatabase();
        }

        public SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
            return conn;
        }

        public void InitializeDatabase()
        {
            using var conn = CreateConnection();
            var version = GetSchemaVersion(conn);

            if (version > CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Cơ sở dữ liệu '{DbPath}' dùng schema phiên bản {version}, mới hơn phiên bản ứng dụng hỗ trợ ({CurrentSchemaVersion}). Hãy dùng ứng dụng mới hơn hoặc khôi phục bản sao lưu tương thích.");
            }

            if (version == CurrentSchemaVersion)
            {
                EnsureExpectedTables(conn);
                return;
            }

            if (version == 1)
            {
                MigrateV1ToV2(conn);
                return;
            }

            var existingTables = GetExistingApplicationTables(conn);
            if (existingTables.Count == 0)
            {
                CreateCurrentSchema(conn);
                return;
            }

            if (existingTables.Count == ApplicationTables.Length)
            {
                MigrateLegacySchema(conn);
                return;
            }

            throw new InvalidOperationException(
                $"Cơ sở dữ liệu '{DbPath}' có schema cũ hoặc không đầy đủ ({string.Join(", ", existingTables)}). Dữ liệu không bị thay đổi. Hãy sao lưu file rồi khôi phục đủ bốn bảng Students, Semesters, TuitionFees, PaymentReceipts hoặc tạo lại cơ sở dữ liệu và chạy chức năng chuyển đổi JSON sang SQL.");
        }

        private static long GetSchemaVersion(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version;";
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private static List<string> GetExistingApplicationTables(SqliteConnection conn)
        {
            var tables = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT name
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('Students', 'Semesters', 'TuitionFees', 'PaymentReceipts');";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        private static void EnsureExpectedTables(SqliteConnection conn)
        {
            var existingTables = GetExistingApplicationTables(conn);
            if (existingTables.Count != ApplicationTables.Length)
            {
                throw new InvalidOperationException(
                    $"Cơ sở dữ liệu '{conn.DataSource}' khai báo schema phiên bản {CurrentSchemaVersion} nhưng thiếu bảng ứng dụng. Dữ liệu không bị thay đổi; hãy khôi phục từ bản sao lưu.");
            }
        }

        private static void CreateCurrentSchema(SqliteConnection conn)
        {
            using var tx = conn.BeginTransaction();
            try
            {
                ExecuteNonQuery(conn, tx, CreateTablesSql("Students", "Semesters", "TuitionFees", "PaymentReceipts"));
                SetSchemaVersion(conn, tx, CurrentSchemaVersion);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void MigrateLegacySchema(SqliteConnection conn)
        {
            using var tx = conn.BeginTransaction();
            try
            {
                const string students = "Students_v1";
                const string semesters = "Semesters_v1";
                const string fees = "TuitionFees_v1";
                const string receipts = "PaymentReceipts_v1";

                ExecuteNonQuery(conn, tx, CreateTablesSql(students, semesters, fees, receipts));
                ExecuteNonQuery(conn, tx, @"
                    INSERT INTO Students_v1 (Id, FullName, Email, PhoneNumber, DateOfBirth, ClassName)
                    SELECT Id, FullName, Email, PhoneNumber, DateOfBirth, ClassName FROM Students;

                    INSERT INTO Semesters_v1 (Id, Name, StartDate, EndDate, DueDate, IsActive)
                    SELECT Id, Name, StartDate, EndDate, DueDate, IsActive FROM Semesters;

                    INSERT INTO TuitionFees_v1 (Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note)
                    SELECT Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note FROM TuitionFees;

                    INSERT INTO PaymentReceipts_v1 (Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note)
                    SELECT Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note FROM PaymentReceipts;

                    DROP TABLE PaymentReceipts;
                    DROP TABLE TuitionFees;
                    DROP TABLE Semesters;
                    DROP TABLE Students;

                    ALTER TABLE Students_v1 RENAME TO Students;
                    ALTER TABLE Semesters_v1 RENAME TO Semesters;
                    ALTER TABLE TuitionFees_v1 RENAME TO TuitionFees;
                    ALTER TABLE PaymentReceipts_v1 RENAME TO PaymentReceipts;");

                SetSchemaVersion(conn, tx, CurrentSchemaVersion);
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw new InvalidOperationException(
                    $"Không thể nâng cấp an toàn cơ sở dữ liệu '{conn.DataSource}' sang schema phiên bản {CurrentSchemaVersion}. Dữ liệu gốc không bị thay đổi. Hãy sao lưu file, sửa dữ liệu không hợp lệ (khóa ngoại, số tiền VND nguyên, mã biên lai trùng hoặc học phí trùng sinh viên/học kỳ), rồi mở lại ứng dụng.", ex);
            }
        }

        private static void MigrateV1ToV2(SqliteConnection conn)
        {
            using var tx = conn.BeginTransaction();
            try
            {
                ExecuteNonQuery(conn, tx, @"
                    ALTER TABLE PaymentReceipts ADD COLUMN StudentNameSnapshot TEXT NOT NULL DEFAULT '';
                    ALTER TABLE PaymentReceipts ADD COLUMN StudentCodeSnapshot TEXT NOT NULL DEFAULT '';
                    ALTER TABLE PaymentReceipts ADD COLUMN ClassNameSnapshot TEXT NOT NULL DEFAULT '';
                    ALTER TABLE PaymentReceipts ADD COLUMN SemesterNameSnapshot TEXT NOT NULL DEFAULT '';
                    ALTER TABLE PaymentReceipts ADD COLUMN TotalTuitionSnapshot INTEGER NOT NULL DEFAULT 0;
                    ALTER TABLE PaymentReceipts ADD COLUMN TotalPaidAfterSnapshot INTEGER NOT NULL DEFAULT 0;
                    ALTER TABLE PaymentReceipts ADD COLUMN RemainingAfterSnapshot INTEGER NOT NULL DEFAULT 0;
                    ALTER TABLE PaymentReceipts ADD COLUMN DueDateSnapshot TEXT;

                    UPDATE PaymentReceipts
                    SET StudentNameSnapshot = COALESCE((SELECT FullName FROM Students WHERE Id=PaymentReceipts.StudentId), ''),
                        StudentCodeSnapshot = printf('SV%04d', StudentId),
                        ClassNameSnapshot = COALESCE((SELECT ClassName FROM Students WHERE Id=PaymentReceipts.StudentId), ''),
                        SemesterNameSnapshot = COALESCE((SELECT Name FROM Semesters WHERE Id=PaymentReceipts.SemesterId), ''),
                        TotalTuitionSnapshot = COALESCE((SELECT TotalAmount FROM TuitionFees WHERE Id=PaymentReceipts.FeeId), 0),
                        TotalPaidAfterSnapshot = COALESCE((SELECT PaidAmount FROM TuitionFees WHERE Id=PaymentReceipts.FeeId), Amount),
                        RemainingAfterSnapshot = COALESCE((SELECT TotalAmount-PaidAmount FROM TuitionFees WHERE Id=PaymentReceipts.FeeId), 0),
                        DueDateSnapshot = COALESCE((SELECT DueDate FROM TuitionFees WHERE Id=PaymentReceipts.FeeId),
                                                   (SELECT DueDate FROM Semesters WHERE Id=PaymentReceipts.SemesterId));");
                SetSchemaVersion(conn, tx, CurrentSchemaVersion);
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw new InvalidOperationException("Không thể bổ sung dữ liệu snapshot cho biên lai. Dữ liệu gốc không bị thay đổi.", ex);
            }
        }

        private static void SetSchemaVersion(SqliteConnection conn, SqliteTransaction tx, int version)
        {
            ExecuteNonQuery(conn, tx, $"PRAGMA user_version = {version};");
        }

        private static void ExecuteNonQuery(SqliteConnection conn, SqliteTransaction tx, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static string CreateTablesSql(string students, string semesters, string fees, string receipts) => $@"
            CREATE TABLE {students} (
                Id INTEGER PRIMARY KEY,
                FullName TEXT NOT NULL CHECK (length(trim(FullName)) > 0),
                Email TEXT,
                PhoneNumber TEXT,
                DateOfBirth TEXT,
                ClassName TEXT
            );

            CREATE TABLE {semesters} (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL CHECK (length(trim(Name)) > 0),
                StartDate TEXT,
                EndDate TEXT,
                DueDate TEXT,
                IsActive INTEGER NOT NULL DEFAULT 0 CHECK (IsActive IN (0, 1))
            );

            CREATE TABLE {fees} (
                Id INTEGER PRIMARY KEY,
                StudentId INTEGER NOT NULL,
                SemesterId INTEGER NOT NULL,
                Credits INTEGER NOT NULL CHECK (Credits > 0),
                TotalAmount INTEGER NOT NULL CHECK (typeof(TotalAmount) = 'integer' AND TotalAmount >= 0),
                DiscountAmount INTEGER NOT NULL DEFAULT 0 CHECK (typeof(DiscountAmount) = 'integer' AND DiscountAmount >= 0),
                DiscountReason TEXT,
                PaidAmount INTEGER NOT NULL DEFAULT 0 CHECK (typeof(PaidAmount) = 'integer' AND PaidAmount >= 0 AND PaidAmount <= TotalAmount),
                PaidDate TEXT,
                DueDate TEXT,
                Status INTEGER NOT NULL DEFAULT 0 CHECK (Status IN (0, 1, 2, 3)),
                Note TEXT,
                CONSTRAINT UQ_TuitionFees_Student_Semester UNIQUE (StudentId, SemesterId),
                CONSTRAINT FK_TuitionFees_Student FOREIGN KEY (StudentId) REFERENCES {students}(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_TuitionFees_Semester FOREIGN KEY (SemesterId) REFERENCES {semesters}(Id) ON DELETE RESTRICT
            );

            CREATE TABLE {receipts} (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FeeId INTEGER NOT NULL,
                StudentId INTEGER NOT NULL,
                SemesterId INTEGER NOT NULL,
                ReceiptCode TEXT NOT NULL CHECK (length(trim(ReceiptCode)) > 0),
                Amount INTEGER NOT NULL CHECK (typeof(Amount) = 'integer' AND Amount > 0),
                PaymentDate TEXT NOT NULL,
                PaymentMethod TEXT,
                PayerName TEXT,
                Note TEXT,
                StudentNameSnapshot TEXT NOT NULL DEFAULT '',
                StudentCodeSnapshot TEXT NOT NULL DEFAULT '',
                ClassNameSnapshot TEXT NOT NULL DEFAULT '',
                SemesterNameSnapshot TEXT NOT NULL DEFAULT '',
                TotalTuitionSnapshot INTEGER NOT NULL DEFAULT 0 CHECK (typeof(TotalTuitionSnapshot) = 'integer' AND TotalTuitionSnapshot >= 0),
                TotalPaidAfterSnapshot INTEGER NOT NULL DEFAULT 0 CHECK (typeof(TotalPaidAfterSnapshot) = 'integer' AND TotalPaidAfterSnapshot >= 0),
                RemainingAfterSnapshot INTEGER NOT NULL DEFAULT 0 CHECK (typeof(RemainingAfterSnapshot) = 'integer' AND RemainingAfterSnapshot >= 0),
                DueDateSnapshot TEXT,
                CONSTRAINT UQ_PaymentReceipts_ReceiptCode UNIQUE (ReceiptCode),
                CONSTRAINT FK_PaymentReceipts_Fee FOREIGN KEY (FeeId) REFERENCES {fees}(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_PaymentReceipts_Student FOREIGN KEY (StudentId) REFERENCES {students}(Id) ON DELETE RESTRICT,
                CONSTRAINT FK_PaymentReceipts_Semester FOREIGN KEY (SemesterId) REFERENCES {semesters}(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IX_{fees}_SemesterId ON {fees}(SemesterId);
            CREATE INDEX IX_{students}_ClassName ON {students}(ClassName);
            CREATE INDEX IX_{receipts}_FeeId ON {receipts}(FeeId);
            CREATE INDEX IX_{receipts}_Student_Semester ON {receipts}(StudentId, SemesterId);";

        public (int Students, int Semesters, int Fees, int Receipts) GetRecordCounts()
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            int GetCount(string table)
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
                var res = cmd.ExecuteScalar();
                return res is long l ? (int)l : Convert.ToInt32(res);
            }

            return (GetCount("Students"), GetCount("Semesters"), GetCount("TuitionFees"), GetCount("PaymentReceipts"));
        }

        public void BackupTo(string destinationPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
            string fullPath = Path.GetFullPath(destinationPath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            using var source = CreateConnection();
            using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = fullPath }.ToString());
            destination.Open();
            source.BackupDatabase(destination);
        }

        public string RestoreFrom(string sourcePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
            string fullSourcePath = Path.GetFullPath(sourcePath);
            if (!File.Exists(fullSourcePath)) throw new FileNotFoundException("Không tìm thấy tệp sao lưu.", fullSourcePath);
            if (string.Equals(fullSourcePath, DbPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tệp phục hồi phải khác cơ sở dữ liệu đang sử dụng.");

            var sourceBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = fullSourcePath,
                Mode = SqliteOpenMode.ReadOnly
            };
            using var source = new SqliteConnection(sourceBuilder.ToString());
            source.Open();
            using (var integrity = source.CreateCommand())
            {
                integrity.CommandText = "PRAGMA integrity_check;";
                if (!string.Equals(Convert.ToString(integrity.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Tệp sao lưu không vượt qua kiểm tra toàn vẹn SQLite.");
            }
            if (GetSchemaVersion(source) != CurrentSchemaVersion || GetExistingApplicationTables(source).Count != ApplicationTables.Length)
                throw new InvalidDataException("Tệp sao lưu không đúng phiên bản hoặc thiếu bảng dữ liệu EduFee.");
            ValidateRestoreContract(source);

            string safetyBackup = DbPath + $".before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db";
            BackupTo(safetyBackup);
            using var destination = CreateConnection();
            source.BackupDatabase(destination);
            return safetyBackup;
        }

        private static void ValidateRestoreContract(SqliteConnection source)
        {
            using (var shape = source.CreateCommand())
            {
                shape.CommandText = @"
                    SELECT Id, FullName, Email, PhoneNumber, DateOfBirth, ClassName FROM Students LIMIT 0;
                    SELECT Id, Name, StartDate, EndDate, DueDate, IsActive FROM Semesters LIMIT 0;
                    SELECT Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount, DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note FROM TuitionFees LIMIT 0;
                    SELECT Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate, PaymentMethod, PayerName, Note,
                           StudentNameSnapshot, StudentCodeSnapshot, ClassNameSnapshot, SemesterNameSnapshot,
                           TotalTuitionSnapshot, TotalPaidAfterSnapshot, RemainingAfterSnapshot, DueDateSnapshot
                    FROM PaymentReceipts LIMIT 0;";
                shape.ExecuteNonQuery();
            }

            if (CountPragmaRows(source, "PRAGMA foreign_key_list('TuitionFees');") < 2 ||
                CountPragmaRows(source, "PRAGMA foreign_key_list('PaymentReceipts');") < 3)
                throw new InvalidDataException("Tệp sao lưu thiếu ràng buộc khóa ngoại bắt buộc.");

            using (var check = source.CreateCommand())
            {
                check.CommandText = "PRAGMA foreign_key_check;";
                using var reader = check.ExecuteReader();
                if (reader.Read()) throw new InvalidDataException("Tệp sao lưu chứa dữ liệu vi phạm khóa ngoại.");
            }

            if (!HasIntegerColumn(source, "TuitionFees", "TotalAmount") ||
                !HasIntegerColumn(source, "TuitionFees", "PaidAmount") ||
                !HasIntegerColumn(source, "PaymentReceipts", "Amount"))
                throw new InvalidDataException("Tệp sao lưu không dùng kiểu INTEGER cho tiền VND.");

            if (!HasUniqueIndex(source, "TuitionFees") || !HasUniqueIndex(source, "PaymentReceipts"))
                throw new InvalidDataException("Tệp sao lưu thiếu ràng buộc duy nhất bắt buộc.");
        }

        private static int CountPragmaRows(SqliteConnection connection, string commandText)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            using var reader = command.ExecuteReader();
            int count = 0;
            while (reader.Read()) count++;
            return count;
        }

        private static bool HasIntegerColumn(SqliteConnection connection, string table, string column)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info('{table}');";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                    return string.Equals(reader.GetString(2), "INTEGER", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private static bool HasUniqueIndex(SqliteConnection connection, string table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA index_list('{table}');";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                if (reader.GetInt32(2) == 1) return true;
            return false;
        }
    }
}
