using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services
{
    public class ReceiptService
    {
        private readonly string _filePath;
        private readonly SqliteRepository? _repository;
        private List<PaymentReceipt> _receipts = new();

        public ReceiptService(string filePath = "receipts.json")
        {
            _filePath = filePath;
            LoadFromFile();
        }

        public ReceiptService(SqlDatabaseContext database)
        {
            ArgumentNullException.ThrowIfNull(database);
            _filePath = string.Empty;
            _repository = new SqliteRepository(database);
            ReloadFromDatabase();
        }

        internal SqliteRepository? Repository => _repository;
        internal string? DatabasePath => _repository?.DatabasePath;

        public List<PaymentReceipt> GetAll() => _receipts.OrderByDescending(r => r.PaymentDate).ToList();

        public PaymentReceipt? GetById(int id) => _receipts.FirstOrDefault(r => r.Id == id);

        public List<PaymentReceipt> GetByTuitionFeeId(int tuitionFeeId) =>
            _receipts.Where(r => r.TuitionFeeId == tuitionFeeId).OrderByDescending(r => r.PaymentDate).ToList();

        public List<PaymentReceipt> GetByStudentId(int studentId) =>
            _receipts.Where(r => r.StudentId == studentId).OrderByDescending(r => r.PaymentDate).ToList();

        public PaymentReceipt CreateReceipt(int tuitionFeeId, int studentId, int semesterId,
            decimal amount, string paymentMethod, string payerName, string note = "")
        {
            if (_repository != null)
                throw new InvalidOperationException("Biên lai SQLite chỉ được tạo cùng giao dịch thu tiền.");
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(payerName)) throw new ArgumentException("Thiếu người nộp tiền.", nameof(payerName));
            int nextId = _receipts.Count > 0 ? _receipts.Max(r => r.Id) + 1 : 1;
            string code = $"BL-{DateTime.Now.Year}-{nextId:D4}";

            var receipt = new PaymentReceipt(nextId, code, tuitionFeeId, studentId, semesterId,
                amount, paymentMethod, payerName, note);

            _receipts.Add(receipt);
            try { SaveToFile(); }
            catch { _receipts.Remove(receipt); throw; }
            return receipt;
        }

        public void LoadFromFile()
        {
            if (_repository != null)
            {
                ReloadFromDatabase();
                return;
            }
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _receipts = JsonSerializer.Deserialize<List<PaymentReceipt>>(json)
                        ?? throw new JsonException("Dữ liệu biên lai không được là null.");
                }
                else
                {
                    _receipts = new List<PaymentReceipt>();
                    SaveToFile();
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Không thể đọc biên lai từ '{_filePath}'. Dữ liệu gốc được giữ nguyên.", ex);
            }
        }

        private void SaveToFile()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(_receipts, options);
                K26_DotNet.Data.AtomicFile.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể lưu biên lai: {ex.Message}", ex);
            }
        }

        internal void ReloadFromDatabase()
        {
            if (_repository == null) return;
            _receipts = _repository.LoadAll().Receipts;
        }

        internal void AcceptCommittedReceipt(PaymentReceipt receipt)
        {
            ArgumentNullException.ThrowIfNull(receipt);
            if (_receipts.Any(existing => existing.Id == receipt.Id || existing.ReceiptCode == receipt.ReceiptCode)) return;
            _receipts.Add(receipt);
        }
    }
}
