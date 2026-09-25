using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Threading.Tasks;
using K26_DotNet.Models;

namespace K26_DotNet.Services
{
    public class EmailService
    {
        private readonly string _settingsFilePath;
        public EmailSettings Settings { get; private set; }

        public EmailService(string settingsFilePath = "email_settings.json")
        {
            _settingsFilePath = settingsFilePath;
            Settings = new EmailSettings();
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var loaded = JsonSerializer.Deserialize<EmailSettings>(json)
                            ?? throw new JsonException("Cấu hình email không được là null.");
                        if (loaded.SenderPassword.StartsWith("dpapi:", StringComparison.Ordinal))
                        {
                            var encrypted = Convert.FromBase64String(loaded.SenderPassword[6..]);
                            loaded.SenderPassword = Encoding.UTF8.GetString(ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
                        }
                        Settings = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Không thể đọc cấu hình email. Mật khẩu đã mã hóa chỉ dùng được với tài khoản Windows đã lưu nó.", ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var stored = JsonSerializer.SerializeToNode(Settings)!.AsObject();
                stored[nameof(EmailSettings.SenderPassword)] = string.IsNullOrEmpty(Settings.SenderPassword)
                    ? ""
                    : "dpapi:" + Convert.ToBase64String(ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(Settings.SenderPassword), null, DataProtectionScope.CurrentUser));
                string json = stored.ToJsonString(opts);
                K26_DotNet.Data.AtomicFile.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lưu cấu hình email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gửi email biên lai điện tử khi sinh viên thanh toán thành công
        /// </summary>
        public async Task<(bool Success, string Message)> SendReceiptEmailAsync(
            Student student, Semester semester, TuitionFee fee, PaymentReceipt receipt)
        {
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                return (false, "Sinh viên chưa có địa chỉ email trong hồ sơ!");
            }

            string subject = $"[EduFee] Biên lai thu học phí điện tử - {receipt.ReceiptCode} - {student.FullName}";
            string bodyHtml = GenerateReceiptEmailHtml(student, semester, fee, receipt);

            return await SendEmailAsync(student.Email, student.FullName, subject, bodyHtml);
        }

        /// <summary>
        /// Gửi email giấy báo nợ học phí kèm thông tin chuyển khoản ngân hàng
        /// </summary>
        public async Task<(bool Success, string Message)> SendDebtNoticeEmailAsync(
            Student student, Semester semester, TuitionFee fee)
        {
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                return (false, "Sinh viên chưa có địa chỉ email trong hồ sơ!");
            }

            string subject = $"[EduFee] Giấy báo nợ học phí - {semester.Name} - {student.FullName}";
            string bodyHtml = GenerateDebtNoticeEmailHtml(student, semester, fee);

            return await SendEmailAsync(student.Email, student.FullName, subject, bodyHtml);
        }

        /// <summary>
        /// Gửi thử nghiệm kết nối SMTP
        /// </summary>
        public async Task<(bool Success, string Message)> TestSmtpConnectionAsync(string testToEmail)
        {
            string subject = "[EduFee] Thử nghiệm kết nối SMTP thành công!";
            string bodyHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b;'>
                    <h2 style='color: #4f46e5;'>🎉 Kết Nối SMTP Thành Công!</h2>
                    <p>Hệ thống Quản lý học phí EduFee PRO đã kết nối thành công tới máy chủ email của bạn.</p>
                    <ul>
                        <li><b>Máy chủ SMTP:</b> {Settings.SmtpHost}:{Settings.Port}</li>
                        <li><b>Email gửi:</b> {Settings.SenderEmail}</li>
                        <li><b>Thời gian gửi:</b> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</li>
                    </ul>
                    <p style='color: #64748b; font-size: 13px;'>Đây là email kiểm thử tự động, bạn không cần phản hồi.</p>
                </div>";

            return await SendEmailAsync(testToEmail, "Tester", subject, bodyHtml);
        }

