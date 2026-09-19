using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    /// <summary>
    /// Modern SaaS-style navigation pill button with rounded corners,
    /// smooth hover highlights, and crisp active states.
    /// Uses UITheme cached fonts/colors to avoid per-paint GDI allocations.
    /// </summary>
    public class NavButton : Control
    {
        private bool _isActive;
        private bool _isHovered;
        private bool _isPressed;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Icon { get; set; } = "";

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                AccessibleName = value;
            }
        }
        private string _title = "";

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    Invalidate();
                }
            }
        }

        public NavButton(string icon, string title)
        {
            Icon = icon;
            Title = title;
            Size = new Size(216, 44);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = UITheme.SidebarBg;
            AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
        }

        public NavButton() : this("", "") { }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            var bg = Parent?.BackColor ?? UITheme.SidebarBg;
            if (bg == Color.Transparent) bg = UITheme.SidebarBg;
            using var brush = new SolidBrush(bg);
            pevent.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        /// <summary>
        /// Handle keyboard activation (Enter/Space triggers click).
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var bg = Parent?.BackColor ?? UITheme.SidebarBg;
            if (bg == Color.Transparent) bg = UITheme.SidebarBg;
            g.Clear(bg);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 8;

            // 1. Background Fill
            if (IsActive)
            {
                // Rich Indigo Pill gradient with subtle border
                using var brush = new LinearGradientBrush(rect,
                    UITheme.PrimaryDark,
                    UITheme.Primary,
                    90F);
                FillRoundedRectangle(g, brush, rect, radius);

                using var borderPen = new Pen(UITheme.ActiveBorder, 1);
                DrawRoundedRectangle(g, borderPen, rect, radius);
            }
            else if (_isHovered)
            {
                // Dark Slate hover highlight
                using var brush = new SolidBrush(UITheme.SidebarHover);
                FillRoundedRectangle(g, brush, rect, radius);

                using var borderPen = new Pen(UITheme.SidebarHoverBorder, 1);
                DrawRoundedRectangle(g, borderPen, rect, radius);
            }

            int yOffset = _isPressed ? 1 : 0;

            // 2. Icon (centered in 30px left box) — uses cached UITheme.FontEmoji
            {
                var iconColor = IsActive ? Color.White : (_isHovered ? Color.White : UITheme.SidebarText);
                var iconRect = new Rectangle(12, yOffset, 30, Height);
                TextRenderer.DrawText(g, Icon, UITheme.FontEmoji, iconRect, iconColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            // 3. Title — uses cached UITheme fonts
            {
                var textFont = IsActive ? UITheme.FontNavBold : UITheme.FontNavRegular;
                var textColor = IsActive ? Color.White : (_isHovered ? Color.White : UITheme.TextLight);
                var textRect = new Rectangle(46, yOffset, Width - 60, Height);
                TextRenderer.DrawText(g, Title, textFont, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // 5. Focus rectangle for keyboard navigation
            if (Focused && ShowFocusCues)
            {
                var focusRect = new Rectangle(2, 2, Width - 5, Height - 5);
                ControlPaint.DrawFocusRectangle(g, focusRect);
            }
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = UITheme.GetRoundedPath(rect, radius);
            g.FillPath(brush, path);
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using var path = UITheme.GetRoundedPath(rect, radius);
            g.DrawPath(pen, path);
        }
    }
}
