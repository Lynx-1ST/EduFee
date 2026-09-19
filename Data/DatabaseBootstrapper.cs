using K26_DotNet.Services;

namespace K26_DotNet.Data;

public static class DatabaseBootstrapper
{
    public static SqlDatabaseContext InitializeDefaultDatabase(string sourceDirectory)
    {
        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EduFee");
        Directory.CreateDirectory(dataDirectory);
        var database = new SqlDatabaseContext(Path.Combine(dataDirectory, "edufee.db"));

        var counts = database.GetRecordCounts();
        if (counts.Students != 0 || counts.Semesters != 0 || counts.Fees != 0 || counts.Receipts != 0)
            return database;

        string studentsPath = Path.Combine(sourceDirectory, "students.json");
        string semestersPath = Path.Combine(sourceDirectory, "semesters.json");
        string feesPath = Path.Combine(sourceDirectory, "tuitionfees.json");
        string receiptsPath = Path.Combine(sourceDirectory, "receipts.json");

        if (!File.Exists(studentsPath) && !File.Exists(semestersPath) &&
            !File.Exists(feesPath) && !File.Exists(receiptsPath))
            return database;

        var students = new StudentService(studentsPath);
        var semesters = new SemesterService(semestersPath);
        var fees = new TuitionService(feesPath);
        var receipts = new ReceiptService(receiptsPath);
        new SqlDataMigrator(database).MigrateFromJson(students, semesters, fees, receipts);
        return database;
    }
}
