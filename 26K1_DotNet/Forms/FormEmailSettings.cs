using System;
using System.Drawing;
using System.Windows.Forms;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormEmailSettings : Form
    {
        private readonly EmailService _emailSvc;

        private CheckBox chkSimulation = null!, chkSsl = null!;
        private TextBox txtHost = null!, txtSenderEmail = null!, txtPassword = null!, txtDisplayName = null!, txtTestEmail = null!;
        private NumericUpDown numPort = null!;
        private Label lblStatus = null!;
        private Button btnTest = null!, btnSave = null!;
        private ErrorProvider _ep = null!;

        public FormEmailSettings(EmailService emailSvc)
        {
            _emailSvc = emailSvc;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            _ep = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
            Text = "Cấu Hình Email Gửi Biên Lai & Báo Nợ";
            ClientSize = new Size(580, 560);
            MinimumSize = new Size(580, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Surface;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Header ──────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = UITheme.Primary,
                Padding = new Padding(20, 0, 20, 0)
            };
            header.Controls.Add(new Label
            {
                Text = "⚙️  CẤU HÌNH GỬI EMAIL TỰ ĐỘNG",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Body ────────────────────────────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 16),
                BackColor = UITheme.Surface
            };

            int y = 14;

            // Simulation mode notice box
            var simBox = new Panel
            {
                Location = new Point(16, y), Size = new Size(492, 60),
                BackColor = UITheme.SimBg
            };
            simBox.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.SimBorder, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, simBox.Width - 1, simBox.Height - 1);
            };

            chkSimulation = new CheckBox
            {
                Text = "Bật Chế Độ Giả Lập / Chạy Thử (Khuyên dùng khi test)",
                Location = new Point(14, 10), AutoSize = true,
                Font = UITheme.FontSmallBold, ForeColor = UITheme.SimText,
                Checked = true, Parent = simBox
            };
            chkSimulation.CheckedChanged += (s, e) => UpdateFieldStates();

            new Label
            {
                Text = "Email sẽ được ghi nhận và tạo mã HTML xem trước, không gửi thật ra internet.",
                Location = new Point(34, 34), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.SimTextDark,
                Parent = simBox
            };
            body.Controls.Add(simBox);
            y += 74;

            // SMTP Server + Port
            AddLabel(body, "Máy chủ SMTP *", y);
            txtHost = new TextBox { Location = new Point(160, y), Size = new Size(200, 28), Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(txtHost);

            numPort = new NumericUpDown { Location = new Point(380, y), Size = new Size(70, 28), Minimum = 1, Maximum = 65535, Value = 587, Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(numPort);

            chkSsl = new CheckBox { Text = "SSL", Location = new Point(460, y + 2), AutoSize = true, Checked = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary };
            body.Controls.Add(chkSsl);
            y += 40;

            // Email người gửi
            AddLabel(body, "Email gửi *", y);
            txtSenderEmail = new TextBox { Location = new Point(160, y), Size = new Size(348, 28), Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(txtSenderEmail);
            y += 40;

            // Mật khẩu ứng dụng
            AddLabel(body, "Mật khẩu ứng dụng *", y);
            txtPassword = new TextBox { Location = new Point(160, y), Size = new Size(348, 28), Font = UITheme.FontBody, UseSystemPasswordChar = true, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(txtPassword);
            y += 24;

            var lblHint = new Label
            {
                Text = "💡 Dùng App Password của Google (không phải mật khẩu đăng nhập Gmail).",
                Location = new Point(160, y), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted
            };
            body.Controls.Add(lblHint);
            y += 26;

            // Tên hiển thị người gửi
            AddLabel(body, "Tên hiển thị", y);
            txtDisplayName = new TextBox { Location = new Point(160, y), Size = new Size(348, 28), Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(txtDisplayName);
            y += 44;

            body.Controls.Add(new Panel { Location = new Point(16, y), Size = new Size(492, 1), BackColor = UITheme.BorderLight });
            y += 14;

            // Test Email section
            AddLabel(body, "Email nhận thử", y);
            txtTestEmail = new TextBox { Location = new Point(160, y), Size = new Size(236, 28), Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt };
            body.Controls.Add(txtTestEmail);

            btnTest = UITheme.GhostBtn("🧪 Gửi Thử", 118, 30);
            btnTest.Location = new Point(408, y - 1);
            btnTest.Click += BtnTest_Click;
            body.Controls.Add(btnTest);
            y += 38;

            lblStatus = new Label
            {
                Location = new Point(160, y), Size = new Size(348, 40),
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                Text = "Sẵn sàng gửi email thông báo."
            };
            body.Controls.Add(lblStatus);

            // ── Footer ──────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.SurfaceAlt };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            btnSave = UITheme.SuccessBtn("💾  Lưu Cấu Hình", 150, 38);
            btnSave.Click += BtnSave_Click;

            var btnCancel = UITheme.GhostBtn("Đóng", 90, 38);
            btnCancel.Click += (s, e) => Close();

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 13, 24, 0),
                BackColor = Color.Transparent
            };
            flowBtns.Controls.AddRange(new Control[] { btnSave, btnCancel });
            footer.Controls.Add(flowBtns);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private static void AddLabel(Panel p, string text, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text + ":", Location = new Point(16, y + 4), AutoSize = true,
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary
            });
        }

        private void UpdateFieldStates()
        {
            bool isSim = chkSimulation.Checked;
            txtHost.Enabled = !isSim;
            numPort.Enabled = !isSim;
            chkSsl.Enabled = !isSim;
            txtSenderEmail.Enabled = !isSim;
            txtPassword.Enabled = !isSim;
        }

        private void LoadData()
        {
            var s = _emailSvc.Settings;
            chkSimulation.Checked = s.IsSimulationMode;
            txtHost.Text = string.IsNullOrEmpty(s.SmtpHost) ? "smtp.gmail.com" : s.SmtpHost;
            numPort.Value = s.Port > 0 ? s.Port : 587;
            chkSsl.Checked = s.EnableSsl;
            txtSenderEmail.Text = s.SenderEmail;
            txtPassword.Text = s.SenderPassword;
            txtDisplayName.Text = string.IsNullOrEmpty(s.SenderDisplayName) ? "Trường Đại học Mỏ - Địa chất (Phòng Tài Vụ)" : s.SenderDisplayName;
            txtTestEmail.Text = s.SenderEmail;
            UpdateFieldStates();
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            string to = txtTestEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(to))
            {
                UiFeedback.ShowWarning("Vui lòng nhập email nhận thử nghiệm!");
                txtTestEmail.Focus();
                return;
            }

            SaveCurrentFieldsToSettings();

            using (UiFeedback.BusyScope(btnTest, lblStatus, "Đang kết nối máy chủ gửi thử nghiệm..."))
            {
                try
                {
                    var res = await _emailSvc.TestSmtpConnectionAsync(to);
                    if (res.Success)
                    {
                        lblStatus.Text = $"✅ {res.Message}";
                        lblStatus.ForeColor = UITheme.Success;
                        UiFeedback.ShowSuccess(res.Message);
                    }
                    else
                    {
                        lblStatus.Text = $"❌ {res.Message}";
                        lblStatus.ForeColor = UITheme.Danger;
                        UiFeedback.ShowError(res.Message);
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ {ex.Message}";
                    lblStatus.ForeColor = UITheme.Danger;
                    UiFeedback.ShowException(ex, "Lỗi kết nối thử nghiệm");
                }
            }
        }

        private void SaveCurrentFieldsToSettings()
        {
            var s = _emailSvc.Settings;
            s.IsSimulationMode = chkSimulation.Checked;
            s.SmtpHost = txtHost.Text.Trim();
            s.Port = (int)numPort.Value;
            s.EnableSsl = chkSsl.Checked;
            s.SenderEmail = txtSenderEmail.Text.Trim();
            s.SenderPassword = txtPassword.Text.Trim();
            s.SenderDisplayName = txtDisplayName.Text.Trim();
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _ep.Clear();

            if (!chkSimulation.Checked)
            {
                bool valid = true;
                if (!UiFeedback.ValidateRequired(_ep, txtHost, "máy chủ SMTP")) valid = false;
                if (!UiFeedback.ValidateRequired(_ep, txtSenderEmail, "email người gửi") ||
                    !UiFeedback.ValidateEmail(_ep, txtSenderEmail)) valid = false;
                if (!UiFeedback.ValidateRequired(_ep, txtPassword, "mật khẩu ứng dụng")) valid = false;

                if (!valid)
                {
                    UiFeedback.FocusFirstError(_ep, txtHost, txtSenderEmail, txtPassword);
                    return;
                }
            }

            SaveCurrentFieldsToSettings();
            try
            {
                _emailSvc.SaveSettings();
                UiFeedback.ShowSuccess("Đã lưu cấu hình email thành công!");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi lưu cấu hình");
            }
        }
    }
}
