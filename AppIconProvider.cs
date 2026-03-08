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

            // 优先读取外部 app.ico，避免在开发阶段拿到默认 EXE 图标
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                _cachedIcon = new Icon(iconPath);
                return _cachedIcon;
            }

            // 回退读取可执行文件关联图标
            var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon != null)
            {
                _cachedIcon = exeIcon;
                return _cachedIcon;
            }

            _cachedIcon = SystemIcons.Application;
            return _cachedIcon;
        }
    }
}
