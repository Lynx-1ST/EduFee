using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class PanelStudents : UserControl
    {
        private readonly StudentService _svc;
        private readonly Form1 _mainForm;

        private DataGridView dgv = null!;
        private TextBox txtSearch = null!;
        private ComboBox cmbClassFilter = null!;
        private Label lblCount = null!;
        private Label lblTitle = null!;
        private Label _lblEmpty = null!;
        private Button btnViewTuition = null!, btnAdd = null!, btnEdit = null!, btnDelete = null!, btnExport = null!, btnImport = null!;

        private List<Student> _currentList = new();

        public PanelStudents(StudentService svc, Form1 mainForm)
        {
            _svc = svc;
            _mainForm = mainForm;
            BuildUI();
            UpdateClassFilterItems();
            LoadData();
        }

        private void BuildUI()
        {
            BackColor = UITheme.Background;

            // ── Student header and compact filter bar ────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top, Height = 116,
                BackColor = UITheme.Surface
            };
            toolbar.Controls.Add(UITheme.HSep(DockStyle.Bottom));

            var heading = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent, Padding = new Padding(28, 14, 28, 0) };
            lblTitle = new Label
            {
                Text = "Sinh viên",
                AutoSize = true,
                Font = UITheme.FontH1,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(28, 14)
            };
            lblCount = new Label
            {
                AutoSize = true,
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextSecondary,
                Location = new Point(128, 19)
            };
            heading.Controls.AddRange(new Control[] { lblTitle, lblCount });
            toolbar.Controls.Add(heading);

            var flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(28, 4, 0, 10)
            };

            txtSearch = UITheme.MakeSearchBox("Tìm theo tên hoặc mã sinh viên", 260, 32);
            txtSearch.AccessibleName = "Tìm kiếm sinh viên";
            txtSearch.Margin = new Padding(0, 2, 10, 0);
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DoSearch();
                }
            };

            cmbClassFilter = new ComboBox
            {
                Width = 150, Height = 32,
                Font = UITheme.FontBody,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt,
                AccessibleName = "Lọc theo lớp",
                Margin = new Padding(0, 2, 10, 0)
            };
            cmbClassFilter.SelectedIndexChanged += (s, e) => DoSearch();

            var btnRefresh = UITheme.GhostBtn("Xóa lọc", 82, 34);
            btnRefresh.Margin = new Padding(0, 1, 0, 0);
            btnRefresh.Click += (s, e) => { txtSearch.Text = ""; if (cmbClassFilter.Items.Count > 0) cmbClassFilter.SelectedIndex = 0; LoadData(); };

            flowLeft.Controls.AddRange(new Control[] { txtSearch, cmbClassFilter, btnRefresh });

            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 28, 10)
            };

            btnExport = UITheme.GhostBtn("Xuất CSV", 100, 34);
            btnExport.Margin = new Padding(8, 2, 0, 0);
            btnExport.Click += (s, e) => ExportStudentsCsv();

            btnImport = UITheme.GhostBtn("Nhập CSV", 100, 34);
            btnImport.Margin = new Padding(8, 2, 0, 0);
            btnImport.Click += (s, e) => ImportStudents();

            btnAdd = UITheme.PrimaryBtn("+ Thêm sinh viên", 145, 34);
            btnAdd.Margin = new Padding(0, 1, 0, 0);
            btnAdd.Click += BtnAdd_Click;

            flowRight.Controls.AddRange(new Control[] { btnAdd, btnExport, btnImport });

            toolbar.Controls.Add(flowLeft);
            toolbar.Controls.Add(flowRight);

            // ── DataGridView (in a card) ───────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleGrid(dgv);
            dgv.RowTemplate.Height = 52;
            dgv.CellPainting += Dgv_CellPainting;
            dgv.DoubleClick += (s, e) => EditSelectedStudent();
            dgv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; EditSelectedStudent(); } };

            _lblEmpty = UITheme.CreateEmptyStateLabel("Chưa có sinh viên nào\nChọn «+ Thêm sinh viên» hoặc «Nhập CSV» để bắt đầu");

            // ── Action bar (Clean 64px, properly centered) ───────────────
            var actionBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.Surface
            };
            actionBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            // Action Buttons
            btnViewTuition = UITheme.GhostBtn("Xem học phí", 130, 36);
            btnEdit        = UITheme.GhostBtn("Sửa hồ sơ", 105, 36);
            btnDelete      = UITheme.GhostBtn("Xóa", 75, 36);
            btnDelete.ForeColor = UITheme.DangerDark;
            dgv.MultiSelect = false;
            dgv.SelectionChanged += (s, e) =>
            {
                bool selected = dgv.SelectedRows.Count > 0;
                btnViewTuition.Enabled = btnEdit.Enabled = btnDelete.Enabled = selected;
            };

            btnViewTuition.Margin = btnEdit.Margin = btnDelete.Margin =
                new Padding(4, 0, 4, 0);

            btnViewTuition.Click += BtnViewTuition_Click;
            btnEdit.Click        += (s, e) => EditSelectedStudent();
            btnDelete.Click      += BtnDelete_Click;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 14, 24, 0),
                BackColor = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnViewTuition, btnEdit, btnDelete });
            actionBar.Controls.Add(flow);

            // ── Card wrapper ──────────────────────────────────────────────
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 14, 28, 20), BackColor = UITheme.Background };
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
            Controls.Add(toolbar);
        }

        public void LoadData(List<Student>? list = null)
        {
            _currentList = list ?? _svc.GetAllStudents();
            dgv.DataSource = null;
            dgv.DataSource = _currentList;

            bool hasFilter = !string.IsNullOrWhiteSpace(txtSearch.Text) || cmbClassFilter.SelectedIndex > 0;
            if (_currentList.Count == 0)
            {
                _lblEmpty.Text = hasFilter
                    ? "Không tìm thấy sinh viên phù hợp\nThử thay đổi từ khóa hoặc chọn «Xóa bộ lọc»"
                    : "Chưa có sinh viên nào trong hệ thống\nChọn «+ Thêm sinh viên» hoặc «Nhập CSV» để bắt đầu";
                _lblEmpty.Visible = true;
            }
            else
            {
                _lblEmpty.Visible = false;
            }

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns["Id"] is { } idColumn)
                {
                    idColumn.Visible = false;
                }
                if (dgv.Columns["StudentCode"] is { } c0)
                {
                    c0.Visible = false;
                }
                if (dgv.Columns["FullName"] is { } c1)
                {
                    c1.HeaderText = "Sinh viên";
                    c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    c1.FillWeight = 150;
                }
                if (dgv.Columns["Email"] is { } c2) c2.HeaderText = "Email";
                if (dgv.Columns["PhoneNumber"] is { } c3)
                { c3.HeaderText = "Điện thoại"; c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c3.Width = 125; }
                if (dgv.Columns["DateOfBirth"] is { } c4)
                {
                    c4.HeaderText = "Ngày sinh";
                    c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c4.Width = 110;
                    c4.DefaultCellStyle.Format = "dd/MM/yyyy";
                    c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    c4.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgv.Columns["ClassName"] is { } c5)
                {
                    c5.HeaderText = "Lớp";
                    c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c5.Width = 110;
                    c5.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    c5.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            lblCount.Text = $"{_currentList.Count:N0} sinh viên";
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "FullName" || e.Value == null)
                return;
            if (e.Graphics == null) return;

            e.PaintBackground(e.CellBounds, true);
            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            var student = dgv.Rows[e.RowIndex].DataBoundItem as Student;
            if (student == null) return;

            var color = (e.State & DataGridViewElementStates.Selected) != 0 ? Color.White : UITheme.TextPrimary;
            var codeColor = (e.State & DataGridViewElementStates.Selected) != 0 ? Color.White : UITheme.TextSecondary;
            var bounds = Rectangle.Inflate(e.CellBounds, -12, 0);
            TextRenderer.DrawText(e.Graphics, student.StudentCode, UITheme.FontSmall, new Rectangle(bounds.X, bounds.Y + 7, bounds.Width, 17), codeColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, student.FullName, UITheme.FontBold, new Rectangle(bounds.X, bounds.Y + 25, bounds.Width, 20), color, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }

        private void UpdateClassFilterItems()
        {
            string currentSelection = cmbClassFilter.SelectedItem?.ToString() ?? "";
            cmbClassFilter.Items.Clear();
            cmbClassFilter.Items.Add("--- Tất cả lớp ---");
            foreach (var c in _svc.GetDistinctClasses())
            {
                if (!string.IsNullOrWhiteSpace(c))
                    cmbClassFilter.Items.Add(c);
            }
            int idx = cmbClassFilter.Items.IndexOf(currentSelection);
            cmbClassFilter.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void DoSearch()
        {
            string q = txtSearch.Text.Trim();
            string selClass = (cmbClassFilter.SelectedIndex > 0 && cmbClassFilter.SelectedItem != null)
                ? cmbClassFilter.SelectedItem.ToString()!
                : "";

            var list = _svc.GetAllStudents();

            if (!string.IsNullOrEmpty(selClass))
            {
                list = list.FindAll(s => string.Equals(s.ClassName, selClass, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(q))
            {
                list = list.FindAll(s =>
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(s.StudentCode) && s.StudentCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Email) && s.Email.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }

            LoadData(list);
        }

        private Student? GetSelectedStudent()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một sinh viên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            int id = dgv.SelectedRows[0].Cells["Id"].Value is int v ? v : 0;
            return _svc.GetStudentById(id);
        }

        private void BtnViewTuition_Click(object? s, EventArgs e)
        {
            var sv = GetSelectedStudent();
            if (sv == null) return;
            // Inter-module deep link: Jumps to Tuition panel filtered by this student
            _mainForm.NavigateToTuitionForStudent(sv.Id);
        }

        private void ImportStudents()
        {
            using var f = new FormImportStudents(_svc);
            if (f.ShowDialog() == DialogResult.OK)
            {
                UpdateClassFilterItems();
                LoadData();
            }
        }

        private void BtnAdd_Click(object? s, EventArgs e)
        {
            using var f = new FormStudentDetail(_svc, null);
            if (f.ShowDialog() == DialogResult.OK)
            {
                UpdateClassFilterItems();
                LoadData();
            }
        }

        private void EditSelectedStudent()
        {
            var sv = GetSelectedStudent();
            if (sv == null) return;
            using var f = new FormStudentDetail(_svc, sv);
            if (f.ShowDialog() == DialogResult.OK)
            {
                UpdateClassFilterItems();
                LoadData();
            }
        }

        private void BtnDelete_Click(object? s, EventArgs e)
        {
            var sv = GetSelectedStudent();
            if (sv == null) return;

            if (MessageBox.Show($"Xác nhận xóa sinh viên «{sv.FullName}»?\nHành động này không thể hoàn tác.",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    _svc.DeleteStudent(sv.Id);
                    UpdateClassFilterItems();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportStudentsCsv()
        {
            var columns = new List<(string Header, Func<Student, object> ValueGetter)>
            {
                ("MaSV", s => s.StudentCode),
                ("HoTen", s => s.FullName),
                ("Lop", s => s.ClassName),
                ("NgaySinh", s => s.DateOfBirth.ToString("dd/MM/yyyy")),
                ("DienThoai", s => s.PhoneNumber),
                ("Email", s => s.Email)
            };

            CsvExportHelper.ExportToCsv("DanhSachSinhVien.csv", _currentList, columns);
        }
    }
}
