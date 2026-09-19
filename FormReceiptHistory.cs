using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormReceiptHistory : Form
    {
        private readonly TuitionFee _fee;
        private readonly Student _student;
        private readonly Semester _semester;
        private readonly ReceiptService _receiptSvc;

        private DataGridView dgv = null!;
        private Label lblTotalSummary = null!;
        private List<PaymentReceipt> _receipts = new();

        public FormReceiptHistory(TuitionFee fee, Student student, Semester semester, ReceiptService receiptSvc)
        {
            _fee = fee;
            _student = student;
            _semester = semester;
            _receiptSvc = receiptSvc;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            Text = $"Lịch Sử Đóng Tiền - {_student.FullName} ({_semester.Name})";
            ClientSize = new Size(960, 560);
            MinimumSize = new Size(920, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Header ────────────────────────────────────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Top, Height = 72,
                BackColor = UITheme.Purple
            };

            var headerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(24, 12, 24, 8),
                BackColor = Color.Transparent
            };

            var lblHeaderTitle = new Label
            {
                Text = $"📜  LỊCH SỬ NỘP TIỀN: {_student.FullName.ToUpper()}",
                Font = UITheme.FontH1, ForeColor = Color.White,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblHeaderSub = new Label
            {
                Text = $"Lớp: {_student.ClassName}  |  {_semester.Name}  |  Học phí: {_fee.TotalAmount:N0} VNĐ",
                Font = UITheme.FontSmall, ForeColor = UITheme.PurpleLight,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0)
            };
            headerFlow.Controls.AddRange(new Control[] { lblHeaderTitle, lblHeaderSub });
            topBar.Controls.Add(headerFlow);

            // ── Content Card ──────────────────────────────────────────────
            var wrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = UITheme.Background
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Surface
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleGrid(dgv);
            dgv.DoubleClick += (s, e) => ViewSelectedReceipt();

            // ── Bottom Action Bar ─────────────────────────────────────────
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.Surface
            };
            bottomBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            lblTotalSummary = new Label
            {
                Location = new Point(20, 20), AutoSize = true,
                Font = UITheme.FontBold, ForeColor = UITheme.PrimaryDark
            };
            bottomBar.Controls.Add(lblTotalSummary);

            var btnView = UITheme.PrimaryBtn("📄  Xem Biên Lai", 145, 36);
            btnView.Margin = new Padding(4, 0, 4, 0);
            btnView.Click += (s, e) => ViewSelectedReceipt();

            var btnExport = UITheme.GhostBtn("📥  Xuất CSV", 120, 36);
            btnExport.Margin = new Padding(4, 0, 4, 0);
            btnExport.Click += (s, e) => ExportHistoryCsv();

            var btnClose = UITheme.GhostBtn("Đóng", 90, 36);
            btnClose.Margin = new Padding(4, 0, 4, 0);
            btnClose.Click += (s, e) => Close();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 14, 20, 0),
                BackColor = Color.Transparent
            };
            AcceptButton = btnView;
            CancelButton = btnClose;

            flow.Controls.AddRange(new Control[] { btnView, btnExport, btnClose });
            bottomBar.Controls.Add(flow);

            card.Controls.Add(bottomBar);
            card.Controls.Add(dgv);
            dgv.BringToFront();

            wrap.Controls.Add(card);

            Controls.Add(wrap);
            Controls.Add(topBar);
        }

        private void LoadData()
        {
            _receipts = _receiptSvc.GetByTuitionFeeId(_fee.Id);

            var rows = _receipts.Select(r => new
            {
                r.Id,
                r.ReceiptCode,
                r.PaymentDate,
                r.Amount,
                r.PaymentMethod,
                r.PayerName,
                r.Note
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = rows;

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns["Id"] is { } c0) { c0.HeaderText = "ID"; c0.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c0.Width = 55; }
                if (dgv.Columns["ReceiptCode"] is { } c1) { c1.HeaderText = "Mã Biên Lai"; c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c1.Width = 140; }
                if (dgv.Columns["PaymentDate"] is { } c2)
                {
                    c2.HeaderText = "Ngày Giờ";
                    c2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c2.Width = 155;
                    c2.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
                if (dgv.Columns["Amount"] is { } c3)
                {
                    c3.HeaderText = "Số Tiền";
                    c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c3.Width = 120;
                    c3.DefaultCellStyle.Format = "N0";
                    c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c3.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgv.Columns["PaymentMethod"] is { } c4) { c4.HeaderText = "Hình Thức"; c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c4.Width = 150; }
                if (dgv.Columns["PayerName"] is { } c5) { c5.HeaderText = "Người Nộp"; c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c5.Width = 175; }
                if (dgv.Columns["Note"] is { } c6) { c6.HeaderText = "Ghi Chú"; c6.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            }

            decimal totalPaid = _receipts.Sum(r => r.Amount);
            lblTotalSummary.Text = $"Đã thu {_receipts.Count} lần: {totalPaid:N0} VNĐ  |  Còn lại: {_fee.RemainingAmount:N0} VNĐ";
        }

        private void ViewSelectedReceipt()
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một biên lai để xem!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = dgv.SelectedRows[0].Cells["Id"].Value is int v ? v : 0;
            var receipt = _receiptSvc.GetById(id);
            if (receipt == null) return;

            using var form = new FormReceipt(receipt, _student, _semester, _fee);
            form.ShowDialog();
        }

        private void ExportHistoryCsv()
        {
            var cols = new List<(string Header, Func<PaymentReceipt, object> ValueGetter)>
            {
                ("Mã biên lai", r => r.ReceiptCode),
                ("Ngày nộp", r => r.PaymentDate.ToString("dd/MM/yyyy HH:mm")),
                ("Số tiền (VNĐ)", r => r.Amount),
                ("Hình thức", r => r.PaymentMethod),
                ("Người nộp", r => r.PayerName),
                ("Ghi chú", r => r.Note)
            };

            CsvExportHelper.ExportToCsv($"LichSuThuTien_{_student.FullName}_{_semester.Name}.csv", _receipts, cols);
        }
    }
}
