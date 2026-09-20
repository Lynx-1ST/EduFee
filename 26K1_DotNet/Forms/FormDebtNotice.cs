using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormDebtNotice : Form
    {
        private readonly TuitionFee _fee;
        private readonly Student _student;
        private readonly Semester _semester;
        private readonly TuitionService _tuiSvc;
        private readonly SemesterService? _semSvc;
        private readonly StudentService? _svSvc;
        private readonly ReceiptService? _receiptSvc;
        private readonly EmailService _emailSvc;

        private Panel paper = null!;
        private PictureBox pbQr = null!;
        private Bitmap? _qrBitmap;

        public FormDebtNotice(TuitionFee fee, Student student, Semester semester,
            TuitionService tuiSvc, SemesterService? semSvc = null,
            StudentService? svSvc = null, ReceiptService? receiptSvc = null,
            EmailService? emailSvc = null)
        {
            _fee = fee;
            _student = student;
            _semester = semester;
            _tuiSvc = tuiSvc;
            _semSvc = semSvc;
            _svSvc = svSvc;
            _receiptSvc = receiptSvc;
            _emailSvc = emailSvc ?? new EmailService();

            BuildUI();
            _ = LoadVietQrAsync();
        }

        private void BuildUI()
        {
            Text = $"Giấy Báo Nợ Học Phí - {_student.FullName} ({_semester.Name})";
            ClientSize = new Size(660, 740);
            MinimumSize = new Size(640, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Top Header Banner ───────────────────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = _fee.Status == PaymentStatus.Overdue ? UITheme.Danger : UITheme.DebtRed,
                Padding = new Padding(24, 0, 24, 0)
            };

            var lblBanner = new Label
            {
                Text = _fee.Status == PaymentStatus.Overdue
                    ? "THÔNG BÁO QUÁ HẠN HỌC PHÍ"
                    : "GIẤY BÁO NỢ HỌC PHÍ & HƯỚNG DẪN ĐÓNG TIỀN",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            topBar.Controls.Add(lblBanner);

            // ── Bottom Action Bar ───────────────────────────────────────────
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.SurfaceAlt
            };
            bottomBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 13, 24, 0),
                BackColor = Color.Transparent
            };

            var btnPrint = UITheme.GhostBtn("In giấy báo", 100, 38);
            btnPrint.Click += (s, e) => PrintNotice();

            var btnEmail = UITheme.GhostBtn("Gửi email", 95, 38);
            btnEmail.Click += BtnEmail_Click;

            var btnPayNow = UITheme.PrimaryBtn("Thu tiền ngay", 120, 38);
            btnPayNow.Click += (s, e) => OpenPaymentDialog();

            var btnClose = UITheme.GhostBtn("Đóng", 80, 38);
            btnClose.Click += (s, e) => Close();

            AcceptButton = btnPrint;
            CancelButton = btnClose;

            flowBtns.Controls.AddRange(new Control[] { btnPayNow, btnEmail, btnPrint, btnClose });
            bottomBar.Controls.Add(flowBtns);

            // ── Paper Card Content ──────────────────────────────────────────
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 14),
                BackColor = UITheme.Background,
                AutoScroll = true
            };

            paper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 18, 24, 18)
            };
            paper.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, paper.Width - 1, paper.Height - 1);
            };

            int y = 14;

            // Header Institution
            var lblUni = new Label
            {
                Text = "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT",
                Font = UITheme.FontBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(24, y), AutoSize = true, Parent = paper
            };
            y += 24;

            var lblDept = new Label
            {
                Text = "PHÒNG TÀI CHÍNH - KẾ TOÁN · QUẢN LÝ HỌC PHÍ",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextMuted,
                Location = new Point(24, y), AutoSize = true, Parent = paper
            };
            y += 24;

            var lblTitle = new Label
            {
                Text = "GIẤY BÁO NỢ HỌC PHÍ",
                Font = UITheme.FontH1,
                ForeColor = UITheme.DangerDark,
                Location = new Point(24, y), AutoSize = true, Parent = paper
            };
            y += 34;

            var dueDate = _fee.DueDate ?? _semester.DueDate;
            var lblSub = new Label
            {
                Text = $"Học kỳ: {_semester.Name}   |   Hạn nộp cuối: {dueDate:dd/MM/yyyy}",
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                Location = new Point(24, y), AutoSize = true, Parent = paper
            };
            y += 24;

            paper.Controls.Add(new Panel { Location = new Point(24, y), Size = new Size(520, 1), BackColor = UITheme.Border });
            y += 14;

            // Student block
            AddInfoRow(paper, "Họ và tên:", _student.FullName, y, isBold: true); y += 26;
            AddInfoRow(paper, "Mã sinh viên:", _student.StudentCode, y); y += 26;
            AddInfoRow(paper, "Lớp học:", _student.ClassName, y); y += 26;
            AddInfoRow(paper, "Số tín chỉ đăng ký:", $"{_fee.Credits} tín chỉ", y); y += 26;

            paper.Controls.Add(new Panel { Location = new Point(24, y), Size = new Size(520, 1), BackColor = UITheme.BorderLight });
            y += 14;

            // Debt summary highlight box
            var debtBox = new Panel
            {
                Location = new Point(24, y), Size = new Size(520, 48),
                BackColor = UITheme.DangerLight
            };
            debtBox.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Danger, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, debtBox.Width - 1, debtBox.Height - 1);
            };

            new Label
            {
                Text = "SỐ TIỀN CÒN NỢ:",
                Location = new Point(14, 15), AutoSize = true,
                Font = UITheme.FontBold,
                ForeColor = UITheme.DangerDark, Parent = debtBox
            };

            new Label
            {
                Text = $"{_fee.RemainingAmount:N0} VNĐ",
                Location = new Point(260, 11), AutoSize = true,
                Font = UITheme.FontH1,
                ForeColor = UITheme.DangerDark, Parent = debtBox
            };
            paper.Controls.Add(debtBox);
            y += 58;

            // Bank Payment & VietQR card
            var bankCard = new Panel
            {
                Location = new Point(24, y), Size = new Size(520, 156),
                BackColor = UITheme.SurfaceAlt
            };
            bankCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, bankCard.Width - 1, bankCard.Height - 1);
            };

            // Bank instructions
            int by = 12;
            new Label
            {
                Text = "THÔNG TIN CHUYỂN KHOẢN NGÂN HÀNG",
                Location = new Point(14, by), AutoSize = true,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.PrimaryDark, Parent = bankCard
            };
            by += 24;

            AddBankRow(bankCard, "Ngân hàng:", "VietinBank (Công Thương)", by); by += 22;
            AddBankRow(bankCard, "Số tài khoản:", "1028-8888-9999", by, isCopyable: true); by += 22;
            AddBankRow(bankCard, "Chủ tài khoản:", "TRUONG DAI HOC MO DIA CHAT", by); by += 22;
            string transferSyntax = $"{_student.StudentCode} {_student.FullName} HP {_semester.Name}";
            AddBankRow(bankCard, "Nội dung CK:", transferSyntax, by, isCopyable: true, isHighlight: true);

            // VietQR Box on the right
            pbQr = new PictureBox
            {
                Location = new Point(376, 12),
                Size = new Size(130, 130),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Parent = bankCard
            };
            DrawQrPlaceholder();

            paper.Controls.Add(bankCard);
            y += 166;

            // Notice text
            var lblNotice = new Label
            {
                Text = "Lưu ý: Sinh viên cần ghi chính xác nội dung chuyển khoản để hệ thống tự động gạch nợ. Quá hạn nộp học phí, sinh viên sẽ bị tạm dừng quyền thi kết thúc học phần và xét học bổng.",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.DebtAmber,
                Location = new Point(24, y), Size = new Size(520, 44),
                Parent = paper
            };

            wrapper.Controls.Add(paper);

            Controls.Add(wrapper);
            Controls.Add(bottomBar);
            Controls.Add(topBar);
        }

        private static void AddInfoRow(Panel parent, string label, string val, int y, bool isBold = false)
        {
            new Label
            {
                Text = label, Location = new Point(24, y), AutoSize = true,
                Font = UITheme.FontBody, ForeColor = UITheme.TextSecondary, Parent = parent
            };
            new Label
            {
                Text = val, Location = new Point(190, y), AutoSize = true,
                Font = isBold ? UITheme.FontBold : UITheme.FontBody,
                ForeColor = UITheme.TextPrimary, Parent = parent
            };
        }

        private static void AddBankRow(Panel parent, string label, string val, int y, bool isCopyable = false, bool isHighlight = false)
        {
            new Label
            {
                Text = label, Location = new Point(14, y), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary, Parent = parent
            };

            var lblVal = new Label
            {
                Text = val, Location = new Point(106, y), AutoSize = true,
                Font = isHighlight ? UITheme.FontSmallBold : UITheme.FontSmall,
                ForeColor = isHighlight ? UITheme.PrimaryDark : UITheme.TextPrimary, Parent = parent
            };

            if (isCopyable)
            {
                var btnCopy = new Label
                {
                    Text = "📋", Font = UITheme.FontSmall,
                    Location = new Point(lblVal.Right + 4, y - 1), AutoSize = true,
                    Cursor = Cursors.Hand, ForeColor = UITheme.Primary, Parent = parent
                };
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(val);
                    UiFeedback.ShowToast(btnCopy, $"Đã sao chép: «{val}»");
                };
            }
        }

        private void DrawQrPlaceholder()
        {
            var bmp = new Bitmap(130, 130);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            using var pen = new Pen(UITheme.QrBorder, 1);
            g.DrawRectangle(pen, 0, 0, 129, 129);

            // Draw clean VietQR styled placeholder
            using var brush = new SolidBrush(UITheme.QrText);
            // Corner target squares
            DrawTargetSquare(g, brush, 10, 10);
            DrawTargetSquare(g, brush, 90, 10);
            DrawTargetSquare(g, brush, 10, 90);

            // Middle matrix dots
            var rnd = new Random(42);
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (rnd.Next(2) == 1)
                        g.FillRectangle(brush, 42 + c * 7, 42 + r * 7, 5, 5);
                }
            }

            var font = UITheme.FontSmallBold;
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("VietQR", font, new SolidBrush(UITheme.Primary), new Rectangle(0, 114, 130, 14), sf);

            _qrBitmap = bmp;
            pbQr.Image = _qrBitmap;
        }

        private static void DrawTargetSquare(Graphics g, Brush brush, int x, int y)
        {
            g.FillRectangle(brush, x, y, 30, 30);
            g.FillRectangle(Brushes.White, x + 5, y + 5, 20, 20);
            g.FillRectangle(brush, x + 9, y + 9, 12, 12);
        }

        private async Task LoadVietQrAsync()
        {
            try
            {
                string bank = "970415"; // VietinBank BIN
                string acc = "102888889999";
                string amount = ((long)_fee.RemainingAmount).ToString();
                string desc = Uri.EscapeDataString($"{_student.StudentCode} {_student.FullName} HP {_semester.Name}");
                string name = Uri.EscapeDataString("TRUONG DAI HOC MO DIA CHAT");
                string url = $"https://img.vietqr.io/image/{bank}-{acc}-compact.png?amount={amount}&addInfo={desc}&accountName={name}";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var bytes = await client.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                var img = Image.FromStream(ms);

                if (IsHandleCreated && !IsDisposed)
                {
                    Invoke(() =>
                    {
                        pbQr.Image = new Bitmap(img);
                    });
                }
            }
            catch
            {
                // Fallback: retains offline clean VietQR template
            }
        }

        private async void BtnEmail_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_student.Email))
            {
                UiFeedback.ShowWarning("Sinh viên này chưa có địa chỉ email trong hồ sơ!");
                return;
            }

            using (UiFeedback.BusyScope(sender as Control ?? this, "Đang gửi..."))
            {
                var res = await _emailSvc.SendDebtNoticeEmailAsync(_student, _semester, _fee);
                if (res.Success)
                {
                    UiFeedback.ShowSuccess(res.Message);
                }
                else
                {
                    UiFeedback.ShowError(res.Message);
                }
            }
        }

        private void OpenPaymentDialog()
        {
            if (_svSvc == null || _semSvc == null || _receiptSvc == null)
            {
                UiFeedback.ShowInfo("Vui lòng vào tab Học Phí để thực hiện thu tiền.");
                return;
            }

            using var payForm = new FormPayment(_fee, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _emailSvc);
            if (payForm.ShowDialog() == DialogResult.OK)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void PrintNotice()
        {
            using var pd = new PrintDocument();
            pd.PrintPage += (s, ev) =>
            {
                if (paper == null || ev.Graphics == null) return;
                using var bmp = new Bitmap(paper.Width, paper.Height);
                paper.DrawToBitmap(bmp, new Rectangle(0, 0, paper.Width, paper.Height));
                ev.Graphics.DrawImage(bmp, ev.MarginBounds.Left, ev.MarginBounds.Top, ev.MarginBounds.Width,
                    (int)((float)paper.Height / paper.Width * ev.MarginBounds.Width));
            };

            using var dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
        }
    }
}
