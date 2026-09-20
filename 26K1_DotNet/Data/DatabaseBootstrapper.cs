using K26_DotNet.Services;

namespace K26_DotNet.Data;

/// <summary>Chooses stable local storage and performs only explicit legacy JSON imports.</summary>
public static class DatabaseBootstrapper
{
    private const string DatabaseFileName = "edufee.db";
    private const string InitializationMarkerFileName = ".edufee-initialized";

    public static string GetDefaultDataDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EduFee");

    public static string GetDemoDataDirectory() => Path.Combine(GetDefaultDataDirectory(), "demo");

    public static string GetEmailSettingsPath(string? dataDirectory = null) => Path.Combine(
        dataDirectory ?? GetDefaultDataDirectory(), "email_settings.json");

    public static SqlDatabaseContext InitializeDefaultDatabase(string sourceDirectory) =>
        InitializeDefaultDatabase(GetDefaultDataDirectory(), sourceDirectory).Database;

    /// <summary>Creates or opens the database. Legacy JSON is never imported automatically.</summary>
    public static BootstrapResult InitializeDefaultDatabase(string dataDirectory, string sourceDirectory) =>
        InitializeDatabase(dataDirectory, sourceDirectory);

    public static BootstrapResult InitializeDemoDatabase(string? dataDirectory = null)
    {
        var result = InitializeDatabase(dataDirectory ?? GetDemoDataDirectory(), null);
        DemoData.EnsureSeeded(result.Database);
        return result with { Notice = "Đang dùng dữ liệu demo riêng. Mọi email chỉ được giả lập." };
    }

    /// <summary>Imports a complete JSON snapshot only when the user explicitly requests it.</summary>
    public static (int Students, int Semesters, int Fees, int Receipts, string Message) ImportLegacyJson(
        SqlDatabaseContext database, string sourceDirectory)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        string studentsPath = Path.Combine(sourceDirectory, "students.json");
        string semestersPath = Path.Combine(sourceDirectory, "semesters.json");
        string feesPath = Path.Combine(sourceDirectory, "tuitionfees.json");
        string receiptsPath = Path.Combine(sourceDirectory, "receipts.json");
        var required = new[] { studentsPath, semestersPath, feesPath, receiptsPath };
        if (required.Any(path => !File.Exists(path)))
            throw new FileNotFoundException("Cần đủ bốn tệp JSON: students.json, semesters.json, tuitionfees.json và receipts.json.");

        // These services only read existing files. Do not use defaults: SemesterService and
        // ReceiptService can otherwise create source files as a side effect.
        var students = new StudentService(studentsPath);
        var semesters = new SemesterService(semestersPath);
        var fees = new TuitionService(feesPath);
        var receipts = new ReceiptService(receiptsPath);
        return new SqlDataMigrator(database).MigrateFromJson(students, semesters, fees, receipts);
    }

    private static BootstrapResult InitializeDatabase(string dataDirectory, string? sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        var database = new SqlDatabaseContext(Path.Combine(dataDirectory, DatabaseFileName));
        string markerPath = Path.Combine(dataDirectory, InitializationMarkerFileName);
        bool wasInitialized = File.Exists(markerPath);
        if (!wasInitialized) File.WriteAllText(markerPath, "initialized");

        string? notice = !string.IsNullOrWhiteSpace(sourceDirectory) && HasAnyLegacyJson(sourceDirectory)
            ? "Phát hiện dữ liệu JSON cũ. Dữ liệu chưa được nhập tự động; hãy mở Quản trị cơ sở dữ liệu SQL để nhập khi đã đối soát."
            : null;
        return new BootstrapResult(database, markerPath, wasInitialized, notice);
    }

    private static bool HasAnyLegacyJson(string sourceDirectory) =>
        new[] { "students.json", "semesters.json", "tuitionfees.json", "receipts.json" }
            .Any(fileName => File.Exists(Path.Combine(sourceDirectory, fileName)));
}

public sealed record BootstrapResult(SqlDatabaseContext Database, string InitializationMarkerPath,
    bool WasPreviouslyInitialized, string? Notice);
