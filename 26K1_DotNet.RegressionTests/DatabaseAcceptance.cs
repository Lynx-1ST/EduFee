using K26_DotNet.Data;
using Microsoft.Data.Sqlite;

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
    }

    private static void SeedCurrentDatabase(SqlDatabaseContext database)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Students(Id, FullName, ClassName) VALUES(1, 'Sinh viên kiểm thử', 'K26');
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
