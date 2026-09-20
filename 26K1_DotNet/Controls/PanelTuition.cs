using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class PanelTuition : UserControl
    {
        private readonly StudentService _svSvc;
        private readonly SemesterService _semSvc;
        private readonly TuitionService _tuiSvc;
        private readonly ReceiptService _receiptSvc;
        private readonly Form1 _mainForm;

        private ComboBox cmbSem = null!, cmbStatus = null!;
        private TextBox txtSearch = null!;
        private DataGridView dgv = null!;
        private Label _lblEmpty = null!;

        private Label lblTotalVal = null!, lblPaidVal = null!, lblLeftVal = null!, lblCountVal = null!;
        private Button btnBatch = null!, btnAdd = null!, btnPay = null!, btnHistory = null!, btnExport = null!, btnMore = null!;
        private Label lblStudentContext = null!;
        private readonly ContextMenuStrip _selectionMenu = new();
        private List<TuitionFee> _current = new();
        private int? _studentContextId;
        private bool _suppressFilterEvents;
        private string? _sortColumn;
        private bool _sortAscending = true;

        public PanelTuition(StudentService sv, SemesterService sem, TuitionService tui,
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

            // ── Actions, search, and filters toolbar ──────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = UITheme.Surface,
                Padding = new Padding(24, 10, 24, 8)
            };
            toolbar.Controls.Add(UITheme.HSep(DockStyle.Bottom));

            // Row 1: Actions (Left) & Search (Right)
            var row1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            var flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            btnAdd = UITheme.PrimaryBtn("+ Lập phiếu", 115, 34);
            btnBatch = UITheme.GhostBtn("Tạo theo lớp", 125, 34);
            btnExport = UITheme.GhostBtn("Xuất CSV", 95, 34);
            btnAdd.Margin = new Padding(0, 1, 8, 0);
            btnBatch.Margin = new Padding(0, 1, 8, 0);
            btnExport.Margin = new Padding(0, 1, 0, 0);
            btnAdd.Click += BtnAdd_Click;
            btnBatch.Click += BtnBatch_Click;
            btnExport.Click += (s, e) => ExportTuitionCsv();
            flowActions.Controls.AddRange(new Control[] { btnAdd, btnBatch, btnExport });

            var flowSearch = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lblSearch = new Label
            {
                Text = "Tìm kiếm:",
                AutoSize = true,
                Margin = new Padding(0, 8, 6, 0),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary
            };
            txtSearch = UITheme.MakeSearchBox("Tìm tên, mã SV, lớp, SĐT...", 210, 32);
            txtSearch.Margin = new Padding(0, 1, 6, 0);
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    LoadData();
                }
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            var btnFilter = UITheme.GhostBtn("Tìm", 55, 32);
            btnFilter.Margin = new Padding(0, 1, 6, 0);
            btnFilter.Click += (s, e) => LoadData();

            var btnReset = UITheme.GhostBtn("Xóa lọc", 72, 32);
            btnReset.Margin = new Padding(0, 1, 0, 0);
            btnReset.Click += (s, e) => ResetFilters();

            flowSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnFilter, btnReset });

            row1.Controls.Add(flowSearch);
            row1.Controls.Add(flowActions);

            // Row 2: Status filter + Student Context badge. The semester is selected
            // globally from the badge in Form1, so it is not repeated here.
            var row2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

            var flowFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            cmbSem = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };

            var lblStatus = new Label
            {
                Text = "Trạng thái:",
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary
            };
            cmbStatus = new ComboBox
            {
                Width = 120,
                Height = 30,
                Font = UITheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt,
                Margin = new Padding(0, 2, 14, 0)
            };
            cmbStatus.Items.AddRange(new object[] { "Tất cả", "Chưa nộp", "Nộp 1 phần", "Đã nộp đủ", "Nộp muộn", "Quá hạn" });
            cmbStatus.SelectedIndex = 0;

            lblStudentContext = new Label
            {
                AutoSize = true,
                Visible = false,
                Cursor = Cursors.Hand,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.Primary,
                Margin = new Padding(4, 8, 0, 0)
            };
            lblStudentContext.Click += (s, e) => ResetFilters();

            flowFilters.Controls.AddRange(new Control[] {
                lblStatus, cmbStatus,
                lblStudentContext
            });

            row2.Controls.Add(flowFilters);

            cmbSem.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;
            cmbStatus.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            toolbar.Controls.Add(row2);
            toolbar.Controls.Add(row1);
            toolbar.Controls.SetChildIndex(row1, 0);
            toolbar.Controls.SetChildIndex(row2, 1);

            // ── Summary cards (Responsive TableLayoutPanel) ───────────────
            var summaryRow = new Panel
            {
                Dock = DockStyle.Top, Height = 106,
                BackColor = UITheme.Background,
                Padding = new Padding(28, 12, 28, 12)
            };

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var c1 = MakeCard("Tổng phải thu", UITheme.Primary, out lblTotalVal);
            var c2 = MakeCard("Đã thu",        UITheme.Success, out lblPaidVal);
            var c3 = MakeCard("Còn phải thu",  UITheme.Danger,  out lblLeftVal);
            var c4 = MakeCard("Số phiếu",      UITheme.Purple,  out lblCountVal);

            tableLayout.Controls.Add(c1, 0, 0);
            tableLayout.Controls.Add(c2, 1, 0);
            tableLayout.Controls.Add(c3, 2, 0);
            tableLayout.Controls.Add(c4, 3, 0);
            summaryRow.Controls.Add(tableLayout);

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill, MultiSelect = false };
            UITheme.StyleGrid(dgv);
            dgv.ContextMenuStrip = _selectionMenu;
            dgv.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = dgv.HitTest(e.X, e.Y);
                    if (hit.RowIndex >= 0)
                    {
                        dgv.ClearSelection();
                        dgv.Rows[hit.RowIndex].Selected = true;
                    }
                }
            };
            dgv.CellPainting += Dgv_CellPainting;
            dgv.ColumnHeaderMouseClick += Dgv_ColumnHeaderMouseClick;
            dgv.SelectionChanged += (s, e) => UpdateSelectionActions();
            dgv.DataBindingComplete += (s, e) => UpdateSelectionActions();
            dgv.DoubleClick += (s, e) => ViewSelectedReceiptHistory();
            dgv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; ViewSelectedReceiptHistory(); } };

            _lblEmpty = UITheme.CreateEmptyStateLabel("Chưa có phiếu học phí\nChọn học kỳ và bấm «+ Lập phiếu» để bắt đầu");

            // ── Selection actions ─────────────────────────────────────────
            var actionBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.Surface
            };
            actionBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            btnPay            = UITheme.PrimaryBtn("Thu tiền", 120, 36);
            btnHistory        = UITheme.GhostBtn  ("Lịch sử thanh toán", 170, 36);
            btnMore           = UITheme.GhostBtn  ("Tác vụ khác ▾", 130, 36);
            btnPay.Margin = btnHistory.Margin = btnMore.Margin =
                new Padding(3, 0, 3, 0);

            btnPay.Click            += BtnPay_Click;
            btnHistory.Click        += (s, e) => ViewSelectedReceiptHistory();
            ConfigureSelectionMenu();
            btnMore.Click += (s, e) => _selectionMenu.Show(btnMore, new Point(0, btnMore.Height));

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 14, 20, 0),
                BackColor = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] {
                btnPay, btnHistory, btnMore
            });
            actionBar.Controls.Add(flow);

            // ── Card wrapper (Generous breathing room) ────────────────────
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 4, 28, 20), BackColor = UITheme.Background };
            var card = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Surface };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Controls.Add(actionBar);
            card.Controls.Add(_lblEmpty);
            card.Controls.Add(dgv);
            dgv.BringToFront();
            _lblEmpty.BringToFront();
            wrap.Controls.Add(card);

            Controls.Add(wrap);
            Controls.Add(summaryRow);
            Controls.Add(toolbar);
            UpdateSelectionActions();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Panel MakeCard(string title, Color accent, out Label valLbl)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 6, 0),
                BackColor = UITheme.Surface
            };
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

            var lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(16, 12),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Parent = card
            };

            valLbl = new Label
            {
                Text = "—",
                AutoSize = true,
                Location = new Point(14, 32),
                Font = UITheme.FontCardValue2,
                ForeColor = accent,
                Parent = card
            };
            return card;
        }

        private static void AddLabel(Control p, string t, int x, int y) =>
            p.Controls.Add(new Label
            {
                Text = t, AutoSize = true, Location = new Point(x, y),
                Font = UITheme.FontGridHeader,
                ForeColor = UITheme.TextSecondary
            });

        private static ComboBox AddCombo(Control p, int x, int y, int w)
        {
            var c = new ComboBox
            {
                Location = new Point(x, y), Size = new Size(w, 32),
                Font = UITheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt
            };
            p.Controls.Add(c);
            return c;
        }

        // ── Data ──────────────────────────────────────────────────────────────

        public void RefreshData(int? selectSemesterId = null)
        {
            int selectedSemesterId = selectSemesterId ?? _semSvc.GetActive()?.Id ?? 0;
            _suppressFilterEvents = true;
            try
            {
                cmbSem.Items.Clear();
                foreach (var s in _semSvc.GetAll()) cmbSem.Items.Add(new SemItem(s.Id, s.Name));

                int index = Enumerable.Range(0, cmbSem.Items.Count)
                    .FirstOrDefault(i => cmbSem.Items[i] is SemItem item && item.Id == selectedSemesterId);
                cmbSem.SelectedIndex = cmbSem.Items.Count == 0 ? -1 : index;
            }
            finally
            {
                _suppressFilterEvents = false;
            }
            LoadData();
        }

        /// <summary>
        /// Lọc danh sách học phí theo một sinh viên cụ thể (được gọi từ Tab Sinh Viên)
        /// </summary>
        public void FilterByStudent(int studentId)
        {
            var sv = _svSvc.GetStudentById(studentId);
            if (sv == null) return;

            _suppressFilterEvents = true;
            try
            {
                cmbStatus.SelectedIndex = 0;
                _studentContextId = studentId;
                txtSearch.Text = sv.FullName;
                lblStudentContext.Text = $"Đang lọc: {sv.FullName} ({studentId})  [x]";
                lblStudentContext.Visible = true;
            }
            finally
            {
                _suppressFilterEvents = false;
            }
            LoadData();
        }

        private void FilterCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_suppressFilterEvents) LoadData();
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            if (_suppressFilterEvents || _studentContextId == null) return;

            _studentContextId = null;
            lblStudentContext.Visible = false;
        }

        private void ResetFilters()
        {
            _suppressFilterEvents = true;
            try
            {
                cmbStatus.SelectedIndex = 0;
                _studentContextId = null;
                txtSearch.Text = string.Empty;
                lblStudentContext.Visible = false;
            }
            finally
            {
                _suppressFilterEvents = false;
            }
            LoadData();
        }

        private void LoadData()
        {
            _tuiSvc.RefreshStatuses(_semSvc.GetAll());
            int? semId = cmbSem.SelectedItem is SemItem s && s.Id > 0 ? s.Id : null;
            PaymentStatus? st = cmbStatus.SelectedIndex switch
            {
                1 => PaymentStatus.Unpaid, 2 => PaymentStatus.PartiallyPaid,
                3 => PaymentStatus.Paid, 4 => PaymentStatus.LatePaid,
                5 => PaymentStatus.Overdue, _ => null
            };
            IEnumerable<int>? ids = _studentContextId.HasValue ? new[] { _studentContextId.Value } : null;
            if (ids == null && !string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string q = txtSearch.Text.Trim();
                var matchedSvIds = _svSvc.GetAllStudents()
                    .Where(s =>
                        (!string.IsNullOrEmpty(s.FullName) && s.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        s.Id.ToString().Contains(q) ||
                        (!string.IsNullOrEmpty(s.ClassName) && s.ClassName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.Contains(q)) ||
                        (!string.IsNullOrEmpty(s.Email) && s.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    )
                    .Select(x => x.Id)
                    .ToHashSet();

                var allFees = _tuiSvc.Filter(semId, st, null);
                _current = allFees.Where(f =>
                    matchedSvIds.Contains(f.StudentId) ||
                    f.Id.ToString() == q ||
                    f.Id.ToString() == q.TrimStart('#') ||
                    (!string.IsNullOrEmpty(f.Note) && f.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }
            else
            {
                _current = _tuiSvc.Filter(semId, st, ids);
            }

            var svDict  = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
            var semDict = _semSvc.GetAll().ToDictionary(x => x.Id);

            _current = SortFees(_current, svDict).ToList();

            var rows = _current.Select(f => new
            {
                f.Id,
                _SvId    = f.StudentId,
                HoTen    = svDict.TryGetValue(f.StudentId, out var sv) ? sv.FullName : $"#{f.StudentId}",
                Lop      = svDict.TryGetValue(f.StudentId, out var sv2) ? sv2.ClassName : "",
                HocKy    = semDict.TryGetValue(f.SemesterId, out var sem) ? sem.Name : $"#{f.SemesterId}",
                TinChi   = f.Credits,
                PhaiNop  = f.TotalAmount,
                DaNop    = f.PaidAmount,
                ConLai   = f.RemainingAmount,
                HanNop   = (f.DueDate ?? (semDict.TryGetValue(f.SemesterId, out var s) ? s.DueDate : (DateTime?)null))?.ToString("dd/MM/yyyy") ?? "—",
                TrangThai = f.StatusDisplayText,
                NgayNop  = f.PaidDate.HasValue ? f.PaidDate.Value.ToString("dd/MM/yyyy") : "—",
                GhiChu   = f.Note
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = rows;

            bool hasFilter = cmbStatus.SelectedIndex > 0 || _studentContextId.HasValue || !string.IsNullOrWhiteSpace(txtSearch.Text);
            if (_current.Count == 0)
            {
                _lblEmpty.Text = hasFilter
                    ? "Không tìm thấy kết quả phù hợp\nBấm Xóa lọc để hiển thị tất cả"
                    : "Chưa có phiếu học phí\nChọn học kỳ và bấm [+ Lập phiếu] để bắt đầu";
                _lblEmpty.Visible = true;
            }
            else
            {
                _lblEmpty.Visible = false;
            }

            if (dgv.Columns.Count > 0)
            {
                void Hide(string n) { if (dgv.Columns[n] is { } c) c.Visible = false; }
                void Num(string n, string h, int w)
                {
                    if (dgv.Columns[n] is { } c)
                    {
                        c.HeaderText = h;
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        c.Width = w;
                        c.DefaultCellStyle.Format = "N0";
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Padding = new Padding(2, 0, 8, 0);
                        c.HeaderCell.Style.Padding = new Padding(2, 0, 8, 0);
                    }
                }
                void Center(string n, string h, int w)
                {
                    if (dgv.Columns[n] is { } c)
                    {
                        c.HeaderText = h;
                        c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        c.Width = w;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        c.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
                        c.HeaderCell.Style.Padding = new Padding(2, 0, 2, 0);
                    }
                }

                Hide("Id");
                Center("_SvId", "Mã SV", 75);
                if (dgv.Columns["HoTen"] is { } name)
                {
                    name.HeaderText = "Họ và tên";
                    name.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    name.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    name.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    name.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
                    name.HeaderCell.Style.Padding = new Padding(8, 0, 4, 0);
                }
                Center("Lop", "Lớp", 100);
                Center("HocKy", "Học kỳ", 125);
                Hide("TinChi");
                Num("PhaiNop", "Phải nộp", 105);
                Num("DaNop", "Đã nộp", 105);
                Num("ConLai", "Còn lại", 105);
                Center("HanNop", "Hạn nộp", 100);
                Center("TrangThai", "Trạng thái", 120);
                Hide("NgayNop");
                Hide("GhiChu");
                ConfigureSortableColumn("HoTen");
                ConfigureSortableColumn("Lop");
                ConfigureSortableColumn("TrangThai");
                ShowSortGlyph();
            }

            decimal tot = _current.Sum(f => f.TotalAmount);
            decimal pid = _current.Sum(f => f.PaidAmount);
            lblTotalVal.Text = $"{tot:N0} ₫";
            lblPaidVal.Text  = $"{pid:N0} ₫";
            lblLeftVal.Text  = $"{(tot - pid):N0} ₫";
            lblCountVal.Text = $"{_current.Count} phiếu";
            UpdateSelectionActions();
        }

        private IEnumerable<TuitionFee> SortFees(IEnumerable<TuitionFee> fees, IReadOnlyDictionary<int, Student> students)
        {
            if (_sortColumn == null) return fees;

            Func<TuitionFee, string> key = _sortColumn switch
            {
                "Lop" => fee => students.TryGetValue(fee.StudentId, out var student) ? student.ClassName : string.Empty,
                "TrangThai" => fee => fee.StatusDisplayText,
                _ => fee => students.TryGetValue(fee.StudentId, out var student) ? student.FullName : string.Empty
            };
            return _sortAscending
                ? fees.OrderBy(key, StringComparer.CurrentCultureIgnoreCase)
                : fees.OrderByDescending(key, StringComparer.CurrentCultureIgnoreCase);
        }

        private void Dgv_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            string column = dgv.Columns[e.ColumnIndex].Name;
            if (column is not ("HoTen" or "Lop" or "TrangThai")) return;

            if (_sortColumn == column) _sortAscending = !_sortAscending;
            else { _sortColumn = column; _sortAscending = true; }
            LoadData();
        }

        private void ConfigureSortableColumn(string name)
        {
            if (dgv.Columns[name] is { } column)
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        private void ShowSortGlyph()
        {
            foreach (DataGridViewColumn column in dgv.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (_sortColumn != null && dgv.Columns[_sortColumn] is { } sorted)
                sorted.HeaderCell.SortGlyphDirection = _sortAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgv.Columns.Count == 0 || e.Graphics == null) return;
            var col = dgv.Columns["TrangThai"];
            if (col != null && e.ColumnIndex == col.Index && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                UITheme.DrawStatusBadge(e.Graphics, e.CellBounds, e.Value.ToString() ?? "");
                e.Handled = true;
            }
        }

        private void ConfigureSelectionMenu()
        {
            var profile = new ToolStripMenuItem("Hồ sơ sinh viên");
            profile.Click += BtnStudentProfile_Click;
            var edit = new ToolStripMenuItem("Sửa phiếu");
            edit.Click += BtnEdit_Click;
            var debtNotice = new ToolStripMenuItem("Giấy báo nợ");
            debtNotice.Click += BtnDebtNotice_Click;
            var delete = new ToolStripMenuItem("Xóa phiếu") { ForeColor = UITheme.Danger };
            delete.Click += BtnDelete_Click;

            _selectionMenu.Items.AddRange(new ToolStripItem[]
            {
                profile, edit, debtNotice, new ToolStripSeparator(), delete
            });
        }

        private TuitionFee? GetSelectedFee()
        {
            if (dgv.SelectedRows.Count == 0) return null;
            int id = dgv.SelectedRows[0].Cells["Id"].Value is int value ? value : 0;
            return _tuiSvc.GetById(id);
        }

        private void UpdateSelectionActions()
        {
            var fee = dgv == null ? null : GetSelectedFee();
            btnHistory.Enabled = fee != null;
            btnMore.Enabled = fee != null;
            btnPay.Enabled = fee != null && !fee.IsFullyPaid;
        }

        // ── Action Handlers ───────────────────────────────────────────────────

        private TuitionFee? Selected()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một phiếu học phí!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return GetSelectedFee();
        }

        private void BtnBatch_Click(object? s, EventArgs e)
        {
            using var f = new FormBatchTuition(_svSvc, _semSvc, _tuiSvc);
            if (f.ShowDialog() == DialogResult.OK) RefreshData();
        }

        private void BtnAdd_Click(object? s, EventArgs e)
        {
            using var f = new FormTuitionDetail(_svSvc, _semSvc, _tuiSvc, null);
            if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object? s, EventArgs e)
        {
            var fee = Selected(); if (fee == null) return;
            using var f = new FormTuitionDetail(_svSvc, _semSvc, _tuiSvc, fee);
            if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnPay_Click(object? s, EventArgs e)
        {
            var fee = Selected(); if (fee == null) return;
            if (fee.IsFullyPaid)
            {
                MessageBox.Show("Học phí này đã được thanh toán đầy đủ!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var f = new FormPayment(fee, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService);
            if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnDebtNotice_Click(object? s, EventArgs e)
        {
            var fee = Selected();
            if (fee == null) return;
            var sv = _svSvc.GetStudentById(fee.StudentId);
            var sem = _semSvc.GetById(fee.SemesterId);
            if (sv == null || sem == null)
            {
                MessageBox.Show("Không tìm thấy thông tin sinh viên hoặc học kỳ!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var f = new FormDebtNotice(fee, sv, sem, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService);
            if (f.ShowDialog() == DialogResult.OK)
            {
                LoadData();
            }
        }

        private void ViewSelectedReceiptHistory()
        {
            var fee = Selected(); if (fee == null) return;
            var sv = _svSvc.GetStudentById(fee.StudentId);
            var sem = _semSvc.GetById(fee.SemesterId);
            if (sv == null || sem == null) return;

            using var f = new FormReceiptHistory(fee, sv, sem, _receiptSvc);
            f.ShowDialog();
        }

        private void BtnStudentProfile_Click(object? s, EventArgs e)
        {
            var fee = Selected(); if (fee == null) return;
            var sv = _svSvc.GetStudentById(fee.StudentId);
            if (sv == null) return;

            using var f = new FormStudentDetail(_svSvc, sv);
            if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object? s, EventArgs e)
        {
            var fee = Selected(); if (fee == null) return;
            if (MessageBox.Show($"Xác nhận xóa phiếu học phí ID {fee.Id}?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { _tuiSvc.Delete(fee.Id); LoadData(); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void ExportTuitionCsv()
        {
            var svDict = _svSvc.GetAllStudents().ToDictionary(x => x.Id);
            var semDict = _semSvc.GetAll().ToDictionary(x => x.Id);

            var cols = new List<(string Header, Func<TuitionFee, object> ValueGetter)>
            {
                ("Mã HP", f => f.Id),
                ("Mã SV", f => f.StudentId),
                ("Họ và Tên", f => svDict.TryGetValue(f.StudentId, out var sv) ? sv.FullName : ""),
                ("Lớp", f => svDict.TryGetValue(f.StudentId, out var sv) ? sv.ClassName : ""),
                ("Học Kỳ", f => semDict.TryGetValue(f.SemesterId, out var sem) ? sem.Name : ""),
                ("Số tín chỉ", f => f.Credits),
                ("Học phí phải nộp (VNĐ)", f => f.TotalAmount),
                ("Đã nộp (VNĐ)", f => f.PaidAmount),
                ("Còn lại (VNĐ)", f => f.RemainingAmount),
                ("Hạn nộp", f => (f.DueDate ?? (semDict.TryGetValue(f.SemesterId, out var s) ? s.DueDate : (DateTime?)null))?.ToString("dd/MM/yyyy") ?? ""),
                ("Trạng thái", f => f.StatusDisplayText),
                ("Ngày nộp", f => f.PaidDate.HasValue ? f.PaidDate.Value.ToString("dd/MM/yyyy") : ""),
                ("Ghi chú", f => f.Note)
            };

            CsvExportHelper.ExportToCsv("DanhSachHocPhi.csv", _current, cols);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _selectionMenu.Dispose();
            base.Dispose(disposing);
        }
    }
}
