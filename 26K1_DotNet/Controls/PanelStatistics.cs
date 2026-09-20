using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Reports;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class PanelStatistics : UserControl
    {
        private readonly StudentService _svSvc;
        private readonly SemesterService _semSvc;
        private readonly TuitionService _tuiSvc;
        private readonly ReceiptService _receiptSvc;
        private readonly Form1 _mainForm;

        private ComboBox cmbSem = null!;
        private ComboBox cmbDebtClass = null!;
        private TextBox txtDebtSearch = null!;
        private CheckBox chkOverdueOnly = null!;

        // Containers
        private Panel scroll = null!;
        private TableLayoutPanel cardsTable = null!;
        private Panel progCard = null!;
        private Panel bottomPanel = null!, breakCard = null!, debtCard = null!;

        // Big stat cards
        private Label lblTotVal = null!, lblPaidVal = null!, lblLeftVal = null!, lblCntVal = null!;
        private Label lblPaidPct = null!;
        private Panel panelBar = null!, panelBarFill = null!;
        private Label lblBarDetail = null!;

        // Breakdown
        private Label lblPaidCntVal = null!, lblPartCntVal = null!, lblUnpaidCntVal = null!, lblOverCntVal = null!;

        // Debt / Class / All View
        private DataGridView dgvDebt = null!;
        private Label lblDebtTitle = null!;
        private Button btnQuickPay = null!, btnDebtNotice = null!, btnExportDebt = null!;
        private Button btnToggleDebt = null!, btnToggleClass = null!, btnToggleAll = null!;
        private FlowLayoutPanel debtFilters = null!;
        private List<TuitionFee> _currentDebtFees = new();
        private List<TuitionFee> _currentAllFees = new();
        private int _currentPct = 0;
        private int _viewMode = 0; // 0 = SV Nợ, 1 = Theo Lớp, 2 = Tất cả SV
        private Label _lblEmpty = null!;
        private bool _updatingDebtFilters;
        private string? _sortColumn;
        private bool _sortAscending = true;

        public PanelStatistics(StudentService sv, SemesterService sem, TuitionService tui,
            ReceiptService receiptSvc, Form1 mainForm)
        {
            _svSvc = sv;
            _semSvc = sem;
            _tuiSvc = tui;
            _receiptSvc = receiptSvc;
            _mainForm = mainForm;
            BuildUI();
            RefreshData();
        }

        private void BuildUI()
        {
            BackColor = UITheme.Background;

            // The global semester badge in Form1 is the single semester selector.
            // Keep this unparented combo as lightweight selection state so the existing
            // report/export logic and regression tests share one selected semester value.
            cmbSem = new ComboBox
            {
                Visible = false,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSem.SelectedIndexChanged += (s, e) => LoadStats();

            // ── Scrollable content container ──────────────────────────────
            scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UITheme.Background
            };
            scroll.Resize += (s, e) => LayoutContent();

            // ── 4 Big Stat Cards (TableLayoutPanel) ───────────────────────
            cardsTable = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            cardsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cardsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cardsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cardsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cardsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var c1 = BuildBigCard("Tổng học phí", UITheme.Primary, out lblTotVal);
            var c2 = BuildBigCard("Đã thu",        UITheme.Success, out lblPaidVal);
            var c3 = BuildBigCard("Còn phải thu",  UITheme.Danger,  out lblLeftVal);
            var c4 = BuildBigCard("Sinh viên có học phí", UITheme.Purple, out lblCntVal);

            cardsTable.Controls.Add(c1, 0, 0);
            cardsTable.Controls.Add(c2, 1, 0);
            cardsTable.Controls.Add(c3, 2, 0);
            cardsTable.Controls.Add(c4, 3, 0);

            // ── Progress section ──────────────────────────────────────────
            progCard = new Panel
            {
                BackColor = UITheme.Surface
            };
            progCard.Resize += (s, e) =>
            {
                progCard.Invalidate();
                UpdateProgCardLayout();
            };
            progCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, progCard.Width - 1, progCard.Height - 1);
                using var pen = new Pen(UITheme.Border, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };

            var lblProgTitle = new Label
            {
                Text = "Tiến độ thu học phí (toàn học kỳ)",
                Font = UITheme.FontH2, ForeColor = UITheme.TextPrimary,
                Location = new Point(20, 14), AutoSize = true,
                Parent = progCard
            };

            lblPaidPct = new Label
            {
                Text = "0%",
                Font = UITheme.FontPercent,
                ForeColor = UITheme.Success,
                AutoSize = true,
                Parent = progCard
            };

            panelBar = new Panel
            {
                Location = new Point(20, 44),
                Height = 14,
                BackColor = UITheme.Border,
                Parent = progCard
            };

            panelBarFill = new Panel
            {
                Location = new Point(0, 0),
                Height = 14,
                BackColor = UITheme.Success,
                Parent = panelBar
            };

            lblBarDetail = new Label
            {
                Text = "—", Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                Location = new Point(20, 66), AutoSize = true, Parent = progCard
            };

            // ── Bottom section (Status breakdown + Debt list) ─────────────
            bottomPanel = new Panel
            {
                BackColor = Color.Transparent
            };

            // Status breakdown card
            breakCard = new Panel
            {
                BackColor = UITheme.Surface
            };
            breakCard.Resize += (s, e) => breakCard.Invalidate();
            breakCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, breakCard.Width - 1, breakCard.Height - 1);
                using var pen = new Pen(UITheme.Border, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };

            var statuses = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                Padding = new Padding(16, 8, 16, 8)
            };
            for (int i = 0; i < 4; i++) statuses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            Label AddStatus(string title, Color color, int column)
            {
                var cell = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4) };
                cell.Controls.Add(new Label { Text = title, AutoSize = true, Font = UITheme.FontBody,
                    ForeColor = UITheme.TextSecondary, Location = new Point(0, 0) });
                var value = new Label { Text = "—", AutoSize = true, Font = UITheme.FontH2,
                    ForeColor = color, Location = new Point(0, 24) };
                cell.Controls.Add(value);
                statuses.Controls.Add(cell, column, 0);
                return value;
            }
            lblPaidCntVal = AddStatus("Đã nộp đủ", UITheme.SuccessDark, 0);
            lblPartCntVal = AddStatus("Nộp một phần", UITheme.WarningDark, 1);
            lblUnpaidCntVal = AddStatus("Chưa nộp", UITheme.TextSecondary, 2);
            lblOverCntVal = AddStatus("Quá hạn", UITheme.DangerDark, 3);
            breakCard.Controls.Add(statuses);

            // Debt list card
            debtCard = new Panel
            {
                BackColor = UITheme.Surface
            };
            debtCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, debtCard.Width - 1, debtCard.Height - 1);
                using var pen = new Pen(UITheme.Border, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };

            var debtHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 138,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 8, 12, 4)
            };

            var flowDebtLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lblDebtTitle = new Label
            {
                Text = "Sinh viên còn nợ",
                Font = UITheme.FontH2, ForeColor = UITheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            btnToggleDebt = UITheme.PrimaryBtn("Còn nợ", 95, 32);
            btnToggleDebt.Margin = new Padding(0, 1, 6, 0);
            btnToggleDebt.Click += (s, e) => SetViewMode(0);

            btnToggleAll = UITheme.GhostBtn("Tất cả", 100, 32);
            btnToggleAll.Margin = new Padding(0, 1, 6, 0);
            btnToggleAll.Click += (s, e) => SetViewMode(2);

            btnToggleClass = UITheme.GhostBtn("Theo lớp", 105, 32);
            btnToggleClass.Margin = new Padding(0, 1, 0, 0);
            btnToggleClass.Click += (s, e) => SetViewMode(1);

            flowDebtLeft.Controls.AddRange(new Control[] { lblDebtTitle, btnToggleDebt, btnToggleAll, btnToggleClass });

            var flowDebtRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Height = 36,
                BackColor = Color.Transparent
            };

            btnExportDebt = UITheme.GhostBtn("Xuất CSV", 115, 32);
            btnExportDebt.Margin = new Padding(0, 1, 0, 0);
            btnExportDebt.Click += (s, e) => ExportDebtCsv();

            var btnExportPdf = UITheme.SuccessBtn("Xuất PDF", 115, 32);
            btnExportPdf.Margin = new Padding(6, 1, 0, 0);
            btnExportPdf.Click += (s, e) => ExportDebtPdf();

            btnDebtNotice = UITheme.GhostBtn("Giấy báo nợ", 130, 32);
            btnDebtNotice.Margin = new Padding(6, 1, 0, 0);
            btnDebtNotice.Click += (s, e) => PrintDebtNoticeForSelected();

            btnQuickPay = UITheme.PrimaryBtn("Thu tiền", 115, 32);
            btnQuickPay.Margin = new Padding(6, 1, 0, 0);
            btnQuickPay.Click += (s, e) => QuickPaySelectedDebt();

            flowDebtRight.Controls.AddRange(new Control[] { btnExportDebt, btnExportPdf, btnDebtNotice, btnQuickPay });

            debtFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };
            cmbDebtClass = new ComboBox
            {
                Width = 145, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.FontSmall, BackColor = UITheme.SurfaceAlt, Margin = new Padding(0, 2, 8, 0)
            };
            cmbDebtClass.AccessibleName = "Lọc công nợ theo lớp";
            cmbDebtClass.SelectedIndexChanged += (s, e) => { if (!_updatingDebtFilters && cmbSem.SelectedItem is SemItem) LoadStats(); };
            txtDebtSearch = new TextBox
            {
                Width = 175, Font = UITheme.FontSmall, PlaceholderText = "Tìm mã, tên hoặc lớp",
                Margin = new Padding(0, 2, 8, 0)
            };
            txtDebtSearch.AccessibleName = "Tìm sinh viên trong danh sách công nợ";
            txtDebtSearch.TextChanged += (s, e) => { if (!_updatingDebtFilters && cmbSem.SelectedItem is SemItem) LoadStats(); };
            chkOverdueOnly = new CheckBox
            {
                Visible = false
            };
            chkOverdueOnly.CheckedChanged += (s, e) => { if (!_updatingDebtFilters && cmbSem.SelectedItem is SemItem) LoadStats(); };
            debtFilters.Controls.AddRange(new Control[]
            {
                new Label { Text = "Lớp:", AutoSize = true, Font = UITheme.FontSmall,
                    ForeColor = UITheme.TextSecondary, Margin = new Padding(0, 7, 5, 0) },
                cmbDebtClass,
                new Label { Text = "Tìm SV:", AutoSize = true, Font = UITheme.FontSmall,
                    ForeColor = UITheme.TextSecondary, Margin = new Padding(0, 7, 5, 0) },
                txtDebtSearch
            });

            debtHeader.Controls.Add(flowDebtLeft);
            debtHeader.Controls.Add(flowDebtRight);
            debtHeader.Controls.Add(debtFilters);

            dgvDebt = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.StyleGrid(dgvDebt);
            dgvDebt.CellPainting += DgvDebt_CellPainting;
            dgvDebt.ColumnHeaderMouseClick += DgvDebt_ColumnHeaderMouseClick;
            dgvDebt.DoubleClick += (s, e) =>
            {
                if (_viewMode != 1) QuickPaySelectedDebt();
            };
            dgvDebt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; if (_viewMode != 1) QuickPaySelectedDebt(); } };

            _lblEmpty = UITheme.CreateEmptyStateLabel("Không có sinh viên nào nợ học phí trong học kỳ này.");

            var dgvWrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 12, 12),
                BackColor = Color.Transparent
            };
            dgvWrap.Controls.Add(_lblEmpty);
            dgvWrap.Controls.Add(dgvDebt);
            dgvDebt.BringToFront();
            _lblEmpty.BringToFront();

            debtCard.Controls.Add(dgvWrap);
            debtCard.Controls.Add(debtHeader);

            scroll.Controls.AddRange(new Control[] { cardsTable, progCard, breakCard, debtCard });

            Controls.Add(scroll);

            LayoutContent();
        }

        private void LayoutContent()
        {
            if (scroll == null || cardsTable == null || progCard == null || breakCard == null || debtCard == null) return;

            int pad = 24;
            int availableWidth = Math.Max(720, scroll.ClientSize.Width - (pad * 2));

            // Row 1: KPI Summary cards
            cardsTable.Location = new Point(pad, 12);
            cardsTable.Size = new Size(availableWidth, 92);

            // Row 2: Progress (Left) & Status breakdown (Right)
            int gap = 12;
            int progW = (availableWidth - gap) / 2;
            int breakW = availableWidth - gap - progW;
            int row2Y = 114;
            int row2H = 88;

            progCard.Location = new Point(pad, row2Y);
            progCard.Size = new Size(progW, row2H);
            UpdateProgCardLayout();

            breakCard.Location = new Point(pad + progW + gap, row2Y);
            breakCard.Size = new Size(breakW, row2H);

            // Row 3: Debt list table
            int row3Y = row2Y + row2H + 12;
            int row3H = Math.Max(260, scroll.ClientSize.Height - row3Y - 16);

            debtCard.Location = new Point(pad, row3Y);
            debtCard.Size = new Size(availableWidth, row3H);

            scroll.AutoScrollMinSize = new Size(0, row3Y + 260 + 16);
        }

        private void UpdateProgCardLayout()
        {
            if (progCard == null || panelBar == null || lblPaidPct == null) return;
            lblPaidPct.Location = new Point(Math.Max(150, progCard.Width - 20 - lblPaidPct.Width), 14);
            panelBar.Width = Math.Max(10, progCard.Width - 40);
            UpdateProgressBar();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            LayoutContent();
        }

        private static Panel BuildBigCard(string title, Color accent, out Label valLbl)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 6, 0),
                BackColor = UITheme.Surface
            };
            card.Resize += (s, e) => card.Invalidate();
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var pen = new Pen(UITheme.Border, 1))
                using (var path = UITheme.GetRoundedPath(rect, 8))
                {
                    e.Graphics.DrawPath(pen, path);
                }
                using (var accentBrush = new SolidBrush(accent))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 8, 4, Math.Max(0, card.Height - 16));
                }
            };

            new Label
            {
                Text = title, AutoSize = true,
                Location = new Point(14, 12),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Parent = card
            };

            valLbl = new Label
            {
                Text = "—", AutoSize = true,
                Location = new Point(14, 34),
                Font = UITheme.FontCardValue,
                ForeColor = accent,
                Parent = card
            };

            return card;
        }

        public void RefreshData(int? targetSemesterId = null)
        {
            int? previousId = targetSemesterId ?? (cmbSem.SelectedItem as SemItem)?.Id;
            cmbSem.Items.Clear();
            foreach (var s in _semSvc.GetAll()) cmbSem.Items.Add(new SemItem(s.Id, s.Name));

            int? targetId = targetSemesterId ?? previousId ?? _semSvc.GetActive()?.Id;
            cmbSem.SelectedIndex = cmbSem.Items.Count > 0 ? 0 : -1;
            if (targetId.HasValue)
                for (int i = 0; i < cmbSem.Items.Count; i++)
                    if (cmbSem.Items[i] is SemItem si && si.Id == targetId) { cmbSem.SelectedIndex = i; break; }

            LoadStats();
        }

        private void LoadStats()
        {
            _tuiSvc.RefreshStatuses(_semSvc.GetAll());
            if (cmbSem.SelectedItem is not SemItem si) return;

            var stats = _tuiSvc.GetStatistics(si.Id, si.Name);
            var svDict = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
            var fees = _tuiSvc.GetBySemester(si.Id);
            PopulateClassFilter(svDict);

            // Big cards
            lblTotVal.Text  = $"{stats.TotalAmount:N0} ₫";
            lblPaidVal.Text = $"{stats.TotalPaid:N0} ₫";
            lblLeftVal.Text = $"{stats.TotalRemaining:N0} ₫";
            lblCntVal.Text  = $"{stats.TotalStudents} SV";

            // Progress bar
            _currentPct = stats.TotalAmount > 0 ? (int)(stats.TotalPaid / stats.TotalAmount * 100) : 0;
            panelBarFill.BackColor = _currentPct >= 80 ? UITheme.Success : _currentPct >= 50 ? UITheme.Warning : UITheme.Danger;
            lblPaidPct.Text = $"{_currentPct}%";
            lblPaidPct.ForeColor = panelBarFill.BackColor;
            lblBarDetail.Text = $"Đã thu  {stats.TotalPaid:N0} ₫  trên tổng  {stats.TotalAmount:N0} ₫";
            UpdateProgressBar();

            // Breakdown
            lblPaidCntVal.Text   = $"{stats.PaidCount} SV";
            lblPartCntVal.Text   = $"{stats.PartialCount} SV";
            lblUnpaidCntVal.Text = $"{stats.UnpaidCount} SV";
            lblOverCntVal.Text   = $"{stats.OverdueCount} SV";

            // Debt table
            // Debt table or Class table or All Students
            if (_viewMode == 1)
            {
                LoadClassStats(si.Id);
                return;
            }

            if (_viewMode == 2)
            {
                LoadAllStudents(si.Id, fees, svDict);
                return;
            }

            _currentDebtFees = SortFees(BuildFilteredDebtFees(fees, svDict), svDict).ToList();
            lblDebtTitle.Text = "Sinh viên còn nợ";

            var currentSem = _semSvc.GetById(si.Id);
            var rows = _currentDebtFees.Select(f => new
            {
                f.Id,
                _HidId = f.StudentId,
                MaSV     = svDict.TryGetValue(f.StudentId, out var studentCode) ? studentCode.StudentCode : "—",
                HoTen    = svDict.TryGetValue(f.StudentId, out var sv) ? sv.FullName : $"#{f.StudentId}",
                Lop      = svDict.TryGetValue(f.StudentId, out var sv2) ? sv2.ClassName : "",
                PhaiNop  = f.TotalAmount,
                DaNop    = f.PaidAmount,
                ConLai   = f.RemainingAmount,
                HanNop   = (f.DueDate ?? currentSem?.DueDate)?.ToString("dd/MM/yyyy") ?? "—",
                TrangThai = f.StatusDisplayText
            }).ToList();

            dgvDebt.DataSource = null;
            dgvDebt.DataSource = rows;

            if (_currentDebtFees.Count == 0)
            {
                _lblEmpty.Text = "Không có sinh viên phù hợp bộ lọc công nợ.";
                _lblEmpty.Visible = true;
            }
            else
            {
                _lblEmpty.Visible = false;
            }

            FormatStudentFeeGrid(isDebtOnly: true);
        }

        private void SetViewMode(int mode)
        {
            _viewMode = mode;

            // Reset all buttons to ghost style
            foreach (var btn in new[] { btnToggleDebt, btnToggleAll, btnToggleClass })
            {
                btn.BackColor = UITheme.SurfaceAlt;
                btn.ForeColor = UITheme.TextPrimary;
            }

            // Highlight active button
            Button activeBtn = mode switch { 1 => btnToggleClass, 2 => btnToggleAll, _ => btnToggleDebt };
            activeBtn.BackColor = UITheme.Primary;
            activeBtn.ForeColor = Color.White;

            // Toggle action buttons visibility
            btnQuickPay.Visible = mode != 1;
            btnDebtNotice.Visible = mode == 0;
            debtFilters.Visible = mode == 0;

            lblDebtTitle.Text = mode switch
            {
                1 => "Tình hình theo lớp",
                2 => "Tất cả sinh viên",
                _ => "Sinh viên còn nợ"
            };

            if (cmbSem.SelectedItem is SemItem si)
            {
                if (mode == 1)
                    LoadClassStats(si.Id);
                else
                    LoadStats();
            }
        }

        private void PopulateClassFilter(IReadOnlyDictionary<int, Student> students)
        {
            string selectedClass = cmbDebtClass.SelectedItem as string ?? "— Tất cả lớp —";
            var classes = students.Values.Select(student => student.ClassName)
                .Where(className => !string.IsNullOrWhiteSpace(className))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(className => className, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _updatingDebtFilters = true;
            try
            {
                cmbDebtClass.Items.Clear();
                cmbDebtClass.Items.Add("— Tất cả lớp —");
                foreach (var className in classes) cmbDebtClass.Items.Add(className);
                int index = cmbDebtClass.FindStringExact(selectedClass);
                cmbDebtClass.SelectedIndex = index >= 0 ? index : 0;
            }
            finally { _updatingDebtFilters = false; }
        }

        private List<TuitionFee> BuildFilteredDebtFees(IEnumerable<TuitionFee> fees, IReadOnlyDictionary<int, Student> students)
        {
            string selectedClass = cmbDebtClass.SelectedItem as string ?? "— Tất cả lớp —";
            string search = txtDebtSearch.Text.Trim();
            bool onlyOverdue = chkOverdueOnly.Checked;

            return fees.Where(fee => fee.RemainingAmount > 0)
                .Where(fee => !onlyOverdue || fee.Status == PaymentStatus.Overdue)
                .Where(fee => selectedClass == "— Tất cả lớp —" ||
                    (students.TryGetValue(fee.StudentId, out var studentForClass) &&
                     string.Equals(studentForClass.ClassName, selectedClass, StringComparison.CurrentCultureIgnoreCase)))
                .Where(fee => string.IsNullOrWhiteSpace(search) ||
                    (students.TryGetValue(fee.StudentId, out var studentForCode) &&
                     studentForCode.StudentCode.Contains(search, StringComparison.CurrentCultureIgnoreCase)) ||
                    (students.TryGetValue(fee.StudentId, out var studentForSearch) &&
                     (studentForSearch.FullName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                      studentForSearch.ClassName.Contains(search, StringComparison.CurrentCultureIgnoreCase))))
                .OrderByDescending(fee => fee.RemainingAmount)
                .ToList();
        }

        private void LoadClassStats(int semesterId)
        {
            var fees = _tuiSvc.GetBySemester(semesterId);
            var svDict = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
            var classGroups = SortClassRows(BuildClassStats(fees, svDict));

            dgvDebt.DataSource = null;
            dgvDebt.DataSource = classGroups;

            if (classGroups.Count == 0)
            {
                _lblEmpty.Text = "Chưa có dữ liệu học phí cho học kỳ này.";
                _lblEmpty.Visible = true;
            }
            else
            {
                _lblEmpty.Visible = false;
            }

            if (dgvDebt.Columns.Count > 0)
            {
                if (dgvDebt.Columns["Lop"] is { } cl)
                {
                    cl.HeaderText = "Lớp học";
                    cl.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    cl.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    cl.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    cl.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
                    cl.HeaderCell.Style.Padding = new Padding(8, 0, 4, 0);
                }
                if (dgvDebt.Columns["SoSV"] is { } csv)
                {
                    csv.HeaderText = "Số SV";
                    csv.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    csv.Width = 85;
                    csv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    csv.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvDebt.Columns["PhaiThu"] is { } cpt)
                {
                    cpt.HeaderText = "Phải thu";
                    cpt.DefaultCellStyle.Format = "N0";
                    cpt.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    cpt.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    cpt.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    cpt.Width = 130;
                    cpt.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                    cpt.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
                }
                if (dgvDebt.Columns["DaThu"] is { } cdt)
                {
                    cdt.HeaderText = "Đã thu";
                    cdt.DefaultCellStyle.Format = "N0";
                    cdt.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    cdt.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    cdt.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    cdt.Width = 130;
                    cdt.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                    cdt.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
                }
                if (dgvDebt.Columns["ConNo"] is { } ccn)
                {
                    ccn.HeaderText = "Còn nợ";
                    ccn.DefaultCellStyle.Format = "N0";
                    ccn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    ccn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    ccn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    ccn.Width = 130;
                    ccn.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                    ccn.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
                }
                if (dgvDebt.Columns["TyLe"] is { } ctl)
                {
                    ctl.HeaderText = "Tiến độ";
                    ctl.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    ctl.Width = 110;
                    ctl.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    ctl.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                foreach (var column in new[] { "Lop", "SoSV", "PhaiThu", "DaThu", "ConNo", "TyLe" })
                    ConfigureSortableColumn(column);
                ShowSortGlyph();
            }
        }

        private void LoadAllStudents(int semesterId, List<TuitionFee> fees, Dictionary<int, Student> svDict)
        {
            _currentAllFees = SortFees(fees, svDict).ToList();

            lblDebtTitle.Text = $"Tất cả sinh viên ({_currentAllFees.Count} SV)";

            var currentSem = _semSvc.GetById(semesterId);
            var rows = _currentAllFees.Select(f => new
            {
                f.Id,
                _HidId = f.StudentId,
                MaSV     = svDict.TryGetValue(f.StudentId, out var studentCode) ? studentCode.StudentCode : "—",
                HoTen    = svDict.TryGetValue(f.StudentId, out var sv) ? sv.FullName : $"#{f.StudentId}",
                Lop      = svDict.TryGetValue(f.StudentId, out var sv2) ? sv2.ClassName : "",
                PhaiNop  = f.TotalAmount,
                DaNop    = f.PaidAmount,
                ConLai   = f.RemainingAmount,
                HanNop   = (f.DueDate ?? currentSem?.DueDate)?.ToString("dd/MM/yyyy") ?? "—",
                TrangThai = f.StatusDisplayText
            }).ToList();

            dgvDebt.DataSource = null;
            dgvDebt.DataSource = rows;

            if (_currentAllFees.Count == 0)
            {
                _lblEmpty.Text = "Chưa có sinh viên nào trong học kỳ này.";
                _lblEmpty.Visible = true;
            }
            else
            {
                _lblEmpty.Visible = false;
            }

            FormatStudentFeeGrid(isDebtOnly: false);
        }

        private void FormatStudentFeeGrid(bool isDebtOnly)
        {
            if (dgvDebt.Columns.Count == 0) return;

            if (dgvDebt.Columns["Id"] is { } cId) cId.Visible = false;
            if (dgvDebt.Columns["_HidId"] is { } c0) c0.Visible = false;

            if (dgvDebt.Columns["MaSV"] is { } cMa)
            {
                cMa.HeaderText = "Mã SV";
                cMa.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                cMa.Width = 90;
                cMa.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cMa.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cMa.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
                cMa.HeaderCell.Style.Padding = new Padding(2, 0, 2, 0);
            }
            if (dgvDebt.Columns["HoTen"] is { } c1)
            {
                c1.HeaderText = "Họ và tên";
                c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                c1.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                c1.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
                c1.HeaderCell.Style.Padding = new Padding(8, 0, 4, 0);
            }
            if (dgvDebt.Columns["Lop"] is { } c2)
            {
                c2.HeaderText = "Lớp";
                c2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c2.Width = 105;
                c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c2.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c2.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
                c2.HeaderCell.Style.Padding = new Padding(2, 0, 2, 0);
            }
            if (dgvDebt.Columns["PhaiNop"] is { } c3)
            {
                c3.HeaderText = "Phải nộp";
                c3.DefaultCellStyle.Format = "N0";
                c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                c3.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c3.Width = 115;
                c3.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                c3.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
            }
            if (dgvDebt.Columns["DaNop"] is { } c4)
            {
                c4.HeaderText = "Đã nộp";
                c4.DefaultCellStyle.Format = "N0";
                c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                c4.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c4.Width = 115;
                c4.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                c4.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
            }
            if (dgvDebt.Columns["ConLai"] is { } c5)
            {
                c5.HeaderText = isDebtOnly ? "Còn nợ" : "Còn lại";
                c5.DefaultCellStyle.Format = "N0";
                c5.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                c5.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c5.Width = 115;
                c5.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                c5.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
            }
            if (dgvDebt.Columns["HanNop"] is { } cHan)
            {
                cHan.HeaderText = "Hạn nộp";
                cHan.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cHan.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cHan.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                cHan.Width = 110;
                cHan.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
                cHan.HeaderCell.Style.Padding = new Padding(2, 0, 2, 0);
                // At the minimum supported window width, prioritize student names and balances.
                // The due date remains available in wider layouts and in exported reports.
                cHan.Visible = debtCard.Width >= 900;
            }
            if (dgvDebt.Columns["TrangThai"] is { } c6)
            {
                c6.HeaderText = "Trạng thái";
                c6.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c6.Width = 125;
                c6.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c6.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c6.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
                c6.HeaderCell.Style.Padding = new Padding(2, 0, 2, 0);
            }

            foreach (var column in new[] { "MaSV", "HoTen", "Lop", "PhaiNop", "DaNop", "ConLai", "HanNop", "TrangThai" })
                ConfigureSortableColumn(column);
            ShowSortGlyph();
        }

        private IEnumerable<TuitionFee> SortFees(IEnumerable<TuitionFee> fees, IReadOnlyDictionary<int, Student> students)
        {
            Func<TuitionFee, string> defaultKey = fee =>
                students.TryGetValue(fee.StudentId, out var student) ? student.ClassName : string.Empty;
            if (_sortColumn == null)
                return fees.OrderBy(defaultKey, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(fee => students.TryGetValue(fee.StudentId, out var student) ? student.FullName : string.Empty,
                        StringComparer.CurrentCultureIgnoreCase);

            return _sortColumn switch
            {
                "MaSV" => SortBy(fees, fee => students.TryGetValue(fee.StudentId, out var student) ? student.StudentCode : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase),
                "Lop" => SortBy(fees, defaultKey, StringComparer.CurrentCultureIgnoreCase),
                "PhaiNop" => SortBy(fees, fee => fee.TotalAmount),
                "DaNop" => SortBy(fees, fee => fee.PaidAmount),
                "ConLai" => SortBy(fees, fee => fee.RemainingAmount),
                "HanNop" => SortBy(fees, fee => fee.DueDate ?? DateTime.MaxValue),
                "TrangThai" => SortBy(fees, fee => fee.Status),
                _ => SortBy(fees, fee => students.TryGetValue(fee.StudentId, out var student) ? student.FullName : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase)
            };

            IEnumerable<TuitionFee> SortBy<TKey>(IEnumerable<TuitionFee> source, Func<TuitionFee, TKey> key, IComparer<TKey>? comparer = null) =>
                _sortAscending ? source.OrderBy(key, comparer) : source.OrderByDescending(key, comparer);
        }

        private void DgvDebt_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            string column = dgvDebt.Columns[e.ColumnIndex].Name;
            string[] allowed = _viewMode == 1
                ? new[] { "Lop", "SoSV", "PhaiThu", "DaThu", "ConNo", "TyLe" }
                : new[] { "MaSV", "HoTen", "Lop", "PhaiNop", "DaNop", "ConLai", "HanNop", "TrangThai" };
            if (!allowed.Contains(column)) return;

            if (_sortColumn == column) _sortAscending = !_sortAscending;
            else { _sortColumn = column; _sortAscending = true; }
            LoadStats();
        }

        private List<ClassStatRow> SortClassRows(List<ClassStatRow> rows)
        {
            if (_sortColumn == null) return rows;
            IEnumerable<ClassStatRow> sorted = _sortColumn switch
            {
                "Lop" => SortBy(rows, row => row.Lop, StringComparer.CurrentCultureIgnoreCase),
                "SoSV" => SortBy(rows, row => row.SoSV),
                "PhaiThu" => SortBy(rows, row => row.PhaiThu),
                "DaThu" => SortBy(rows, row => row.DaThu),
                "ConNo" => SortBy(rows, row => row.ConNo),
                "TyLe" => SortBy(rows, row => int.TryParse(row.TyLe.TrimEnd('%'), out var value) ? value : 0),
                _ => rows
            };
            return sorted.ToList();

            IEnumerable<ClassStatRow> SortBy<TKey>(IEnumerable<ClassStatRow> source, Func<ClassStatRow, TKey> key, IComparer<TKey>? comparer = null) =>
                _sortAscending ? source.OrderBy(key, comparer) : source.OrderByDescending(key, comparer);
        }

        private void ConfigureSortableColumn(string name)
        {
            if (dgvDebt.Columns[name] is { } column)
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        private void ShowSortGlyph()
        {
            foreach (DataGridViewColumn column in dgvDebt.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (_sortColumn != null && dgvDebt.Columns[_sortColumn] is { } sorted)
                sorted.HeaderCell.SortGlyphDirection = _sortAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void QuickPaySelectedDebt()
        {
            if (dgvDebt.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một sinh viên trong bảng nợ!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int feeId = dgvDebt.SelectedRows[0].Cells["Id"].Value is int v ? v : 0;
            var fee = _tuiSvc.GetById(feeId);
            if (fee == null) return;

            // Direct link: Open payment dialog right here
            using var payForm = new FormPayment(fee, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService,
                _mainForm.MomoGateway, _mainForm.GatewayPaymentPersistence, _mainForm.MomoSettingsService);
            if (payForm.ShowDialog() == DialogResult.OK)
            {
                LoadStats();
            }
        }

        private void PrintDebtNoticeForSelected()
        {
            if (dgvDebt.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một sinh viên trong bảng nợ!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int feeId = dgvDebt.SelectedRows[0].Cells["Id"].Value is int v ? v : 0;
            var fee = _tuiSvc.GetById(feeId);
            if (fee == null) return;
            var sv = _svSvc.GetStudentById(fee.StudentId);
            var sem = _semSvc.GetById(fee.SemesterId);
            if (sv == null || sem == null) return;

            using var f = new FormDebtNotice(fee, sv, sem, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService,
                _mainForm.MomoGateway, _mainForm.GatewayPaymentPersistence, _mainForm.MomoSettingsService);
            if (f.ShowDialog() == DialogResult.OK)
            {
                LoadStats();
            }
        }

        private void UpdateProgressBar()
        {
            if (panelBar == null || panelBarFill == null) return;
            panelBarFill.Width = Math.Max(0, (int)(panelBar.Width * _currentPct / 100.0));
        }

        private void DgvDebt_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvDebt.Columns.Count == 0 || e.Graphics == null) return;

            if (_viewMode == 1)
            {
                var col = dgvDebt.Columns["TyLe"];
                if (col != null && e.ColumnIndex == col.Index && e.Value != null)
                {
                    e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                    string pctStr = e.Value.ToString() ?? "0%";
                    int.TryParse(pctStr.Replace("%", ""), out int pctVal);
                    Color bg = pctVal >= 80 ? UITheme.SuccessLight : pctVal >= 50 ? UITheme.WarningLight : UITheme.DangerLight;
                    Color fg = pctVal >= 80 ? UITheme.SuccessDark : pctVal >= 50 ? UITheme.WarningDark : UITheme.DangerDark;
                    UITheme.DrawCustomBadge(e.Graphics, e.CellBounds, pctStr, bg, fg);
                    e.Handled = true;
                }
            }
            else
            {
                var col = dgvDebt.Columns["TrangThai"];
                if (col != null && e.ColumnIndex == col.Index && e.Value != null)
                {
                    e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                    UITheme.DrawStatusBadge(e.Graphics, e.CellBounds, e.Value.ToString() ?? "");
                    e.Handled = true;
                }
            }
        }

        private void ExportDebtCsv()
        {
            string semName = (cmbSem.SelectedItem as SemItem)?.Name ?? "HocKy";

            if (_viewMode == 1)
            {
                if (cmbSem.SelectedItem is not SemItem si) return;
                var fees = _tuiSvc.GetBySemester(si.Id);
                var svDict = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
                var classGroups = BuildClassStats(fees, svDict);

                var cols = new List<(string Header, Func<ClassStatRow, object> ValueGetter)>
                {
                    ("Lớp Học", r => r.Lop),
                    ("Số Lượng Sinh Viên", r => r.SoSV),
                    ("Tổng Học Phí Phải Thu (VNĐ)", r => r.PhaiThu),
                    ("Đã Thu (VNĐ)", r => r.DaThu),
                    ("Còn Nợ (VNĐ)", r => r.ConNo),
                    ("Tỷ Lệ Hoàn Thành", r => r.TyLe)
                };

                CsvExportHelper.ExportToCsv($"ThongKeHocPhiTheoLop_{semName}.csv", classGroups, cols);
                return;
            }

            var svDict2 = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
            var currentSem = (cmbSem.SelectedItem as SemItem) is { } selected
                ? _semSvc.GetById(selected.Id)
                : null;
            var cols2 = new List<(string Header, Func<TuitionFee, object> ValueGetter)>
            {
                ("Mã HP", f => f.Id),
                ("Mã SV", f => svDict2.TryGetValue(f.StudentId, out var sv) ? sv.StudentCode : ""),
                ("Họ và Tên", f => svDict2.TryGetValue(f.StudentId, out var sv) ? sv.FullName : ""),
                ("Lớp", f => svDict2.TryGetValue(f.StudentId, out var sv) ? sv.ClassName : ""),
                ("Số tín chỉ", f => f.Credits),
                ("Phải nộp (VNĐ)", f => f.TotalAmount),
                ("Đã nộp (VNĐ)", f => f.PaidAmount),
                ("Còn nợ (VNĐ)", f => f.RemainingAmount),
                ("Hạn nộp", f => (f.DueDate ?? currentSem?.DueDate)?.ToString("dd/MM/yyyy") ?? ""),
                ("Trạng thái", f => f.StatusDisplayText)
            };

            var exportData = _viewMode == 2 ? _currentAllFees : _currentDebtFees;
            string fileName = _viewMode == 2 ? $"TatCaSinhVien_{semName}.csv" : $"DanhSachNoHocPhi_{semName}.csv";
            CsvExportHelper.ExportToCsv(fileName, exportData, cols2);
        }

        private void ExportDebtPdf()
        {
            if (cmbSem.SelectedItem is not SemItem selectedSemester) return;
            var students = _svSvc.GetAllStudents().ToDictionary(student => student.Id);
            var semester = _semSvc.GetById(selectedSemester.Id);
            List<DebtReportRow> rows;
            if (_viewMode == 1)
            {
                var classStats = BuildClassStats(_tuiSvc.GetBySemester(selectedSemester.Id), students);
                rows = classStats.Select(item => new DebtReportRow(
                    item.Lop, $"{item.SoSV} sinh viên", item.Lop, item.PhaiThu, item.DaThu,
                    item.ConNo, semester?.DueDate, item.TyLe)).ToList();
            }
            else
            {
                var fees = _viewMode == 2 ? _currentAllFees : _currentDebtFees;
                rows = fees.Select(fee =>
                {
                    students.TryGetValue(fee.StudentId, out var student);
                    return new DebtReportRow(
                        student?.StudentCode ?? "—", student?.FullName ?? "Không tìm thấy hồ sơ",
                        student?.ClassName ?? string.Empty, fee.TotalAmount, fee.PaidAmount,
                        fee.RemainingAmount, fee.DueDate ?? semester?.DueDate, fee.StatusDisplayText);
                }).ToList();
            }

            string viewDescription = _viewMode switch
            {
                1 => "Tổng hợp theo lớp",
                2 => "Tất cả sinh viên",
                _ => "Sinh viên còn nợ"
            };
            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"BaoCaoCongNo_{selectedSemester.Name}.pdf",
                Title = "Xuất báo cáo công nợ PDF"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                DebtReportPdfRenderer.Export(dialog.FileName, new DebtReportPdfData(
                    "TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT", selectedSemester.Name,
                    viewDescription, DateTime.Now, rows));
                UiFeedback.ShowSuccess($"Đã xuất báo cáo PDF:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Không thể xuất báo cáo công nợ PDF");
            }
        }

        private record ClassStatRow(string Lop, int SoSV, decimal PhaiThu, decimal DaThu, decimal ConNo, string TyLe);

        private static List<ClassStatRow> BuildClassStats(IEnumerable<TuitionFee> fees, IReadOnlyDictionary<int, Student> students)
        {
            return fees
                .GroupBy(fee => students.TryGetValue(fee.StudentId, out var student) && !string.IsNullOrWhiteSpace(student.ClassName)
                    ? student.ClassName
                    : "Chưa phân lớp")
                .Select(group =>
                {
                    decimal total = group.Sum(fee => fee.TotalAmount);
                    decimal paid = group.Sum(fee => fee.PaidAmount);
                    decimal remaining = total - paid;
                    int percent = total > 0 ? (int)(paid / total * 100) : 100;
                    return new ClassStatRow(group.Key, group.Select(fee => fee.StudentId).Distinct().Count(), total, paid, remaining, $"{percent}%");
                })
                .OrderByDescending(row => row.ConNo)
                .ToList();
        }
    }
}
