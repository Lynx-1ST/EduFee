using System;
using System.Drawing;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormPayment : Form
    {
        private readonly TuitionFee _fee;
        private readonly TuitionService _tuiSvc;
        private readonly SemesterService _semSvc;
        private readonly StudentService _studentSvc;
        private readonly ReceiptService _receiptSvc;
        private readonly EmailService _emailSvc;
        private readonly IPaymentGateway _mockGateway;
        private IPaymentGateway? _momoGateway;
        private readonly GatewayPaymentPersistenceService? _gatewayPersistence;
        private readonly MomoSettingsService? _momoSettings;

        private NumericUpDown numAmount = null!;
        private ComboBox cmbMethod = null!;
        private ComboBox cmbGateway = null!;
        private TextBox txtPayer = null!;
        private TextBox txtNote = null!;
        private CheckBox chkSendEmail = null!;
        private Button btnConfirm = null!;
        private bool _paymentRecorded;
        private bool _submitting;

        public FormPayment(TuitionFee fee, TuitionService tui, SemesterService sem,
            StudentService studentSvc, ReceiptService receiptSvc, EmailService? emailSvc = null,
            IPaymentGateway? momoGateway = null, GatewayPaymentPersistenceService? gatewayPersistence = null,
            MomoSettingsService? momoSettings = null)
        {
            _fee = fee;
            _tuiSvc = tui;
            _semSvc = sem;
            _studentSvc = studentSvc;
            _receiptSvc = receiptSvc;
            _emailSvc = emailSvc ?? new EmailService();
            _mockGateway = new MockQrPaymentGateway();
            _momoSettings = momoSettings;
            _momoGateway = momoGateway ?? (_momoSettings?.Settings.Enabled == true ? new MomoSandboxPaymentGateway(_momoSettings) : null);
            _gatewayPersistence = gatewayPersistence;
            BuildUI();
        }

        private void BuildUI()
        {
            var student = _studentSvc.GetStudentById(_fee.StudentId);
            string studentName = student?.FullName ?? $"SV #{_fee.StudentId}";
            string studentCode = student?.StudentCode ?? "—";

            Text = $"Ghi Nhận Thanh Toán - {studentName}";
            ClientSize = new Size(520, 680);
            MinimumSize = new Size(520, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Header ────────────────────────────────────────────────────
            var semester = _semSvc.GetById(_fee.SemesterId);
            string sub = $"{studentName} · {studentCode}" + (semester != null ? $" · {semester.Name}" : "");
            var header = UITheme.CreateDialogHeader("", "Thu học phí", sub, UITheme.PrimaryDark, 64);

            // ── Footer with action buttons (Always pinned and visible) ───
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.Surface };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            btnConfirm = UITheme.PrimaryBtn("Xác nhận thu", 160, 38);
            btnConfirm.Font = UITheme.FontBold;
            btnConfirm.Click += BtnConfirm_Click;

            var btnCancel = UITheme.GhostBtn("Hủy", 80, 38);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 13, 24, 0),
                BackColor = Color.Transparent
            };
            flowBtns.Controls.Add(btnConfirm);
            flowBtns.Controls.Add(btnCancel);
            footer.Controls.Add(flowBtns);

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;

            // ── Card ──────────────────────────────────────────────────────
            var card = new Panel { BackColor = UITheme.Surface, Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), AutoScroll = true };

            // Info block
            var infoPanel = new Panel
            {
                Location = new Point(24, 14), Size = new Size(472, 122),
                BackColor = UITheme.SurfaceAlt,
                Parent = card
            };
            infoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
                using (var pen = new Pen(UITheme.Border, 1))
                using (var path = UITheme.GetRoundedPath(rect, 8))
                {
                    e.Graphics.DrawPath(pen, path);
                }
                using (var accentBrush = new SolidBrush(UITheme.Primary))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 8, 4, Math.Max(0, infoPanel.Height - 16));
                }
            };

            var dueDate = _fee.DueDate ?? _semSvc.GetById(_fee.SemesterId)?.DueDate;
            string dueStr = dueDate.HasValue ? dueDate.Value.ToString("dd/MM/yyyy") : "—";
            bool isOverdue = dueDate.HasValue && DateTime.Today > dueDate.Value.Date && !_fee.IsFullyPaid;

            InfoRow(infoPanel, "Sinh viên:", $"{studentName} ({student?.ClassName})", UITheme.TextPrimary, 10);
            InfoRow(infoPanel, "Tổng học phí:", $"{_fee.TotalAmount:N0} ₫", UITheme.TextPrimary, 32);
            InfoRow(infoPanel, "Đã nộp:", $"{_fee.PaidAmount:N0} ₫", UITheme.Success, 54);
            InfoRow(infoPanel, "Còn phải nộp:", $"{_fee.RemainingAmount:N0} ₫", UITheme.Danger, 76);
            InfoRow(infoPanel, "Hạn nộp:", isOverdue ? $"{dueStr}  (Quá hạn)" : dueStr, isOverdue ? UITheme.Danger : UITheme.TextSecondary, 98);

            int y = 146;

            // Amount input
            card.Controls.Add(FieldLabel("Số tiền nộp (VNĐ) *:", y)); y += 22;
            numAmount = new NumericUpDown
            {
                Location = new Point(24, y), Size = new Size(472, 34),
                Font = UITheme.FontInputLarge, BackColor = UITheme.SurfaceAlt,
                Minimum = 0, Maximum = Math.Max(0, _fee.RemainingAmount),
                Value = Math.Max(0, _fee.RemainingAmount),
                DecimalPlaces = 0, Increment = 500_000, ThousandsSeparator = true,
                AccessibleName = "Số tiền nộp"
            };
            card.Controls.Add(numAmount);
            y += 42;

            // Payment Method
            card.Controls.Add(FieldLabel("Hình thức thanh toán *:", y)); y += 22;
            cmbMethod = new ComboBox
            {
                Location = new Point(24, y), Size = new Size(472, 30),
                Font = UITheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt,
                AccessibleName = "Hình thức thanh toán"
            };
            cmbMethod.Items.AddRange(new object[]
            {
                "Thanh toán QR", "Tiền mặt", "Thẻ ATM / Thẻ tín dụng"
            });
            cmbMethod.SelectedIndex = 0;
            cmbMethod.SelectedIndexChanged += (s, e) =>
            {
                btnConfirm.Text = IsQrMethod() ? "Tạo mã QR" : "Xác nhận thu";
                cmbGateway.Enabled = IsQrMethod();
            };
            card.Controls.Add(cmbMethod);
            y += 42;

            card.Controls.Add(FieldLabel("Cổng thanh toán QR:", y)); y += 22;
            cmbGateway = new ComboBox
            {
                Location = new Point(24, y), Size = new Size(472, 30), Font = UITheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UITheme.SurfaceAlt,
                AccessibleName = "Cổng thanh toán QR"
            };
            cmbGateway.Items.Add("VietQR");
            cmbGateway.Items.Add("MoMo Sandbox");
            cmbGateway.SelectedIndex = 0;
            card.Controls.Add(cmbGateway);
            y += 42;

            // Payer Name
            card.Controls.Add(FieldLabel("Người nộp tiền *:", y)); y += 22;
            txtPayer = new TextBox
            {
                Location = new Point(24, y), Size = new Size(472, 30),
                Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt,
                Text = studentName,
                AccessibleName = "Người nộp tiền"
            };
            card.Controls.Add(txtPayer);
            y += 42;

            // Note
            card.Controls.Add(FieldLabel("Ghi chú nộp tiền:", y)); y += 22;
            txtNote = new TextBox
            {
                Location = new Point(24, y), Size = new Size(472, 54),
                Font = UITheme.FontBody, Multiline = true,
                BackColor = UITheme.SurfaceAlt, BorderStyle = BorderStyle.FixedSingle,
                AccessibleName = "Ghi chú nộp tiền"
            };
            card.Controls.Add(txtNote);
            y += 62;

            // Email confirmation checkbox
            chkSendEmail = new CheckBox
            {
                Text = !string.IsNullOrWhiteSpace(student?.Email)
                    ? $"Gửi email biên lai điện tử cho sinh viên ({student.Email})"
                    : "Sinh viên chưa có email trong hồ sơ (Không gửi)",
                Location = new Point(24, y),
                AutoSize = true,
                Font = UITheme.FontSmallBold,
                ForeColor = !string.IsNullOrWhiteSpace(student?.Email) ? UITheme.Primary : UITheme.TextMuted,
                Checked = !string.IsNullOrWhiteSpace(student?.Email),
                Enabled = !string.IsNullOrWhiteSpace(student?.Email),
                AccessibleName = "Gửi email biên lai"
            };
            card.Controls.Add(chkSendEmail);

            Controls.Add(card);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private static Label FieldLabel(string text, int y) => new()
        {
            Text = text, Location = new Point(24, y), AutoSize = true,
            Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary
        };

        private static void InfoRow(Panel parent, string label, string value, Color valColor, int y)
        {
            new Label { Text = label, Location = new Point(14, y), AutoSize = true, Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary, Parent = parent };
            new Label { Text = value, Location = new Point(160, y), AutoSize = true, Font = UITheme.FontBold, ForeColor = valColor, Parent = parent };
        }

        private ErrorProvider _ep = null!;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ep = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        }

        private async void BtnConfirm_Click(object? s, EventArgs e)
        {
            if (_submitting || _paymentRecorded) return;
            _submitting = true;
            try
            {
                bool valid = true;
                decimal amount = numAmount.Value;
                if (amount <= 0)
                {
                    _ep.SetError(numAmount, "Số tiền thanh toán phải lớn hơn 0");
                    valid = false;
                }
                else
                {
                    _ep.SetError(numAmount, string.Empty);
                }

                valid &= UiFeedback.ValidateRequired(_ep, txtPayer, "người nộp tiền");
                if (!valid)
                {
                    UiFeedback.FocusFirstError(_ep, numAmount, txtPayer);
                    return;
                }

                string payer = txtPayer.Text.Trim();
                string method = cmbMethod.SelectedItem?.ToString() ?? "Chuyển khoản";
                string note = txtNote.Text.Trim();

                var sem = _semSvc.GetById(_fee.SemesterId);
                var student = _studentSvc.GetStudentById(_fee.StudentId);

                if (IsQrMethod())
                {
                    var gateway = SelectedGateway();
                    if (gateway == null)
                    {
                        var dr = MessageBox.Show(this,
                            "Cổng MoMo Sandbox chưa được cấu hình hoặc chưa bật.\nBạn có muốn mở màn hình Cấu hình MoMo Sandbox ngay bây giờ không?",
                            "Cấu hình MoMo Sandbox",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        if (dr == DialogResult.Yes)
                        {
                            var settingsSvc = _momoSettings ?? new MomoSettingsService();
                            using var f = new FormMomoSettings(settingsSvc);
                            if (f.ShowDialog(this) == DialogResult.OK && settingsSvc.Settings.Enabled)
                            {
                                _momoGateway = new MomoSandboxPaymentGateway(settingsSvc);
                                gateway = _momoGateway;
                            }
                        }

                        if (gateway == null) return;
                    }

                    string description = $"{student?.StudentCode ?? "—"} HP {sem?.Name ?? _fee.SemesterId.ToString()}";
                    string orderId = $"EDUFEE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";
                    var request = new PaymentGatewayRequest(orderId, amount, description,
                        student?.StudentCode ?? "—", student?.FullName ?? $"SV #{_fee.StudentId}");
                    _gatewayPersistence?.PersistIntent(_fee.Id, gateway.Provider, request);
                    var session = await gateway.CreatePaymentAsync(request);
                    using var qrForm = new FormQrPayment(gateway, session);
                    if (qrForm.ShowDialog(this) != DialogResult.OK || qrForm.PaymentStatus?.Status != GatewayPaymentStatus.Success) return;
                    var confirmation = qrForm.PaymentStatus with { Provider = gateway.Provider };
                    if (_gatewayPersistence != null)
                    {
                        var gatewayReceipt = _gatewayPersistence.RecordConfirmedSuccess(confirmation, payer, sem?.DueDate);
                        if (gatewayReceipt == null)
                        {
                            UiFeedback.ShowWarning("Giao dịch này đã được ghi nhận trước đó.");
                            DialogResult = DialogResult.OK;
                            Close();
                            return;
                        }
                        await CompleteRecordedPaymentAsync(gatewayReceipt, student, sem);
                        return;
                    }
                    method = gateway.Provider;
                    note = string.IsNullOrWhiteSpace(note)
                        ? $"Mã đơn: {session.OrderId}"
                        : $"{note} | Mã đơn: {session.OrderId}";
                }

                // Record the balance update and receipt in one SQLite transaction.
                var receipt = _tuiSvc.RecordPaymentWithReceipt(_fee.Id, amount, sem?.DueDate,
                    _receiptSvc, method, payer, note);
                _paymentRecorded = true;
                var savedFee = _tuiSvc.GetById(_fee.Id);
                if (savedFee != null)
                {
                    _fee.PaidAmount = savedFee.PaidAmount;
                    _fee.PaidDate = savedFee.PaidDate;
                    _fee.Status = savedFee.Status;
                }

                // Send email only after the durable transaction commits.
                if (chkSendEmail.Checked && student != null && sem != null)
                {
                    using (UiFeedback.BusyScope(this, "Đang gửi email..."))
                    {
                        var result = await _emailSvc.SendReceiptEmailAsync(student, sem, _fee, receipt);
                        if (!result.Success)
                            UiFeedback.ShowWarning($"Thanh toán đã lưu, nhưng chưa gửi được email. Có thể gửi lại từ biên lai.\n{result.Message}");
                    }
                }

                // Open Receipt form for preview / printing.
                if (student != null && sem != null)
                {
                    using var receiptForm = new FormReceipt(receipt, student, sem, _fee, _emailSvc);
                    receiptForm.ShowDialog();
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                if (_paymentRecorded)
                {
                    UiFeedback.ShowException(ex, "Thanh toán đã lưu. Vui lòng kiểm tra biên lai; không ghi nhận lại khoản tiền này.");
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                    UiFeedback.ShowException(ex, "Lỗi ghi nhận thanh toán");
            }
            finally { _submitting = false; }
        }

        private bool IsQrMethod() => cmbMethod.SelectedIndex == 0;

        private IPaymentGateway? SelectedGateway()
        {
            if (cmbGateway.SelectedIndex == 1)
            {
                if (_momoGateway != null) return _momoGateway;
                if (_momoSettings != null && _momoSettings.Settings.Enabled)
                {
                    _momoGateway = new MomoSandboxPaymentGateway(_momoSettings);
                    return _momoGateway;
                }
                return null;
            }
            return _mockGateway;
        }

        private async Task CompleteRecordedPaymentAsync(PaymentReceipt receipt, Student? student, Semester? sem)
        {
            _paymentRecorded = true;
            var savedFee = _tuiSvc.GetById(_fee.Id);
            if (savedFee != null)
            {
                _fee.PaidAmount = savedFee.PaidAmount;
                _fee.PaidDate = savedFee.PaidDate;
                _fee.Status = savedFee.Status;
            }
            if (chkSendEmail.Checked && student != null && sem != null)
            {
                using (UiFeedback.BusyScope(this, "Đang gửi email..."))
                {
                    var result = await _emailSvc.SendReceiptEmailAsync(student, sem, _fee, receipt);
                    if (!result.Success) UiFeedback.ShowWarning($"Thanh toán đã lưu, nhưng chưa gửi được email. Có thể gửi lại từ biên lai.\n{result.Message}");
                }
            }
            if (student != null && sem != null)
            {
                using var receiptForm = new FormReceipt(receipt, student, sem, _fee, _emailSvc);
                receiptForm.ShowDialog();
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
