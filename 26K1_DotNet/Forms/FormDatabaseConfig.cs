using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using K26_DotNet.Data;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormDatabaseConfig : Form
    {
        private readonly SqlDatabaseContext _dbContext;
        private readonly StudentService _studentSvc;
        private readonly SemesterService _semSvc;
        private readonly TuitionService _tuiSvc;
        private readonly ReceiptService _receiptSvc;
        private readonly Form1 _mainForm;

        private Label lblPath = null!, lblCounts = null!, lblStatus = null!;
        private Button btnMigrate = null!, btnLoadSql = null!, btnBackup = null!, btnRestore = null!;

        public FormDatabaseConfig(SqlDatabaseContext dbContext, StudentService studentSvc,
            SemesterService semSvc, TuitionService tuiSvc, ReceiptService receiptSvc, Form1 mainForm)
        {
            _dbContext = dbContext;
            _studentSvc = studentSvc;
            _semSvc = semSvc;
            _tuiSvc = tuiSvc;
            _receiptSvc = receiptSvc;
            _mainForm = mainForm;
            BuildUI();
            RefreshStats();
        }

        private void BuildUI()
        {
            Text = "Quản Trị Cơ Sở Dữ Liệu SQL";
            ClientSize = new Size(580, 540);
            MinimumSize = new Size(580, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Surface;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Header ──────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = UITheme.PrimaryDark,
                Padding = new Padding(20, 0, 20, 0)
            };
            header.Controls.Add(new Label
            {
                Text = "QUẢN TRỊ CƠ SỞ DỮ LIỆU (SQLITE)",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            // ── Body ────────────────────────────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 16, 28, 16),
                BackColor = UITheme.Surface
            };

            int y = 14;

            // Database info card
            var cardInfo = new Panel
            {
                Location = new Point(16, y), Size = new Size(532, 110),
                BackColor = UITheme.SurfaceAlt
            };
            cardInfo.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, cardInfo.Width - 1, cardInfo.Height - 1);
                e.Graphics.FillRectangle(new SolidBrush(UITheme.Primary), 0, 0, 4, cardInfo.Height);
            };

            new Label
            {
                Text = "THÔNG TIN CƠ SỞ DỮ LIỆU HIỆN TẠI",
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary,
                Location = new Point(16, 12), AutoSize = true, Parent = cardInfo
            };

            lblPath = new Label
            {
                Text = $"Tệp CSDL: {_dbContext.DbPath}",
                Font = UITheme.FontSmall, ForeColor = UITheme.TextPrimary,
                Location = new Point(16, 36), Size = new Size(500, 36),
                Parent = cardInfo
            };

            lblCounts = new Label
            {
                Text = "Đang kiểm tra số lượng bản ghi...",
                Font = UITheme.FontBold, ForeColor = UITheme.Primary,
                Location = new Point(16, 76), AutoSize = true,
                Parent = cardInfo
            };
            body.Controls.Add(cardInfo);
            y += 126;

            // Migration section
            var cardActions = new Panel
            {
                Location = new Point(16, y), Size = new Size(532, 210),
                BackColor = UITheme.Surface
            };
            cardActions.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, cardActions.Width - 1, cardActions.Height - 1);
            };

            new Label
            {
                Text = "CÔNG CỤ ĐỒNG BỘ & SAO LƯU DỮ LIỆU",
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary,
                Location = new Point(16, 14), AutoSize = true, Parent = cardActions
            };

            btnMigrate = UITheme.PrimaryBtn("Nhập lại dữ liệu JSON vào SQL", 260, 38);
            btnMigrate.Location = new Point(16, 44);
            btnMigrate.Click += BtnMigrate_Click;
            cardActions.Controls.Add(btnMigrate);

            new Label
            {
                Text = "Chuyển toàn bộ Sinh viên, Học kỳ, Học phí và Biên lai từ JSON sang CSDL SQL.",
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted,
                Location = new Point(16, 88), AutoSize = true, Parent = cardActions
            };

            btnBackup = UITheme.GhostBtn("Sao lưu", 100, 36);
            btnBackup.Margin = new Padding(0, 0, 10, 0);
            btnBackup.Click += BtnBackup_Click;

            btnRestore = UITheme.GhostBtn("Phục hồi", 100, 36);
            btnRestore.Margin = new Padding(0, 0, 10, 0);
            btnRestore.Click += BtnRestore_Click;

            btnLoadSql = UITheme.SuccessBtn("Làm mới", 100, 36);
            btnLoadSql.Margin = new Padding(0);
            btnLoadSql.Click += BtnLoadSql_Click;

            var flowDbActions = new FlowLayoutPanel
            {
                Location = new Point(16, 120),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            flowDbActions.Controls.AddRange(new Control[] { btnBackup, btnRestore, btnLoadSql });
            cardActions.Controls.Add(flowDbActions);

            lblStatus = new Label
            {
                Text = "Hệ thống sẵn sàng thao tác với CSDL.",
                Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary,
                Location = new Point(16, 172), AutoSize = true, Parent = cardActions
            };

            body.Controls.Add(cardActions);

            // ── Footer ──────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.SurfaceAlt };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            var btnClose = UITheme.GhostBtn("Đóng", 100, 38);
            btnClose.Click += (s, e) => Close();
            CancelButton = btnClose;

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 13, 24, 0),
                BackColor = Color.Transparent
            };
            flowBtns.Controls.Add(btnClose);
            footer.Controls.Add(flowBtns);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
        }

        private void RefreshStats()
        {
            try
            {
                var (sv, sem, fee, rec) = _dbContext.GetRecordCounts();
                lblCounts.Text = $"Trong CSDL SQL:  {sv} Sinh viên  |  {sem} Học kỳ  |  {fee} Học phí  |  {rec} Biên lai";
            }
            catch (Exception ex)
            {
                lblCounts.Text = $"Lỗi đọc CSDL: {ex.Message}";
                lblCounts.ForeColor = UITheme.Danger;
            }
        }

        private void BtnMigrate_Click(object? sender, EventArgs e)
        {
            using var folder = new FolderBrowserDialog { Description = "Chọn thư mục chứa đủ students.json, semesters.json, tuitionfees.json và receipts.json" };
            if (folder.ShowDialog(this) != DialogResult.OK) return;
            if (!UiFeedback.ConfirmAction($"Thay dữ liệu SQL bằng JSON trong:\n{folder.SelectedPath}\nDữ liệu hiện tại sẽ được sao lưu tự động. Bạn có muốn tiếp tục?"))
            {
                return;
            }

            btnMigrate.Enabled = false;
            lblStatus.Text = "Đang thực hiện chuyển đổi dữ liệu sang SQL...";
            lblStatus.ForeColor = UITheme.Primary;

            try
            {
                string backup = _dbContext.DbPath + $".before-import-{Guid.NewGuid():N}.db";
                _dbContext.BackupTo(backup);
                var res = DatabaseBootstrapper.ImportLegacyJson(_dbContext, folder.SelectedPath);
                _studentSvc.LoadStudents();
                _semSvc.LoadSemesters();
                _tuiSvc.LoadFees();
                _receiptSvc.LoadFromFile();
                _mainForm.RefreshCurrentPanel();

                RefreshStats();
                lblStatus.Text = $"✓ {res.Message} ({res.Students} SV, {res.Semesters} HK, {res.Fees} HP, {res.Receipts} Biên lai)";
                lblStatus.ForeColor = UITheme.Success;

                UiFeedback.ShowSuccess($"Đồng bộ thành công!\n- Sinh viên: {res.Students}\n- Học kỳ: {res.Semesters}\n- Phiếu học phí: {res.Fees}\n- Biên lai: {res.Receipts}\nBản sao lưu trước khi nhập:\n{backup}");
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                lblStatus.ForeColor = UITheme.Danger;
                UiFeedback.ShowException(ex, "Lỗi chuyển đổi dữ liệu");
            }
            finally
            {
                btnMigrate.Enabled = true;
            }
        }

        private void BtnBackup_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                FileName = $"EduFee_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                Title = "Chọn nơi lưu file sao lưu CSDL"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _dbContext.BackupTo(sfd.FileName);
                    UiFeedback.ShowSuccess($"Đã sao lưu CSDL thành công tại:\n{sfd.FileName}");
                }
                catch (Exception ex)
                {
                    UiFeedback.ShowException(ex, "Lỗi sao lưu CSDL");
                }
            }
        }

        private void BtnLoadSql_Click(object? sender, EventArgs e)
        {
            if (!UiFeedback.ConfirmAction("Nạp lại toàn bộ dữ liệu từ CSDL SQL vào ứng dụng hiện tại?"))
            {
                return;
            }

            using (UiFeedback.BusyScope(btnLoadSql, lblStatus, "Đang nạp dữ liệu từ SQL..."))
            {
                try
                {
                    _studentSvc.LoadStudents();
                    _semSvc.LoadSemesters();
                    _tuiSvc.LoadFees();
                    _receiptSvc.LoadFromFile();

                    _mainForm.RefreshCurrentPanel();
                    var counts = _dbContext.GetRecordCounts();
                    lblStatus.Text = "✓ Đã làm mới dữ liệu từ SQL.";
                    lblStatus.ForeColor = UITheme.Success;
                    UiFeedback.ShowSuccess($"Đã làm mới {counts.Students} sinh viên, {counts.Semesters} học kỳ, {counts.Fees} phiếu học phí và {counts.Receipts} biên lai từ SQL.");
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Lỗi nạp: {ex.Message}";
                    lblStatus.ForeColor = UITheme.Danger;
                    UiFeedback.ShowException(ex, "Lỗi nạp dữ liệu từ SQL");
                }
            }
        }

        private void BtnRestore_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                Title = "Chọn bản sao lưu EduFee"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            if (!UiFeedback.ConfirmAction("Dữ liệu hiện tại sẽ được sao lưu tự động rồi thay bằng bản đã chọn. Bạn có muốn tiếp tục?")) return;

            try
            {
                string safetyBackup = _dbContext.RestoreFrom(dialog.FileName);
                _studentSvc.LoadStudents();
                _semSvc.LoadSemesters();
                _tuiSvc.LoadFees();
                _receiptSvc.LoadFromFile();
                _mainForm.RefreshCurrentPanel();
                RefreshStats();
                UiFeedback.ShowSuccess($"Phục hồi thành công. Bản dữ liệu trước khi phục hồi được lưu tại:\n{safetyBackup}");
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Không thể phục hồi cơ sở dữ liệu");
            }
        }
    }
}
