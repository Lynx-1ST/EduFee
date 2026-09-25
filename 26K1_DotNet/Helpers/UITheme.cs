using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    /// <summary>
    /// Centralized UI Theme - Academic Ledger design system.
    /// Ink navy communicates trust; mineral gold ties the product to HUMG.
    /// </summary>
    public static class UITheme
    {
        // ── Palette ───────────────────────────────────────────────────────────
        public static readonly Color Primary       = Color.FromArgb(24, 59, 78);     // #183B4E ink blue
        public static readonly Color PrimaryDark   = Color.FromArgb(12, 38, 54);     // #0C2636
        public static readonly Color PrimaryLight  = Color.FromArgb(232, 241, 244);  // #E8F1F4

        public static readonly Color Success       = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color SuccessDark   = Color.FromArgb(5, 150, 105);
        public static readonly Color SuccessLight  = Color.FromArgb(209, 250, 229);

        public static readonly Color Warning       = Color.FromArgb(245, 158, 11);   // #F59E0B
        public static readonly Color WarningDark   = Color.FromArgb(180, 110, 0);
        public static readonly Color WarningLight  = Color.FromArgb(254, 243, 199);

        public static readonly Color Danger        = Color.FromArgb(239, 68, 68);    // #EF4444
        public static readonly Color DangerDark    = Color.FromArgb(185, 28, 28);
        public static readonly Color DangerLight   = Color.FromArgb(254, 226, 226);

        public static readonly Color Purple        = Color.FromArgb(180, 122, 35);   // #B47A23 mineral gold
        public static readonly Color PurpleDark    = Color.FromArgb(137, 86, 17);
        public static readonly Color PurpleLight   = Color.FromArgb(251, 243, 223);

        public static readonly Color Info          = Color.FromArgb(14, 165, 233);   // #0EA5E9
        public static readonly Color InfoDark      = Color.FromArgb(3, 105, 161);    // #0369A1
        public static readonly Color InfoLight     = Color.FromArgb(224, 242, 254);

        // ── Surfaces ──────────────────────────────────────────────────────────
        public static readonly Color Background    = Color.FromArgb(244, 246, 243);  // #F4F6F3 warm paper
        public static readonly Color Surface       = Color.White;
        public static readonly Color SurfaceAlt    = Color.FromArgb(248, 250, 252);  // #F8FAFC
        public static readonly Color Border        = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color BorderLight   = Color.FromArgb(241, 245, 249);
        public static readonly Color AlternatingRow = Color.FromArgb(248, 250, 247); // grid alternate row

        // ── Text ──────────────────────────────────────────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(15, 23, 42);     // #0F172A
        public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);  // #64748B
        public static readonly Color TextMuted     = Color.FromArgb(100, 116, 139);  // #64748B
        public static readonly Color TextLight     = Color.FromArgb(203, 213, 225);  // #CBD5E1 (NavButton inactive text)

        // ── Sidebar ───────────────────────────────────────────────────────────
        public static readonly Color SidebarBg          = Color.FromArgb(13, 35, 47);   // #0D232F
        public static readonly Color SidebarHover       = Color.FromArgb(24, 54, 68);   // #183644
        public static readonly Color SidebarActive      = Color.FromArgb(20, 47, 61);
        public static readonly Color SidebarAccent      = Color.FromArgb(205, 151, 57); // mineral gold
        public static readonly Color SidebarText        = Color.FromArgb(153, 177, 185);
        public static readonly Color SidebarActiveText  = Color.White;
        public static readonly Color SidebarDivider     = Color.FromArgb(36, 66, 78);
        public static readonly Color SidebarDeep        = Color.FromArgb(8, 27, 38);
        public static readonly Color SidebarCard        = Color.FromArgb(17, 43, 55);
        public static readonly Color SidebarHoverBorder = Color.FromArgb(57, 89, 99);

        // ── Accent Colors ─────────────────────────────────────────────────────
        public static readonly Color AccentLavender = Color.FromArgb(229, 198, 126);  // parchment gold
        public static readonly Color AccentIndigo   = Color.FromArgb(20, 66, 84);     // deep teal
        public static readonly Color ActiveBorder   = Color.FromArgb(205, 151, 57);   // mineral gold
        public static readonly Color ActiveDot      = Color.FromArgb(244, 220, 164);
        public static readonly Color SubtitleLight  = Color.FromArgb(205, 220, 222);

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
                "Nộp muộn"    => (WarningLight, WarningDark),                  // Amber-700
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

        // ── Phase 4 Reusable Component Builders ────────────────────────────────

        /// <summary>
        /// Create a rounded card panel with custom border and background.
        /// </summary>
        public static Panel CreateRoundedCard(int radius = RadiusMd, Color? bg = null, Color? border = null)
        {
            var p = new Panel
            {
                BackColor = bg ?? Surface,
                Padding = new Padding(PadCard)
            };
            var borderColor = border ?? Border;
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using var pen = new Pen(borderColor, 1);
                using var path = GetRoundedPath(rect, radius);
                e.Graphics.DrawPath(pen, path);
            };
            return p;
        }

        /// <summary>
        /// Create a modern Stat Card with left accent bar and typography hierarchy (Title, Value, Subtitle).
        /// </summary>
        public static Panel CreateStatCard(string title, string value, string? subtitle, Color accentColor, Color? valueColor = null, int radius = RadiusMd)
        {
            var card = new Panel
            {
                BackColor = Surface,
                Padding = new Padding(18, 14, 16, 14),
                Margin = new Padding(6)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var pen = new Pen(Border, 1))
                using (var path = GetRoundedPath(rect, radius))
                {
                    e.Graphics.DrawPath(pen, path);
                }
                using (var accentBrush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 10, 4, Math.Max(0, card.Height - 20));
                }
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = FontSmallBold,
                ForeColor = TextSecondary,
                Location = new Point(16, 12),
                AutoSize = true
            };

            var lblValue = new Label
            {
                Text = value,
                Font = FontCardValue2,
                ForeColor = valueColor ?? TextPrimary,
                Location = new Point(16, 32),
                AutoSize = true
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = TextMuted,
                    Location = new Point(16, 62),
                    AutoSize = true
                };
                card.Controls.Add(lblSub);
            }

            return card;
        }

        /// <summary>
        /// Create a search input field with integrated vector search icon, focus border and placeholder.
        /// </summary>
        public static Panel CreateSearchInput(string placeholder, out TextBox txt, int width = 260, int height = 36, Action<string>? onTextChanged = null)
        {
            var pnl = new Panel
            {
                Size = new Size(width, height),
                BackColor = Surface,
                Cursor = Cursors.IBeam
            };

            var innerTxt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = FontBody,
                ForeColor = TextPrimary,
                BackColor = Surface,
                PlaceholderText = placeholder,
                Location = new Point(34, (height - 20) / 2),
                Width = width - 42,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            if (onTextChanged != null)
            {
                innerTxt.TextChanged += (s, e) => onTextChanged(innerTxt.Text);
            }

            bool hasFocus = false;
            innerTxt.GotFocus += (s, e) => { hasFocus = true; pnl.Invalidate(); };
            innerTxt.LostFocus += (s, e) => { hasFocus = false; pnl.Invalidate(); };
            pnl.Click += (s, e) => innerTxt.Focus();

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using (var pen = new Pen(hasFocus ? Primary : Border, hasFocus ? 1.5f : 1f))
                using (var path = GetRoundedPath(rect, RadiusMd))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                var iconRect = new Rectangle(10, (pnl.Height - 16) / 2, 16, 16);
                DrawIcon(e.Graphics, IconType.Search, iconRect, hasFocus ? Primary : TextSecondary);
            };

            pnl.Controls.Add(innerTxt);
            txt = innerTxt;
            return pnl;
        }

        /// <summary>
        /// Create a clean, centered empty-state display with icon, title, description and optional action button.
        /// </summary>
        public static Panel CreateEmptyState(string title, string description, string? actionText = null, EventHandler? onAction = null)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Surface
            };

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            var iconBox = new PictureBox
            {
                Size = new Size(48, 48),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };
            iconBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, iconBox.Width, iconBox.Height);
                using var brush = new SolidBrush(PrimaryLight);
                e.Graphics.FillEllipse(brush, rect);
                var iconRect = new Rectangle(14, 14, 20, 20);
                DrawIcon(e.Graphics, IconType.Search, iconRect, Primary);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = FontH2,
                ForeColor = TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = FontBody,
                ForeColor = TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, actionText != null ? 16 : 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            flow.Controls.Add(iconBox);
            flow.Controls.Add(lblTitle);
            flow.Controls.Add(lblDesc);

            if (!string.IsNullOrEmpty(actionText) && onAction != null)
            {
                var btn = PrimaryBtn(actionText);
                btn.Click += onAction;
                flow.Controls.Add(btn);
            }

            pnl.Controls.Add(flow);

            void CenterFlow()
            {
                iconBox.Margin = new Padding(Math.Max(0, (flow.Width - iconBox.Width) / 2), 0, 0, 12);
                lblTitle.Margin = new Padding(Math.Max(0, (flow.Width - lblTitle.Width) / 2), 0, 0, 6);
                lblDesc.Margin = new Padding(Math.Max(0, (flow.Width - lblDesc.Width) / 2), 0, 0, actionText != null ? 16 : 0);
                flow.Location = new Point(Math.Max(0, (pnl.Width - flow.Width) / 2), Math.Max(0, (pnl.Height - flow.Height) / 2));
            }

            pnl.Resize += (s, e) => CenterFlow();
            flow.SizeChanged += (s, e) => CenterFlow();

            return pnl;
        }

        /// <summary>
        /// Create a section header with title and subtitle.
        /// </summary>
        public static Panel CreateSectionHeader(string title, string? subtitle = null)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = subtitle != null ? 48 : 34,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 4)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = FontH2,
                ForeColor = TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            };
            pnl.Controls.Add(lblTitle);

            if (subtitle != null)
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = FontSmall,
                    ForeColor = TextSecondary,
                    Location = new Point(0, 24),
                    AutoSize = true
                };
                pnl.Controls.Add(lblSub);
            }

            return pnl;
        }

        // ── Phase 7 Monochrome Vector Iconography ──────────────────────────────

        public enum IconType
        {
            Students,
            Tuition,
            Statistics,
            Settings,
            Search,
            Filter,
            Add,
            Edit,
            Delete,
            Receipt,
            Calendar,
            Export,
            Import,
            Refresh,
            More,
            Check,
            Warning,
            Close,
            Money,
            Pdf,
            Email,
            Database
        }

        public static void DrawIcon(Graphics g, IconType icon, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(color);

            float x = r.X, y = r.Y, w = r.Width, h = r.Height;

            switch (icon)
            {
                case IconType.Students:
                    float capMidX = x + w * 0.5f;
                    PointF[] diamond = [
                        new PointF(capMidX, y + h * 0.2f),
                        new PointF(x + w * 0.9f, y + h * 0.45f),
                        new PointF(capMidX, y + h * 0.7f),
                        new PointF(x + w * 0.1f, y + h * 0.45f)
                    ];
                    g.DrawPolygon(pen, diamond);
                    g.DrawArc(pen, x + w * 0.25f, y + h * 0.45f, w * 0.5f, h * 0.35f, 0, 180);
                    g.DrawLine(pen, x + w * 0.85f, y + h * 0.48f, x + w * 0.85f, y + h * 0.8f);
                    break;

                case IconType.Tuition:
                case IconType.Money:
                    var cardR = new RectangleF(x + w * 0.1f, y + h * 0.22f, w * 0.8f, h * 0.56f);
                    using (var cardPath = GetRoundedPath(Rectangle.Round(cardR), 3))
                    {
                        g.DrawPath(pen, cardPath);
                    }
                    g.DrawEllipse(pen, x + w * 0.38f, y + h * 0.38f, w * 0.24f, h * 0.24f);
                    break;

                case IconType.Statistics:
                    float barW = w * 0.2f;
                    float gap = w * 0.1f;
                    float startX = x + w * 0.15f;
                    g.FillRectangle(brush, startX, y + h * 0.55f, barW, h * 0.35f);
                    g.FillRectangle(brush, startX + barW + gap, y + h * 0.25f, barW, h * 0.65f);
                    g.FillRectangle(brush, startX + (barW + gap) * 2, y + h * 0.4f, barW, h * 0.5f);
                    break;

                case IconType.Settings:
                    float rMidX = x + w * 0.5f, rMidY = y + h * 0.5f;
                    float rad = w * 0.32f;
                    g.DrawEllipse(pen, rMidX - rad, rMidY - rad, rad * 2, rad * 2);
                    g.DrawEllipse(pen, rMidX - rad * 0.45f, rMidY - rad * 0.45f, rad * 0.9f, rad * 0.9f);
                    for (int i = 0; i < 4; i++)
                    {
                        double ang = i * Math.PI / 4;
                        float cos = (float)Math.Cos(ang), sin = (float)Math.Sin(ang);
                        g.DrawLine(pen, rMidX - (rad + 2.5f) * cos, rMidY - (rad + 2.5f) * sin,
                                        rMidX + (rad + 2.5f) * cos, rMidY + (rad + 2.5f) * sin);
                    }
                    break;

                case IconType.Search:
                    float cr = w * 0.30f;
                    g.DrawEllipse(pen, x + w * 0.15f, y + h * 0.15f, cr * 2, cr * 2);
                    g.DrawLine(pen, x + w * 0.15f + cr * 1.6f, y + h * 0.15f + cr * 1.6f, x + w * 0.85f, y + h * 0.85f);
                    break;

                case IconType.Filter:
                    PointF[] funnel = [
                        new PointF(x + w * 0.15f, y + h * 0.2f),
                        new PointF(x + w * 0.85f, y + h * 0.2f),
                        new PointF(x + w * 0.55f, y + h * 0.55f),
                        new PointF(x + w * 0.55f, y + h * 0.85f),
                        new PointF(x + w * 0.45f, y + h * 0.85f),
                        new PointF(x + w * 0.45f, y + h * 0.55f)
                    ];
                    g.DrawPolygon(pen, funnel);
                    break;

                case IconType.Add:
                    g.DrawLine(pen, x + w * 0.5f, y + h * 0.15f, x + w * 0.5f, y + h * 0.85f);
                    g.DrawLine(pen, x + w * 0.15f, y + h * 0.5f, x + w * 0.85f, y + h * 0.5f);
                    break;

                case IconType.Edit:
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.8f, x + w * 0.35f, y + h * 0.8f);
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.8f, x + w * 0.2f, y + h * 0.65f);
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.65f, x + w * 0.7f, y + h * 0.15f);
                    g.DrawLine(pen, x + w * 0.35f, y + h * 0.8f, x + w * 0.85f, y + h * 0.3f);
                    g.DrawLine(pen, x + w * 0.7f, y + h * 0.15f, x + w * 0.85f, y + h * 0.3f);
                    break;

                case IconType.Delete:
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.3f, x + w * 0.8f, y + h * 0.3f);
                    g.DrawLine(pen, x + w * 0.4f, y + h * 0.2f, x + w * 0.6f, y + h * 0.2f);
                    PointF[] can = [
                        new PointF(x + w * 0.28f, y + h * 0.3f),
                        new PointF(x + w * 0.32f, y + h * 0.85f),
                        new PointF(x + w * 0.68f, y + h * 0.85f),
                        new PointF(x + w * 0.72f, y + h * 0.3f)
                    ];
                    g.DrawPolygon(pen, can);
                    break;

                case IconType.Calendar:
                    var calRect = new RectangleF(x + w * 0.15f, y + h * 0.25f, w * 0.7f, h * 0.65f);
                    using (var cp = GetRoundedPath(Rectangle.Round(calRect), 2))
                    {
                        g.DrawPath(pen, cp);
                    }
                    g.DrawLine(pen, calRect.Left, y + h * 0.45f, calRect.Right, y + h * 0.45f);
                    g.DrawLine(pen, x + w * 0.35f, y + h * 0.15f, x + w * 0.35f, y + h * 0.3f);
                    g.DrawLine(pen, x + w * 0.65f, y + h * 0.15f, x + w * 0.65f, y + h * 0.3f);
                    break;

                case IconType.Export:
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.5f, x + w * 0.2f, y + h * 0.85f);
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.85f, x + w * 0.8f, y + h * 0.85f);
                    g.DrawLine(pen, x + w * 0.8f, y + h * 0.85f, x + w * 0.8f, y + h * 0.5f);
                    g.DrawLine(pen, x + w * 0.5f, y + h * 0.65f, x + w * 0.5f, y + h * 0.15f);
                    g.DrawLine(pen, x + w * 0.3f, y + h * 0.35f, x + w * 0.5f, y + h * 0.15f);
                    g.DrawLine(pen, x + w * 0.7f, y + h * 0.35f, x + w * 0.5f, y + h * 0.15f);
                    break;

                case IconType.Import:
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.5f, x + w * 0.2f, y + h * 0.85f);
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.85f, x + w * 0.8f, y + h * 0.85f);
                    g.DrawLine(pen, x + w * 0.8f, y + h * 0.85f, x + w * 0.8f, y + h * 0.5f);
                    g.DrawLine(pen, x + w * 0.5f, y + h * 0.15f, x + w * 0.5f, y + h * 0.65f);
                    g.DrawLine(pen, x + w * 0.3f, y + h * 0.45f, x + w * 0.5f, y + h * 0.65f);
                    g.DrawLine(pen, x + w * 0.7f, y + h * 0.45f, x + w * 0.5f, y + h * 0.65f);
                    break;

                case IconType.Refresh:
                    g.DrawArc(pen, x + w * 0.2f, y + h * 0.2f, w * 0.6f, h * 0.6f, 45, 270);
                    g.DrawLine(pen, x + w * 0.7f, y + h * 0.2f, x + w * 0.85f, y + h * 0.2f);
                    g.DrawLine(pen, x + w * 0.85f, y + h * 0.2f, x + w * 0.85f, y + h * 0.35f);
                    break;

                case IconType.Check:
                    g.DrawLine(pen, x + w * 0.2f, y + h * 0.5f, x + w * 0.45f, y + h * 0.75f);
                    g.DrawLine(pen, x + w * 0.45f, y + h * 0.75f, x + w * 0.85f, y + h * 0.25f);
                    break;

                case IconType.Pdf:
                    var pdfRect = new RectangleF(x + w * 0.2f, y + h * 0.15f, w * 0.6f, h * 0.75f);
                    using (var pp = GetRoundedPath(Rectangle.Round(pdfRect), 2))
                    {
                        g.DrawPath(pen, pp);
                    }
                    g.DrawLine(pen, x + w * 0.35f, y + h * 0.4f, x + w * 0.65f, y + h * 0.4f);
                    g.DrawLine(pen, x + w * 0.35f, y + h * 0.55f, x + w * 0.65f, y + h * 0.55f);
                    g.DrawLine(pen, x + w * 0.35f, y + h * 0.7f, x + w * 0.55f, y + h * 0.7f);
                    break;

                case IconType.More:
                    float dotR = w * 0.08f;
                    g.FillEllipse(brush, x + w * 0.25f - dotR, y + h * 0.5f - dotR, dotR * 2, dotR * 2);
                    g.FillEllipse(brush, x + w * 0.5f - dotR, y + h * 0.5f - dotR, dotR * 2, dotR * 2);
                    g.FillEllipse(brush, x + w * 0.75f - dotR, y + h * 0.5f - dotR, dotR * 2, dotR * 2);
                    break;

                default:
                    g.DrawEllipse(pen, x + w * 0.2f, y + h * 0.2f, w * 0.6f, h * 0.6f);
                    break;
            }
        }
    }
}