        private async Task<(bool Success, string Message)> SendEmailAsync(
            string toEmail, string toName, string subject, string bodyHtml)
        {
            if (Settings.IsSimulationMode)
            {
                // Chế độ giả lập an toàn (Simulation Mode): Ghi log xem trước
                try { LogSimulationEmail(toEmail, subject, bodyHtml); }
                catch (Exception ex) { return (false, $"Không thể ghi nhật ký email: {ex.Message}"); }
                await Task.Delay(400); // Giả lập độ trễ mạng nhẹ
                return (true, $"[Chế độ giả lập] Đã tạo và ghi nhận email gửi đến {toEmail} thành công!");
            }

            if (string.IsNullOrWhiteSpace(Settings.SenderEmail) || string.IsNullOrWhiteSpace(Settings.SenderPassword))
            {
                return (false, "Chưa cấu hình tài khoản Email gửi (Sender Email/Password) trong Cài đặt!");
            }

            try
            {
                using var client = new SmtpClient(Settings.SmtpHost, Settings.Port)
                {
                    EnableSsl = Settings.EnableSsl,
                    Credentials = new NetworkCredential(Settings.SenderEmail.Trim(), Settings.SenderPassword.Trim()),
                    Timeout = 15000
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(Settings.SenderEmail.Trim(), Settings.SenderDisplayName),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mail.To.Add(new MailAddress(toEmail.Trim(), toName));

                await client.SendMailAsync(mail);
                return (true, $"Đã gửi email thành công đến {toEmail}!");
            }
            catch (Exception ex)
            {
                Settings.LastError = ex.Message;
                return (false, $"Lỗi khi gửi email qua SMTP: {ex.Message}");
            }
        }

        private void LogSimulationEmail(string toEmail, string subject, string bodyHtml)
        {
            string logEntry = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] TO: {toEmail.ReplaceLineEndings(" ")} | SUBJECT: {subject.ReplaceLineEndings(" ")}\n";
            File.AppendAllText("simulated_emails.log", logEntry, Encoding.UTF8);
        }

