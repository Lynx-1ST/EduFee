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

        private ComboBox cmbSem = null!, cmbStatus = null!, cmbClass = null!;
        private TextBox txtSearch = null!;
        private DataGridView dgv = null!;
        private Label _lblEmpty = null!;

        private Label lblTotalVal = null!, lblPaidVal = null!, lblLeftVal = null!, lblCountVal = null!;
        private Button btnBatch = null!, btnAdd = null!, btnPay = null!, btnHistory = null!, btnExport = null!, btnMore = null!;
        private Label lblStudentContext = null!;
        private Label lblSemesterName = null!, lblSemesterMeta = null!;
        private readonly List<Button> _statusSegments = new();
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

            // ── Tuition context, actions, and filters ─────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 142,
                BackColor = UITheme.Surface,
                Padding = new Padding(28, 12, 28, 8)
            };
            toolbar.Controls.Add(UITheme.HSep(DockStyle.Bottom));

            var row1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent
            };

            var context = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = Color.Transparent
            };
            lblSemesterName = new Label
            {
                Text = "Học phí",
                AutoSize = true,
                Location = new Point(0, 0),
                Font = UITheme.FontDisplay,
                ForeColor = UITheme.TextPrimary
            };
            lblSemesterMeta = new Label
            {
                Text = "Chọn học kỳ từ thanh điều hướng",
                AutoSize = true,
                Location = new Point(1, 24),
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary
            };
            context.Controls.AddRange(new Control[] { lblSemesterName, lblSemesterMeta });

            var flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            btnAdd = UITheme.PrimaryBtn("+ Lập phiếu", 116, 34);
            btnBatch = UITheme.GhostBtn("Tạo hàng loạt", 126, 34);
            btnExport = UITheme.GhostBtn("Xuất CSV", 92, 34);
            btnAdd.Margin = new Padding(0, 1, 8, 0);
            btnBatch.Margin = new Padding(0, 1, 8, 0);
            btnExport.Margin = new Padding(0, 1, 0, 0);
            btnAdd.Click += BtnAdd_Click;
            btnBatch.Click += BtnBatch_Click;
            btnExport.Click += (s, e) => ExportTuitionCsv();
            flowActions.Controls.AddRange(new Control[] { btnAdd, btnBatch, btnExport });
            row1.Controls.Add(flowActions);
            row1.Controls.Add(context);

            var row2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent
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

            txtSearch = UITheme.MakeSearchBox("Tìm sinh viên, mã SV, lớp...", 250, 32);
            txtSearch.Margin = new Padding(0, 2, 8, 0);
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

            cmbStatus = new ComboBox
            {
                Width = 138,
                Height = 32,
                Font = UITheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt,
                Margin = new Padding(0, 2, 8, 0)
            };
            cmbStatus.Items.AddRange(new object[] { "Tất cả", "Chưa nộp", "Nộp 1 phần", "Đã nộp đủ", "Nộp muộn", "Quá hạn" });
            cmbStatus.SelectedIndex = 0;

            cmbClass = new ComboBox
            {
                Width = 150,
                Height = 32,
                Font = UITheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt,
                Margin = new Padding(0, 2, 8, 0)
            };

            var btnReset = UITheme.GhostBtn("Xóa lọc", 76, 32);
            btnReset.Margin = new Padding(0, 2, 0, 0);
            btnReset.Click += (s, e) => ResetFilters();

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
                txtSearch, cmbStatus, cmbClass, btnReset, lblStudentContext
            });

            row2.Controls.Add(flowFilters);

            cmbSem.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;
            cmbStatus.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;
            cmbClass.SelectedIndexChanged += FilterCombo_SelectedIndexChanged;

            var row3 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };
            var segments = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (var index = 0; index < 5; index++)
            {
                int statusIndex = new[] { 0, 1, 2, 3, 5 }[index];
                var segment = UITheme.GhostBtn(string.Empty, 78, 28);
                segment.Margin = new Padding(0, 3, 6, 0);
                segment.Padding = new Padding(8, 0, 8, 0);
                segment.Font = UITheme.FontSmallBold;
                segment.Click += (s, e) => cmbStatus.SelectedIndex = statusIndex;
                _statusSegments.Add(segment);
                segments.Controls.Add(segment);
            }
            row3.Controls.Add(segments);

            toolbar.Controls.Add(row1);
            toolbar.Controls.Add(row2);
            toolbar.Controls.Add(row3);
            toolbar.Controls.SetChildIndex(row1, 1);
            toolbar.Controls.SetChildIndex(row2, 2);
            toolbar.Controls.SetChildIndex(row3, 3);

            // ── Compact financial metrics ─────────────────────────────────
            var summaryRow = new Panel
            {
                Dock = DockStyle.Top, Height = 88,
                BackColor = UITheme.Background,
                Padding = new Padding(28, 10, 28, 8)
            };

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = UITheme.Surface,
                Padding = new Padding(6, 0, 6, 0)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            tableLayout.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, tableLayout.Width - 1, tableLayout.Height - 1);
            };

            var c1 = MakeMetric("Tổng phải thu", UITheme.TextPrimary, out lblTotalVal);
            var c2 = MakeMetric("Đã thu", UITheme.SuccessDark, out lblPaidVal);
            var c3 = MakeMetric("Còn phải thu", UITheme.DangerDark, out lblLeftVal);
            var c4 = MakeMetric("Phiếu học phí", UITheme.PrimaryDark, out lblCountVal);

            tableLayout.Controls.Add(c1, 0, 0);
            tableLayout.Controls.Add(c2, 1, 0);
            tableLayout.Controls.Add(c3, 2, 0);
            tableLayout.Controls.Add(c4, 3, 0);
            summaryRow.Controls.Add(tableLayout);

            // ── DataGridView ──────────────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill, MultiSelect = false };
            UITheme.StyleGrid(dgv);
            dgv.RowTemplate.Height = 48;
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

            // ── Ledger table ───────────────────────────────────────────────
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

        private static Panel MakeMetric(string title, Color valueColor, out Label valLbl)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = UITheme.Surface,
                Padding = new Padding(16, 0, 12, 0)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawLine(pen, card.Width - 1, 14, card.Width - 1, Math.Max(14, card.Height - 14));
            };

            var lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(16, 11),
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                Parent = card
            };

            valLbl = new Label
            {
                Text = "—",
                AutoSize = true,
                Location = new Point(15, 30),
                Font = UITheme.FontCardValue,
                ForeColor = valueColor,
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

                var selectedClass = cmbClass.SelectedItem?.ToString();
                cmbClass.Items.Clear();
                cmbClass.Items.Add("Tất cả lớp");
                foreach (var className in _svSvc.GetAllStudents()
                    .Select(student => student.ClassName)
                    .Where(className => !string.IsNullOrWhiteSpace(className))
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(className => className, StringComparer.CurrentCultureIgnoreCase))
                    cmbClass.Items.Add(className);
                int classIndex = selectedClass == null ? 0 : cmbClass.FindStringExact(selectedClass);
                cmbClass.SelectedIndex = classIndex >= 0 ? classIndex : 0;
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
                cmbClass.SelectedIndex = 0;
                _studentContextId = studentId;
                txtSearch.Text = sv.FullName;
                lblStudentContext.Text = $"Đang lọc: {sv.FullName} ({sv.StudentCode})  [x]";
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
                cmbClass.SelectedIndex = 0;
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
            var students = _svSvc.GetAllStudents();
            var filteredFees = _tuiSvc.Filter(semId, st, ids);
            string? selectedClass = cmbClass.SelectedIndex > 0 ? cmbClass.SelectedItem?.ToString() : null;
            if (!string.IsNullOrWhiteSpace(selectedClass))
            {
                var classStudentIds = students
                    .Where(student => string.Equals(student.ClassName, selectedClass, StringComparison.CurrentCultureIgnoreCase))
                    .Select(student => student.Id)
                    .ToHashSet();
                filteredFees = filteredFees.Where(fee => classStudentIds.Contains(fee.StudentId)).ToList();
            }

            if (ids == null && !string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string q = txtSearch.Text.Trim();
                var matchedSvIds = students
                    .Where(s =>
                        (!string.IsNullOrEmpty(s.FullName) && s.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(s.StudentCode) && s.StudentCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(s.ClassName) && s.ClassName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.Contains(q)) ||
                        (!string.IsNullOrEmpty(s.Email) && s.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    )
                    .Select(x => x.Id)
                    .ToHashSet();

                _current = filteredFees.Where(f =>
                    matchedSvIds.Contains(f.StudentId) ||
                    f.Id.ToString() == q ||
                    f.Id.ToString() == q.TrimStart('#') ||
                    (!string.IsNullOrEmpty(f.Note) && f.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }
            else
            {
                _current = filteredFees.ToList();
            }

            var svDict  = students.ToDictionary(x => x.Id);
            var semDict = _semSvc.GetAll().ToDictionary(x => x.Id);

            var selectedSemester = semId.HasValue && semDict.TryGetValue(semId.Value, out var semester)
                ? semester
                : null;
            lblSemesterName.Text = "Học phí";
            lblSemesterMeta.Text = selectedSemester == null
                ? "Chưa chọn học kỳ"
                : $"{selectedSemester.Name}  ·  {selectedSemester.TuitionPerCredit:N0} ₫ / tín chỉ  ·  Hạn nộp {selectedSemester.DueDate:dd/MM/yyyy}";
            UpdateStatusSegments(_tuiSvc.Filter(semId, null, null));

            _current = SortFees(_current, svDict, semDict).ToList();

            var rows = _current.Select(f => new
            {
                f.Id,
                MaSV     = svDict.TryGetValue(f.StudentId, out var student) ? student.StudentCode : "—",
                HoTen    = svDict.TryGetValue(f.StudentId, out var sv) ? $"{sv.FullName}\n{sv.StudentCode}" : $"#{f.StudentId}",
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

            bool hasFilter = cmbStatus.SelectedIndex > 0 || cmbClass.SelectedIndex > 0 || _studentContextId.HasValue || !string.IsNullOrWhiteSpace(txtSearch.Text);
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
                Hide("MaSV");
                if (dgv.Columns["HoTen"] is { } name)
                {
                    name.HeaderText = "Sinh viên";
                    name.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    name.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    name.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    name.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
                    name.HeaderCell.Style.Padding = new Padding(8, 0, 4, 0);
                }
                Center("Lop", "Lớp", 100);
                Hide("HocKy");
                Hide("TinChi");
                Num("PhaiNop", "Học phí", 110);
                Num("DaNop", "Đã nộp", 110);
                Num("ConLai", "Còn lại", 105);
                Center("HanNop", "Hạn", 82);
                if (dgv.Columns["TrangThai"] is { } status)
                {
                    status.HeaderText = "Trạng thái";
                    status.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    status.Width = 118;
                    status.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    status.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                Hide("NgayNop");
                Hide("GhiChu");
                foreach (var column in new[] { "MaSV", "HoTen", "Lop", "HocKy", "PhaiNop", "DaNop", "ConLai", "HanNop", "TrangThai" })
                    ConfigureSortableColumn(column);
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

        private void UpdateStatusSegments(IEnumerable<TuitionFee> fees)
        {
            var items = new[]
            {
                ("Tất cả", (PaymentStatus?)null),
                ("Chưa nộp", (PaymentStatus?)PaymentStatus.Unpaid),
                ("Nộp 1 phần", (PaymentStatus?)PaymentStatus.PartiallyPaid),
                ("Đã nộp", (PaymentStatus?)PaymentStatus.Paid),
                ("Quá hạn", (PaymentStatus?)PaymentStatus.Overdue)
            };
            var snapshot = fees.ToList();
            for (var i = 0; i < _statusSegments.Count && i < items.Length; i++)
            {
                var (title, status) = items[i];
                int count = status.HasValue ? snapshot.Count(fee => fee.Status == status.Value) : snapshot.Count;
                var button = _statusSegments[i];
                bool selected = cmbStatus.SelectedIndex == new[] { 0, 1, 2, 3, 5 }[i];
                button.Text = $"{title} {count}";
                button.BackColor = selected ? UITheme.PrimaryLight : UITheme.Surface;
                button.ForeColor = selected ? UITheme.PrimaryDark : UITheme.TextSecondary;
                button.FlatAppearance.BorderColor = selected ? UITheme.ActiveBorder : UITheme.Border;
            }
        }

        private IEnumerable<TuitionFee> SortFees(IEnumerable<TuitionFee> fees,
            IReadOnlyDictionary<int, Student> students, IReadOnlyDictionary<int, Semester> semesters)
        {
            if (_sortColumn == null) return fees;

            return _sortColumn switch
            {
                "MaSV" => SortBy(fees, fee => students.TryGetValue(fee.StudentId, out var student) ? student.StudentCode : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase),
                "Lop" => SortBy(fees, fee => students.TryGetValue(fee.StudentId, out var student) ? student.ClassName : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase),
                "HocKy" => SortBy(fees, fee => semesters.TryGetValue(fee.SemesterId, out var semester) ? semester.Name : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase),
                "PhaiNop" => SortBy(fees, fee => fee.TotalAmount),
                "DaNop" => SortBy(fees, fee => fee.PaidAmount),
                "ConLai" => SortBy(fees, fee => fee.RemainingAmount),
                "HanNop" => SortBy(fees, fee => fee.DueDate ??
                    (semesters.TryGetValue(fee.SemesterId, out var semester) ? semester.DueDate : DateTime.MaxValue)),
                "TrangThai" => SortBy(fees, fee => fee.Status),
                _ => SortBy(fees, fee => students.TryGetValue(fee.StudentId, out var student) ? student.FullName : string.Empty,
                    StringComparer.CurrentCultureIgnoreCase)
            };

            IEnumerable<TuitionFee> SortBy<TKey>(IEnumerable<TuitionFee> source, Func<TuitionFee, TKey> key, IComparer<TKey>? comparer = null) =>
                _sortAscending ? source.OrderBy(key, comparer) : source.OrderByDescending(key, comparer);
        }

        private void Dgv_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            string column = dgv.Columns[e.ColumnIndex].Name;
            if (column is not ("MaSV" or "HoTen" or "Lop" or "HocKy" or "PhaiNop" or "DaNop" or "ConLai" or "HanNop" or "TrangThai")) return;

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
                string status = e.Value.ToString() ?? string.Empty;
                Color cue = status switch
                {
                    "Đã nộp đủ" => UITheme.Success,
                    "Nộp muộn" or "Nộp 1 phần" => UITheme.Warning,
                    "Quá hạn" => UITheme.Danger,
                    _ => UITheme.TextMuted
                };
                var dot = new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y + (e.CellBounds.Height - 8) / 2, 8, 8);
                using var brush = new SolidBrush(cue);
                e.Graphics.FillEllipse(brush, dot);
                var textBounds = new Rectangle(dot.Right + 7, e.CellBounds.Y, e.CellBounds.Width - (dot.Right - e.CellBounds.X) - 10, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, status, UITheme.FontSmallBold, textBounds,
                    (e.State & DataGridViewElementStates.Selected) != 0 ? Color.White : UITheme.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
            using var f = new FormPayment(fee, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService,
                _mainForm.MomoGateway, _mainForm.GatewayPaymentPersistence, _mainForm.MomoSettingsService);
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

            using var f = new FormDebtNotice(fee, sv, sem, _tuiSvc, _semSvc, _svSvc, _receiptSvc, _mainForm.EmailService,
                _mainForm.MomoGateway, _mainForm.GatewayPaymentPersistence, _mainForm.MomoSettingsService);
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
                ("Mã SV", f => svDict.TryGetValue(f.StudentId, out var sv) ? sv.StudentCode : ""),
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
