using System;

namespace K26_DotNet.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string ClassName { get; set; } = string.Empty;

        public Student()
        {
        }

        public Student(int id, string fullName, string email, string phoneNumber, DateTime dateOfBirth, string className)
            : this(id, $"SV{id:D4}", fullName, email, phoneNumber, dateOfBirth, className)
        {
        }

        public Student(int id, string studentCode, string fullName, string email, string phoneNumber, DateTime dateOfBirth, string className)
        {
            Id = id;
            StudentCode = studentCode;
            FullName = fullName;
            Email = email;
            PhoneNumber = phoneNumber;
            DateOfBirth = dateOfBirth;
            ClassName = className;
        }

        public override string ToString()
        {
            return $"Mã SV: {StudentCode}, Tên: {FullName}, Email: {Email}, Điện thoại: {PhoneNumber}, Sinh: {DateOfBirth:dd/MM/yyyy}, Lớp: {ClassName}";
        }
    }
}
