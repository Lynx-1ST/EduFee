using System.Globalization;
using System.Net.Mail;
using Microsoft.VisualBasic.FileIO;
using K26_DotNet.Models;

namespace K26_DotNet.Services;

public sealed class StudentImportRow
{
    public int RowIndex { get; set; }
    // Internal database key, assigned independently from the business student code.
    public int Id { get; set; }
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsValid { get; set; }
}

public static class StudentCsvImporter
{
    private static readonly string[] Headers = ["MaSV", "HoTen", "Lop", "NgaySinh", "DienThoai", "Email"];

    public static List<StudentImportRow> Read(string path, IEnumerable<string> existingStudentCodes, int firstNewId)
    {
        using var reader = new StreamReader(path, System.Text.Encoding.UTF8, true);
        var header = reader.ReadLine() ?? throw new FormatException("File CSV trống.");
        string separator = header.Contains(';') ? ";" : ",";
        using var headerParser = new TextFieldParser(new StringReader(header));
        headerParser.SetDelimiters(separator);
        var names = headerParser.ReadFields();
        if (names == null || !names.SequenceEqual(Headers, StringComparer.OrdinalIgnoreCase))
            throw new FormatException("Tiêu đề CSV phải là: " + string.Join(separator, Headers));

        using var parser = new TextFieldParser(reader) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(separator);
        var codes = new HashSet<string>(existingStudentCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()), StringComparer.OrdinalIgnoreCase);
        int nextId = Math.Max(firstNewId, 1);
        var rows = new List<StudentImportRow>();
        while (!parser.EndOfData)
        {
            var row = new StudentImportRow { RowIndex = checked((int)parser.LineNumber + 1) };
            rows.Add(row);
            string[]? fields;
            try { fields = parser.ReadFields(); }
            catch (MalformedLineException)
            {
                row.Status = "Dòng CSV sai định dạng";
                continue;
            }
            if (fields == null || fields.Length != 6)
            {
                row.Status = "Cần đủ 6 cột";
                continue;
            }
            row.StudentCode = fields[0].Trim();
            row.FullName = fields[1];
            row.ClassName = fields[2];
            row.PhoneNumber = fields[4];
            row.Email = fields[5];
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.StudentCode)) errors.Add("Thiếu mã SV");
            else if (!codes.Add(row.StudentCode)) errors.Add("Trùng mã SV");
            if (string.IsNullOrWhiteSpace(row.FullName)) errors.Add("Thiếu họ tên");
            if (string.IsNullOrWhiteSpace(row.ClassName)) errors.Add("Thiếu lớp");
            if (!DateTime.TryParseExact(fields[3], ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob) || dob.Date > DateTime.Today)
                errors.Add("Ngày sinh không hợp lệ");
            row.DateOfBirth = dob;
            if (string.IsNullOrWhiteSpace(row.PhoneNumber)) errors.Add("Thiếu điện thoại");
            if (!MailAddress.TryCreate(row.Email, out var address) || address.Address != row.Email)
                errors.Add("Email không hợp lệ");
            row.IsValid = errors.Count == 0;
            if (row.IsValid) row.Id = nextId++;
            row.Status = row.IsValid ? "Hợp lệ" : string.Join("; ", errors);
        }
        return rows;
    }

    // Compatibility overload for callers that previously supplied internal IDs.
    public static List<StudentImportRow> Read(string path, IEnumerable<int> existingIds)
    {
        var ids = existingIds.ToList();
        return Read(path, ids.Select(id => id.ToString(CultureInfo.InvariantCulture)), ids.Count == 0 ? 1 : ids.Max() + 1);
    }
}
