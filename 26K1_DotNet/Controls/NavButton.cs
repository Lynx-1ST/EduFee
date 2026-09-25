using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    /// <summary>
    /// Academic Ledger navigation with a quiet ink surface and mineral-gold marker.
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
        public UITheme.IconType? VectorIcon { get; set; }

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

        public NavButton(UITheme.IconType icon, string title) : this("", title)
        {
            VectorIcon = icon;
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

            if (IsActive)
            {
                using var brush = new SolidBrush(UITheme.SidebarActive);
                FillRoundedRectangle(g, brush, rect, radius);

                using var markerBrush = new SolidBrush(UITheme.SidebarAccent);
                using var markerPath = UITheme.GetRoundedPath(new Rectangle(0, 8, 4, Height - 16), 2);
                g.FillPath(markerBrush, markerPath);
            }
            else if (_isHovered)
            {
                using var brush = new SolidBrush(UITheme.SidebarHover);
                FillRoundedRectangle(g, brush, rect, radius);

                using var borderPen = new Pen(UITheme.SidebarHoverBorder, 1);
                DrawRoundedRectangle(g, borderPen, rect, radius);
            }

            int yOffset = _isPressed ? 1 : 0;

            var iconColor = IsActive ? UITheme.ActiveDot : (_isHovered ? Color.White : UITheme.SidebarText);
            if (VectorIcon.HasValue)
            {
                var iconRect = new Rectangle(14, (Height - 18) / 2 + yOffset, 18, 18);
                UITheme.DrawIcon(g, VectorIcon.Value, iconRect, iconColor);
            }
            else if (!string.IsNullOrEmpty(Icon))
            {
                var iconRect = new Rectangle(12, yOffset, 30, Height);
                TextRenderer.DrawText(g, Icon, UITheme.FontEmoji, iconRect, iconColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            {
                var textFont = IsActive ? UITheme.FontNavBold : UITheme.FontNavRegular;
                var textColor = IsActive ? Color.White : (_isHovered ? Color.White : UITheme.TextLight);
                var textRect = new Rectangle(46, yOffset, Width - 60, Height);
                TextRenderer.DrawText(g, Title, textFont, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

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
