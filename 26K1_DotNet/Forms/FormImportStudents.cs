using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormImportStudents : Form
    {
        private readonly StudentService _svc;
        private DataGridView dgv = null!;
        private Label lblSummary = null!, lblFile = null!;
        private Button btnConfirm = null!;
        private List<StudentImportRow> _rows = new();

        public FormImportStudents(StudentService svc)
        {
            _svc = svc;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "Nhập danh sách sinh viên từ CSV";
            ClientSize = new Size(880, 560);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Header Banner ───────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = UITheme.PrimaryDark,
                Padding = new Padding(24, 0, 24, 0)
            };
            header.Controls.Add(new Label
            {
                Text = "NHẬP DANH SÁCH SINH VIÊN TỰ ĐỘNG",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Toolbar ─────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top, Height = 64,
                BackColor = UITheme.Surface,
                Padding = new Padding(24, 14, 24, 14)
            };
            toolbar.Controls.Add(UITheme.HSep(DockStyle.Bottom));

            var flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent
            };

            var btnSelect = UITheme.PrimaryBtn("Chọn file CSV...", 130, 36);
            btnSelect.Click += (s, e) => SelectFile();

            var btnSample = UITheme.GhostBtn("Tải file mẫu", 110, 36);
            btnSample.Margin = new Padding(8, 0, 16, 0);
            btnSample.Click += (s, e) => DownloadSampleCsv();

            lblFile = new Label
            {
                Text = "Chưa chọn file nào (Vui lòng chọn file .csv định dạng UTF-8)",
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                AutoSize = true, Margin = new Padding(0, 9, 0, 0)
            };

            flowLeft.Controls.AddRange(new Control[] { btnSelect, btnSample, lblFile });
            toolbar.Controls.Add(flowLeft);

            // ── Grid Container ──────────────────────────────────────────────
            var wrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 14),
                BackColor = UITheme.Background
            };

            var card = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Surface };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleGrid(dgv);
            dgv.CellPainting += Dgv_CellPainting;
            card.Controls.Add(dgv);
            wrap.Controls.Add(card);

            // ── Bottom Footer ───────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.SurfaceAlt,
                Padding = new Padding(24, 0, 24, 0)
            };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            lblSummary = new Label
            {
                Text = "Chưa có dữ liệu để nhập.",
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary,
                Dock = DockStyle.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 22, 0, 0)
            };

            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 14, 0, 0)
            };

            btnConfirm = UITheme.SuccessBtn("Xác nhận nhập", 130, 36);
            btnConfirm.Enabled = false;
            btnConfirm.Click += (s, e) => ConfirmImport();

            var btnCancel = UITheme.GhostBtn("Đóng", 85, 36);
            btnCancel.Margin = new Padding(8, 0, 0, 0);
            btnCancel.Click += (s, e) => Close();

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;

            flowRight.Controls.AddRange(new Control[] { btnConfirm, btnCancel });

            footer.Controls.Add(lblSummary);
            footer.Controls.Add(flowRight);

            Controls.Add(wrap);
            Controls.Add(toolbar);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private void SelectFile()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Chọn file danh sách sinh viên"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                lblFile.Text = Path.GetFileName(ofd.FileName);
                ParseCsvFile(ofd.FileName);
            }
        }

        private void ParseCsvFile(string filePath)
        {
            _rows.Clear();
            BindGrid();
            try
            {
                _rows = StudentCsvImporter.Read(filePath, _svc.GetAllStudents().Select(s => s.Id));
                BindGrid();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi xử lý dữ liệu");
            }
        }
        private void BindGrid()
        {
            var display = _rows.Select(r => new
            {
                TrangThai = r.Status,
                MaSV = r.Id,
                HoTen = r.FullName,
                Lop = r.ClassName,
                NgaySinh = r.DateOfBirth.ToString("dd/MM/yyyy"),
                DienThoai = r.PhoneNumber,
                Email = r.Email
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = display;

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns["TrangThai"] is { } c0) { c0.HeaderText = "Trạng Thái"; c0.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c0.Width = 120; }
                if (dgv.Columns["MaSV"] is { } c1) { c1.HeaderText = "Mã SV"; c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c1.Width = 75; }
                if (dgv.Columns["HoTen"] is { } c2) c2.HeaderText = "Họ và Tên";
                if (dgv.Columns["Lop"] is { } c3) { c3.HeaderText = "Lớp"; c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c3.Width = 110; }
                if (dgv.Columns["NgaySinh"] is { } c4) { c4.HeaderText = "Ngày sinh"; c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c4.Width = 100; c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                if (dgv.Columns["DienThoai"] is { } c5) { c5.HeaderText = "Điện thoại"; c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c5.Width = 110; }
                if (dgv.Columns["Email"] is { } c6) c6.HeaderText = "Email";
            }

            int validCount = _rows.Count(r => r.IsValid);
            int errorCount = _rows.Count - validCount;

            lblSummary.Text = $"Tổng số: {_rows.Count} dòng   |   Hợp lệ: {validCount}   |   Lỗi/Trùng: {errorCount}";
            btnConfirm.Enabled = validCount > 0;
            btnConfirm.Text = $"Nhập {validCount} sinh viên";
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgv.Columns.Count == 0 || e.Graphics == null) return;
            var col = dgv.Columns["TrangThai"];
            if (col != null && e.ColumnIndex == col.Index && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
                string text = e.Value.ToString() ?? "";
                bool isValid = text == "Hợp lệ";
                Color bg = isValid ? UITheme.SuccessLight : UITheme.DangerLight;
                Color fg = isValid ? UITheme.SuccessDark : UITheme.DangerDark;
                UITheme.DrawCustomBadge(e.Graphics, e.CellBounds, text, bg, fg);
                e.Handled = true;
            }
        }

        private void ConfirmImport()
        {
            var valid = _rows.Where(r => r.IsValid).ToList();
            if (valid.Count == 0) return;

            try
            {
                _svc.AddStudents(valid.Select(r => new Student(r.Id, r.FullName, r.Email, r.PhoneNumber, r.DateOfBirth, r.ClassName)));

                UiFeedback.ShowSuccess($"Đã nhập thành công {valid.Count} sinh viên vào hệ thống!");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi xử lý dữ liệu");
            }
        }

        private void DownloadSampleCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = "MauNhapSinhVien.csv",
                Title = "Lưu file mẫu nhập sinh viên"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var sb = new StringBuilder();
                sb.AppendLine("MaSV,HoTen,Lop,NgaySinh,DienThoai,Email");
                sb.AppendLine("2601,Nguyễn Văn An,26K1_CNTT,15/08/2004,0912345678,an.nv@edu.vn");
                sb.AppendLine("2602,Trần Thị Mai,26K1_CNTT,20/11/2004,0987654321,mai.tt@edu.vn");
                sb.AppendLine("2603,Lê Hoàng Nam,26K1_KTPM,05/03/2004,0905123456,nam.lh@edu.vn");
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                UiFeedback.ShowSuccess($"Đã lưu file mẫu tại:\n{sfd.FileName}");
            }
        }

    }
}
