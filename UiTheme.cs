using System.Drawing;

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
}
