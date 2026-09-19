using System.Globalization;
using System.Net.Mail;
using Microsoft.VisualBasic.FileIO;
using K26_DotNet.Models;

namespace K26_DotNet.Services;

public sealed class StudentImportRow
{
    public int RowIndex { get; set; }
    public int Id { get; set; }
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

    public static List<StudentImportRow> Read(string path, IEnumerable<int> existingIds)
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
        var ids = existingIds.ToHashSet();
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
            row.FullName = fields[1];
            row.ClassName = fields[2];
            row.PhoneNumber = fields[4];
            row.Email = fields[5];
            var errors = new List<string>();
            if (!int.TryParse(fields[0], out var id) || id <= 0) errors.Add("Mã SV không hợp lệ");
            else if (!ids.Add(id)) errors.Add("Trùng mã SV");
            row.Id = id;
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
            row.Status = row.IsValid ? "Hợp lệ" : string.Join("; ", errors);
        }
        return rows;
    }
}
