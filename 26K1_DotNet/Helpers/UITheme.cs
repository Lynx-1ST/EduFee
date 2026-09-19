using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    /// <summary>
    /// Centralized UI Theme - Indigo + Slate design system
    /// </summary>
    public static class UITheme
    {
        // ── Palette ───────────────────────────────────────────────────────────
        public static readonly Color Primary       = Color.FromArgb(99, 102, 241);   // #6366F1
        public static readonly Color PrimaryDark   = Color.FromArgb(79, 70, 229);    // #4F46E5
        public static readonly Color PrimaryLight  = Color.FromArgb(238, 242, 255);  // #EEF2FF

        public static readonly Color Success       = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color SuccessDark   = Color.FromArgb(5, 150, 105);
        public static readonly Color SuccessLight  = Color.FromArgb(209, 250, 229);

        public static readonly Color Warning       = Color.FromArgb(245, 158, 11);   // #F59E0B
        public static readonly Color WarningDark   = Color.FromArgb(180, 110, 0);
        public static readonly Color WarningLight  = Color.FromArgb(254, 243, 199);

        public static readonly Color Danger        = Color.FromArgb(239, 68, 68);    // #EF4444
        public static readonly Color DangerDark    = Color.FromArgb(185, 28, 28);
        public static readonly Color DangerLight   = Color.FromArgb(254, 226, 226);

        public static readonly Color Purple        = Color.FromArgb(139, 92, 246);   // #8B5CF6
        public static readonly Color PurpleDark    = Color.FromArgb(109, 40, 217);
        public static readonly Color PurpleLight   = Color.FromArgb(237, 233, 254);

        public static readonly Color Info          = Color.FromArgb(14, 165, 233);   // #0EA5E9
        public static readonly Color InfoDark      = Color.FromArgb(3, 105, 161);    // #0369A1
        public static readonly Color InfoLight     = Color.FromArgb(224, 242, 254);

        // ── Surfaces ──────────────────────────────────────────────────────────
        public static readonly Color Background    = Color.FromArgb(241, 245, 249);  // #F1F5F9
        public static readonly Color Surface       = Color.White;
        public static readonly Color SurfaceAlt    = Color.FromArgb(248, 250, 252);  // #F8FAFC
        public static readonly Color Border        = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color BorderLight   = Color.FromArgb(241, 245, 249);
        public static readonly Color AlternatingRow = Color.FromArgb(250, 252, 255); // grid alternate row

        // ── Text ──────────────────────────────────────────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(15, 23, 42);     // #0F172A
        public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);  // #64748B
        public static readonly Color TextMuted     = Color.FromArgb(100, 116, 139);  // #64748B
        public static readonly Color TextLight     = Color.FromArgb(203, 213, 225);  // #CBD5E1 (NavButton inactive text)

        // ── Sidebar ───────────────────────────────────────────────────────────
        public static readonly Color SidebarBg          = Color.FromArgb(15, 23, 42);   // #0F172A
        public static readonly Color SidebarHover       = Color.FromArgb(30, 41, 59);   // #1E293B
        public static readonly Color SidebarActive      = Color.FromArgb(30, 41, 59);
        public static readonly Color SidebarAccent      = Color.FromArgb(99, 102, 241); // same as Primary
        public static readonly Color SidebarText        = Color.FromArgb(148, 163, 184);
        public static readonly Color SidebarActiveText  = Color.White;
        public static readonly Color SidebarDivider     = Color.FromArgb(30, 41, 59);   // #1E293B
        public static readonly Color SidebarDeep        = Color.FromArgb(10, 15, 29);   // deepest sidebar bg
        public static readonly Color SidebarCard        = Color.FromArgb(16, 24, 39);   // semester widget bg
        public static readonly Color SidebarHoverBorder = Color.FromArgb(51, 65, 85);   // #334155

        // ── Accent Colors ─────────────────────────────────────────────────────
        public static readonly Color AccentLavender = Color.FromArgb(165, 180, 252);  // #A5B4FC brand accent
        public static readonly Color AccentIndigo   = Color.FromArgb(49, 46, 129);    // #312E81 dark indigo
        public static readonly Color ActiveBorder   = Color.FromArgb(129, 140, 248);  // #818CF8 NavButton active
        public static readonly Color ActiveDot      = Color.FromArgb(224, 231, 255);  // #E0E7FF active indicator
        public static readonly Color SubtitleLight  = Color.FromArgb(224, 231, 255);  // #E0E7FF dialog subtitle

        // ── Simulation Panel ──────────────────────────────────────────────────
        public static readonly Color SimBg          = Color.FromArgb(240, 253, 244);  // green-50
        public static readonly Color SimBorder      = Color.FromArgb(134, 239, 172);  // green-300
        public static readonly Color SimText        = Color.FromArgb(22, 101, 52);    // green-800
        public static readonly Color SimTextDark    = Color.FromArgb(21, 128, 61);    // green-700

        // ── Debt / Notice ─────────────────────────────────────────────────────
        public static readonly Color DebtRed        = Color.FromArgb(220, 38, 38);    // red-600
        public static readonly Color DebtAmber      = Color.FromArgb(180, 83, 9);     // amber-700
        public static readonly Color QrBorder       = Color.FromArgb(203, 213, 225);  // slate-300
        public static readonly Color QrText         = Color.FromArgb(30, 41, 59);     // slate-800

        // ── Fonts ─────────────────────────────────────────────────────────────
        public static readonly Font FontDisplay    = new("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font FontH1         = new("Segoe UI", 14F, FontStyle.Bold);
        public static readonly Font FontH2         = new("Segoe UI", 11F, FontStyle.Bold);
        public static readonly Font FontH2Regular  = new("Segoe UI", 11.5F, FontStyle.Bold);  // FormTuitionDetail total
        public static readonly Font FontBody       = new("Segoe UI", 9.5F);
        public static readonly Font FontBodyLarge  = new("Segoe UI", 10.5F);          // larger input fields
        public static readonly Font FontSmall      = new("Segoe UI", 8.5F);
        public static readonly Font FontSmallBold  = new("Segoe UI", 8.5F, FontStyle.Bold);
        public static readonly Font FontBold       = new("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FontNav        = new("Segoe UI", 10F);
        public static readonly Font FontNavBold    = new("Segoe UI", 9.75F, FontStyle.Bold);  // active nav text
        public static readonly Font FontNavRegular = new("Segoe UI", 9.75F);          // inactive nav text
        public static readonly Font FontMono       = new("Consolas", 9F);
        public static readonly Font FontGridHeader = new("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font FontCardTitle  = new("Segoe UI", 8F, FontStyle.Bold);     // summary card headings
        public static readonly Font FontCardValue  = new("Segoe UI", 15F, FontStyle.Bold);    // summary card values
        public static readonly Font FontCardValue2 = new("Segoe UI", 16F, FontStyle.Bold);    // alt card value size
        public static readonly Font FontPercent    = new("Segoe UI", 12F, FontStyle.Bold);     // progress percentage
        public static readonly Font FontEmoji      = new("Segoe UI Emoji", 12.5F);            // NavButton icon
        public static readonly Font FontEmojiLarge = new("Segoe UI Emoji", 16F);              // Form1 logo paint
        public static readonly Font FontLogoBrand  = new("Segoe UI", 15F, FontStyle.Bold);     // Sidebar logo brand text
        public static readonly Font FontLogoBadge  = new("Segoe UI", 7F, FontStyle.Bold);      // Sidebar PRO badge
        public static readonly Font FontInputLarge = new("Segoe UI", 12F, FontStyle.Bold);    // payment amount input

        // ── Spacing Constants ─────────────────────────────────────────────────
        public const int PadPage    = 24;
        public const int PadCard    = 16;
        public const int PadToolbar = 28;

        // ── Corner Radii Constants ────────────────────────────────────────────
        public const int RadiusSm   = 4;
        public const int RadiusMd   = 8;
        public const int RadiusLg   = 12;
        public const int RadiusPill = 999;
        public const int RowHeight  = 44;
        public const int HeaderHeight = 46;

        // ── Cached GDI Resources ──────────────────────────────────────────────
        private static readonly Pen BorderPen = new(Border, 1);

        // ── Button Factory ────────────────────────────────────────────────────
        public static Button MakeButton(string text, Color bg, Color hover, int w = 130, int h = 36, Color? fg = null)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(w, h),
                Font = FontBold, BackColor = bg, ForeColor = fg ?? Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(w, h),
                Padding = new Padding(12, 0, 12, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, hover.R - 20), Math.Max(0, hover.G - 20), Math.Max(0, hover.B - 20));
            return btn;
        }

        public static Button PrimaryBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Primary, PrimaryDark, w, h);
        public static Button SecondaryBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, PrimaryLight, Color.FromArgb(224, 231, 255), w, h, PrimaryDark);
        public static Button SuccessBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Color.FromArgb(4, 120, 87), Color.FromArgb(6, 95, 70), w, h);
        public static Button DangerBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Danger, DangerDark, w, h);
        public static Button PurpleBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Purple, PurpleDark, w, h);
        public static Button WarningBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Color.FromArgb(146, 64, 14), Color.FromArgb(120, 53, 15), w, h);
        public static Button InfoBtn(string text, int w = 130, int h = 36) =>
            MakeButton(text, Info, InfoDark, w, h);
        public static Button GhostBtn(string text, int w = 130, int h = 36)
        {
            var btn = new Button
            {
                Text = text, Size = new Size(w, h),
                Font = FontBody, BackColor = Surface, ForeColor = TextSecondary,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(w, h),
                Padding = new Padding(12, 0, 12, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.MouseOverBackColor = SurfaceAlt;
            return btn;
        }

        // ── TextBox Factory ───────────────────────────────────────────────────
        public static TextBox MakeSearchBox(string placeholder, int w = 260, int h = 34)
        {
            return new TextBox
            {
                Size = new Size(w, h),
                Font = FontBody,
                BackColor = SurfaceAlt,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = placeholder
            };
        }

        /// <summary>
        /// Apply theme styling to a TextBox input field.
        /// </summary>
        public static void StyleTextBox(TextBox txt)
        {
            txt.Font = FontBody;
            txt.BackColor = SurfaceAlt;
            txt.ForeColor = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        /// <summary>
        /// Apply theme styling to a ComboBox.
        /// </summary>
        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.Font = FontBody;
            cmb.BackColor = SurfaceAlt;
            cmb.ForeColor = TextPrimary;
            cmb.FlatStyle = FlatStyle.Flat;
        }

        // ── DataGridView ──────────────────────────────────────────────────────
        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Surface;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.GridColor = Border;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Font = FontBody;
            dgv.RowTemplate.Height = RowHeight;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = HeaderHeight;
            dgv.ShowCellToolTips = true;

            // Header
            dgv.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontGridHeader;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextSecondary;

            // Rows
            dgv.DefaultCellStyle.BackColor = Surface;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            dgv.DefaultCellStyle.SelectionForeColor = PrimaryDark;
            dgv.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRow;
        }

        // ── Card Panel ────────────────────────────────────────────────────────
        /// <summary>
        /// Create an absolute-positioned card panel (legacy API, preserved for backward compat).
        /// </summary>
        public static Panel Card(int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Surface,
                Padding = new Padding(PadCard)
            };
            p.Paint += CardBorderPaint;
            return p;
        }

        /// <summary>
        /// Create a docking-friendly card panel with themed border.
        /// </summary>
        public static Panel DockCard(DockStyle dock = DockStyle.Fill, int height = 0)
        {
            var p = new Panel
            {
                Dock = dock,
                BackColor = Surface,
                Padding = new Padding(PadCard)
            };
            if (height > 0) p.Height = height;
            p.Paint += CardBorderPaint;
            return p;
        }

        /// <summary>
        /// Shared card border paint handler using cached pen.
        /// </summary>
        private static void CardBorderPaint(object? sender, PaintEventArgs e)
        {
            if (sender is Control c)
            {
                e.Graphics.DrawRectangle(BorderPen, 0, 0, c.Width - 1, c.Height - 1);
            }
        }

        // ── Dialog Header / Footer Factories ──────────────────────────────────

        /// <summary>
        /// Create a standard dialog header bar (Dock Top).
        /// </summary>
        public static Panel CreateDialogHeader(string icon, string title, string? subtitle = null, Color? accentColor = null, int height = 60)
        {
            var accent = accentColor ?? PrimaryDark;
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = accent,
                Padding = new Padding(PadPage, 0, PadPage, 0)
            };

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(PadPage, subtitle != null ? 8 : (height - 24) / 2),
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lblTitle = new Label
            {
                Text = $"{icon}  {title}",
                Font = FontH1,
                ForeColor = Color.White,
                UseMnemonic = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };
            flow.Controls.Add(lblTitle);

            if (subtitle != null)
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = SubtitleLight,
                    UseMnemonic = false,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 0)
                };
                flow.Controls.Add(lblSub);
            }

            header.Controls.Add(flow);
            return header;
        }

        /// <summary>
        /// Create a standard dialog footer bar (Dock Bottom) with right-aligned buttons.
        /// </summary>
        public static Panel CreateDialogFooter(params Button[] buttons)
        {
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Surface,
                Padding = new Padding(PadPage, 0, PadPage, 0)
            };

            var sep = HSep(DockStyle.Top);
            footer.Controls.Add(sep);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(0, 14, 0, 0),
                BackColor = Surface
            };
            foreach (var btn in buttons)
            {
                btn.Margin = new Padding(6, 0, 0, 0);
                flow.Controls.Add(btn);
            }
            footer.Controls.Add(flow);

            return footer;
        }

        // ── Empty State Overlay ───────────────────────────────────────────────

        /// <summary>
        /// Create an empty-state label overlay for grids/panels.
        /// </summary>
        public static Label CreateEmptyStateLabel(string message)
        {
            return new Label
            {
                Text = message,
                Font = FontBody,
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Surface,
                Visible = false
            };
        }

        // ── Separator ─────────────────────────────────────────────────────────
        public static Panel HSep(DockStyle dock = DockStyle.Top)
        {
            return new Panel { Dock = dock, Height = 1, BackColor = Border };
        }

        // ── Pill Badge Renderer ────────────────────────────────────────────────
        public static void DrawStatusBadge(Graphics g, Rectangle cellBounds, string status)
        {
            var (bg, fg) = status switch
            {
                "Đã nộp đủ"  => (SuccessLight, Color.FromArgb(22, 101, 52)),   // Green-800 (WCAG 5.9:1)
                "Nộp 1 phần" => (WarningLight, DebtAmber),                     // Amber-700
                "Quá hạn"    => (DangerLight, DangerDark),                     // Red-700
                "Chưa nộp"   => (BorderLight, Color.FromArgb(51, 65, 85)),     // Slate-700 (WCAG 7.2:1 AAA)
                _            => (BorderLight, TextPrimary)
            };

            var font = FontSmallBold;
            var size = TextRenderer.MeasureText(status, font);
            int badgeW = Math.Max(size.Width + 16, 82);
            int badgeH = 24;
            int badgeX = cellBounds.X + (cellBounds.Width - badgeW) / 2;
            int badgeY = cellBounds.Y + (cellBounds.Height - badgeH) / 2;
            var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new SolidBrush(bg))
            using (var path = GetRoundedPath(badgeRect, 7))
            {
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(g, status, font, badgeRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public static void DrawCustomBadge(Graphics g, Rectangle cellBounds, string text, Color bg, Color fg)
        {
            var font = FontSmallBold;
            var size = TextRenderer.MeasureText(text, font);
            int badgeW = Math.Max(size.Width + 16, 75);
            int badgeH = 24;
            int badgeX = cellBounds.X + (cellBounds.Width - badgeW) / 2;
            int badgeY = cellBounds.Y + (cellBounds.Height - badgeH) / 2;
            var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new SolidBrush(bg))
            using (var path = GetRoundedPath(badgeRect, 7))
            {
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(g, text, font, badgeRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
