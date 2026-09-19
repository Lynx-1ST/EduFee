using System;

namespace K26_DotNet.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string ClassName { get; set; } = string.Empty;

        public Student()
        {
        }

        public Student(int id, string fullName, string email, string phoneNumber, DateTime dateOfBirth, string className)
        {
            Id = id;
            FullName = fullName;
            Email = email;
            PhoneNumber = phoneNumber;
            DateOfBirth = dateOfBirth;
            ClassName = className;
        }

        public override string ToString()
        {
            return $"ID: {Id}, Tên: {FullName}, Email: {Email}, Điện thoại: {PhoneNumber}, Sinh: {DateOfBirth:dd/MM/yyyy}, Lớp: {ClassName}";
        }
    }
}
