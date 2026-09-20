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
            ValidateFee(fee, allowExistingPayment: false);
            if (ExistsForStudentInSemester(fee.StudentId, fee.SemesterId))
                throw new Exception("Sinh viên này đã có phiếu học phí trong học kỳ đã chọn!");

            int previousId = fee.Id;
            fee.Id = _fees.Any() ? _fees.Max(f => f.Id) + 1 : 1;
            fee.UpdateStatus();
            try
            {
                if (_repository != null) _repository.AddTuitionFees([fee]);
                _fees.Add(fee);
                if (_repository == null) SaveFees();
            }
            catch { _fees.Remove(fee); fee.Id = previousId; throw; }
        }

        public void AddRange(IEnumerable<TuitionFee> fees)
        {
            if (fees == null) throw new ArgumentNullException(nameof(fees));
            // Materialize and validate before mutating the cache.  This also makes an
            // iterator which throws midway through enumeration leave the cache intact.
            var requested = fees.ToList();
            foreach (var fee in requested)
            {
                if (fee == null) throw new ArgumentException("Danh sách học phí có phần tử rỗng.", nameof(fees));
                ValidateFee(fee, allowExistingPayment: false);
            }

            var additions = new List<TuitionFee>();
            var assignedIds = new List<(TuitionFee Fee, int PreviousId)>();
            var keys = _fees.Select(f => (f.StudentId, f.SemesterId)).ToHashSet();
            int nextId = _fees.Any() ? _fees.Max(f => f.Id) + 1 : 1;
            foreach (var fee in requested)
            {
                if (!keys.Add((fee.StudentId, fee.SemesterId)))
                    continue;

                assignedIds.Add((fee, fee.Id));
                fee.Id = nextId++;
                fee.UpdateStatus();
                additions.Add(fee);
            }
            try
            {
                if (_repository != null) _repository.AddTuitionFees(additions);
                _fees.AddRange(additions);
                if (_repository == null) SaveFees();
            }
            catch
            {
                foreach (var fee in additions) _fees.Remove(fee);
                foreach (var assigned in assignedIds) assigned.Fee.Id = assigned.PreviousId;
                throw;
            }
        }

        public void Update(TuitionFee fee)
        {
            if (fee == null) throw new ArgumentNullException(nameof(fee));
            var existing = _fees.FirstOrDefault(f => f.Id == fee.Id)
                ?? throw new Exception($"Không tìm thấy học phí ID {fee.Id}");
            ValidateFee(fee, allowExistingPayment: true);
            if (fee.PaidAmount != existing.PaidAmount || fee.PaidDate != existing.PaidDate)
                throw new InvalidOperationException("Không thể sửa số tiền hoặc ngày thu trực tiếp. Hãy lập biên lai thu tiền.");
            if (fee.TotalAmount < existing.PaidAmount)
                throw new ArgumentException("Tổng học phí không được nhỏ hơn số tiền đã thu.");
            var previous = Clone(existing);
            CopyEditableFields(fee, existing);
            existing.UpdateStatus();
            try
            {
                if (_repository != null) existing.Status = _repository.UpdateTuitionFee(existing);
                else SaveFees();
            }
            catch
            {
                CopyEditableFields(previous, existing);
                existing.Status = previous.Status;
                throw;
            }
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

            if (amount <= 0 || amount != decimal.Truncate(amount) || amount > long.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền thanh toán phải là số nguyên VND dương hợp lệ.");
            if (string.IsNullOrWhiteSpace(payerName))
                throw new ArgumentException("Thiếu người nộp tiền.", nameof(payerName));
            var fee = _fees.FirstOrDefault(current => current.Id == id)
                ?? throw new InvalidOperationException($"Không tìm thấy học phí ID {id} trong dữ liệu hiện tại.");
            if (amount > fee.RemainingAmount)
                throw new InvalidOperationException($"Số tiền vượt quá số còn lại ({fee.RemainingAmount:N0} VNĐ).");

            var receipt = _repository.RecordPayment(id, amount, paymentMethod, payerName, note, dueDate);
            // The database transaction is already committed.  Only use the receipt's
            // ledger snapshot, so a stale input amount cannot make the cache diverge.
            fee.PaidAmount = receipt.TotalPaidAfterSnapshot;
            fee.PaidDate = receipt.PaymentDate;
            fee.UpdateStatus(receipt.DueDateSnapshot);
            receiptService.AcceptCommittedReceipt(receipt);
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
            target.DueDate = source.DueDate;
            target.Note = source.Note;
        }

        private static void ValidateFee(TuitionFee fee, bool allowExistingPayment)
        {
            if (fee.Id < 0)
                throw new ArgumentOutOfRangeException(nameof(fee.Id), "Mã phiếu học phí không hợp lệ.");
            if (fee.StudentId <= 0 || fee.SemesterId <= 0)
                throw new ArgumentOutOfRangeException(nameof(fee), "Mã sinh viên và học kỳ phải lớn hơn 0.");
            if (fee.Credits <= 0)
                throw new ArgumentOutOfRangeException(nameof(fee.Credits), "Số tín chỉ phải lớn hơn 0.");
            if (fee.TotalAmount < 0 || fee.DiscountAmount < 0 || fee.PaidAmount < 0 || fee.PaidAmount > fee.TotalAmount)
                throw new ArgumentException("Số tiền học phí, miễn giảm hoặc đã thu không hợp lệ.", nameof(fee));
            if (!allowExistingPayment && (fee.PaidAmount != 0 || fee.PaidDate.HasValue))
                throw new InvalidOperationException("Phiếu học phí mới không được có số đã thu; hãy ghi nhận bằng biên lai.");
            ValidateVnd(fee.TotalAmount, nameof(fee.TotalAmount));
            ValidateVnd(fee.DiscountAmount, nameof(fee.DiscountAmount));
            ValidateVnd(fee.PaidAmount, nameof(fee.PaidAmount));
            _ = checked(fee.TotalAmount + fee.DiscountAmount);
        }

        private static void ValidateVnd(decimal value, string name)
        {
            if (value != decimal.Truncate(value) || value > long.MaxValue)
                throw new ArgumentOutOfRangeException(name, "Số tiền VND phải là số nguyên trong phạm vi hợp lệ.");
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
                PaidCount = fees.Count(f => f.Status is PaymentStatus.Paid or PaymentStatus.LatePaid),
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
