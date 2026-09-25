using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services
{
    public class SemesterService
    {
        private readonly string _filePath;
        private readonly SqliteRepository? _repository;
        private List<Semester> _semesters;

        public SemesterService(string filePath = "semesters.json")
        {
            _filePath = filePath;
            _semesters = new List<Semester>();
            LoadSemesters();
            if (!File.Exists(_filePath)) SeedData();
        }

        public SemesterService(SqlDatabaseContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            _filePath = string.Empty;
            _repository = new SqliteRepository(dbContext);
            _semesters = new List<Semester>();
            LoadSemesters();
        }

        private void SeedData()
        {
            _semesters.AddRange([
                new Semester(1, "HK1 2024-2025", new DateTime(2024, 9, 2), new DateTime(2025, 1, 17), new DateTime(2024, 9, 30)),
                new Semester(2, "HK2 2024-2025", new DateTime(2025, 2, 10), new DateTime(2025, 6, 20), new DateTime(2025, 2, 28)),
                new Semester(3, "HK1 2025-2026", new DateTime(2025, 9, 1), new DateTime(2026, 1, 16), new DateTime(2025, 9, 30)) { IsActive = true }
            ]);
            SaveSemesters();
        }

        public void LoadSemesters()
        {
            try
            {
                if (_repository != null)
                {
                    _semesters = _repository.LoadAll().Semesters;
                    return;
                }
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json)) throw new JsonException("Tệp dữ liệu trống hoặc bị hỏng.");
                    _semesters = JsonSerializer.Deserialize<List<Semester>>(json) ?? throw new JsonException("Dữ liệu không được là null.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tải học kỳ: {ex.Message}", ex);
            }
        }

        public void SaveSemesters()
        {
            if (_repository != null) return;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(_semesters, options));
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu học kỳ: {ex.Message}", ex);
            }
        }

        public List<Semester> GetAll() => _semesters.OrderByDescending(s => s.StartDate).Select(CloneSemester).ToList();
        public Semester? GetById(int id) => _semesters.Where(s => s.Id == id).Select(CloneSemester).FirstOrDefault();
        public Semester? GetActive() => _semesters.Where(s => s.IsActive).Select(CloneSemester).FirstOrDefault();

        public void Add(Semester semester)
        {
            ArgumentNullException.ThrowIfNull(semester);
            ValidateSemester(semester);
            if (_semesters.Any(s => s.Name.Equals(semester.Name, StringComparison.OrdinalIgnoreCase)))
                throw new Exception($"Học kỳ '{semester.Name}' đã tồn tại!");
            semester.Id = _semesters.Any() ? _semesters.Max(s => s.Id) + 1 : 1;
            if (_repository != null)
            {
                _repository.AddSemester(semester);
                _semesters.Add(semester);
                return;
            }
            var previous = _semesters;
            _semesters = [.. _semesters, semester];
            try { SaveSemesters(); }
            catch { _semesters = previous; throw; }
        }

        public void Update(Semester semester)
        {
            ArgumentNullException.ThrowIfNull(semester);
            ValidateSemester(semester);
            var existing = _semesters.FirstOrDefault(s => s.Id == semester.Id)
                ?? throw new Exception($"Không tìm thấy học kỳ ID {semester.Id}");
            if (_repository != null)
            {
                _repository.UpdateSemester(semester);
                CopySemester(semester, existing);
                return;
            }
            var previous = CloneSemesters(_semesters);
            CopySemester(semester, existing);
            try { SaveSemesters(); }
            catch { _semesters = previous; throw; }
        }

        public void Delete(int id)
        {
            var semester = _semesters.FirstOrDefault(s => s.Id == id)
                ?? throw new Exception($"Không tìm thấy học kỳ ID {id}");
            if (_repository != null)
            {
                _repository.DeleteSemester(id);
                _semesters.Remove(semester);
                return;
            }
            var previous = _semesters;
            _semesters = _semesters.Where(s => s.Id != id).ToList();
            try { SaveSemesters(); }
            catch { _semesters = previous; throw; }
        }

        public void SetActive(int id)
        {
            if (!_semesters.Any(s => s.Id == id)) throw new Exception($"Không tìm thấy học kỳ ID {id}");
            if (_repository != null)
            {
                _repository.SetActiveSemester(id);
                foreach (var semester in _semesters) semester.IsActive = semester.Id == id;
                return;
            }
            var previous = CloneSemesters(_semesters);
            foreach (var semester in _semesters) semester.IsActive = semester.Id == id;
            try { SaveSemesters(); }
            catch { _semesters = previous; throw; }
        }

        public int GetMaxId() => _semesters.Any() ? _semesters.Max(s => s.Id) : 0;

        private static void ValidateSemester(Semester semester)
        {
            if (string.IsNullOrWhiteSpace(semester.Name)) throw new ArgumentException("Tên học kỳ không được để trống.", nameof(semester.Name));
            if (semester.StartDate.Date > semester.EndDate.Date) throw new ArgumentException("Ngày kết thúc không thể trước ngày bắt đầu.");
            if (semester.DueDate.Date < semester.StartDate.Date || semester.DueDate.Date > semester.EndDate.Date)
                throw new ArgumentException("Hạn nộp học phí phải nằm trong thời gian học kỳ.", nameof(semester.DueDate));
            if (semester.TuitionPerCredit <= 0 || decimal.Truncate(semester.TuitionPerCredit) != semester.TuitionPerCredit)
                throw new ArgumentException("Đơn giá tín chỉ phải là số nguyên VNĐ lớn hơn 0.", nameof(semester.TuitionPerCredit));
        }

        private static void CopySemester(Semester source, Semester destination)
        {
            destination.Name = source.Name;
            destination.StartDate = source.StartDate;
            destination.EndDate = source.EndDate;
            destination.DueDate = source.DueDate;
            destination.IsActive = source.IsActive;
            destination.TuitionPerCredit = source.TuitionPerCredit;
        }

        private static Semester CloneSemester(Semester s) => new()
        {
            Id = s.Id, Name = s.Name, StartDate = s.StartDate, EndDate = s.EndDate,
            DueDate = s.DueDate, IsActive = s.IsActive, TuitionPerCredit = s.TuitionPerCredit
        };

        private static List<Semester> CloneSemesters(IEnumerable<Semester> semesters) => semesters.Select(CloneSemester).ToList();
    }
}
