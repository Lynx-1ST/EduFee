using K26_DotNet.Data;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public partial class Form1 : Form
    {
        private StudentService _studentService = null!;
        private SemesterService _semesterService = null!;
        private TuitionService _tuitionService = null!;
        private ReceiptService _receiptService = null!;
        private EmailService _emailService = null!;
        private MomoSettingsService _momoSettingsService = null!;
        private GatewayPaymentPersistenceService _gatewayPaymentPersistence = null!;
        private SqlDatabaseContext _dbContext = null!;
        private readonly CancellationTokenSource _gatewayRecoveryCancellation = new();
        private bool _gatewayRecoveryRunning;

        public EmailService EmailService => _emailService;
        public SqlDatabaseContext DbContext => _dbContext;
        public MomoSettingsService MomoSettingsService => _momoSettingsService;
        public GatewayPaymentPersistenceService GatewayPaymentPersistence => _gatewayPaymentPersistence;
        public IPaymentGateway? MomoGateway => !_demoMode && _momoSettingsService.Settings.Enabled
            ? new MomoSandboxPaymentGateway(_momoSettingsService) : null;

        private PanelStudents? _panelStudents;
        private PanelTuition? _panelTuition;
        private PanelStatistics? _panelStatistics;
        private PanelOverview? _panelOverview;
        private string _currentPanel = "";
        private readonly bool _demoMode;
        private readonly string? _dataDirectory;

        public Form1(bool demoMode = false, string? dataDirectory = null)
        {
            _demoMode = demoMode;
            _dataDirectory = dataDirectory;
            InitializeComponent();
            DoubleBuffered = true;
            Text = demoMode ? "EduFee — DỮ LIỆU DEMO" : "EduFee";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var bootstrap = _demoMode
                    ? DatabaseBootstrapper.InitializeDemoDatabase(_dataDirectory)
                    : DatabaseBootstrapper.InitializeDefaultDatabase(_dataDirectory ?? DatabaseBootstrapper.GetDefaultDataDirectory(), Environment.CurrentDirectory);
                _dbContext       = bootstrap.Database;
                _studentService  = new StudentService(_dbContext);
                _semesterService = new SemesterService(_dbContext);
                _tuitionService  = new TuitionService(_dbContext);
                _receiptService  = new ReceiptService(_dbContext);
                _emailService    = new EmailService(DatabaseBootstrapper.GetEmailSettingsPath(_dataDirectory ?? (_demoMode ? DatabaseBootstrapper.GetDemoDataDirectory() : null)));
                var settingsDirectory = _dataDirectory ?? (_demoMode ? DatabaseBootstrapper.GetDemoDataDirectory() : DatabaseBootstrapper.GetDefaultDataDirectory());
                _momoSettingsService = new MomoSettingsService(Path.Combine(settingsDirectory, "momo-settings.json"));
                _gatewayPaymentPersistence = new GatewayPaymentPersistenceService(_dbContext, _tuitionService, _receiptService);
                if (_demoMode) _emailService.Settings.IsSimulationMode = true;

                UpdateHeaderActiveSemester();
                ShowPanel("overview");
                if (!string.IsNullOrWhiteSpace(bootstrap.Notice))
                    lblPageSub.Text = bootstrap.Notice;
                if (!_demoMode && _momoSettingsService.Settings.Enabled)
                    BeginInvoke(new Action(async () => await RecoverPendingMomoPaymentsAsync(showSummary: false)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo dữ liệu: {ex.Message}\nỨng dụng sẽ đóng để bảo vệ dữ liệu. Vui lòng kiểm tra tệp dữ liệu hoặc bản sao lưu.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
            }
        }

        internal void ShowPanel(string name)
        {
            if (_currentPanel == name && panelContent.Controls.Count > 0) return;
            _currentPanel = name;

            panelContent.Controls.Clear();
            SetNavActive(name);

            switch (name)
            {
                case "overview":
                    labelPageTitle.Text = "Tổng quan";
                    lblPageSub.Text = "Tình hình thu học phí và các khoản cần xử lý";
                    if (_panelOverview == null)
                        _panelOverview = new PanelOverview(_studentService, _semesterService, _tuitionService,
                            _receiptService, NavigateToTuitionForStudent);
                    else
                        _panelOverview.RefreshData();
                    Show(_panelOverview);
                    break;

                case "students":
                    labelPageTitle.Text = "Sinh viên";
                    lblPageSub.Text = "Hồ sơ và danh sách sinh viên";
                    _panelStudents ??= new PanelStudents(_studentService, this);
                    Show(_panelStudents);
                    break;

                case "tuition":
                    labelPageTitle.Text = "Học phí";
                    lblPageSub.Text = "Theo dõi nghĩa vụ tài chính và ghi nhận thanh toán";
                    if (_panelTuition == null)
                        _panelTuition = new PanelTuition(_studentService, _semesterService, _tuitionService, _receiptService, this);
                    else
                        _panelTuition.RefreshData();
                    Show(_panelTuition);
                    break;

                case "statistics":
                    labelPageTitle.Text = "Báo cáo";
                    lblPageSub.Text = "Công nợ, tổng hợp theo lớp và dữ liệu xuất báo cáo";
                    if (_panelStatistics == null)
                        _panelStatistics = new PanelStatistics(_studentService, _semesterService, _tuitionService, _receiptService, this);
                    else
                        _panelStatistics.RefreshData();
                    Show(_panelStatistics);
                    break;
            }
        }

        private void Show(Control panel)
        {
            panel.Dock = DockStyle.Fill;
            panelContent.Controls.Add(panel);
        }

        private void SetNavActive(string name)
        {
            btnNavOverview.IsActive   = (name == "overview");
            btnNavStudents.IsActive   = (name == "students");
            btnNavTuition.IsActive    = (name == "tuition");
            btnNavStatistics.IsActive = (name == "statistics");
        }

        // ── Smart Cross-Linking Methods ───────────────────────────────────────

        /// <summary>
        /// Chuyển ngay sang tab Học Phí và lọc danh sách cho riêng một sinh viên
        /// </summary>
        public void NavigateToTuitionForStudent(int studentId)
        {
            ShowPanel("tuition");
            _panelTuition?.FilterByStudent(studentId);
        }

        /// <summary>
        /// Mở hộp thoại quản lý danh sách học kỳ
        /// </summary>
        public void OpenSemesterManager()
        {
            using var form = new FormSemesterManage(_semesterService, _tuitionService);
            form.ShowDialog();
            UpdateHeaderActiveSemester();
            var act = _semesterService.GetActive();
            _panelTuition?.RefreshData(act?.Id);
            _panelStatistics?.RefreshData(act?.Id);
            _panelOverview?.RefreshData();
        }

        public void OpenSemesterQuickSwitch(Control anchor, Point offset)
        {
            var cm = new ContextMenuStrip();
            cm.Font = UITheme.FontBody;
            var active = _semesterService.GetActive();
            foreach (var sem in _semesterService.GetAll())
            {
                bool isCurrent = (active != null && active.Id == sem.Id);
                var item = new ToolStripMenuItem(
                    isCurrent ? $"●  {sem.Name}  (Đang áp dụng)" : $"    {sem.Name}",
                    null,
                    (s, e) =>
                    {
                        _semesterService.SetActive(sem.Id);
                        UpdateHeaderActiveSemester();
                        _panelTuition?.RefreshData(sem.Id);
                        _panelStatistics?.RefreshData(sem.Id);
                        _panelOverview?.RefreshData();
                        UiFeedback.ShowSuccess($"Đã đặt «{sem.Name}» làm học kỳ hiện tại!");
                    });
                if (isCurrent)
                {
                    item.Font = UITheme.FontBold;
                    item.ForeColor = UITheme.Primary;
                }
                cm.Items.Add(item);
            }

            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add(new ToolStripMenuItem("Quản lý danh sách học kỳ...", null, (s, e) => OpenSemesterManager()));
            cm.Show(anchor, offset);
        }

        public void UpdateHeaderActiveSemester()
        {
            if (_semesterService == null) return;
            var act = _semesterService.GetActive();
            if (lblHeaderSemesterBadge != null)
            {
                lblHeaderSemesterBadge.Text = act != null ? $"Học kỳ: {act.Name}" : "Chưa chọn học kỳ";
            }
            if (lblWidgetSemName != null)
            {
                lblWidgetSemName.Text = act != null ? act.Name : "Chưa chọn";
            }
            if (lblWidgetStatus != null)
            {
                lblWidgetStatus.Text = act != null ? "● Đang áp dụng" : "○ Chưa chọn";
                lblWidgetStatus.ForeColor = act != null ? UITheme.Success : UITheme.SidebarText;
            }
        }

        internal void OpenSystemSettingsMenu()
        {
            var cm = new ContextMenuStrip();
            cm.Font = UITheme.FontBody;
            var itemEmail = new ToolStripMenuItem("Cấu hình gửi Email SMTP...", null, (s, e) => OpenEmailSettings());
            var itemMomo = new ToolStripMenuItem("Cấu hình thanh toán MoMo Sandbox...", null, (s, e) => OpenMomoSettings());
            var itemRecoverMomo = new ToolStripMenuItem("Kiểm tra lại giao dịch MoMo đang chờ...", null,
                async (s, e) => await RecoverPendingMomoPaymentsAsync(showSummary: true));
            var itemDb = new ToolStripMenuItem("Quản trị cơ sở dữ liệu SQL...", null, (s, e) => OpenDatabaseConfig());
            var itemSem = new ToolStripMenuItem("Quản lý danh sách học kỳ...", null, (s, e) => OpenSemesterManager());

            cm.Items.AddRange(new ToolStripItem[]
                { itemEmail, itemMomo, itemRecoverMomo, itemDb, new ToolStripSeparator(), itemSem });
            cm.Show(btnNavSettings, new Point(0, btnNavSettings.Height));
        }

        public void OpenEmailSettings()
        {
            if (_demoMode)
            {
                MessageBox.Show("Chế độ demo chỉ mô phỏng email và không cho phép thay đổi cấu hình SMTP.",
                    "Dữ liệu demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var form = new FormEmailSettings(_emailService);
            form.ShowDialog();
        }

        public void OpenMomoSettings()
        {
            if (_demoMode)
            {
                UiFeedback.ShowInfo("Chế độ demo luôn dùng VietQR và không gọi MoMo Sandbox.");
                return;
            }
            using var form = new FormMomoSettings(_momoSettingsService);
            form.ShowDialog(this);
        }

        public async Task RecoverPendingMomoPaymentsAsync(bool showSummary)
        {
            if (_gatewayRecoveryRunning)
            {
                if (showSummary) UiFeedback.ShowInfo("Ứng dụng đang kiểm tra các giao dịch MoMo đang chờ.");
                return;
            }
            if (_demoMode)
            {
                if (showSummary) UiFeedback.ShowInfo("Chế độ demo không truy vấn MoMo Sandbox.");
                return;
            }
            var gateway = MomoGateway;
            if (gateway == null)
            {
                if (showSummary) UiFeedback.ShowWarning("MoMo Sandbox chưa được bật hoặc cấu hình chưa đầy đủ.");
                return;
            }

            _gatewayRecoveryRunning = true;
            try
            {
                var recovery = new GatewayPaymentRecoveryService(_gatewayPaymentPersistence);
                var result = await recovery.RecoverAsync(gateway, transaction =>
                {
                    var fee = _tuitionService.GetById(transaction.TuitionFeeId)
                        ?? throw new InvalidOperationException("Không tìm thấy học phí của giao dịch đang khôi phục.");
                    var student = _studentService.GetStudentById(fee.StudentId);
                    var semester = _semesterService.GetById(fee.SemesterId);
                    return new GatewayRecoveryContext(student?.FullName ?? "Thanh toán MoMo", semester?.DueDate);
                }, _gatewayRecoveryCancellation.Token);

                if (result.ReceiptsCreated > 0) RefreshCurrentPanel();
                if (result.Errors.Count > 0)
                {
                    string message = $"Đã kiểm tra {result.Checked} giao dịch; {result.Errors.Count} giao dịch chưa thể khôi phục.";
                    if (showSummary) UiFeedback.ShowWarning(message + "\n" + string.Join("\n", result.Errors.Take(3)));
                    else lblPageSub.Text = message + " Có thể kiểm tra lại từ menu Cài đặt.";
                }
                else if (showSummary || result.ReceiptsCreated > 0)
                {
                    UiFeedback.ShowSuccess($"Đã kiểm tra {result.Checked} giao dịch; " +
                        $"tạo {result.ReceiptsCreated} biên lai và cập nhật {result.StatusesUpdated} trạng thái.");
                }
            }
            catch (OperationCanceledException) when (_gatewayRecoveryCancellation.IsCancellationRequested)
            {
                // Application is closing.
            }
            catch (Exception ex)
            {
                if (showSummary) UiFeedback.ShowException(ex, "Không thể kiểm tra lại giao dịch MoMo");
                else lblPageSub.Text = "Chưa thể kiểm tra lại giao dịch MoMo. Có thể thử lại từ menu Cài đặt.";
            }
            finally
            {
                _gatewayRecoveryRunning = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _gatewayRecoveryCancellation.Cancel();
            _gatewayRecoveryCancellation.Dispose();
            base.OnFormClosed(e);
        }

        public void OpenDatabaseConfig()
        {
            using var form = new FormDatabaseConfig(_dbContext, _studentService, _semesterService, _tuitionService, _receiptService, this);
            form.ShowDialog();
        }

        public void RefreshCurrentPanel()
        {
            _panelStudents?.LoadData();
            _panelTuition?.RefreshData();
            _panelStatistics?.RefreshData();
            _panelOverview?.RefreshData();
            UpdateHeaderActiveSemester();
        }
    }
}
