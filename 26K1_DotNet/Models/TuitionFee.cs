using System;

namespace K26_DotNet.Models
{
    public enum PaymentStatus
    {
        Unpaid,           // Chưa nộp
        PartiallyPaid,    // Nộp một phần
        Paid,             // Đã nộp đủ
        Overdue           // Quá hạn
    }

    public class TuitionFee
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SemesterId { get; set; }
        public int Credits { get; set; }              // Số tín chỉ
        public decimal TotalAmount { get; set; }      // Học phí phải nộp (sau khi trừ miễn giảm)
        public decimal DiscountAmount { get; set; } = 0; // Tiền miễn giảm / học bổng
        public string DiscountReason { get; set; } = string.Empty; // Diện miễn giảm (Học bổng, hộ nghèo...)
        public decimal PaidAmount { get; set; }       // Đã nộp
        public DateTime? PaidDate { get; set; }       // Ngày nộp gần nhất
        public DateTime? DueDate { get; set; }        // Hạn nộp học phí
        public PaymentStatus Status { get; set; }
        public string Note { get; set; } = string.Empty;

        // Calculated properties
        public decimal OriginalAmount => TotalAmount + DiscountAmount;
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public bool IsFullyPaid => PaidAmount >= TotalAmount;

        public TuitionFee() { }

        public TuitionFee(int id, int studentId, int semesterId, int credits, decimal pricePerCredit = 620_000m, string note = "", DateTime? dueDate = null, decimal discountAmount = 0, string discountReason = "")
        {
            Id = id;
            StudentId = studentId;
            SemesterId = semesterId;
            Credits = credits;
            DiscountAmount = discountAmount;
            DiscountReason = discountReason ?? string.Empty;
            TotalAmount = Math.Max(0, (credits * pricePerCredit) - discountAmount);
            PaidAmount = 0;
            PaidDate = null;
            DueDate = dueDate;
            Status = PaymentStatus.Unpaid;
            Note = note ?? string.Empty;
        }

        /// <summary>
        /// Cập nhật trạng thái dựa trên số tiền đã nộp và hạn nộp
        /// </summary>
        public void UpdateStatus(DateTime? semesterDueDate = null, DateTime? today = null)
        {
            var effectiveDueDate = DueDate ?? semesterDueDate;
            if (PaidAmount >= TotalAmount)
            {
                Status = PaymentStatus.Paid;
            }
            else if (effectiveDueDate.HasValue && (today ?? DateTime.Today).Date > effectiveDueDate.Value.Date)
            {
                Status = PaymentStatus.Overdue;
            }
            else if (PaidAmount > 0)
            {
                Status = PaymentStatus.PartiallyPaid;
            }
            else
            {
                Status = PaymentStatus.Unpaid;
            }
        }

        public string StatusDisplayText => Status switch
        {
            PaymentStatus.Unpaid => "Chưa nộp",
            PaymentStatus.PartiallyPaid => "Nộp 1 phần",
            PaymentStatus.Paid => "Đã nộp đủ",
            PaymentStatus.Overdue => "Quá hạn",
            _ => "Không xác định"
        };
    }
}
