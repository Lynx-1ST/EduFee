using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    public static class CsvExportHelper
    {
        /// <summary>
        /// Xuất dữ liệu ra file CSV qua SaveFileDialog với định dạng UTF-8 BOM
        /// để mở trên Excel không bị lỗi phông tiếng Việt.
        /// Thin UI wrapper around CsvWriter.
        /// </summary>
        public static void ExportToCsv<T>(string defaultFileName, IEnumerable<T> items, List<(string Header, Func<T, object> ValueGetter)> columns)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "File Excel CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
                FileName = defaultFileName,
                Title = "Lưu tệp CSV Excel"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                CsvWriter.WriteCsv(items, columns, sfd.FileName);
                UiFeedback.ShowSuccess($"Đã xuất file thành công tại:\n{sfd.FileName}");
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi khi xuất file");
            }
        }

        /// <summary>
        /// Escape a single CSV field value.
        /// </summary>
        internal static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            // Quoting alone does not prevent spreadsheet formula evaluation.
            var trimmed = field.TrimStart();
            if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) ||
                field[0] is '\t' or '\r' or '\n')
                field = "'" + field;
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return "\"" + field + "\"";
        }
    }

    /// <summary>
    /// UI-independent CSV writer. Can be used in headless/test contexts.
    /// </summary>
    public static class CsvWriter
    {
        /// <summary>
        /// Write items to a CSV file with UTF-8 BOM encoding.
        /// </summary>
        public static void WriteCsv<T>(IEnumerable<T> items, List<(string Header, Func<T, object> ValueGetter)> columns, string filePath)
        {
            var sb = new StringBuilder();

            // Header line
            var headers = new List<string>();
            foreach (var col in columns)
            {
                headers.Add(CsvExportHelper.EscapeCsv(col.Header));
            }
            sb.AppendLine(string.Join(",", headers));

            // Rows
            foreach (var item in items)
            {
                var values = new List<string>();
                foreach (var col in columns)
                {
                    var val = col.ValueGetter(item)?.ToString() ?? string.Empty;
                    values.Add(CsvExportHelper.EscapeCsv(val));
                }
                sb.AppendLine(string.Join(",", values));
            }

            // Write with UTF-8 BOM
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
