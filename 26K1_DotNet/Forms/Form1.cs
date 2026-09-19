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
        private SqlDatabaseContext _dbContext = null!;

        public EmailService EmailService => _emailService;
        public SqlDatabaseContext DbContext => _dbContext;

        private PanelStudents? _panelStudents;
        private PanelTuition? _panelTuition;
        private PanelStatistics? _panelStatistics;
        private string _currentPanel = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                _dbContext       = DatabaseBootstrapper.InitializeDefaultDatabase(Environment.CurrentDirectory);
                _studentService  = new StudentService(_dbContext);
                _semesterService = new SemesterService(_dbContext);
                _tuitionService  = new TuitionService(_dbContext);
                _receiptService  = new ReceiptService(_dbContext);
                _emailService    = new EmailService("email_settings.json");

                UpdateHeaderActiveSemester();
                ShowPanel("students");
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
                case "students":
                    labelPageTitle.Text = "Quản Lý Sinh Viên";
                    lblPageSub.Text = "Xem, thêm, sửa và quản lý danh sách sinh viên các lớp";
                    _panelStudents ??= new PanelStudents(_studentService, this);
                    Show(_panelStudents);
                    break;

                case "tuition":
                    labelPageTitle.Text = "Quản Lý Học Phí";
                    lblPageSub.Text = "Theo dõi, tính học phí theo tín chỉ và ghi nhận thu tiền";
                    if (_panelTuition == null)
                        _panelTuition = new PanelTuition(_studentService, _semesterService, _tuitionService, _receiptService, this);
                    else
                        _panelTuition.RefreshData();
                    Show(_panelTuition);
                    break;

                case "statistics":
                    labelPageTitle.Text = "Thống Kê & Báo Cáo";
                    lblPageSub.Text = "Tổng hợp tài chính, tiến độ thu học phí và theo dõi nợ";
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
            using var form = new FormSemesterManage(_semesterService);
            form.ShowDialog();
            UpdateHeaderActiveSemester();
            var act = _semesterService.GetActive();
            _panelTuition?.RefreshData(act?.Id);
            _panelStatistics?.RefreshData(act?.Id);
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
                    isCurrent ? $"⭐  {sem.Name}  (Đang áp dụng)" : $"      {sem.Name}",
                    null,
                    (s, e) =>
                    {
                        _semesterService.SetActive(sem.Id);
                        UpdateHeaderActiveSemester();
                        _panelTuition?.RefreshData(sem.Id);
                        _panelStatistics?.RefreshData(sem.Id);
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
            cm.Items.Add(new ToolStripMenuItem("⚙️  Quản lý danh sách học kỳ...", null, (s, e) => OpenSemesterManager()));
            cm.Show(anchor, offset);
        }

        public void UpdateHeaderActiveSemester()
        {
            if (_semesterService == null) return;
            var act = _semesterService.GetActive();
            if (lblHeaderSemesterBadge != null)
            {
                lblHeaderSemesterBadge.Text = act != null ? $"📅  Học kỳ: {act.Name}" : "📅  Chưa chọn học kỳ";
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
            var itemEmail = new ToolStripMenuItem("📧  Cấu Hình Gửi Email SMTP...", null, (s, e) => OpenEmailSettings());
            var itemDb = new ToolStripMenuItem("🗄️  Quản Trị Cơ Sở Dữ Liệu SQL...", null, (s, e) => OpenDatabaseConfig());
            var itemSem = new ToolStripMenuItem("📅  Quản Lý Danh Sách Học Kỳ...", null, (s, e) => OpenSemesterManager());

            cm.Items.AddRange(new ToolStripItem[] { itemEmail, itemDb, new ToolStripSeparator(), itemSem });
            cm.Show(btnNavSettings, new Point(0, btnNavSettings.Height));
        }

        public void OpenEmailSettings()
        {
            using var form = new FormEmailSettings(_emailService);
            form.ShowDialog();
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
            UpdateHeaderActiveSemester();
        }
    }
}
