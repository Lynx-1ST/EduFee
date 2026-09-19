using System;

namespace K26_DotNet.Models
{
    /// <summary>
    /// Model biên lai thu tiền học phí
    /// </summary>
    public class PaymentReceipt
    {
        public int Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty; // Mã biên lai: BL-2026-0001
        public int TuitionFeeId { get; set; }
        public int StudentId { get; set; }
        public int SemesterId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Chuyển khoản"; // Tiền mặt, Chuyển khoản, Thẻ
        public string PayerName { get; set; } = string.Empty;      // Người nộp tiền
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Note { get; set; } = string.Empty;
        public string StudentNameSnapshot { get; set; } = string.Empty;
        public string StudentCodeSnapshot { get; set; } = string.Empty;
        public string ClassNameSnapshot { get; set; } = string.Empty;
        public string SemesterNameSnapshot { get; set; } = string.Empty;
        public decimal TotalTuitionSnapshot { get; set; }
        public decimal TotalPaidAfterSnapshot { get; set; }
        public decimal RemainingAfterSnapshot { get; set; }
        public DateTime? DueDateSnapshot { get; set; }

        public PaymentReceipt() { }

        public PaymentReceipt(int id, string receiptCode, int tuitionFeeId, int studentId, int semesterId,
            decimal amount, string paymentMethod, string payerName, string note = "")
        {
            Id = id;
            ReceiptCode = receiptCode;
            TuitionFeeId = tuitionFeeId;
            StudentId = studentId;
            SemesterId = semesterId;
            Amount = amount;
            PaymentMethod = paymentMethod;
            PayerName = payerName;
            PaymentDate = DateTime.Now;
            Note = note ?? string.Empty;
        }
    }
}
