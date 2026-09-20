using System.Collections;
using System.Reflection;
using System.Windows.Forms;
using _26K1_DotNet;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

public static class ReportAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        string directory = Path.Combine(root, "report-acceptance");
        Directory.CreateDirectory(directory);

        var students = new StudentService(Path.Combine(directory, "students.json"));
        students.AddStudents([
            new Student(101, "Nguyễn An", "an@example.com", "0900000001", new DateTime(2005, 1, 1), "K26A"),
            new Student(202, "Trần Bình", "binh@example.com", "0900000002", new DateTime(2005, 1, 2), "K26A"),
            new Student(303, "Lê Chi", "chi@example.com", "0900000003", new DateTime(2005, 1, 3), "K26B")
        ]);
        var semesters = new SemesterService(Path.Combine(directory, "semesters.json"));
        semesters.Add(new Semester(0, "HK báo cáo", DateTime.Today.AddDays(-30), DateTime.Today.AddMonths(3), DateTime.Today.AddDays(-1), true));
        var semester = semesters.GetActive() ?? throw new InvalidOperationException("Missing report semester.");
        var tuition = new TuitionService(Path.Combine(directory, "tuition.json"));
        tuition.Add(new TuitionFee(0, 101, semester.Id, 1, 500_000m));
        tuition.Add(new TuitionFee(0, 202, semester.Id, 1, 500_000m, dueDate: DateTime.Today.AddDays(4)));
        tuition.Add(new TuitionFee(0, 303, semester.Id, 1, 500_000m, dueDate: DateTime.Today.AddDays(4)));
        tuition.RecordPayment(tuition.GetAll().Single(fee => fee.StudentId == 202).Id, 100_000m, DateTime.Today.AddDays(4));

        string receiptsPath = Path.Combine(directory, "receipts.json");
        File.WriteAllText(receiptsPath, "[]");
        using var mainForm = new Form1();
        using var host = new Form { ClientSize = new Size(1080, 720), StartPosition = FormStartPosition.Manual };
        using var panel = new PanelStatistics(students, semesters, tuition, new ReceiptService(receiptsPath), mainForm) { Dock = DockStyle.Fill };
        host.Controls.Add(panel);
        host.CreateControl();
        panel.CreateControl();
        Application.DoEvents();
        panel.RefreshData(semester.Id);

        var classFilter = GetPrivateField<ComboBox>(panel, "cmbDebtClass");
        var search = GetPrivateField<TextBox>(panel, "txtDebtSearch");
        var overdueOnly = GetPrivateField<CheckBox>(panel, "chkOverdueOnly");
        var remainingLabel = GetPrivateField<Label>(panel, "lblLeftVal");

        classFilter.SelectedItem = "K26A";
        Application.DoEvents();
        check(CurrentDebtFees(panel).Select(fee => fee.StudentId).OrderBy(id => id).SequenceEqual([101, 202]),
            "Debt report class filter matches the selected class exactly");

        search.Text = "SV0101";
        Application.DoEvents();
        check(CurrentDebtFees(panel).Select(fee => fee.StudentId).SequenceEqual([101]),
            "Debt report search filters by student code");

        classFilter.SelectedIndex = 0;
        search.Text = "K26B";
        Application.DoEvents();
        check(CurrentDebtFees(panel).Select(fee => fee.StudentId).SequenceEqual([303]),
            "Debt report search also recognizes class names");

        search.Clear();
        overdueOnly.Checked = true;
        Application.DoEvents();
        check(CurrentDebtFees(panel).Select(fee => fee.StudentId).SequenceEqual([101]),
            "Overdue-only report excludes future-due balances");
        check(remainingLabel.Text == $"{1_400_000m:N0} ₫",
            "Semester KPI remains semester-wide when the debt list is filtered");

        var buildClassStats = typeof(PanelStatistics).GetMethod("BuildClassStats", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("PanelStatistics", "BuildClassStats");
        var studentMap = students.GetAllStudents().ToDictionary(student => student.Id);
        var classRows = ((IEnumerable)(buildClassStats.Invoke(null, [tuition.GetBySemester(semester.Id), studentMap])
            ?? throw new InvalidOperationException("Missing class report data."))).Cast<object>().ToList();
        decimal classRemaining = classRows.Sum(row => (decimal)(row.GetType().GetProperty("ConNo")?.GetValue(row)
            ?? throw new InvalidOperationException("Missing class debt total.")));
        check(classRows.Count == 2 && classRemaining == 1_400_000m,
            "Class grid, CSV, and PDF share the same aggregated class-report totals");
    }

    private static List<TuitionFee> CurrentDebtFees(PanelStatistics panel)
    {
        var field = typeof(PanelStatistics).GetField("_currentDebtFees", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException("PanelStatistics", "_currentDebtFees");
        return ((IEnumerable<TuitionFee>)(field.GetValue(panel) ?? throw new InvalidOperationException("Missing debt data."))).ToList();
    }

    private static T GetPrivateField<T>(object target, string name) where T : class
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().Name, name);
        return field.GetValue(target) as T ?? throw new InvalidOperationException($"Field {name} was not initialized.");
    }
}
