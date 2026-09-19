using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormBatchTuition : Form
    {
        private readonly StudentService _studentSvc;
        private readonly SemesterService _semesterSvc;
        private readonly TuitionService _tuitionSvc;

        private const decimal PRICE_PER_CREDIT = TuitionService.DefaultPricePerCredit;

        private ComboBox cmbClass = null!;
        private ComboBox cmbSemester = null!;
        private NumericUpDown numCredits = null!;
        private DateTimePicker dtpDueDate = null!;
        private TextBox txtNote = null!;
        private Label lblPreviewSummary = null!;
        private Label lblPerStudentTotal = null!;

        public FormBatchTuition(StudentService studentSvc, SemesterService semesterSvc, TuitionService tuitionSvc)
        {
            _studentSvc = studentSvc;
            _semesterSvc = semesterSvc;
            _tuitionSvc = tuitionSvc;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            Text = "⚡ Tạo Học Phí Hàng Loạt Theo Lớp";
            ClientSize = new Size(540, 560);
            MinimumSize = new Size(540, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Surface;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Top Header ────────────────────────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = UITheme.Primary,
                Padding = new Padding(24, 0, 24, 0)
            };

            topBar.Controls.Add(new Label
            {
                Text = "⚡  TẠO HỌC PHÍ HÀNG LOẠT THEO LỚP",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Form Body ─────────────────────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 20),
                BackColor = UITheme.Surface
            };

            int y = 20;

            // Class selection
            AddLabel(body, "Chọn Lớp học *:", y);
            cmbClass = new ComboBox
            {
                Location = new Point(160, y), Size = new Size(320, 30),
                Font = UITheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt
            };
            cmbClass.SelectedIndexChanged += (s, e) => UpdatePreview();
            body.Controls.Add(cmbClass);
            y += 42;

            // Semester selection
            AddLabel(body, "Chọn Học kỳ *:", y);
            cmbSemester = new ComboBox
            {
                Location = new Point(160, y), Size = new Size(320, 30),
                Font = UITheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt
            };
            cmbSemester.SelectedIndexChanged += (s, e) =>
            {
                if (cmbSemester.SelectedItem is SemItem si)
                {
                    var sem = _semesterSvc.GetById(si.Id);
                    if (sem != null) dtpDueDate.Value = sem.DueDate;
                }
                UpdatePreview();
            };
            body.Controls.Add(cmbSemester);
            y += 42;

            // Credits
            AddLabel(body, "Số tín chỉ *:", y);
            numCredits = new NumericUpDown
            {
                Location = new Point(160, y), Size = new Size(110, 30),
                Font = UITheme.FontBodyLarge,
                Minimum = 1, Maximum = 100, Value = 15,
                BackColor = UITheme.SurfaceAlt
            };
            numCredits.ValueChanged += (s, e) => UpdatePreview();
            body.Controls.Add(numCredits);

            lblPerStudentTotal = new Label
            {
                Location = new Point(285, y + 4), AutoSize = true,
                Font = UITheme.FontBold, ForeColor = UITheme.PrimaryDark
            };
            body.Controls.Add(lblPerStudentTotal);
            y += 44;

            // Due date
            AddLabel(body, "Hạn nộp *:", y);
            dtpDueDate = new DateTimePicker
            {
                Location = new Point(160, y), Size = new Size(160, 30),
                Font = UITheme.FontBody, Format = DateTimePickerFormat.Short
            };
            body.Controls.Add(dtpDueDate);
            y += 42;

            // Preview box
            var infoBox = new Panel
            {
                Location = new Point(28, y), Size = new Size(452, 70),
                BackColor = UITheme.PrimaryLight
            };
            infoBox.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Primary, 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, infoBox.Width - 1, infoBox.Height - 1);
            };

            lblPreviewSummary = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 12),
                Font = UITheme.FontBody, ForeColor = UITheme.TextPrimary
            };
            infoBox.Controls.Add(lblPreviewSummary);
            body.Controls.Add(infoBox);
            y += 84;

            // Note
            AddLabel(body, "Ghi chú:", y);
            txtNote = new TextBox
            {
                Location = new Point(160, y), Size = new Size(320, 56),
                Font = UITheme.FontBody, Multiline = true,
                BackColor = UITheme.SurfaceAlt, BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(txtNote);
            y += 70;

            // Footer with action buttons
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.SurfaceAlt };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            var btnCreate = UITheme.SuccessBtn("⚡  Tạo Ngay", 150, 38);
            btnCreate.Click += BtnCreate_Click;
            btnCreate.AccessibleName = "Tạo học phí hàng loạt";

            var btnCancel = UITheme.GhostBtn("Hủy", 100, 38);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnCancel.AccessibleName = "Hủy bỏ";

            AcceptButton = btnCreate;
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
            flowBtns.Controls.Add(btnCreate);
            flowBtns.Controls.Add(btnCancel);
            footer.Controls.Add(flowBtns);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(topBar);
        }

        private static void AddLabel(Panel p, string text, int y) =>
            p.Controls.Add(new Label
            {
                Text = text, Location = new Point(28, y + 5), AutoSize = true,
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary
            });

        private void LoadData()
        {
            var classes = _studentSvc.GetDistinctClasses();
            cmbClass.Items.Clear();
            foreach (var c in classes) cmbClass.Items.Add(c);
            if (cmbClass.Items.Count > 0) cmbClass.SelectedIndex = 0;

            var semesters = _semesterSvc.GetAll();
            cmbSemester.Items.Clear();
            foreach (var s in semesters) cmbSemester.Items.Add(new SemItem(s.Id, s.Name));

            var active = _semesterSvc.GetActive();
            if (active != null)
            {
                for (int i = 0; i < cmbSemester.Items.Count; i++)
                    if (cmbSemester.Items[i] is SemItem si && si.Id == active.Id)
                    { cmbSemester.SelectedIndex = i; break; }
                dtpDueDate.Value = active.DueDate;
            }
            else
            {
                dtpDueDate.Value = DateTime.Now.AddMonths(1);
            }
            if (cmbSemester.SelectedIndex < 0 && cmbSemester.Items.Count > 0) cmbSemester.SelectedIndex = 0;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            int credits = (int)numCredits.Value;
            decimal totalPerStudent = credits * PRICE_PER_CREDIT;
            lblPerStudentTotal.Text = $"= {totalPerStudent:N0} VNĐ / SV";

            string? className = cmbClass.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(className) || cmbSemester.SelectedItem is not SemItem sem)
            {
                lblPreviewSummary.Text = "Vui lòng chọn đầy đủ lớp và học kỳ.";
                return;
            }

            var studentsInClass = _studentSvc.GetAllStudents().Where(student => string.Equals(student.ClassName, className, StringComparison.OrdinalIgnoreCase)).ToList();
            var existingFeesInSem = _tuitionSvc.GetBySemester(sem.Id);
            var existingStudentIds = existingFeesInSem.Select(f => f.StudentId).ToHashSet();

            int toCreateCount = studentsInClass.Count(s => !existingStudentIds.Contains(s.Id));
            int alreadyHasCount = studentsInClass.Count - toCreateCount;

            lblPreviewSummary.Text = $"• Tổng sinh viên lớp {className}: {studentsInClass.Count} sinh viên\n" +
                                     $"• Đã có phiếu học kỳ này: {alreadyHasCount}  |  Sẽ tạo mới: {toCreateCount} sinh viên";
        }

        private void BtnCreate_Click(object? s, EventArgs e)
        {
            string? className = cmbClass.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(className))
            {
                UiFeedback.ShowWarning("Vui lòng chọn lớp học!");
                return;
            }
            if (cmbSemester.SelectedItem is not SemItem sem)
            {
                UiFeedback.ShowWarning("Vui lòng chọn học kỳ!");
                return;
            }

            var studentsInClass = _studentSvc.GetAllStudents().Where(student => string.Equals(student.ClassName, className, StringComparison.OrdinalIgnoreCase)).ToList();
            if (studentsInClass.Count == 0)
            {
                UiFeedback.ShowWarning($"Lớp {className} hiện không có sinh viên nào!");
                return;
            }

            var existingFeesInSem = _tuitionSvc.GetBySemester(sem.Id);
            var existingStudentIds = existingFeesInSem.Select(f => f.StudentId).ToHashSet();
            var targetStudents = studentsInClass.Where(st => !existingStudentIds.Contains(st.Id)).ToList();

            if (targetStudents.Count == 0)
            {
                UiFeedback.ShowSuccess($"Tất cả {studentsInClass.Count} sinh viên lớp {className} đều đã có phiếu học phí kỳ này rồi!");
                return;
            }

            int credits = (int)numCredits.Value;
            decimal totalPerStudent = credits * PRICE_PER_CREDIT;
            string note = txtNote.Text.Trim();
            DateTime dueDate = dtpDueDate.Value;

            var confirm = UiFeedback.ConfirmAction(
                $"Xác nhận tạo {targetStudents.Count} phiếu học phí cho lớp {className}?\n" +
                $"- Học kỳ: {sem.Name}\n" +
                $"- Số tín chỉ: {credits} tín ({totalPerStudent:N0} VNĐ / SV)\n" +
                $"- Hạn nộp: {dueDate:dd/MM/yyyy}\n" +
                $"- Tổng dự thu: {(totalPerStudent * targetStudents.Count):N0} VNĐ");

            if (!confirm) return;

            var feesToCreate = targetStudents
                .Select(st => new TuitionFee(0, st.Id, sem.Id, credits, PRICE_PER_CREDIT, note, dueDate))
                .ToList();

            _tuitionSvc.AddRange(feesToCreate);

            UiFeedback.ShowSuccess($"Đã tạo thành công {feesToCreate.Count} phiếu học phí cho lớp {className}!\nHạn nộp: {dueDate:dd/MM/yyyy}");

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
