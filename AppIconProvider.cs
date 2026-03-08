using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NetworkMonitor
{
    internal static class AppIconProvider
    {
        private static Icon? _cachedIcon;

        public static Icon GetIcon()
        {
            if (_cachedIcon != null)
            {
                return _cachedIcon;
            }

            // 优先读取可执行文件自身图标，发布后最稳定
            var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon != null)
            {
                _cachedIcon = exeIcon;
                return _cachedIcon;
            }

            // 回退到外部 app.ico
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                _cachedIcon = new Icon(iconPath);
            }
            else
            {
                _cachedIcon = SystemIcons.Application;
            }

            return _cachedIcon;
        }
    }
}
