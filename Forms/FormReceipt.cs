using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Reports;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormReceipt : Form
    {
        private readonly PaymentReceipt _receipt;
        private readonly Student _student;
        private readonly Semester _semester;
        private readonly TuitionFee _fee;
        private readonly EmailService _emailSvc;
        private string ReceiptStudentName => string.IsNullOrWhiteSpace(_receipt.StudentNameSnapshot) ? _student.FullName : _receipt.StudentNameSnapshot;
        private string ReceiptStudentCode => string.IsNullOrWhiteSpace(_receipt.StudentCodeSnapshot) ? $"SV{_student.Id:D4}" : _receipt.StudentCodeSnapshot;
        private string ReceiptClassName => string.IsNullOrWhiteSpace(_receipt.ClassNameSnapshot) ? _student.ClassName : _receipt.ClassNameSnapshot;
        private string ReceiptSemesterName => string.IsNullOrWhiteSpace(_receipt.SemesterNameSnapshot) ? _semester.Name : _receipt.SemesterNameSnapshot;
        private decimal ReceiptTotal => _receipt.TotalTuitionSnapshot > 0 ? _receipt.TotalTuitionSnapshot : _fee.TotalAmount;
        private decimal ReceiptPaidAfter => _receipt.TotalPaidAfterSnapshot > 0 ? _receipt.TotalPaidAfterSnapshot : _fee.PaidAmount;
        private decimal ReceiptRemaining => _receipt.TotalTuitionSnapshot > 0 ? _receipt.RemainingAfterSnapshot : _fee.RemainingAmount;
        private DateTime? ReceiptDueDate => _receipt.DueDateSnapshot ?? _fee.DueDate ?? _semester.DueDate;

        public FormReceipt(PaymentReceipt receipt, Student student, Semester semester, TuitionFee fee, EmailService? emailSvc = null)
        {
            _receipt = receipt;
            _student = student;
            _semester = semester;
            _fee = fee;
            _emailSvc = emailSvc ?? new EmailService();
            BuildUI();
        }

        private void BuildUI()
        {
            Text = $"Biên Lai Thu Học Phí - {_receipt.ReceiptCode}";
            ClientSize = new Size(620, 760);
            MinimumSize = new Size(600, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Top Action Header ─────────────────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = UITheme.Success,
                Padding = new Padding(24, 0, 24, 0)
            };

            var lblTitle = new Label
            {
                Text = "✅  GHI NHẬN THU TIỀN THÀNH CÔNG",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Left, AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 14, 0, 0)
            };
            topBar.Controls.Add(lblTitle);

            // ── Printable Receipt Paper Card ──────────────────────────────
            var cardWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 16),
                BackColor = UITheme.Background,
                AutoScroll = true
            };

            var paper = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20)
            };
            paper.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, paper.Width - 1, paper.Height - 1);
            };

            int y = 14;

            // University / Header
            var lblUni = new Label
            {
                Text = "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(28, y), AutoSize = true, Parent = paper
            };
            y += 26;

            var lblDocTitle = new Label
            {
                Text = "BIÊN LAI THU TIỀN HỌC PHÍ",
                Font = UITheme.FontH1,
                ForeColor = UITheme.PrimaryDark,
                Location = new Point(28, y), AutoSize = true, Parent = paper
            };
            y += 36;

            var lblMeta = new Label
            {
                Text = $"Mã biên lai: {_receipt.ReceiptCode}   |   Ngày lập: {_receipt.PaymentDate:dd/MM/yyyy HH:mm}",
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                Location = new Point(28, y), AutoSize = true, Parent = paper
            };
            y += 26;

            paper.Controls.Add(new Panel { Location = new Point(28, y), Size = new Size(508, 1), BackColor = UITheme.Border, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right });
            y += 16;

            // Student Information block
            AddInfoRow(paper, "Họ và tên SV:", ReceiptStudentName, y, isBold: true); y += 28;
            AddInfoRow(paper, "Mã sinh viên:", ReceiptStudentCode, y); y += 28;
            AddInfoRow(paper, "Lớp học:", ReceiptClassName, y); y += 28;
            AddInfoRow(paper, "Học kỳ:", ReceiptSemesterName, y); y += 28;

            paper.Controls.Add(new Panel { Location = new Point(28, y), Size = new Size(508, 1), BackColor = UITheme.BorderLight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right });
            y += 16;

            // Financial block
            AddInfoRow(paper, "Người nộp tiền:", _receipt.PayerName, y); y += 28;
            AddInfoRow(paper, "Hình thức:", _receipt.PaymentMethod, y); y += 28;

            // Highlighted Amount box
            var amountBox = new Panel
            {
                Location = new Point(28, y), Size = new Size(508, 52),
                BackColor = UITheme.SuccessLight,
                AccessibleName = "Tổng số tiền thu",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            amountBox.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Success, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, amountBox.Width - 1, amountBox.Height - 1);
            };

            new Label
            {
                Text = "SỐ TIỀN THU:",
                Location = new Point(16, 17), AutoSize = true,
                Font = UITheme.FontBold,
                ForeColor = UITheme.SuccessDark, Parent = amountBox
            };

            new Label
            {
                Text = $"{_receipt.Amount:N0} VNĐ",
                Location = new Point(240, 13),
                Size = new Size(250, 28),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = UITheme.FontH1,
                ForeColor = UITheme.SuccessDark, Parent = amountBox
            };
            paper.Controls.Add(amountBox);
            y += 64;

            // Remaining summary
            AddInfoRow(paper, "Tổng học phí kỳ này:", $"{ReceiptTotal:N0} VNĐ", y); y += 24;
            AddInfoRow(paper, "Đã nộp tổng cộng:", $"{ReceiptPaidAfter:N0} VNĐ", y); y += 24;
            AddInfoRow(paper, "Còn lại phải nộp:", $"{ReceiptRemaining:N0} VNĐ", y,
                customColor: ReceiptRemaining > 0 ? UITheme.Danger : UITheme.Success, isBold: true); y += 24;
            var due = ReceiptDueDate;
            AddInfoRow(paper, "Hạn nộp học phí:", $"{due:dd/MM/yyyy}", y); y += 28;

            if (!string.IsNullOrWhiteSpace(_receipt.Note))
            {
                AddInfoRow(paper, "Ghi chú:", _receipt.Note, y);
                y += 24;
            }

            // Signature placeholders
            y = Math.Max(y + 16, 500);
            var lblSignPayer = new Label
            {
                Text = "NGƯỜI NỘP TIỀN\n(Ký và ghi rõ họ tên)",
                TextAlign = ContentAlignment.TopCenter,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary,
                Location = new Point(40, y), Size = new Size(180, 42), Parent = paper
            };

            var lblSignCashier = new Label
            {
                Text = "NGƯỜI THU TIỀN\n(Ký, đóng dấu)",
                TextAlign = ContentAlignment.TopCenter,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary,
                Location = new Point(320, y), Size = new Size(180, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Parent = paper
            };

            paper.Height = y + 70;

            cardWrapper.Controls.Add(paper);

            // ── Bottom Action Bar ─────────────────────────────────────────
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.Surface,
                Padding = new Padding(24, 12, 24, 12)
            };
            bottomBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            var flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            var btnPrint = UITheme.PrimaryBtn("🖨 In Phiếu", 125, 38);
            btnPrint.Margin = new Padding(0, 0, 10, 0);
            btnPrint.Click += (s, e) => PrintReceipt(paper);

            var btnPdf = UITheme.SuccessBtn("PDF", 82, 38);
            btnPdf.Margin = new Padding(0, 0, 10, 0);
            btnPdf.Click += (s, e) => ExportPdf();

            var btnSendEmail = UITheme.PurpleBtn("📧 Gửi Email", 130, 38);
            btnSendEmail.Margin = new Padding(0);
            btnSendEmail.Click += BtnSendEmail_Click;

            flowLeft.Controls.AddRange(new Control[] { btnPrint, btnPdf, btnSendEmail });

            var btnClose = UITheme.GhostBtn("Đóng", 100, 38);
            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };
            flowRight.Controls.Add(btnClose);
            btnClose.Click += (s, e) => Close();

            AcceptButton = btnPrint;
            CancelButton = btnClose;

            bottomBar.Controls.AddRange(new Control[] { flowRight, flowLeft });

            Controls.Add(cardWrapper);
            Controls.Add(bottomBar);
            Controls.Add(topBar);
        }

        private async void BtnSendEmail_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_student.Email))
            {
                UiFeedback.ShowWarning("Sinh viên này chưa có địa chỉ email trong hồ sơ!");
                return;
            }

            using (UiFeedback.BusyScope(this, "Đang gửi email..."))
            {
                var res = await _emailSvc.SendReceiptEmailAsync(_student, _semester, _fee, _receipt);
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

        private static void AddInfoRow(Panel parent, string label, string val, int y,
            bool isBold = false, Color? customColor = null)
        {
            parent.Controls.Add(new Label
            {
                Text = label, Location = new Point(28, y), AutoSize = true,
                Font = UITheme.FontBody, ForeColor = UITheme.TextSecondary
            });

            parent.Controls.Add(new Label
            {
                Text = val, Location = new Point(190, y), AutoSize = true,
                Font = isBold ? UITheme.FontBold : UITheme.FontBody,
                ForeColor = customColor ?? UITheme.TextPrimary
            });
        }

        private void PrintReceipt(Panel paper)
        {
            try
            {
                using var printDoc = new PrintDocument();
                printDoc.PrintPage += (s, ev) =>
                {
                    using var bmp = new Bitmap(paper.Width, paper.Height);
                    paper.DrawToBitmap(bmp, new Rectangle(0, 0, paper.Width, paper.Height));
                    if (ev.Graphics != null)
                    {
                        ev.Graphics.DrawImage(bmp, 50, 50);
                    }
                };

                using var printPreview = new PrintPreviewDialog
                {
                    Document = printDoc,
                    Size = new Size(800, 700)
                };
                printPreview.ShowDialog();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Không thể mở xem trước in");
            }
        }

        private void ExportPdf()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"BienLai_{_receipt.ReceiptCode}.pdf",
                Title = "Lưu biên lai PDF"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ReceiptPdfRenderer.Export(dialog.FileName, new ReceiptPdfData(
                    "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT", _receipt.ReceiptCode, _receipt.PaymentDate,
                    ReceiptStudentName, ReceiptStudentCode, ReceiptClassName, ReceiptSemesterName,
                    _receipt.PayerName, _receipt.PaymentMethod, _receipt.Amount, ReceiptTotal,
                    ReceiptPaidAfter, ReceiptRemaining, ReceiptDueDate, _receipt.Note));
                UiFeedback.ShowSuccess($"Đã xuất biên lai PDF:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Không thể xuất biên lai PDF");
            }
        }
    }
}
