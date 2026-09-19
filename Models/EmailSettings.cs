using System;

namespace K26_DotNet.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderPassword { get; set; } = "";
        public string SenderDisplayName { get; set; } = "Trường Đại học Mỏ - Địa chất (Phòng Tài Vụ)";
        public bool IsSimulationMode { get; set; } = true; // Mặc định chế độ giả lập an toàn để xem trước email
        public string LastError { get; set; } = "";
    }
}
