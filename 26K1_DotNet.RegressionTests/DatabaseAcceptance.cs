using K26_DotNet.Data;
using Microsoft.Data.Sqlite;
using System.Reflection;

internal static class DatabaseAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        string targetPath = Path.Combine(root, "restore-acceptance.db");
        var target = new SqlDatabaseContext(targetPath);
        SeedCurrentDatabase(target);

        string backupPath = Path.Combine(root, "restore-source.db");
        target.BackupTo(backupPath);
        using (var backup = new SqliteConnection($"Data Source={backupPath}"))
        {
            backup.Open();
            using var command = backup.CreateCommand();
            command.CommandText = "UPDATE TuitionFees SET PaidAmount = 900 WHERE Id = 1;";
            command.ExecuteNonQuery();
        }

        bool rejected = Throws(() => target.RestoreFrom(backupPath));
        check(rejected && target.GetRecordCounts() == (1, 1, 1, 1),
            "Restore rejects a receipt ledger that disagrees with paid tuition and preserves current data");
        check(Throws(() => target.BackupTo(target.DbPath)), "Backup rejects its own live database path");

        string validBackup = Path.Combine(root, "restore-valid.db");
        target.BackupTo(validBackup);
        string safetyOne = target.RestoreFrom(validBackup);
        string safetyTwo = target.RestoreFrom(validBackup);
        check(!string.Equals(safetyOne, safetyTwo, StringComparison.OrdinalIgnoreCase) &&
              File.Exists(safetyOne) && File.Exists(safetyTwo),
            "Consecutive restores retain distinct safety backups");

        string legacyPath = Path.Combine(root, "legacy-v1.db");
        CreateV1Database(legacyPath);
        var upgraded = new SqlDatabaseContext(legacyPath);
        using var connection = upgraded.CreateConnection();
        using var snapshots = connection.CreateCommand();
        snapshots.CommandText = @"
            SELECT TotalPaidAfterSnapshot, RemainingAfterSnapshot
            FROM PaymentReceipts
            ORDER BY PaymentDate, Id;";
        using var reader = snapshots.ExecuteReader();
        var values = new List<(long PaidAfter, long Remaining)>();
        while (reader.Read()) values.Add((reader.GetInt64(0), reader.GetInt64(1)));
        check(values.SequenceEqual([(300L, 700L), (1000L, 0L)]),
            "Schema v1 migration rebuilds receipt snapshots cumulatively by payment date and ID");

        check(ReadVersion(legacyPath) == 4 && CanStoreLatePaid(legacyPath) && ReadStudentCode(legacyPath, 1) == "SV0001",
            "Schema v1 migrates sequentially through v2, v3 and v4 with a deterministic student code");

        string v2Path = Path.Combine(root, "legacy-v2.db");
        CreateV2Database(v2Path);
        var beforeV2 = ReadFinancialSummary(v2Path);
        _ = new SqlDatabaseContext(v2Path);
        check(ReadVersion(v2Path) == 4 && CanStoreLatePaid(v2Path) && ReadStudentCode(v2Path, 1) == "SV0001" && ReadFinancialSummary(v2Path) == beforeV2,
            "Schema v2 migrates through v3 to v4 without changing record counts or tuition totals");

        string retryPath = Path.Combine(root, "migration-retry-v2.db");
        CreateV2Database(retryPath);
        var beforeRetry = ReadFinancialSummary(retryPath);
        var checkpoint = typeof(SqlDatabaseContext).GetProperty("MigrationCheckpointForTests",
            BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Missing migration checkpoint.");
        checkpoint.SetValue(null, (Action<string>)(name =>
        {
            if (name == "V2ToV3.BeforeSwap") throw new IOException("Injected migration failure.");
        }));
        bool failed;
        try { failed = Throws(() => _ = new SqlDatabaseContext(retryPath)); }
        finally { checkpoint.SetValue(null, null); }
        check(failed && ReadVersion(retryPath) == 2 && ReadFinancialSummary(retryPath) == beforeRetry,
            "Failed v2 to v3 migration rolls back and keeps schema version 2");

        _ = new SqlDatabaseContext(retryPath);
        check(ReadVersion(retryPath) == 4 && CanStoreLatePaid(retryPath) && ReadFinancialSummary(retryPath) == beforeRetry,
            "A rolled-back migration can retry without changing records or tuition totals");

        string v3Path = Path.Combine(root, "legacy-v3.db");
        CreateV3Database(v3Path);
        var beforeV3 = ReadFinancialSummary(v3Path);
        _ = new SqlDatabaseContext(v3Path);
        check(ReadVersion(v3Path) == 4 && ReadStudentCode(v3Path, 1) == "SV0001" && ReadFinancialSummary(v3Path) == beforeV3,
            "Schema v3 migrates to v4 and preserves IDs, records and tuition totals");

        string retryV4Path = Path.Combine(root, "migration-retry-v3.db");
        CreateV3Database(retryV4Path);
        var beforeRetryV4 = ReadFinancialSummary(retryV4Path);
        checkpoint.SetValue(null, (Action<string>)(name =>
        {
            if (name == "V3ToV4.BeforeSwap") throw new IOException("Injected StudentCode migration failure.");
        }));
        try { failed = Throws(() => _ = new SqlDatabaseContext(retryV4Path)); }
        finally { checkpoint.SetValue(null, null); }
        check(failed && ReadVersion(retryV4Path) == 3 && !HasStudentCodeColumn(retryV4Path) && ReadFinancialSummary(retryV4Path) == beforeRetryV4,
            "Failed v3 to v4 migration rolls back StudentCode and keeps schema version 3");
        _ = new SqlDatabaseContext(retryV4Path);
        check(ReadVersion(retryV4Path) == 4 && ReadStudentCode(retryV4Path, 1) == "SV0001" && ReadFinancialSummary(retryV4Path) == beforeRetryV4,
            "A rolled-back StudentCode migration can retry without changing financial data");

        string mismatchedPath = Path.Combine(root, "mismatched-v4.db");
        CreateV2Database(mismatchedPath);
        SetVersion(mismatchedPath, 4);
        check(Throws(() => _ = new SqlDatabaseContext(mismatchedPath)),
            "Startup rejects a declared v4 database whose schema lacks StudentCode");
    }

    private static void SeedCurrentDatabase(SqlDatabaseContext database)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Students(Id, StudentCode, FullName, ClassName) VALUES(1, '2121050001', 'Sinh viên kiểm thử', 'K26');
            INSERT INTO Semesters(Id, Name, IsActive) VALUES(1, 'HK kiểm thử', 1);
            INSERT INTO TuitionFees(Id, StudentId, SemesterId, Credits, TotalAmount, PaidAmount, Status)
            VALUES(1, 1, 1, 1, 1000, 500, 1);
            INSERT INTO PaymentReceipts(Id, FeeId, StudentId, SemesterId, ReceiptCode, Amount, PaymentDate,
                StudentNameSnapshot, StudentCodeSnapshot, ClassNameSnapshot, SemesterNameSnapshot,
                TotalTuitionSnapshot, TotalPaidAfterSnapshot, RemainingAfterSnapshot)
            VALUES(1, 1, 1, 1, 'BL-TEST-1', 500, '2026-09-01',
                'Sinh viên kiểm thử', 'SV0001', 'K26', 'HK kiểm thử', 1000, 500, 500);";
        command.ExecuteNonQuery();
    }

    private static void CreateV1Database(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE Students (Id INTEGER PRIMARY KEY, FullName TEXT NOT NULL, Email TEXT, PhoneNumber TEXT, DateOfBirth TEXT, ClassName TEXT);
            CREATE TABLE Semesters (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, StartDate TEXT, EndDate TEXT, DueDate TEXT, IsActive INTEGER NOT NULL);
            CREATE TABLE TuitionFees (Id INTEGER PRIMARY KEY, StudentId INTEGER NOT NULL, SemesterId INTEGER NOT NULL, Credits INTEGER NOT NULL,
                TotalAmount INTEGER NOT NULL, DiscountAmount INTEGER NOT NULL DEFAULT 0, DiscountReason TEXT, PaidAmount INTEGER NOT NULL,
                PaidDate TEXT, DueDate TEXT, Status INTEGER NOT NULL, Note TEXT);
            CREATE TABLE PaymentReceipts (Id INTEGER PRIMARY KEY, FeeId INTEGER NOT NULL, StudentId INTEGER NOT NULL, SemesterId INTEGER NOT NULL,
                ReceiptCode TEXT NOT NULL, Amount INTEGER NOT NULL, PaymentDate TEXT NOT NULL, PaymentMethod TEXT, PayerName TEXT, Note TEXT);
            INSERT INTO Students VALUES(1, 'Sinh viên cũ', NULL, NULL, NULL, 'K26');
            INSERT INTO Semesters VALUES(1, 'HK cũ', NULL, NULL, '2026-09-30', 1);
            INSERT INTO TuitionFees VALUES(1, 1, 1, 1, 1000, 0, NULL, 1000, NULL, '2026-09-30', 2, NULL);
            INSERT INTO PaymentReceipts VALUES(1, 1, 1, 1, 'BL-CU-1', 300, '2026-09-01', NULL, NULL, NULL);
            INSERT INTO PaymentReceipts VALUES(2, 1, 1, 1, 'BL-CU-2', 700, '2026-09-02', NULL, NULL, NULL);
            PRAGMA user_version = 1;";
        command.ExecuteNonQuery();
    }

    private static void CreateV2Database(string path)
    {
        CreateV1Database(path);
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            ALTER TABLE PaymentReceipts ADD COLUMN StudentNameSnapshot TEXT NOT NULL DEFAULT '';
            ALTER TABLE PaymentReceipts ADD COLUMN StudentCodeSnapshot TEXT NOT NULL DEFAULT '';
            ALTER TABLE PaymentReceipts ADD COLUMN ClassNameSnapshot TEXT NOT NULL DEFAULT '';
            ALTER TABLE PaymentReceipts ADD COLUMN SemesterNameSnapshot TEXT NOT NULL DEFAULT '';
            ALTER TABLE PaymentReceipts ADD COLUMN TotalTuitionSnapshot INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE PaymentReceipts ADD COLUMN TotalPaidAfterSnapshot INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE PaymentReceipts ADD COLUMN RemainingAfterSnapshot INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE PaymentReceipts ADD COLUMN DueDateSnapshot TEXT;
            PRAGMA user_version = 2;";
        command.ExecuteNonQuery();
    }

    private static void CreateV3Database(string path)
    {
        CreateV2Database(path);
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE TuitionFees_v3 (
                Id INTEGER PRIMARY KEY, StudentId INTEGER NOT NULL, SemesterId INTEGER NOT NULL,
                Credits INTEGER NOT NULL, TotalAmount INTEGER NOT NULL,
                DiscountAmount INTEGER NOT NULL DEFAULT 0, DiscountReason TEXT,
                PaidAmount INTEGER NOT NULL, PaidDate TEXT, DueDate TEXT,
                Status INTEGER NOT NULL CHECK (Status IN (0,1,2,3,4)), Note TEXT);
            INSERT INTO TuitionFees_v3 SELECT * FROM TuitionFees;
            DROP TABLE TuitionFees;
            ALTER TABLE TuitionFees_v3 RENAME TO TuitionFees;
            PRAGMA user_version = 3;";
        command.ExecuteNonQuery();
    }

    private static long ReadVersion(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void SetVersion(string path, int version)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }

    private static string ReadStudentCode(string path, int id)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT StudentCode FROM Students WHERE Id=@id;";
        command.Parameters.AddWithValue("@id", id);
        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    private static bool HasStudentCodeColumn(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Students') WHERE name='StudentCode';";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static (long Students, long Fees, long Receipts, long TuitionTotal, long PaidTotal) ReadFinancialSummary(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT (SELECT COUNT(*) FROM Students),
                   (SELECT COUNT(*) FROM TuitionFees),
                   (SELECT COUNT(*) FROM PaymentReceipts),
                   (SELECT COALESCE(SUM(TotalAmount), 0) FROM TuitionFees),
                   (SELECT COALESCE(SUM(PaidAmount), 0) FROM TuitionFees);";
        using var reader = command.ExecuteReader();
        reader.Read();
        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    private static bool CanStoreLatePaid(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE TuitionFees SET Status=4 WHERE Id=(SELECT Id FROM TuitionFees LIMIT 1);";
        bool updated = command.ExecuteNonQuery() == 1;
        transaction.Rollback();
        return updated;
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch
        {
            return true;
        }
    }
}
