namespace _26K1_DotNet
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Sidebar
        private Panel panelSidebar;
        private Panel panelLogo;
        internal NavButton btnNavOverview, btnNavStudents, btnNavTuition, btnNavStatistics, btnNavSettings;
        internal Panel panelSemWidget;
        internal Label lblWidgetSemName;
        internal Label lblWidgetStatus;
        private Panel panelSidebarFooter;
        private Label lblVersion;

        // Header
        private Panel panelHeader;
        private Label labelPageTitle;
        private Label lblPageSub;
        internal Label lblHeaderSemesterBadge;

        // Content
        private Panel panelContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            panelSidebar       = new Panel();
            panelLogo          = new Panel();
            panelSidebarFooter = new Panel();
            lblVersion         = new Label();
            panelHeader        = new Panel();
            labelPageTitle     = new Label();
            lblPageSub         = new Label();
            panelContent       = new Panel();

            SuspendLayout();

            // ═══════════════════════════════════════════════════════════════
            // SIDEBAR
            // ═══════════════════════════════════════════════════════════════
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Width = 220;
            panelSidebar.BackColor = UITheme.SidebarBg;
            panelSidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.SidebarDivider, 1);
                e.Graphics.DrawLine(pen, panelSidebar.Width - 1, 0, panelSidebar.Width - 1, panelSidebar.Height);

            };

            // Logo area
            panelLogo.Dock = DockStyle.Top;
            panelLogo.Height = 72;
            panelLogo.BackColor = UITheme.SidebarDeep;
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, panelLogo, new object[] { true });

            panelLogo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.Clear(UITheme.SidebarDeep);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // Vector ledger mark: document lines + Vietnamese currency symbol.
                var badgeRect = new Rectangle(14, 16, 38, 38);
                using (var logoBg = new SolidBrush(UITheme.Primary))
                using (var path = UITheme.GetRoundedPath(badgeRect, 9))
                {
                    g.FillPath(logoBg, path);
                }

                using (var markPen = new Pen(Color.White, 1.6F))
                {
                    markPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    markPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLine(markPen, 23, 27, 35, 27);
                    g.DrawLine(markPen, 23, 34, 31, 34);
                    g.DrawLine(markPen, 23, 41, 35, 41);
                }
                using (var currencyBrush = new SolidBrush(Color.White))
                using (var currencyFont = new Font("Segoe UI", 8F, FontStyle.Bold))
                {
                    g.DrawString("₫", currencyFont, currencyBrush, new PointF(37, 29));
                }

                // 2. Title: "EduFee" (pure grayscale anti-aliasing against dark background, no ghosting/fringing)
                using (var titleBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("EduFee", UITheme.FontLogoBrand, titleBrush, new PointF(60, 13), StringFormat.GenericTypographic);
                }

                // 3. Product subtitle
                using (var subBrush = new SolidBrush(UITheme.AccentLavender))
                {
                    g.DrawString("Tuition Management", UITheme.FontSmall, subBrush, new PointF(60, 39), StringFormat.GenericTypographic);
                }

                // 4. Bottom separator line
                using var linePen = new Pen(UITheme.SidebarDivider, 1);
                g.DrawLine(linePen, 0, panelLogo.Height - 1, panelLogo.Width, panelLogo.Height - 1);
            };

            // Middle zone container (FlowLayoutPanel for responsive layout)
            var flowNavMiddle = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = UITheme.SidebarBg,
                Padding = new Padding(12, 14, 12, 14)
            };

            // Section label "MENU"
            var lblSection = new Label
            {
                Text = "Workspace",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.SidebarText,
                BackColor = Color.Transparent,
                AutoSize = true,
                Margin = new Padding(6, 0, 0, 8)
            };
            flowNavMiddle.Controls.Add(lblSection);

            // Nav buttons — floating contiguous pills with vector icons
            btnNavOverview   = new NavButton(UITheme.IconType.Overview, "Tổng quan")  { Size = new Size(196, 42), Margin = new Padding(0, 0, 0, 4) };
            btnNavStudents   = new NavButton(UITheme.IconType.Students, "Sinh viên")  { Size = new Size(196, 42), Margin = new Padding(0, 0, 0, 4) };
            btnNavTuition    = new NavButton(UITheme.IconType.Tuition, "Học phí")     { Size = new Size(196, 42), Margin = new Padding(0, 0, 0, 4) };
            btnNavStatistics = new NavButton(UITheme.IconType.Statistics, "Báo cáo")  { Size = new Size(196, 42), Margin = new Padding(0, 0, 0, 12) };
            btnNavSettings   = new NavButton(UITheme.IconType.Settings, "Cài đặt")    { Size = new Size(196, 42), Margin = new Padding(0, 8, 0, 14) };

            btnNavOverview.Click   += (s, e) => ShowPanel("overview");
            btnNavStudents.Click   += (s, e) => ShowPanel("students");
            btnNavTuition.Click    += (s, e) => ShowPanel("tuition");
            btnNavStatistics.Click += (s, e) => ShowPanel("statistics");
            btnNavSettings.Click   += (s, e) => OpenSystemSettingsMenu();

            flowNavMiddle.Controls.Add(btnNavOverview);
            flowNavMiddle.Controls.Add(btnNavStudents);
            flowNavMiddle.Controls.Add(btnNavTuition);
            flowNavMiddle.Controls.Add(btnNavStatistics);
            flowNavMiddle.Controls.Add(new Panel { Size = new Size(196, 1), BackColor = UITheme.SidebarDivider, Margin = new Padding(0, 2, 0, 2) });
            flowNavMiddle.Controls.Add(btnNavSettings);

            // Quick Semester Info Widget in Sidebar (Placed below navigation)
            panelSemWidget = new Panel
            {
                Size = new Size(196, 78),
                Margin = new Padding(0, 0, 0, 14),
                BackColor = UITheme.SidebarCard,
                Cursor = Cursors.Hand
            };
            bool semWidgetHover = false;
            panelSemWidget.MouseEnter += (s, e) => { semWidgetHover = true; panelSemWidget.Invalidate(); };
            panelSemWidget.MouseLeave += (s, e) => { semWidgetHover = false; panelSemWidget.Invalidate(); };
            panelSemWidget.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panelSemWidget.Width - 1, panelSemWidget.Height - 1);
                using var borderPen = new Pen(semWidgetHover ? UITheme.ActiveBorder : UITheme.SidebarDivider, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(borderPen, path);
            };

            var lblWidgetTitle = new Label
            {
                Text = "HỌC KỲ HIỆN TẠI ▾",
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.SidebarText,
                BackColor = UITheme.SidebarCard,
                Location = new Point(12, 10),
                AutoSize = true
            };

            lblWidgetSemName = new Label
            {
                Text = "Đang tải...",
                Font = UITheme.FontBold,
                ForeColor = Color.White,
                BackColor = UITheme.SidebarCard,
                Location = new Point(12, 28),
                AutoSize = true
            };

            lblWidgetStatus = new Label
            {
                Text = "● Đang áp dụng",
                Font = UITheme.FontSmall,
                ForeColor = UITheme.Success,
                BackColor = UITheme.SidebarCard,
                Location = new Point(12, 52),
                AutoSize = true
            };

            void OnSemWidgetClick(object s, MouseEventArgs e)
            {
                OpenSemesterQuickSwitch(panelSemWidget, new Point(0, panelSemWidget.Height + 2));
            }
            panelSemWidget.MouseClick += OnSemWidgetClick;
            lblWidgetTitle.MouseClick += OnSemWidgetClick;
            lblWidgetSemName.MouseClick += OnSemWidgetClick;
            lblWidgetStatus.MouseClick += OnSemWidgetClick;

            void WireHover(Control c)
            {
                c.MouseEnter += (s, e) => { semWidgetHover = true; panelSemWidget.Invalidate(); };
                c.MouseLeave += (s, e) => { semWidgetHover = false; panelSemWidget.Invalidate(); };
            }
            WireHover(lblWidgetTitle);
            WireHover(lblWidgetSemName);
            WireHover(lblWidgetStatus);

            panelSemWidget.Controls.AddRange(new Control[] { lblWidgetTitle, lblWidgetSemName, lblWidgetStatus });

            // Sidebar footer
            panelSidebarFooter.Dock = DockStyle.Bottom;
            panelSidebarFooter.Height = 48;
            panelSidebarFooter.BackColor = UITheme.SidebarDeep;
            panelSidebarFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.SidebarDivider, 1);
                e.Graphics.DrawLine(pen, 0, 0, panelSidebarFooter.Width, 0);
            };

            lblVersion.Text = "EduFee Desktop  ·  UI V2";
            lblVersion.Location = new Point(16, 16);
            lblVersion.Font = UITheme.FontSmall;
            lblVersion.ForeColor = UITheme.TextSecondary;
            lblVersion.BackColor = UITheme.SidebarDeep;
            lblVersion.AutoSize = true;

            panelSidebarFooter.Controls.Add(lblVersion);

            panelSidebar.Controls.Add(flowNavMiddle);
            panelSidebar.Controls.Add(panelLogo);
            panelSidebar.Controls.Add(panelSidebarFooter);

            // ═══════════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════════
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 76;
            panelHeader.BackColor = UITheme.Surface;
            panelHeader.Padding = new Padding(32, 0, 32, 0);

            var panelTitleBox = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(32, 11),
                BackColor = UITheme.Surface,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            labelPageTitle.Text = "";
            labelPageTitle.Font = UITheme.FontDisplay;
            labelPageTitle.ForeColor = UITheme.TextPrimary;
            labelPageTitle.UseMnemonic = false;
            labelPageTitle.Margin = new Padding(0, 0, 0, 3);
            labelPageTitle.AutoSize = true;

            lblPageSub.Text = "";
            lblPageSub.Font = UITheme.FontBody;
            lblPageSub.ForeColor = UITheme.TextSecondary;
            lblPageSub.UseMnemonic = false;
            lblPageSub.Margin = new Padding(0, 0, 0, 0);
            lblPageSub.AutoSize = true;

            panelTitleBox.Controls.Add(labelPageTitle);
            panelTitleBox.Controls.Add(lblPageSub);

            lblHeaderSemesterBadge = new Label
            {
                AutoSize = true,
                Font = UITheme.FontBold,
                ForeColor = UITheme.PrimaryDark,
                BackColor = UITheme.PrimaryLight,
                Padding = new Padding(14, 8, 14, 8),
                Location = new Point(880, 22),
                Text = "Học kỳ: —",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            lblHeaderSemesterBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, lblHeaderSemesterBadge.Width - 1, lblHeaderSemesterBadge.Height - 1);
                using var pen = new Pen(UITheme.ActiveBorder, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };
            lblHeaderSemesterBadge.Click += (s, e) => OpenSemesterQuickSwitch(lblHeaderSemesterBadge, new Point(0, lblHeaderSemesterBadge.Height + 2));

            void PositionSemesterBadge()
            {
                lblHeaderSemesterBadge.Left = Math.Max(420,
                    panelHeader.ClientSize.Width - panelHeader.Padding.Right - lblHeaderSemesterBadge.Width);
            }
            panelHeader.Resize += (s, e) => PositionSemesterBadge();
            lblHeaderSemesterBadge.TextChanged += (s, e) => PositionSemesterBadge();

            panelHeader.Controls.AddRange(new Control[] { panelTitleBox, lblHeaderSemesterBadge });
            lblHeaderSemesterBadge.BringToFront();
            panelHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Border, 1);
                e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };

            // ═══════════════════════════════════════════════════════════════
            // CONTENT
            // ═══════════════════════════════════════════════════════════════
            panelContent.Dock = DockStyle.Fill;
            panelContent.BackColor = UITheme.Background;

            // ═══════════════════════════════════════════════════════════════
            // FORM
            // ═══════════════════════════════════════════════════════════════
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1320, 800);
            MinimumSize = new Size(1160, 680);
            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelSidebar);
            Text = "EduFee — Student Tuition Management";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UITheme.Background;
            Load += Form1_Load;

            ResumeLayout(false);
        }
        #endregion
    }
}
