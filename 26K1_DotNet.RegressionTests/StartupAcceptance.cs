using K26_DotNet.Data;
using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

public static class StartupAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        var dataDirectory = Path.Combine(root, "application-data");
        var sourceDirectory = Path.Combine(root, "legacy-source");
        Directory.CreateDirectory(sourceDirectory);
        string invalidStudents = Path.Combine(sourceDirectory, "students.json");
        File.WriteAllText(invalidStudents, "invalid json");

        var initialized = DatabaseBootstrapper.InitializeDefaultDatabase(dataDirectory, sourceDirectory);
        check(File.Exists(initialized.InitializationMarkerPath) && initialized.Database.GetRecordCounts() == (0, 0, 0, 0),
            "Startup marks an empty SQLite database without importing legacy JSON");
        check(File.ReadAllText(invalidStudents) == "invalid json" && !string.IsNullOrWhiteSpace(initialized.Notice),
            "Startup leaves invalid legacy JSON unchanged and reports an actionable notice");

        var secondStart = DatabaseBootstrapper.InitializeDefaultDatabase(dataDirectory, sourceDirectory);
        check(secondStart.WasPreviouslyInitialized && secondStart.Database.GetRecordCounts() == (0, 0, 0, 0),
            "A marked empty database stays empty on later starts");

        var demoDirectory = Path.Combine(root, "demo-data");
        var demo = DatabaseBootstrapper.InitializeDemoDatabase(demoDirectory);
        var demoCounts = demo.Database.GetRecordCounts();
        check(demoCounts.Students == 3 && demoCounts.Semesters == 1 && demoCounts.Fees == 3 && demoCounts.Receipts == 1,
            "Demo mode seeds isolated students, tuition fees and a payment receipt");
        check(demo.Database.DbPath != initialized.Database.DbPath && new EmailService(DatabaseBootstrapper.GetEmailSettingsPath(demoDirectory)).Settings.IsSimulationMode,
            "Demo mode uses a separate data folder and simulation-only email defaults");
        using var form = new _26K1_DotNet.Form1(true, demoDirectory);
        form.ShowInTaskbar = false;
        form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
        form.Location = new System.Drawing.Point(-10000, -10000);
        form.Show();
        System.Windows.Forms.Application.DoEvents();
        check(form.DbContext.DbPath == demo.Database.DbPath && form.Text.Contains("DEMO") && form.EmailService.Settings.IsSimulationMode,
            "Demo main form loads the isolated SQLite database from an unrelated working directory");
        var totals = new SqlDataMigrator(form.DbContext).LoadAllFromSql().Fees;
        check(totals.Sum(f => f.TotalAmount) == 22_440_000m && totals.Sum(f => f.PaidAmount) == 3_000_000m &&
            totals.Sum(f => f.RemainingAmount) == 19_440_000m, "Demo tuition and receipt totals reconcile");
        typeof(_26K1_DotNet.Form1).GetMethod("ShowPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(form, new object[] { "statistics" });
        form.PerformLayout();
        System.Windows.Forms.Application.DoEvents();
        using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
        string? artifacts = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
        if (artifacts != null) bitmap.Save(Path.Combine(artifacts, "demo-statistics.png"));
        check(!form.IsDisposed, "Demo statistics screen renders with seeded financial data");
        typeof(_26K1_DotNet.Form1).GetMethod("ShowPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(form, new object[] { "tuition" });
        form.PerformLayout();
        System.Windows.Forms.Application.DoEvents();
        using var tuitionBitmap = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(tuitionBitmap, new System.Drawing.Rectangle(0, 0, tuitionBitmap.Width, tuitionBitmap.Height));
        if (artifacts != null) tuitionBitmap.Save(Path.Combine(artifacts, "demo-tuition.png"));
        check(!form.IsDisposed, "Demo tuition screen renders with the global semester selector");
    }
}
