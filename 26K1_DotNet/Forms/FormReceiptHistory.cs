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
        private Label lblStudentName = null!;
        private Label lblStudentMeta = null!;
        private Label lblLedgerHint = null!;
        private Panel ledgerSummary = null!;
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
            Text = $"Sổ giao dịch học phí - {_student.FullName}";
            ClientSize = new Size(1040, 650);
            MinimumSize = new Size(920, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Background;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            var topBar = UITheme.CreateDialogHeader(
                string.Empty,
                "Sổ giao dịch học phí",
                $"{_student.StudentCode} · {_semester.Name}",
                UITheme.PrimaryDark,
                68);

            var wrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UITheme.PadPage, 16, UITheme.PadPage, 16),
                BackColor = UITheme.Background
            };

            ledgerSummary = UITheme.CreateRoundedCard();
            ledgerSummary.Dock = DockStyle.Top;
            ledgerSummary.Height = 124;
            ledgerSummary.AccessibleName = "Tóm tắt sổ giao dịch học phí";

            lblStudentName = new Label
            {
                Text = _student.FullName,
                Font = UITheme.FontH1,
                ForeColor = UITheme.TextPrimary,
                AutoSize = true
            };
            lblStudentMeta = new Label
            {
                Text = $"{_student.StudentCode}  ·  {_student.ClassName}  ·  {_semester.Name}",
                Font = UITheme.FontBody,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true
            };
            lblLedgerHint = new Label
            {
                Text = "Các giao dịch đã được ghi nhận theo thời gian. Chọn một dòng để mở biên lai.",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextMuted,
                AutoSize = true
            };
            ledgerSummary.Controls.AddRange(new Control[] { lblStudentName, lblStudentMeta, lblLedgerHint });

            var totalCard = UITheme.CreateStatCard("TỔNG HỌC PHÍ", $"{_fee.TotalAmount:N0} ₫", null, UITheme.Primary, UITheme.TextPrimary);
            var paidCard = UITheme.CreateStatCard("ĐÃ GHI NHẬN", $"{_fee.PaidAmount:N0} ₫", null, UITheme.Success, UITheme.SuccessDark);
            var remainingCard = UITheme.CreateStatCard("CÒN LẠI", $"{_fee.RemainingAmount:N0} ₫", null,
                _fee.RemainingAmount > 0m ? UITheme.Danger : UITheme.Success,
                _fee.RemainingAmount > 0m ? UITheme.DangerDark : UITheme.SuccessDark);
            ledgerSummary.Controls.AddRange(new Control[] { totalCard, paidCard, remainingCard });

            ledgerSummary.Resize += (_, _) =>
            {
                const int leftWidth = 330;
                const int cardGap = 8;
                int cardWidth = Math.Max(150, (ledgerSummary.ClientSize.Width - leftWidth - UITheme.PadCard * 2 - cardGap * 2) / 3);
                lblStudentName.Location = new Point(UITheme.PadCard, 20);
                lblStudentMeta.Location = new Point(UITheme.PadCard, 50);
                lblLedgerHint.Location = new Point(UITheme.PadCard, 78);
                for (int i = 0; i < 3; i++)
                {
                    var stat = ledgerSummary.Controls[3 + i];
                    stat.SetBounds(leftWidth + i * (cardWidth + cardGap), 16, cardWidth, 90);
                }
            };

            var timelineHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 16, 0, 0)
            };
            var lblTimelineTitle = new Label
            {
                Text = "Dòng thời gian thanh toán",
                Font = UITheme.FontH2,
                ForeColor = UITheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 17)
            };
            var lblTimelineSub = new Label
            {
                Text = "Biên lai mới nhất hiển thị trước",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            timelineHeader.Controls.AddRange(new Control[] { lblTimelineTitle, lblTimelineSub });
            timelineHeader.Resize += (_, _) => lblTimelineSub.Location = new Point(
                Math.Max(lblTimelineTitle.Right + 24, timelineHeader.ClientSize.Width - lblTimelineSub.Width), 19);

            var card = UITheme.CreateRoundedCard();
            card.Dock = DockStyle.Fill;
            card.Padding = Padding.Empty;
            card.AccessibleName = "Dòng thời gian các biên lai";

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UITheme.StyleGrid(dgv);
            dgv.DoubleClick += (s, e) => ViewSelectedReceipt();
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.Handled = true;
                ViewSelectedReceipt();
            };
            dgv.CellPainting += DrawTimelineMarker;

            // ── Bottom Action Bar ─────────────────────────────────────────
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = UITheme.Surface
            };
            bottomBar.Controls.Add(UITheme.HSep(DockStyle.Top));

            lblTotalSummary = new Label
            {
                Location = new Point(UITheme.PadCard, 20), AutoSize = true,
                Font = UITheme.FontBold, ForeColor = UITheme.TextPrimary,
                AccessibleName = "Tổng kết giao dịch"
            };
            bottomBar.Controls.Add(lblTotalSummary);

            var btnView = UITheme.PrimaryBtn("Xem biên lai", 115, 36);
            btnView.Margin = new Padding(4, 0, 4, 0);
            btnView.Click += (s, e) => ViewSelectedReceipt();

            var btnExport = UITheme.GhostBtn("Xuất CSV", 95, 36);
            btnExport.Margin = new Padding(4, 0, 4, 0);
            btnExport.Click += (s, e) => ExportHistoryCsv();

            var btnClose = UITheme.GhostBtn("Đóng", 80, 36);
            btnClose.Margin = new Padding(4, 0, 4, 0);
            btnClose.Click += (s, e) => Close();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 14, UITheme.PadCard, 0),
                BackColor = Color.Transparent
            };
            AcceptButton = btnView;
            CancelButton = btnClose;

            flow.Controls.AddRange(new Control[] { btnView, btnExport, btnClose });
            bottomBar.Controls.Add(flow);

            card.Controls.Add(dgv);

            wrap.Controls.Add(card);
            wrap.Controls.Add(timelineHeader);
            wrap.Controls.Add(ledgerSummary);

            Controls.Add(wrap);
            Controls.Add(topBar);
            Controls.Add(bottomBar);
        }

        private void LoadData()
        {
            _receipts = _receiptSvc.GetByTuitionFeeId(_fee.Id);

            var rows = _receipts
                .OrderByDescending(r => r.PaymentDate)
                .Select(r => new
            {
                r.Id,
                Timeline = string.Empty,
                r.ReceiptCode,
                r.PaymentDate,
                r.Amount,
                r.PaymentMethod,
                r.PayerName,
                Status = "Đã ghi nhận",
                r.Note
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = rows;

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns["Id"] is { } c0) c0.Visible = false;
                if (dgv.Columns["Timeline"] is { } timeline) { timeline.HeaderText = string.Empty; timeline.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; timeline.Width = 42; timeline.SortMode = DataGridViewColumnSortMode.NotSortable; }
                if (dgv.Columns["ReceiptCode"] is { } c1) { c1.HeaderText = "BIÊN LAI"; c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c1.Width = 150; c1.DefaultCellStyle.Font = UITheme.FontBold; }
                if (dgv.Columns["PaymentDate"] is { } c2)
                {
                    c2.HeaderText = "THỜI ĐIỂM";
                    c2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c2.Width = 155;
                    c2.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                }
                if (dgv.Columns["Amount"] is { } c3)
                {
                    c3.HeaderText = "SỐ TIỀN";
                    c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    c3.Width = 120;
                    c3.DefaultCellStyle.Format = "N0";
                    c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c3.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgv.Columns["PaymentMethod"] is { } c4) { c4.HeaderText = "HÌNH THỨC"; c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c4.Width = 135; }
                if (dgv.Columns["PayerName"] is { } c5) { c5.HeaderText = "NGƯỜI NỘP"; c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c5.Width = 155; }
                if (dgv.Columns["Status"] is { } c6) { c6.HeaderText = "TRẠNG THÁI"; c6.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c6.Width = 120; c6.DefaultCellStyle.ForeColor = UITheme.SuccessDark; c6.DefaultCellStyle.Font = UITheme.FontSmallBold; }
                if (dgv.Columns["Note"] is { } c7) { c7.HeaderText = "GHI CHÚ"; c7.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            }

            decimal totalPaid = _receipts.Sum(r => r.Amount);
            lblTotalSummary.Text = _receipts.Count == 0
                ? "Chưa có giao dịch được ghi nhận cho khoản học phí này."
                : $"{_receipts.Count:N0} giao dịch  ·  Đã ghi nhận {totalPaid:N0} ₫  ·  Còn lại {_fee.RemainingAmount:N0} ₫";
        }

        private void DrawTimelineMarker(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "Timeline") return;
            if (e.Graphics is not { } graphics) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
            var centerX = e.CellBounds.Left + e.CellBounds.Width / 2;
            using var line = new Pen(UITheme.BorderStrong, 2);
            using var dot = new SolidBrush(UITheme.Success);
            if (e.RowIndex > 0) graphics.DrawLine(line, centerX, e.CellBounds.Top, centerX, e.CellBounds.Top + e.CellBounds.Height / 2);
            if (e.RowIndex < dgv.Rows.Count - 1) graphics.DrawLine(line, centerX, e.CellBounds.Top + e.CellBounds.Height / 2, centerX, e.CellBounds.Bottom);
            graphics.FillEllipse(dot, centerX - 5, e.CellBounds.Top + e.CellBounds.Height / 2 - 5, 10, 10);
            e.Handled = true;
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
