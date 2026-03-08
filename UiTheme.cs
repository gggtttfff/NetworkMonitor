using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NetworkMonitor
{
    internal static class UiTheme
    {
        private sealed class Palette
        {
            public Color PrimaryGreen { get; init; }
            public Color DeepGreen { get; init; }
            public Color Excellent { get; init; }
            public Color Warning { get; init; }
            public Color Error { get; init; }
            public Color Info { get; init; }
            public Color BgDarkest { get; init; }
            public Color BgDark { get; init; }
            public Color Panel { get; init; }
            public Color Border { get; init; }
            public Color TextSecondary { get; init; }
            public Color TextPrimary { get; init; }
        }

        private static Palette _current = CreateTechDark();
        public static string CurrentMode { get; private set; } = "TechDark";

        public static Color PrimaryGreen => _current.PrimaryGreen;
        public static Color DeepGreen => _current.DeepGreen;
        public static Color Excellent => _current.Excellent;
        public static Color Warning => _current.Warning;
        public static Color Error => _current.Error;
        public static Color Info => _current.Info;
        public static Color BgDarkest => _current.BgDarkest;
        public static Color BgDark => _current.BgDark;
        public static Color Panel => _current.Panel;
        public static Color Border => _current.Border;
        public static Color TextSecondary => _current.TextSecondary;
        public static Color TextPrimary => _current.TextPrimary;

        public static string[] Modes => new[] { "TechDark", "MintLight" };

        public static void Apply(string? mode)
        {
            switch (mode)
            {
                case "MintLight":
                    _current = CreateMintLight();
                    CurrentMode = "MintLight";
                    break;
                case "TechDark":
                default:
                    _current = CreateTechDark();
                    CurrentMode = "TechDark";
                    break;
            }
        }

        private static Palette CreateTechDark()
        {
            return new Palette
            {
                PrimaryGreen = FromHex("#10B981"),
                DeepGreen = FromHex("#059669"),
                Excellent = FromHex("#22C55E"),
                Warning = FromHex("#F59E0B"),
                Error = FromHex("#F87171"),
                Info = FromHex("#3B82F6"),
                BgDarkest = FromHex("#111827"),
                BgDark = FromHex("#1F2937"),
                Panel = FromHex("#273444"),
                Border = FromHex("#374151"),
                TextSecondary = FromHex("#9CA3AF"),
                TextPrimary = FromHex("#F9FAFB")
            };
        }

        private static Palette CreateMintLight()
        {
            return new Palette
            {
                PrimaryGreen = FromHex("#0F9D66"),
                DeepGreen = FromHex("#0A7A52"),
                Excellent = FromHex("#22C55E"),
                Warning = FromHex("#F59E0B"),
                Error = FromHex("#F87171"),
                Info = FromHex("#76B5E4"),
                BgDarkest = FromHex("#EEF6F1"),
                BgDark = FromHex("#F8FAFC"),
                Panel = FromHex("#FFFFFF"),
                Border = FromHex("#D1D5DB"),
                TextSecondary = FromHex("#475569"),
                TextPrimary = FromHex("#1E293B")
            };
        }

        private static Color FromHex(string hex)
        {
            return ColorTranslator.FromHtml(hex);
        }
    }

    /// <summary>
    /// 支持圆角和禁用状态的按钮控件
    /// </summary>
    public class RoundedButton : Button
    {
        private int _borderRadius = 6;
        private Color _normalBackColor;
        private Color _normalForeColor;
        private Color _hoverBackColor;
        private Color _disabledBackColor = Color.FromArgb(229, 231, 235);
        private Color _disabledForeColor = Color.FromArgb(156, 163, 175);
        private bool _isHovered = false;

        /// <summary>
        /// 圆角半径
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        /// <summary>
        /// 禁用时的背景色
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DisabledBackColor
        {
            get => _disabledBackColor;
            set
            {
                _disabledBackColor = value;
                if (!Enabled) Invalidate();
            }
        }

        /// <summary>
        /// 禁用时的前景色（文字颜色）
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color DisabledForeColor
        {
            get => _disabledForeColor;
            set
            {
                _disabledForeColor = value;
                if (!Enabled) Invalidate();
            }
        }

        /// <summary>
        /// 鼠标悬停时的背景色
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set => _hoverBackColor = value;
        }

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = UiTheme.PrimaryGreen;
            ForeColor = UiTheme.TextPrimary;
            _normalBackColor = BackColor;
            _normalForeColor = ForeColor;
            _hoverBackColor = ControlPaint.Light(_normalBackColor, 0.1f);

            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            _normalBackColor = BackColor;
            _normalForeColor = ForeColor;
            _hoverBackColor = ControlPaint.Light(_normalBackColor, 0.1f);
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

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 清除背景
            pevent.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

            // 创建圆角路径
            using (GraphicsPath path = GetRoundedPath(ClientRectangle, _borderRadius))
            {
                // 确定当前颜色
                Color currentBackColor = Enabled
                    ? (_isHovered ? _hoverBackColor : _normalBackColor)
                    : _disabledBackColor;

                Color currentForeColor = Enabled ? _normalForeColor : _disabledForeColor;

                // 填充背景
                using (SolidBrush brush = new SolidBrush(currentBackColor))
                {
                    pevent.Graphics.FillPath(brush, path);
                }

                // 绘制文字
                TextRenderer.DrawText(
                    pevent.Graphics,
                    Text,
                    Font,
                    ClientRectangle,
                    currentForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// 设置按钮的颜色主题
        /// </summary>
        /// <param name="backColor">正常背景色</param>
        /// <param name="foreColor">正常前景色</param>
        public void SetTheme(Color backColor, Color foreColor)
        {
            _normalBackColor = backColor;
            _normalForeColor = foreColor;
            _hoverBackColor = ControlPaint.Light(backColor, 0.1f);
            BackColor = backColor;
            ForeColor = foreColor;
            Invalidate();
        }
    }
}
