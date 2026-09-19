using System;

namespace K26_DotNet.Models
{
    public class Semester
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;          // VD: "HK1 2024-2025"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime DueDate { get; set; }     // Hạn nộp học phí
        public bool IsActive { get; set; }

        public Semester() { }

        public Semester(int id, string name, DateTime startDate, DateTime endDate, DateTime dueDate, bool isActive = false)
        {
            Id = id;
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            DueDate = dueDate;
            IsActive = isActive;
        }

        public override string ToString() => Name;
    }
}
