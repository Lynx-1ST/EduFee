using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormSemesterManage : Form
    {
        private readonly SemesterService _semSvc;

        private DataGridView dgv = null!;
        private TextBox txtName = null!;
        private DateTimePicker dtpStart = null!, dtpEnd = null!, dtpDue = null!;
        private CheckBox chkActive = null!;
        private Button btnSave = null!, btnSetCurrent = null!, btnDelete = null!, btnClear = null!;
        private Label lblFormTitle = null!;

        private Semester? _selectedSem;

        public FormSemesterManage(SemesterService semSvc)
        {
            _semSvc = semSvc;
            BuildUI();
        }

        private ErrorProvider _ep = null!;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ep = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
            LoadData();
        }

        private void BuildUI()
        {
            Text = "Quản Lý Danh Sách Học Kỳ";
            ClientSize = new Size(920, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Top Header ────────────────────────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = UITheme.PrimaryDark,
                Padding = new Padding(24, 0, 24, 0)
            };

            topBar.Controls.Add(new Label
            {
                Text = "QUẢN LÝ DANH SÁCH HỌC KỲ",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Left Side: DataGridView ───────────────────────────────────
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left, Width = 520,
                Padding = new Padding(20, 16, 10, 20),
                BackColor = UITheme.Background
            };

            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Surface };
            gridCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, gridCard.Width - 1, gridCard.Height - 1);
            };

            dgv = new DataGridView { Dock = DockStyle.Fill, AccessibleName = "Danh sách học kỳ" };
            UITheme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) => OnGridSelectionChanged();
            dgv.CellClick += (s, e) => OnGridSelectionChanged();
            dgv.CellDoubleClick += (s, e) =>
            {
                if (_selectedSem != null)
                {
                    _semSvc.SetActive(_selectedSem.Id);
                    UiFeedback.ShowSuccess($"Đã đặt học kỳ «{_selectedSem.Name}» làm học kỳ hiện tại!");
                    LoadData(_selectedSem.Id);
                }
            };
            gridCard.Controls.Add(dgv);
            leftPanel.Controls.Add(gridCard);

            // ── Right Side: Edit / Add Form ───────────────────────────────
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 16, 20, 20),
                BackColor = UITheme.Background
            };

            var formCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Surface,
                Padding = new Padding(20, 16, 20, 16)
            };
            formCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, formCard.Width - 1, formCard.Height - 1);
            };

            int y = 14;
            lblFormTitle = new Label
            {
                Text = "Thông Tin Học Kỳ",
                Font = UITheme.FontH2, ForeColor = UITheme.TextPrimary,
                Location = new Point(20, y), AutoSize = true, Parent = formCard
            };
            y += 34;

            // Name
            AddLabel(formCard, "Tên học kỳ *:", y); y += 22;
            txtName = new TextBox
            {
                Location = new Point(20, y), Size = new Size(260, 30),
                Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt,
                PlaceholderText = "VD: HK2 2025-2026",
                AccessibleName = "Tên học kỳ"
            };
            formCard.Controls.Add(txtName);
            y += 36;

            // Start Date
            AddLabel(formCard, "Ngày bắt đầu:", y); y += 22;
            dtpStart = new DateTimePicker
            {
                Location = new Point(20, y), Size = new Size(260, 30),
                Font = UITheme.FontBody, Format = DateTimePickerFormat.Short,
                AccessibleName = "Ngày bắt đầu"
            };
            formCard.Controls.Add(dtpStart);
            y += 36;

            // End Date
            AddLabel(formCard, "Ngày kết thúc:", y); y += 22;
            dtpEnd = new DateTimePicker
            {
                Location = new Point(20, y), Size = new Size(260, 30),
                Font = UITheme.FontBody, Format = DateTimePickerFormat.Short,
                AccessibleName = "Ngày kết thúc"
            };
            formCard.Controls.Add(dtpEnd);
            y += 36;

            // Due Date
            AddLabel(formCard, "Hạn nộp học phí *:", y); y += 22;
            dtpDue = new DateTimePicker
            {
                Location = new Point(20, y), Size = new Size(260, 30),
                Font = UITheme.FontBody, Format = DateTimePickerFormat.Short,
                AccessibleName = "Hạn nộp học phí"
            };
            formCard.Controls.Add(dtpDue);
            y += 36;

            // Active Checkbox
            chkActive = new CheckBox
            {
                Text = "Đặt làm học kỳ hiện tại",
                Location = new Point(20, y), Size = new Size(260, 24),
                Font = UITheme.FontBold, ForeColor = UITheme.PrimaryDark,
                AccessibleName = "Đặt làm học kỳ hiện tại"
            };
            formCard.Controls.Add(chkActive);
            y += 36;

            formCard.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(320, 1), BackColor = UITheme.Border });
            y += 12;

            // Action Buttons
            btnSave = UITheme.SuccessBtn("Thêm mới", 100, 36);
            btnSave.Margin = new Padding(0, 0, 10, 0);
            btnSave.Click += BtnSave_Click;
            AcceptButton = btnSave;

            btnClear = UITheme.GhostBtn("Làm mới", 85, 36);
            btnClear.Margin = new Padding(0);
            btnClear.Click += (s, e) => ClearForm();

            var flowRow1 = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            flowRow1.Controls.AddRange(new Control[] { btnSave, btnClear });
            formCard.Controls.Add(flowRow1);
            y += 44;

            btnSetCurrent = UITheme.PurpleBtn("Kích hoạt kỳ này", 145, 36);
            btnSetCurrent.Margin = new Padding(0, 0, 10, 0);
            btnSetCurrent.Click += BtnSetCurrent_Click;

            btnDelete = UITheme.DangerBtn("Xóa", 75, 36);
            btnDelete.Margin = new Padding(0);
            btnDelete.Click += BtnDelete_Click;

            var flowRow2 = new FlowLayoutPanel
            {
                Location = new Point(20, y),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            flowRow2.Controls.AddRange(new Control[] { btnSetCurrent, btnDelete });
            formCard.Controls.Add(flowRow2);
            y += 44;

            var btnClose = UITheme.GhostBtn("Đóng", 80, 34);
            btnClose.Location = new Point(210, y);
            btnClose.Click += (s, e) => Close();
            CancelButton = btnClose;
            formCard.Controls.Add(btnClose);

            rightPanel.Controls.Add(formCard);

            Controls.Add(rightPanel);
            Controls.Add(leftPanel);
            Controls.Add(topBar);
        }

        private static void AddLabel(Panel p, string text, int y) =>
            p.Controls.Add(new Label
            {
                Text = text, Location = new Point(20, y), AutoSize = true,
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary
            });

        private void LoadData(int? targetSelectId = null)
        {
            var list = _semSvc.GetAll();
            var rows = list.Select(s => new
            {
                s.Id,
                s.Name,
                s.StartDate,
                s.DueDate,
                TrangThai = s.IsActive ? "Hiện tại" : "—"
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = rows;

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns["Id"] is { } c0) { c0.HeaderText = "Mã"; c0.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c0.Width = 45; }
                if (dgv.Columns["Name"] is { } c1) { c1.HeaderText = "Tên Học Kỳ"; c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
                if (dgv.Columns["StartDate"] is { } c2)
                {
                    c2.HeaderText = "Bắt Đầu";
                    c2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c2.Width = 100;
                    c2.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
                if (dgv.Columns["DueDate"] is { } c3)
                {
                    c3.HeaderText = "Hạn Nộp HP";
                    c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c3.Width = 105;
                    c3.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
                if (dgv.Columns["TrangThai"] is { } c4)
                {
                    c4.HeaderText = "Trạng Thái";
                    c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c4.Width = 95;
                    c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    c4.DefaultCellStyle.Font = UITheme.FontBold;
                }
            }

            if (list.Count > 0)
            {
                int targetIndex = -1;
                if (targetSelectId.HasValue)
                    targetIndex = list.FindIndex(s => s.Id == targetSelectId.Value);
                if (targetIndex < 0)
                    targetIndex = list.FindIndex(s => s.IsActive);
                if (targetIndex < 0)
                    targetIndex = 0;

                dgv.ClearSelection();
                if (targetIndex < dgv.Rows.Count)
                {
                    dgv.Rows[targetIndex].Selected = true;
                    dgv.CurrentCell = dgv.Rows[targetIndex].Cells["Name"];
                }
                OnGridSelectionChanged();
            }
            else
            {
                ClearForm();
            }
        }

        private void OnGridSelectionChanged()
        {
            var row = dgv.CurrentRow;
            if (row == null || row.Index < 0) return;
            int id = row.Cells["Id"].Value is int v ? v : 0;
            if (id <= 0) return;

            var sem = _semSvc.GetById(id);
            if (sem == null) return;
            _selectedSem = sem;

            _ep?.Clear();
            txtName.Text = _selectedSem.Name;
            dtpStart.Value = _selectedSem.StartDate;
            dtpEnd.Value = _selectedSem.EndDate;
            dtpDue.Value = _selectedSem.DueDate;
            chkActive.Checked = _selectedSem.IsActive;

            btnSave.Text = "Lưu thay đổi";
            lblFormTitle.Text = $"Chỉnh Sửa: {_selectedSem.Name}";
        }

        private void ClearForm()
        {
            _selectedSem = null;
            dgv.ClearSelection();
            _ep?.Clear();
            txtName.Text = string.Empty;
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Today.AddMonths(4);
            dtpDue.Value = DateTime.Today.AddMonths(1);
            chkActive.Checked = false;
            btnSave.Text = "Thêm mới";
            lblFormTitle.Text = "Thêm Học Kỳ Mới";
            txtName.Focus();
        }

        private void BtnSave_Click(object? s, EventArgs e)
        {
            if (!UiFeedback.ValidateRequired(_ep, txtName, "tên học kỳ"))
            {
                UiFeedback.FocusFirstError(_ep, txtName);
                return;
            }

            if (dtpEnd.Value.Date < dtpStart.Value.Date)
            {
                _ep.SetError(dtpEnd, "Ngày kết thúc không thể trước ngày bắt đầu!");
                dtpEnd.Focus();
                return;
            }

            string name = txtName.Text.Trim();

            try
            {
                int targetId;
                if (_selectedSem == null)
                {
                    var sem = new Semester(0, name, dtpStart.Value, dtpEnd.Value, dtpDue.Value, chkActive.Checked);
                    _semSvc.Add(sem);
                    if (chkActive.Checked) _semSvc.SetActive(sem.Id);
                    targetId = sem.Id;
                    UiFeedback.ShowSuccess("Thêm học kỳ thành công!");
                }
                else
                {
                    _selectedSem.Name = name;
                    _selectedSem.StartDate = dtpStart.Value;
                    _selectedSem.EndDate = dtpEnd.Value;
                    _selectedSem.DueDate = dtpDue.Value;
                    _selectedSem.IsActive = chkActive.Checked;
                    _semSvc.Update(_selectedSem);
                    if (chkActive.Checked) _semSvc.SetActive(_selectedSem.Id);
                    targetId = _selectedSem.Id;
                    UiFeedback.ShowSuccess("Cập nhật học kỳ thành công!");
                }

                LoadData(targetId);
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi lưu học kỳ");
            }
        }

        private void BtnSetCurrent_Click(object? s, EventArgs e)
        {
            if (_selectedSem == null && dgv.CurrentRow != null)
            {
                int id = dgv.CurrentRow.Cells["Id"].Value is int v ? v : 0;
                if (id > 0) _selectedSem = _semSvc.GetById(id);
            }

            if (_selectedSem == null)
            {
                UiFeedback.ShowWarning("Vui lòng chọn một học kỳ trên danh sách!");
                return;
            }

            _semSvc.SetActive(_selectedSem.Id);
            UiFeedback.ShowSuccess($"Đã đặt học kỳ «{_selectedSem.Name}» làm học kỳ hiện tại!");
            LoadData(_selectedSem.Id);
        }

        private void BtnDelete_Click(object? s, EventArgs e)
        {
            if (_selectedSem == null && dgv.CurrentRow != null)
            {
                int id = dgv.CurrentRow.Cells["Id"].Value is int v ? v : 0;
                if (id > 0) _selectedSem = _semSvc.GetById(id);
            }

            if (_selectedSem == null)
            {
                UiFeedback.ShowWarning("Vui lòng chọn một học kỳ để xóa!");
                return;
            }

            if (UiFeedback.ConfirmDelete(_selectedSem.Name))
            {
                try
                {
                    _semSvc.Delete(_selectedSem.Id);
                    LoadData();
                }
                catch (Exception ex)
                {
                    UiFeedback.ShowException(ex, "Lỗi xóa học kỳ");
                }
            }
        }
    }
}
