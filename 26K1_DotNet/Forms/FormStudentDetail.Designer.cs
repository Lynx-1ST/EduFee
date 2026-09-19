namespace _26K1_DotNet
{
    partial class FormStudentDetail
    {
        private System.ComponentModel.IContainer components = null;
        private Label labelHeaderTitle;
        private Label labelHeaderSub;
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
            panelHeader.BackColor = UITheme.PrimaryDark;
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 64;
            panelHeader.Padding = new Padding(24, 10, 24, 10);

            var panelTitleFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(20, 10),
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // labelHeaderTitle
            labelHeaderTitle.AutoSize = true;
            labelHeaderTitle.Font = UITheme.FontH1;
            labelHeaderTitle.ForeColor = Color.White;
            labelHeaderTitle.UseMnemonic = false;
            labelHeaderTitle.Margin = new Padding(0, 0, 0, 2);
            labelHeaderTitle.Text = "Thông tin sinh viên";

            // labelHeaderSub
            labelHeaderSub.AutoSize = true;
            labelHeaderSub.Font = UITheme.FontSmall;
            labelHeaderSub.ForeColor = UITheme.SubtitleLight;
            labelHeaderSub.UseMnemonic = false;
            labelHeaderSub.Margin = new Padding(0, 0, 0, 0);
            labelHeaderSub.Text = "Vui lòng nhập đầy đủ các thông tin sinh viên";

            panelTitleFlow.Controls.Add(labelHeaderTitle);
            panelTitleFlow.Controls.Add(labelHeaderSub);
            panelHeader.Controls.Add(panelTitleFlow);

            // panelCard
            panelCard.BackColor = UITheme.Surface;
            panelCard.Location = new Point(24, 80);
            panelCard.Size = new Size(480, 290);
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
                Padding = new Padding(16, 12, 16, 12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 6; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            }

            Label CreateLabel(string text) => new Label
            {
                Text = text,
                Font = UITheme.FontBold,
                ForeColor = UITheme.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 0)
            };

            // Mã SV
            textBoxId = new TextBox { ReadOnly = true, BackColor = UITheme.SurfaceAlt, AccessibleName = "Mã sinh viên", TabIndex = 0, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelId = CreateLabel("Mã SV:");
            table.Controls.Add(labelId, 0, 0);
            table.Controls.Add(textBoxId, 1, 0);

            // Họ Tên
            textBoxFullName = new TextBox { AccessibleName = "Họ và tên", TabIndex = 1, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelFullName = CreateLabel("Họ và Tên *:");
            table.Controls.Add(labelFullName, 0, 1);
            table.Controls.Add(textBoxFullName, 1, 1);

            // Lớp
            textBoxClassName = new TextBox { AccessibleName = "Lớp học", TabIndex = 2, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelClassName = CreateLabel("Lớp học *:");
            table.Controls.Add(labelClassName, 0, 2);
            table.Controls.Add(textBoxClassName, 1, 2);

            // Email
            textBoxEmail = new TextBox { AccessibleName = "Email", TabIndex = 3, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelEmail = CreateLabel("Email *:");
            table.Controls.Add(labelEmail, 0, 3);
            table.Controls.Add(textBoxEmail, 1, 3);

            // Điện thoại
            textBoxPhoneNumber = new TextBox { AccessibleName = "Điện thoại", TabIndex = 4, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelPhoneNumber = CreateLabel("Điện thoại *:");
            table.Controls.Add(labelPhoneNumber, 0, 4);
            table.Controls.Add(textBoxPhoneNumber, 1, 4);

            // Ngày sinh
            dateTimePickerDOB = new DateTimePicker { Format = DateTimePickerFormat.Short, AccessibleName = "Ngày sinh", TabIndex = 5, Font = UITheme.FontBody, Dock = DockStyle.Fill };
            labelDOB = CreateLabel("Ngày sinh:");
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
            ClientSize = new Size(528, 440);
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
