using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services
{
    public class TuitionStatistics
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int PaidCount { get; set; }
        public int PartialCount { get; set; }
        public int UnpaidCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining => TotalAmount - TotalPaid;
        public double PaidPercentage => TotalAmount > 0 ? (double)(TotalPaid / TotalAmount * 100) : 0;
    }

    public class TuitionService
    {
        public const decimal DefaultPricePerCredit = 620_000m;
        private readonly string _filePath;
        private readonly SqliteRepository? _repository;
        private List<TuitionFee> _fees;

        public TuitionService(string filePath = "tuitionfees.json")
        {
            _filePath = filePath;
            _fees = new List<TuitionFee>();
            LoadFees();
        }

        public TuitionService(SqlDatabaseContext database)
        {
            ArgumentNullException.ThrowIfNull(database);
            _filePath = string.Empty;
            _repository = new SqliteRepository(database);
            _fees = _repository.LoadAll().Fees;
        }

        internal SqliteRepository? Repository => _repository;

        public void LoadFees()
        {
            if (_repository != null)
            {
                _fees = _repository.LoadAll().Fees;
                return;
            }
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json)) throw new JsonException("Tệp dữ liệu trống hoặc bị hỏng.");
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        _fees = JsonSerializer.Deserialize<List<TuitionFee>>(json, options) ?? throw new JsonException("Dữ liệu không được là null.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tải học phí: {ex.Message}", ex);
            }
        }

        public void SaveFees()
        {
            if (_repository != null)
                throw new InvalidOperationException("Dữ liệu SQLite phải được lưu qua thao tác nghiệp vụ cụ thể.");
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                K26_DotNet.Data.AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(_fees, options));
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu học phí: {ex.Message}", ex);
            }
        }

        // ─── Queries ──────────────────────────────────────────────────────────

        public void RefreshStatuses(IEnumerable<Semester> semesters)
        {
            var dueDates = semesters.ToDictionary(s => s.Id, s => s.DueDate);
            foreach (var fee in _fees)
                fee.UpdateStatus(dueDates.TryGetValue(fee.SemesterId, out var due) ? due : null);
        }

        public List<TuitionFee> GetAll() => _fees.OrderBy(f => f.Id).Select(Clone).ToList();

        public TuitionFee? GetById(int id) => _fees.Where(f => f.Id == id).Select(Clone).FirstOrDefault();

        public List<TuitionFee> GetByStudent(int studentId) =>
            _fees.Where(f => f.StudentId == studentId).OrderByDescending(f => f.SemesterId).Select(Clone).ToList();

        public List<TuitionFee> GetBySemester(int semesterId) =>
            _fees.Where(f => f.SemesterId == semesterId).OrderBy(f => f.StudentId).Select(Clone).ToList();

        public List<TuitionFee> GetByStatus(PaymentStatus status) =>
            _fees.Where(f => f.Status == status).Select(Clone).ToList();

        /// <summary>Lọc kết hợp học kỳ + trạng thái + tìm kiếm (theo studentId)</summary>
        public List<TuitionFee> Filter(int? semesterId, PaymentStatus? status, IEnumerable<int>? matchingStudentIds)
        {
            var query = _fees.AsEnumerable();

            if (semesterId.HasValue)
                query = query.Where(f => f.SemesterId == semesterId.Value);

            if (status.HasValue)
                query = query.Where(f => f.Status == status.Value);

            if (matchingStudentIds != null)
            {
                var idSet = new HashSet<int>(matchingStudentIds);
                query = query.Where(f => idSet.Contains(f.StudentId));
            }

            return query.OrderBy(f => f.StudentId).Select(Clone).ToList();
        }

        public bool ExistsForStudentInSemester(int studentId, int semesterId) =>
            _fees.Any(f => f.StudentId == studentId && f.SemesterId == semesterId);

        // ─── CRUD ─────────────────────────────────────────────────────────────

        public void Add(TuitionFee fee)
        {
            if (fee == null) throw new ArgumentNullException(nameof(fee));
            if (ExistsForStudentInSemester(fee.StudentId, fee.SemesterId))
                throw new Exception("Sinh viên này đã có phiếu học phí trong học kỳ đã chọn!");

            fee.Id = _fees.Any() ? _fees.Max(f => f.Id) + 1 : 1;
            _fees.Add(fee);
            try
            {
                if (_repository != null) _repository.AddTuitionFees([fee]);
                else SaveFees();
            }
            catch { _fees.Remove(fee); throw; }
        }

        public void AddRange(IEnumerable<TuitionFee> fees)
        {
            if (fees == null) throw new ArgumentNullException(nameof(fees));
            var additions = new List<TuitionFee>();
            int nextId = _fees.Any() ? _fees.Max(f => f.Id) + 1 : 1;
            foreach (var fee in fees)
            {
                if (ExistsForStudentInSemester(fee.StudentId, fee.SemesterId))
                    continue;

                fee.Id = nextId++;
                _fees.Add(fee);
                additions.Add(fee);
            }
            try
            {
                if (_repository != null) _repository.AddTuitionFees(additions);
                else SaveFees();
            }
            catch
            {
                foreach (var fee in additions) _fees.Remove(fee);
                throw;
            }
        }

        public void Update(TuitionFee fee)
        {
            if (fee == null) throw new ArgumentNullException(nameof(fee));
            var existing = _fees.FirstOrDefault(f => f.Id == fee.Id)
                ?? throw new Exception($"Không tìm thấy học phí ID {fee.Id}");
            if (fee.TotalAmount < existing.PaidAmount)
                throw new ArgumentException("Tổng học phí không được nhỏ hơn số tiền đã thu.");
            var previous = Clone(existing);
            CopyEditableFields(fee, existing);
            try
            {
                if (_repository != null) _repository.UpdateTuitionFee(existing);
                else SaveFees();
            }
            catch { CopyEditableFields(previous, existing); throw; }
        }

        public void Delete(int id)
        {
            var fee = _fees.FirstOrDefault(f => f.Id == id)
                ?? throw new Exception($"Không tìm thấy học phí ID {id}");
            if (fee.PaidAmount > 0)
                throw new InvalidOperationException("Không thể xóa phiếu học phí đã phát sinh thu tiền.");
            _fees.Remove(fee);
            try
            {
                if (_repository != null) _repository.DeleteTuitionFee(id);
                else SaveFees();
            }
            catch { _fees.Add(fee); throw; }
        }

        // ─── Business Logic ───────────────────────────────────────────────────

        /// <summary>Ghi nhận thanh toán thêm cho 1 phiếu học phí</summary>
        public void RecordPayment(int id, decimal amount, DateTime? dueDate = null)
        {
            if (_repository != null)
                throw new InvalidOperationException("Với SQLite, hãy dùng RecordPaymentWithReceipt để bảo đảm giao dịch nguyên tử.");
            if (amount <= 0)
                throw new Exception("Số tiền thanh toán phải lớn hơn 0!");

            var fee = _fees.FirstOrDefault(f => f.Id == id)
                ?? throw new Exception($"Không tìm thấy học phí ID {id}");

            if (fee.IsFullyPaid)
                throw new Exception("Học phí này đã được thanh toán đầy đủ!");

            if (fee.PaidAmount + amount > fee.TotalAmount)
                throw new Exception($"Số tiền vượt quá số còn lại ({fee.RemainingAmount:N0} VNĐ)!");

            var previous = (fee.PaidAmount, fee.PaidDate, fee.Status);
            try
            {
                fee.PaidAmount += amount;
                fee.PaidDate = DateTime.Now;
                fee.UpdateStatus(dueDate);
                SaveFees();
            }
            catch
            {
                (fee.PaidAmount, fee.PaidDate, fee.Status) = previous;
                throw;
            }
        }

        public PaymentReceipt RecordPaymentWithReceipt(int id, decimal amount, DateTime? dueDate,
            ReceiptService receiptService, string paymentMethod, string payerName, string note = "")
        {
            ArgumentNullException.ThrowIfNull(receiptService);
            if (_repository == null || receiptService.Repository == null)
                throw new InvalidOperationException("Thu tiền kèm biên lai yêu cầu SQLite để bảo đảm giao dịch nguyên tử.");
            if (!ReferenceEquals(_repository, receiptService.Repository) && receiptService.DatabasePath != DatabasePath)
                throw new InvalidOperationException("Dịch vụ học phí và biên lai phải dùng cùng một cơ sở dữ liệu.");

            var receipt = _repository.RecordPayment(id, amount, paymentMethod, payerName, note, dueDate);
            try
            {
                var fee = _fees.FirstOrDefault(f => f.Id == id);
                if (fee != null)
                {
                    fee.PaidAmount += amount;
                    fee.PaidDate = receipt.PaymentDate;
                    fee.UpdateStatus(dueDate);
                }
                else
                {
                    LoadFees();
                }
                receiptService.AcceptCommittedReceipt(receipt);
            }
            catch
            {
                try { LoadFees(); } catch { }
                try { receiptService.ReloadFromDatabase(); } catch { }
            }
            return receipt;
        }

        internal string? DatabasePath => _repository == null ? null : _repository.DatabasePath;

        private static TuitionFee Clone(TuitionFee source) => new()
        {
            Id = source.Id, StudentId = source.StudentId, SemesterId = source.SemesterId, Credits = source.Credits,
            TotalAmount = source.TotalAmount, DiscountAmount = source.DiscountAmount, DiscountReason = source.DiscountReason,
            PaidAmount = source.PaidAmount, PaidDate = source.PaidDate, DueDate = source.DueDate,
            Status = source.Status, Note = source.Note
        };

        private static void CopyEditableFields(TuitionFee source, TuitionFee target)
        {
            target.Credits = source.Credits;
            target.TotalAmount = source.TotalAmount;
            target.DiscountAmount = source.DiscountAmount;
            target.DiscountReason = source.DiscountReason;
            target.PaidAmount = source.PaidAmount;
            target.PaidDate = source.PaidDate;
            target.DueDate = source.DueDate;
            target.Status = source.Status;
            target.Note = source.Note;
        }

        // ─── Statistics ───────────────────────────────────────────────────────

        public TuitionStatistics GetStatistics(int semesterId, string semesterName)
        {
            var fees = GetBySemester(semesterId);
            return new TuitionStatistics
            {
                SemesterId = semesterId,
                SemesterName = semesterName,
                TotalStudents = fees.Count,
                PaidCount = fees.Count(f => f.Status == PaymentStatus.Paid),
                PartialCount = fees.Count(f => f.Status == PaymentStatus.PartiallyPaid),
                UnpaidCount = fees.Count(f => f.Status == PaymentStatus.Unpaid),
                OverdueCount = fees.Count(f => f.Status == PaymentStatus.Overdue),
                TotalAmount = fees.Sum(f => f.TotalAmount),
                TotalPaid = fees.Sum(f => f.PaidAmount),
            };
        }

        public int GetMaxId() => _fees.Any() ? _fees.Max(f => f.Id) : 0;
    }
}