        private string GenerateReceiptEmailHtml(Student student, Semester semester, TuitionFee fee, PaymentReceipt receipt)
        {
            string studentName = !string.IsNullOrWhiteSpace(receipt.StudentNameSnapshot) ? receipt.StudentNameSnapshot : student.FullName;
            string studentCode = !string.IsNullOrWhiteSpace(receipt.StudentCodeSnapshot) ? receipt.StudentCodeSnapshot : student.StudentCode;
            string className = !string.IsNullOrWhiteSpace(receipt.ClassNameSnapshot) ? receipt.ClassNameSnapshot : student.ClassName;
            string semesterName = !string.IsNullOrWhiteSpace(receipt.SemesterNameSnapshot) ? receipt.SemesterNameSnapshot : semester.Name;
            decimal totalAmount = receipt.TotalTuitionSnapshot > 0 ? receipt.TotalTuitionSnapshot : fee.TotalAmount;
            decimal paidAmount = receipt.TotalPaidAfterSnapshot > 0 ? receipt.TotalPaidAfterSnapshot : fee.PaidAmount;
            decimal remainingAmount = receipt.TotalTuitionSnapshot > 0 ? receipt.RemainingAfterSnapshot : fee.RemainingAmount;
            var due = receipt.DueDateSnapshot ?? fee.DueDate ?? semester.DueDate;

            string discountSection = fee.DiscountAmount > 0
                ? $"<tr><td style='padding:8px 0;color:#64748b;'>Học phí gốc:</td><td style='padding:8px 0;font-weight:600;text-align:right;'>{fee.OriginalAmount:N0} VNĐ</td></tr>" +
                  $"<tr><td style='padding:8px 0;color:#16a34a;'>Miễn giảm / Học bổng:</td><td style='padding:8px 0;font-weight:600;color:#16a34a;text-align:right;'>-{fee.DiscountAmount:N0} VNĐ ({fee.DiscountReason})</td></tr>"
                : "";

            string remainingColor = remainingAmount > 0 ? "#dc2626" : "#16a34a";
            string remainingText = remainingAmount > 0 ? $"{remainingAmount:N0} VNĐ" : "0 VNĐ (Đã hoàn tất)";

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; color: #1e293b; }}
  .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
  .header {{ background: linear-gradient(135deg, #16a34a 0%, #059669 100%); color: white; padding: 28px 24px; text-align: center; }}
  .header h1 {{ margin: 0 0 6px 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px; }}
  .header p {{ margin: 0; font-size: 13px; opacity: 0.9; }}
  .amount-card {{ background: #f0fdf4; border: 1.5px solid #86efac; border-radius: 10px; margin: 20px 24px; padding: 18px; text-align: center; }}
  .amount-val {{ font-size: 26px; font-weight: 800; color: #15803d; margin: 6px 0 0 0; }}
  .body-content {{ padding: 0 24px 24px 24px; }}
  table {{ width: 100%; border-collapse: collapse; font-size: 14px; }}
  .divider {{ height: 1px; background-color: #e2e8f0; margin: 16px 0; }}
  .footer {{ background: #f1f5f9; padding: 16px 24px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <p style='text-transform: uppercase; letter-spacing: 1px; font-size: 11px; margin-bottom: 4px;'>EduFee · Student Tuition Management</p>
      <h1>XÁC NHẬN NỘP HỌC PHÍ THÀNH CÔNG</h1>
      <p>Mã biên lai điện tử: <b>{receipt.ReceiptCode}</b></p>
    </div>

    <div class='amount-card'>
      <div style='font-size: 12px; font-weight: bold; color: #166534; text-transform: uppercase;'>SỐ TIỀN ĐÃ THANH TOÁN</div>
      <div class='amount-val'>+{receipt.Amount:N0} VNĐ</div>
      <div style='font-size: 12px; color: #15803d; margin-top: 4px;'>Hình thức: {receipt.PaymentMethod}</div>
    </div>

    <div class='body-content'>
      <p style='font-size: 14px;'>Kính gửi sinh viên <b>{studentName}</b>,</p>
      <p style='font-size: 13px; color: #475569;'>Hệ thống EduFee xác nhận đã nhận được khoản thanh toán học phí của bạn với các thông tin chi tiết dưới đây:</p>

      <table>
        <tr><td style='padding:6px 0;color:#64748b;'>Mã sinh viên:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{studentCode}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Họ và tên:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{studentName}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Lớp:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{className}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Học kỳ:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{semesterName}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Người nộp tiền:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{receipt.PayerName}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Thời gian giao dịch:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{receipt.PaymentDate:dd/MM/yyyy HH:mm:ss}</td></tr>
      </table>

      <div class='divider'></div>

      <div style='font-weight: bold; font-size: 13px; color: #334155; margin-bottom: 8px;'>TỔNG KẾT TÀI CHÍNH KỲ NÀY:</div>
      <table>
        {discountSection}
        <tr><td style='padding:6px 0;color:#64748b;'>Học phí phải nộp:</td><td style='padding:6px 0;font-weight:700;text-align:right;'>{totalAmount:N0} VNĐ</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Đã nộp tổng cộng:</td><td style='padding:6px 0;font-weight:700;color:#16a34a;text-align:right;'>{paidAmount:N0} VNĐ</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Còn lại phải nộp:</td><td style='padding:6px 0;font-weight:700;color:{remainingColor};text-align:right;'>{remainingText}</td></tr>
        <tr><td style='padding:6px 0;color:#64748b;'>Hạn nộp học phí:</td><td style='padding:6px 0;font-weight:600;text-align:right;'>{due:dd/MM/yyyy}</td></tr>
      </table>
    </div>

    <div class='footer'>
      <p style='margin: 0 0 4px 0;'>Biên lai điện tử có giá trị xác nhận tương đương biên lai giấy.</p>
      <p style='margin: 0;'>Mọi thắc mắc xin vui lòng liên hệ <b>EduFee Student Support</b> để được hỗ trợ.</p>
    </div>
  </div>
</body>
</html>";
        }

        private string GenerateDebtNoticeEmailHtml(Student student, Semester semester, TuitionFee fee)
        {
            var due = fee.DueDate ?? semester.DueDate;
            string transferSyntax = $"HP {student.StudentCode} {semester.Name.Replace(" ", "")}";

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; color: #1e293b; }}
  .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06); border: 1px solid #e2e8f0; }}
  .header {{ background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%); color: white; padding: 28px 24px; text-align: center; }}
  .header h1 {{ margin: 0 0 6px 0; font-size: 20px; font-weight: 700; }}
  .debt-card {{ background: #fef2f2; border: 1.5px solid #fca5a5; border-radius: 10px; margin: 20px 24px; padding: 18px; text-align: center; }}
  .debt-val {{ font-size: 26px; font-weight: 800; color: #b91c1c; margin: 6px 0 0 0; }}
  .bank-card {{ background: #eff6ff; border: 1.5px dashed #60a5fa; border-radius: 10px; padding: 16px; margin: 18px 0; }}
  .body-content {{ padding: 0 24px 24px 24px; }}
  table {{ width: 100%; border-collapse: collapse; font-size: 14px; }}
  .divider {{ height: 1px; background-color: #e2e8f0; margin: 16px 0; }}
  .footer {{ background: #f1f5f9; padding: 16px 24px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <p style='text-transform: uppercase; letter-spacing: 1px; font-size: 11px; margin-bottom: 4px;'>EduFee · Student Tuition Management</p>
      <h1>GIẤY BÁO NỢ HỌC PHÍ</h1>
      <p>Học kỳ: <b>{semester.Name}</b></p>
    </div>

    <div class='debt-card'>
      <div style='font-size: 12px; font-weight: bold; color: #991b1b; text-transform: uppercase;'>SỐ TIỀN CÒN NỢ CẦN THANH TOÁN</div>
      <div class='debt-val'>{fee.RemainingAmount:N0} VNĐ</div>
      <div style='font-size: 12px; color: #b91c1c; margin-top: 4px;'>Hạn nộp cuối cùng: <b>{due:dd/MM/yyyy}</b></div>
    </div>

    <div class='body-content'>
      <p style='font-size: 14px;'>Kính gửi sinh viên <b>{student.FullName}</b> (Lớp {student.ClassName}),</p>
      <p style='font-size: 13px; color: #475569;'>Hệ thống ghi nhận bạn hiện vẫn còn khoản nợ học phí cho học kỳ <b>{semester.Name}</b>. Vui lòng hoàn tất nghĩa vụ học phí đúng thời hạn để bảo đảm quyền lợi học tập và dự thi.</p>

      <div class='bank-card'>
        <div style='font-weight: bold; font-size: 13px; color: #1e40af; margin-bottom: 8px;'>🏦 THÔNG TIN CHUYỂN KHOẢN NGÂN HÀNG:</div>
        <table>
          <tr><td style='padding:4px 0;color:#475569;'>Ngân hàng:</td><td style='padding:4px 0;font-weight:600;'>VietinBank (Chi nhánh Hà Nội)</td></tr>
          <tr><td style='padding:4px 0;color:#475569;'>Số tài khoản:</td><td style='padding:4px 0;font-weight:700;color:#1e40af;font-size:15px;'>1028-8888-9999</td></tr>
          <tr><td style='padding:4px 0;color:#475569;'>Chủ tài khoản:</td><td style='padding:4px 0;font-weight:600;'>TRUONG DAI HOC MO DIA CHAT</td></tr>
          <tr><td style='padding:4px 0;color:#475569;'>Nội dung CK:</td><td style='padding:4px 0;font-weight:700;color:#b91c1c;'>{transferSyntax}</td></tr>
        </table>
      </div>

      <p style='font-size: 13px; color: #64748b;'>Sau khi chuyển khoản thành công, hệ thống sẽ tự động gạch nợ và gửi email xác nhận biên lai cho bạn.</p>
    </div>

    <div class='footer'>
      <p style='margin: 0;'>EduFee · Student Tuition Management</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
