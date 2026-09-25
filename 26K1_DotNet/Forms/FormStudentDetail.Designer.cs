namespace _26K1_DotNet
{
    partial class FormStudentDetail
    {
        private System.ComponentModel.IContainer components = null;
        private Label labelHeaderTitle;
        private Label labelHeaderSub;
        private Label labelAvatar;
        private Label labelProfileName;
        private Label labelProfileMeta;
        private Panel panelHeader;
        private Panel panelCard;
        private Label labelId;
        private TextBox textBoxId;
        private Label labelFullName;
        private TextBox textBoxFullName;
        private Label labelEmail;
        private TextBox textBoxEmail;
        private Label labelPhoneNumber;
        private TextBox textBoxPhoneNumber;
        private Label labelDOB;
        private DateTimePicker dateTimePickerDOB;
        private Label labelClassName;
        private TextBox textBoxClassName;
        private Panel panelFooter;
        private Button buttonSave;
        private Button buttonCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelHeader = new Panel();
            labelHeaderTitle = new Label();
            labelHeaderSub = new Label();
            labelAvatar = new Label();
            labelProfileName = new Label();
            labelProfileMeta = new Label();
            panelCard = new Panel();
            labelId = new Label();
            textBoxId = new TextBox();
            labelFullName = new Label();
            textBoxFullName = new TextBox();
            labelClassName = new Label();
            textBoxClassName = new TextBox();
            labelEmail = new Label();
            textBoxEmail = new TextBox();
            labelPhoneNumber = new Label();
            textBoxPhoneNumber = new TextBox();
            labelDOB = new Label();
            dateTimePickerDOB = new DateTimePicker();
            panelFooter = new Panel();
            buttonSave = UITheme.PrimaryBtn("Lưu thay đổi", 125, 36);
            buttonCancel = UITheme.GhostBtn("Hủy", 80, 36);
            panelHeader.SuspendLayout();
            panelCard.SuspendLayout();
            panelFooter.SuspendLayout();
            SuspendLayout();

            // panelHeader
            panelHeader.BackColor = UITheme.Surface;
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 116;
            panelHeader.Padding = new Padding(24, 14, 24, 12);
            panelHeader.Controls.Add(UITheme.HSep(DockStyle.Bottom));

            var panelTitleFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(96, 17),
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // labelHeaderTitle
            labelHeaderTitle.AutoSize = true;
            labelHeaderTitle.Font = UITheme.FontSmallBold;
            labelHeaderTitle.ForeColor = UITheme.TextSecondary;
            labelHeaderTitle.UseMnemonic = false;
            labelHeaderTitle.Margin = new Padding(0, 0, 0, 2);
            labelHeaderTitle.Text = "Thông tin sinh viên";

            // labelHeaderSub
            labelHeaderSub.AutoSize = true;
            labelHeaderSub.Font = UITheme.FontSmall;
            labelHeaderSub.ForeColor = UITheme.TextMuted;
            labelHeaderSub.UseMnemonic = false;
            labelHeaderSub.Margin = new Padding(0, 0, 0, 0);
            labelHeaderSub.Text = "Vui lòng nhập đầy đủ các thông tin sinh viên";

            panelTitleFlow.Controls.Add(labelHeaderTitle);
            panelTitleFlow.Controls.Add(labelHeaderSub);
            panelHeader.Controls.Add(panelTitleFlow);

            labelAvatar.BackColor = UITheme.PrimaryLight;
            labelAvatar.ForeColor = UITheme.PrimaryDark;
            labelAvatar.Font = UITheme.FontH1;
            labelAvatar.Location = new Point(24, 22);
            labelAvatar.Size = new Size(56, 56);
            labelAvatar.TextAlign = ContentAlignment.MiddleCenter;
            labelAvatar.Text = "SV";

            labelProfileName.AutoSize = true;
            labelProfileName.Font = UITheme.FontH1;
            labelProfileName.ForeColor = UITheme.TextPrimary;
            labelProfileName.Location = new Point(96, 45);
            labelProfileName.Text = "Hồ sơ sinh viên";

            labelProfileMeta.AutoSize = true;
            labelProfileMeta.Font = UITheme.FontSmall;
            labelProfileMeta.ForeColor = UITheme.TextSecondary;
            labelProfileMeta.Location = new Point(96, 70);
            labelProfileMeta.Text = "Thông tin học tập và liên hệ";

            panelHeader.Controls.AddRange(new Control[] { labelAvatar, labelProfileName, labelProfileMeta });

            // panelCard
            panelCard.BackColor = UITheme.Surface;
            panelCard.Location = new Point(24, 132);
            panelCard.Size = new Size(480, 282);
            panelCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panelCard.Width - 1, panelCard.Height - 1);
                using var pen = new Pen(UITheme.Border, 1);
                using var path = UITheme.GetRoundedPath(rect, 8);
                e.Graphics.DrawPath(pen, path);
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(20, 14, 20, 14)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 6; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 41F));
            }

            Label CreateLabel(string text) => new Label
            {
                Text = text,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 0)
            };

            // Mã SV
            textBoxId = new TextBox { AccessibleName = "Mã sinh viên", TabIndex = 0, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelId = CreateLabel("Mã sinh viên");
            table.Controls.Add(labelId, 0, 0);
            table.Controls.Add(textBoxId, 1, 0);

            // Họ Tên
            textBoxFullName = new TextBox { AccessibleName = "Họ và tên", TabIndex = 1, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelFullName = CreateLabel("Họ và tên *");
            table.Controls.Add(labelFullName, 0, 1);
            table.Controls.Add(textBoxFullName, 1, 1);

            // Lớp
            textBoxClassName = new TextBox { AccessibleName = "Lớp học", TabIndex = 2, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelClassName = CreateLabel("Lớp học *");
            table.Controls.Add(labelClassName, 0, 2);
            table.Controls.Add(textBoxClassName, 1, 2);

            // Email
            textBoxEmail = new TextBox { AccessibleName = "Email", TabIndex = 3, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelEmail = CreateLabel("Email *");
            table.Controls.Add(labelEmail, 0, 3);
            table.Controls.Add(textBoxEmail, 1, 3);

            // Điện thoại
            textBoxPhoneNumber = new TextBox { AccessibleName = "Điện thoại", TabIndex = 4, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelPhoneNumber = CreateLabel("Điện thoại *");
            table.Controls.Add(labelPhoneNumber, 0, 4);
            table.Controls.Add(textBoxPhoneNumber, 1, 4);

            // Ngày sinh
            dateTimePickerDOB = new DateTimePicker { Format = DateTimePickerFormat.Short, AccessibleName = "Ngày sinh", TabIndex = 5, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelDOB = CreateLabel("Ngày sinh");
            table.Controls.Add(labelDOB, 0, 5);
            table.Controls.Add(dateTimePickerDOB, 1, 5);

            panelCard.Controls.Add(table);

            // panelFooter
            panelFooter.BackColor = UITheme.SurfaceAlt;
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 60;
            panelFooter.Controls.Add(UITheme.HSep(DockStyle.Top));

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 12, 24, 0)
            };
            buttonSave.Click += ButtonSave_Click;
            buttonCancel.Click += ButtonCancel_Click;
            flowBtns.Controls.AddRange(new Control[] { buttonSave, buttonCancel });
            panelFooter.Controls.Add(flowBtns);

            // FormStudentDetail
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = UITheme.Background;
            ClientSize = new Size(528, 484);
            Controls.Add(panelCard);
            Controls.Add(panelFooter);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormStudentDetail";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Chi Tiết Sinh Viên";
            AcceptButton = buttonSave;
            CancelButton = buttonCancel;
            Load += FormStudentDetail_Load;

            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelCard.ResumeLayout(false);
            panelCard.PerformLayout();
            panelFooter.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
