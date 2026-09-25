using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    /// <summary>
    /// Read-only academic-ledger overview for the active semester.
    /// Data entry remains in the existing Students and Tuition screens.
    /// </summary>
    public sealed class PanelOverview : UserControl
    {
        private readonly StudentService _studentService;
        private readonly SemesterService _semesterService;
        private readonly TuitionService _tuitionService;
        private readonly ReceiptService _receiptService;
        private readonly Action<int>? _navigateToTuition;

        private readonly Panel _scroll;
        private readonly Panel _content;
        private readonly Panel _hero;
        private readonly Panel _metricStrip;
        private readonly Panel _attentionCard;
        private readonly FlowLayoutPanel _attentionRows;
        private readonly Label _semesterName;
        private readonly Label _semesterDates;
        private readonly Label _dueDate;
        private readonly Label _pricePerCredit;
        private readonly Label _collectedAmount;
        private readonly Label _collectionDetail;
        private readonly Label _receiptDetail;
        private readonly Panel _progressTrack;
        private readonly Panel _progressFill;
        private readonly Label[] _metricValues = new Label[4];
        private readonly Label[] _metricTitles = new Label[4];
        private readonly Label _attentionSummary;
        private decimal _collectionRatio;

        public PanelOverview(
            StudentService studentService,
            SemesterService semesterService,
            TuitionService tuitionService,
            ReceiptService receiptService,
            Action<int>? navigateToTuition = null)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _semesterService = semesterService ?? throw new ArgumentNullException(nameof(semesterService));
            _tuitionService = tuitionService ?? throw new ArgumentNullException(nameof(tuitionService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _navigateToTuition = navigateToTuition;

            BackColor = UITheme.Background;
            AccessibleName = "Tổng quan học phí";

            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UITheme.Background,
                AccessibleName = "Nội dung tổng quan học phí"
            };
            _content = new Panel { BackColor = Color.Transparent, Location = new Point(UITheme.PadPage, UITheme.PadPage) };
            _scroll.Controls.Add(_content);
            Controls.Add(_scroll);

            _hero = UITheme.DockCard();
            _hero.AccessibleName = "Tóm tắt học kỳ đang áp dụng";
            _hero.Paint += DrawHeroDivider;
            _content.Controls.Add(_hero);

            _semesterName = MakeLabel("Chưa chọn học kỳ", UITheme.FontH1, UITheme.TextPrimary);
            _semesterDates = MakeLabel("Chọn học kỳ để xem tổng quan học phí.", UITheme.FontBody, UITheme.TextSecondary);
            _dueDate = MakeLabel("Hạn nộp —", UITheme.FontBold, UITheme.TextPrimary);
            _pricePerCredit = MakeLabel("— / tín chỉ", UITheme.FontBody, UITheme.TextSecondary);
            _collectedAmount = MakeLabel("0 ₫", new Font("Segoe UI", 24F, FontStyle.Bold), UITheme.TextPrimary);
            _collectionDetail = MakeLabel("Chưa có dữ liệu phải thu", UITheme.FontBody, UITheme.TextSecondary);
            _receiptDetail = MakeLabel("0 biên lai đã ghi nhận", UITheme.FontSmall, UITheme.TextMuted);
            _progressTrack = new Panel { BackColor = UITheme.Border, Height = 10, AccessibleName = "Tiến độ thu học phí" };
            _progressFill = new Panel { BackColor = UITheme.Success, Height = 10, AccessibleName = "Phần học phí đã thu" };
            _progressTrack.Controls.Add(_progressFill);
            _hero.Controls.AddRange(new Control[]
            {
                _semesterName, _semesterDates, _dueDate, _pricePerCredit,
                _collectedAmount, _collectionDetail, _receiptDetail, _progressTrack
            });

            _metricStrip = UITheme.DockCard();
            _metricStrip.AccessibleName = "Chỉ số học phí";
            _content.Controls.Add(_metricStrip);
            string[] metricTitles = { "Tổng phải thu", "Đã thu", "Còn nợ", "Quá hạn" };
            for (int i = 0; i < metricTitles.Length; i++)
            {
                _metricValues[i] = MakeLabel("—", UITheme.FontCardValue2, i == 2 ? UITheme.DangerDark : UITheme.TextPrimary);
                _metricTitles[i] = MakeLabel(metricTitles[i], UITheme.FontSmallBold, UITheme.TextSecondary);
                _metricStrip.Controls.Add(_metricValues[i]);
                _metricStrip.Controls.Add(_metricTitles[i]);
            }

            _attentionCard = UITheme.DockCard();
            _attentionCard.AccessibleName = "Danh sách cần xử lý";
            _content.Controls.Add(_attentionCard);
            var attentionTitle = MakeLabel("Cần xử lý", UITheme.FontH2, UITheme.TextPrimary);
            attentionTitle.Location = new Point(UITheme.PadCard, 14);
            _attentionSummary = MakeLabel("Các khoản còn nợ cần được ưu tiên.", UITheme.FontSmall, UITheme.TextSecondary);
            _attentionSummary.Location = new Point(UITheme.PadCard, 38);
            _attentionRows = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Location = new Point(UITheme.PadCard, 64),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _attentionCard.Controls.AddRange(new Control[] { attentionTitle, _attentionSummary, _attentionRows });

            _scroll.Resize += (_, _) => LayoutOverview();
            _content.Resize += (_, _) => LayoutOverview();
            LayoutOverview();
            RefreshData();
        }

        public void RefreshData()
        {
            var semester = _semesterService.GetActive();
            if (semester == null)
            {
                ShowNoSemester();
                return;
            }

            var fees = _tuitionService.GetBySemester(semester.Id);
            var statistics = _tuitionService.GetStatistics(semester.Id, semester.Name);
            var students = _studentService.GetAllStudents().ToDictionary(student => student.Id);
            var receiptCount = _receiptService.GetAll().Count(receipt => receipt.SemesterId == semester.Id);
            var percentage = statistics.TotalAmount <= 0m ? 0m : statistics.TotalPaid / statistics.TotalAmount;
            _collectionRatio = Math.Clamp(percentage, 0m, 1m);

            _semesterName.Text = semester.Name;
            _semesterDates.Text = $"{semester.StartDate:dd/MM/yyyy} — {semester.EndDate:dd/MM/yyyy}";
            _dueDate.Text = $"Hạn nộp  {semester.DueDate:dd/MM/yyyy}";
            _pricePerCredit.Text = $"{semester.TuitionPerCredit:N0} ₫ / tín chỉ";
            _collectedAmount.Text = $"{statistics.TotalPaid:N0} ₫";
            _collectionDetail.Text = $"Đã thu {percentage:P0} trên tổng {statistics.TotalAmount:N0} ₫";
            _receiptDetail.Text = $"{receiptCount:N0} biên lai đã ghi nhận";
            _progressFill.Width = (int)Math.Round(_collectionRatio * _progressTrack.Width);

            _metricValues[0].Text = $"{statistics.TotalAmount:N0} ₫";
            _metricValues[1].Text = $"{statistics.TotalPaid:N0} ₫";
            _metricValues[2].Text = $"{statistics.TotalRemaining:N0} ₫";
            _metricValues[3].Text = $"{statistics.OverdueCount:N0} SV";
            _metricValues[1].ForeColor = UITheme.SuccessDark;
            _metricValues[2].ForeColor = statistics.TotalRemaining > 0m ? UITheme.DangerDark : UITheme.TextPrimary;
            _metricValues[3].ForeColor = statistics.OverdueCount > 0 ? UITheme.DangerDark : UITheme.TextPrimary;

            var attention = fees
                .Where(fee => fee.RemainingAmount > 0m)
                .OrderByDescending(fee => fee.Status == PaymentStatus.Overdue)
                .ThenBy(fee => fee.DueDate ?? semester.DueDate)
                .ThenByDescending(fee => fee.RemainingAmount)
                .Take(5)
                .ToList();
            PopulateAttention(attention, students, semester);
            LayoutOverview();
        }

        private void ShowNoSemester()
        {
            _semesterName.Text = "Chưa chọn học kỳ";
            _semesterDates.Text = "Chọn một học kỳ đang áp dụng trong phần quản lý học kỳ.";
            _dueDate.Text = "Hạn nộp —";
            _pricePerCredit.Text = "— / tín chỉ";
            _collectedAmount.Text = "0 ₫";
            _collectionDetail.Text = "Chưa có dữ liệu phải thu";
            _receiptDetail.Text = "0 biên lai đã ghi nhận";
            _collectionRatio = 0m;
            _progressFill.Width = 0;
            foreach (var metric in _metricValues) metric.Text = "—";
            PopulateAttention([], new Dictionary<int, Student>(), null);
            LayoutOverview();
        }

        private void PopulateAttention(IReadOnlyList<TuitionFee> fees, IReadOnlyDictionary<int, Student> students, Semester? semester)
        {
            _attentionRows.SuspendLayout();
            _attentionRows.Controls.Clear();
            if (fees.Count == 0)
            {
                _attentionSummary.Text = "Không có khoản học phí còn nợ cần xử lý.";
                var empty = MakeLabel("Tất cả sinh viên trong học kỳ này đã hoàn tất nghĩa vụ học phí.", UITheme.FontBody, UITheme.TextSecondary);
                empty.Margin = new Padding(0, 12, 0, 12);
                _attentionRows.Controls.Add(empty);
            }
            else
            {
                _attentionSummary.Text = $"Hiển thị {fees.Count} khoản còn nợ cần ưu tiên.";
                foreach (var fee in fees)
                {
                    students.TryGetValue(fee.StudentId, out var student);
                    _attentionRows.Controls.Add(CreateAttentionRow(fee, student, semester));
                }
            }
            _attentionRows.ResumeLayout();
        }

        private Control CreateAttentionRow(TuitionFee fee, Student? student, Semester? semester)
        {
            var row = new Panel
            {
                Height = 66,
                BackColor = UITheme.SurfaceAlt,
                Margin = new Padding(0, 0, 0, 6),
                AccessibleName = $"Khoản còn nợ của {student?.FullName ?? $"Sinh viên {fee.StudentId}"}"
            };
            row.Paint += (_, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, row.Width - 1), Math.Max(0, row.Height - 1));
            };

            var name = MakeLabel(student?.FullName ?? $"Sinh viên #{fee.StudentId}", UITheme.FontBold, UITheme.TextPrimary);
            name.Location = new Point(12, 10);
            var detail = MakeLabel($"{student?.StudentCode ?? "—"}  ·  {student?.ClassName ?? "Chưa xếp lớp"}", UITheme.FontSmall, UITheme.TextSecondary);
            detail.Location = new Point(12, 32);
            var status = fee.Status == PaymentStatus.Overdue
                ? $"Quá hạn {(DateTime.Today - (fee.DueDate ?? semester?.DueDate ?? DateTime.Today)).Days:N0} ngày"
                : $"Còn nợ · Hạn {(fee.DueDate ?? semester?.DueDate):dd/MM}";
            var debt = MakeLabel($"{fee.RemainingAmount:N0} ₫", UITheme.FontBold, UITheme.DangerDark);
            debt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var state = MakeLabel(status, UITheme.FontSmall, fee.Status == PaymentStatus.Overdue ? UITheme.DangerDark : UITheme.TextSecondary);
            state.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var view = UITheme.GhostBtn("Xem học phí", 110, 30);
            view.AccessibleName = $"Xem học phí của {student?.FullName ?? $"sinh viên {fee.StudentId}"}";
            view.TabIndex = 0;
            view.Click += (_, _) => _navigateToTuition?.Invoke(fee.StudentId);
            view.Enabled = _navigateToTuition != null;
            row.Controls.AddRange(new Control[] { name, detail, debt, state, view });
            row.Resize += (_, _) =>
            {
                view.Location = new Point(Math.Max(260, row.ClientSize.Width - view.Width - 12), 18);
                debt.Location = new Point(Math.Max(140, view.Left - debt.Width - 16), 10);
                state.Location = new Point(Math.Max(140, view.Left - state.Width - 16), 33);
            };
            return row;
        }

        private void LayoutOverview()
        {
            if (_scroll.ClientSize.Width <= 0) return;
            var contentWidth = Math.Max(720, _scroll.ClientSize.Width - (UITheme.PadPage * 2));
            _content.Width = contentWidth;
            _hero.SetBounds(0, 0, contentWidth, 174);
            _metricStrip.SetBounds(0, 190, contentWidth, 92);
            _attentionCard.SetBounds(0, 298, contentWidth, Math.Max(150, 86 + (_attentionRows.Controls.Count * 72)));
            _content.Height = _attentionCard.Bottom;
            _attentionRows.Width = Math.Max(0, _attentionCard.ClientSize.Width - UITheme.PadCard * 2);
            foreach (Control row in _attentionRows.Controls) row.Width = _attentionRows.ClientSize.Width;

            int half = (_hero.ClientSize.Width - UITheme.PadCard * 2) / 2;
            _semesterName.Location = new Point(UITheme.PadCard, 20);
            _semesterDates.Location = new Point(UITheme.PadCard, 49);
            _dueDate.Location = new Point(UITheme.PadCard, 87);
            _pricePerCredit.Location = new Point(UITheme.PadCard, 115);
            _collectedAmount.Location = new Point(UITheme.PadCard + half, 20);
            _collectionDetail.Location = new Point(UITheme.PadCard + half, 55);
            _progressTrack.Location = new Point(UITheme.PadCard + half, 86);
            _progressTrack.Width = Math.Max(120, half - UITheme.PadCard);
            _progressFill.Width = (int)Math.Round(_collectionRatio * _progressTrack.Width);
            _receiptDetail.Location = new Point(UITheme.PadCard + half, 111);

            int metricWidth = Math.Max(130, _metricStrip.ClientSize.Width / 4);
            for (int i = 0; i < _metricValues.Length; i++)
            {
                var x = UITheme.PadCard + i * metricWidth;
                _metricValues[i].Location = new Point(x, 18);
                _metricTitles[i].Location = new Point(x, 53);
            }
        }

        private static Label MakeLabel(string text, Font font, Color color) => new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoSize = true,
            UseMnemonic = false
        };

        private void DrawHeroDivider(object? sender, PaintEventArgs e)
        {
            var x = _hero.ClientSize.Width / 2;
            using var pen = new Pen(UITheme.Border, 1);
            e.Graphics.DrawLine(pen, x, 18, x, Math.Max(18, _hero.ClientSize.Height - 18));
        }
    }
}
