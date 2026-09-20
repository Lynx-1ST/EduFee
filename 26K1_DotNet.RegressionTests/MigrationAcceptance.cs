using System.Text.Json;
using K26_DotNet.Data;

internal static class MigrationAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        var source = new SqlDatabaseContext(Path.Combine(root, "snapshot-source.db"));
        DemoData.EnsureSeeded(source);
        using (var connection = source.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE Students SET FullName='Tên sau khi sửa' WHERE Id=1001";
            command.ExecuteNonQuery();
        }
        var data = new SqlDataMigrator(source).LoadAllFromSql();
        string folder = Path.Combine(root, "snapshot-json");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "students.json"), JsonSerializer.Serialize(data.Students));
        File.WriteAllText(Path.Combine(folder, "semesters.json"), JsonSerializer.Serialize(data.Semesters));
        File.WriteAllText(Path.Combine(folder, "tuitionfees.json"), JsonSerializer.Serialize(data.Fees));
        File.WriteAllText(Path.Combine(folder, "receipts.json"), JsonSerializer.Serialize(data.Receipts));
        var target = new SqlDatabaseContext(Path.Combine(root, "snapshot-target.db"));
        DatabaseBootstrapper.ImportLegacyJson(target, folder);
        var result = new SqlDataMigrator(target).LoadAllFromSql();
        check(result.Students.Single(s => s.Id == 1001).FullName == "Tên sau khi sửa" &&
            result.Receipts.Single().StudentNameSnapshot == data.Receipts.Single().StudentNameSnapshot &&
            result.Receipts.Single().RemainingAfterSnapshot == data.Receipts.Single().RemainingAfterSnapshot,
            "JSON import preserves existing historical receipt snapshots after profile edits");
        var duplicate = JsonSerializer.Deserialize<K26_DotNet.Models.PaymentReceipt>(JsonSerializer.Serialize(data.Receipts.Single()))!;
        duplicate.Id += 1;
        data.Receipts.Add(duplicate);
        data.Fees.Single(f => f.Id == duplicate.TuitionFeeId).PaidAmount += duplicate.Amount;
        File.WriteAllText(Path.Combine(folder, "tuitionfees.json"), JsonSerializer.Serialize(data.Fees));
        File.WriteAllText(Path.Combine(folder, "receipts.json"), JsonSerializer.Serialize(data.Receipts));
        bool rejected = false;
        try { DatabaseBootstrapper.ImportLegacyJson(target, folder); }
        catch (InvalidOperationException) { rejected = true; }
        var unchanged = new SqlDataMigrator(target).LoadAllFromSql();
        check(rejected && unchanged.Receipts.Count == 1 && unchanged.Fees.Sum(f => f.PaidAmount) == 3_000_000m,
            "Duplicate imported receipt codes reject the full import and preserve the previous ledger");
    }
}
