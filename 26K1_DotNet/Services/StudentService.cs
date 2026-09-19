using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services
{
    public class StudentService
    {
        private readonly string _filePath;
        private readonly SqliteRepository? _repository;
        private List<Student> _students;

        public StudentService(string filePath = "students.json")
        {
            _filePath = filePath;
            _students = new List<Student>();
            LoadStudents();
        }

        public StudentService(SqlDatabaseContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            _filePath = string.Empty;
            _repository = new SqliteRepository(dbContext);
            _students = new List<Student>();
            LoadStudents();
        }

        public void LoadStudents()
        {
            try
            {
                if (_repository != null)
                {
                    _students = _repository.LoadAll().Students;
                    return;
                }
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json)) throw new JsonException("Tệp dữ liệu trống hoặc bị hỏng.");
                    _students = JsonSerializer.Deserialize<List<Student>>(json) ?? throw new JsonException("Dữ liệu không được là null.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tải sinh viên: {ex.Message}", ex);
            }
        }

        public void SaveStudents()
        {
            if (_repository != null) return;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(_students, options));
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu sinh viên: {ex.Message}", ex);
            }
        }

        public List<Student> GetAllStudents() => _students.OrderBy(s => s.Id).Select(CloneStudent).ToList();
        public Student? GetStudentById(int id) => _students.Where(s => s.Id == id).Select(CloneStudent).FirstOrDefault();

        public void AddStudent(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);
            if (_students.Any(s => s.Id == student.Id)) throw new Exception($"Sinh viên với ID {student.Id} đã tồn tại!");
            AddStudents([student]);
        }

        public void AddStudents(IEnumerable<Student> students)
        {
            ArgumentNullException.ThrowIfNull(students);
            var additions = students.ToList();
            var ids = _students.Select(s => s.Id).ToHashSet();
            foreach (var student in additions)
            {
                ArgumentNullException.ThrowIfNull(student);
                ValidateStudent(student);
                if (!ids.Add(student.Id)) throw new ArgumentException($"Mã sinh viên không hợp lệ hoặc bị trùng: {student.Id}");
            }
            if (_repository != null)
            {
                _repository.AddStudents(additions);
                _students.AddRange(additions);
                return;
            }
            var previous = _students;
            _students = [.. _students, .. additions];
            try { SaveStudents(); }
            catch { _students = previous; throw; }
        }

        public void UpdateStudent(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);
            ValidateStudent(student);
            var existingStudent = _students.FirstOrDefault(s => s.Id == student.Id)
                ?? throw new Exception($"Không tìm thấy sinh viên với ID {student.Id}");
            if (_repository != null)
            {
                _repository.UpdateStudent(student);
                CopyStudent(student, existingStudent);
                return;
            }
            var previous = CloneStudents(_students);
            CopyStudent(student, existingStudent);
            try { SaveStudents(); }
            catch { _students = previous; throw; }
        }

        public void DeleteStudent(int id)
        {
            var student = _students.FirstOrDefault(s => s.Id == id)
                ?? throw new Exception($"Không tìm thấy sinh viên với ID {id}");
            if (_repository != null)
            {
                _repository.DeleteStudent(id);
                _students.Remove(student);
                return;
            }
            var previous = _students;
            _students = _students.Where(s => s.Id != id).ToList();
            try { SaveStudents(); }
            catch { _students = previous; throw; }
        }

        public List<Student> SearchByName(string name) => string.IsNullOrEmpty(name) ? GetAllStudents() : _students
            .Where(s => s.FullName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(s => s.Id).Select(CloneStudent).ToList();

        public List<Student> SearchByClass(string className) => string.IsNullOrEmpty(className) ? GetAllStudents() : _students
            .Where(s => s.ClassName.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(s => s.Id).Select(CloneStudent).ToList();

        public List<string> GetDistinctClasses() => _students.Select(s => s.ClassName).Distinct().OrderBy(c => c).ToList();
        public int GetMaxStudentId() => _students.Any() ? _students.Max(s => s.Id) : 0;

        private static void ValidateStudent(Student student)
        {
            if (student.Id <= 0) throw new ArgumentOutOfRangeException(nameof(student.Id), "Mã sinh viên phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(student.FullName)) throw new ArgumentException("Họ tên không được để trống.", nameof(student.FullName));
            if (string.IsNullOrWhiteSpace(student.ClassName)) throw new ArgumentException("Lớp không được để trống.", nameof(student.ClassName));
            if (student.DateOfBirth.Date > DateTime.Today) throw new ArgumentException("Ngày sinh không thể ở tương lai.", nameof(student.DateOfBirth));
            if (!string.IsNullOrWhiteSpace(student.Email) &&
                (!MailAddress.TryCreate(student.Email.Trim(), out var email) || !string.Equals(email.Address, student.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Email không hợp lệ.", nameof(student.Email));
        }

        private static void CopyStudent(Student source, Student destination)
        {
            destination.FullName = source.FullName;
            destination.Email = source.Email;
            destination.PhoneNumber = source.PhoneNumber;
            destination.DateOfBirth = source.DateOfBirth;
            destination.ClassName = source.ClassName;
        }

        private static Student CloneStudent(Student s) => new()
        {
            Id = s.Id, FullName = s.FullName, Email = s.Email, PhoneNumber = s.PhoneNumber,
            DateOfBirth = s.DateOfBirth, ClassName = s.ClassName
        };

        private static List<Student> CloneStudents(IEnumerable<Student> students) => students.Select(CloneStudent).ToList();
    }
}
