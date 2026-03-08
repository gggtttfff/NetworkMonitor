using System;
using Microsoft.Win32;

namespace NetworkMonitor
{
    public static class StartupServiceManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ServiceValueName = "NetworkMonitor";

        public static bool IsInstalled()
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var value = runKey?.GetValue(ServiceValueName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        public static bool Install(string executablePath, out string message)
        {
            try
            {
                using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
                if (runKey == null)
                {
                    message = "无法打开启动项注册表键";
                    return false;
                }

                string command = $"\"{executablePath}\"";
                runKey.SetValue(ServiceValueName, command, RegistryValueKind.String);
                message = "服务安装成功（已注册开机启动）";
                return true;
            }
            catch (Exception ex)
            {
                message = $"服务安装失败: {ex.Message}";
                return false;
            }
        }

        public static bool Uninstall(out string message)
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (runKey == null)
                {
                    message = "服务卸载成功（启动项不存在）";
                    return true;
                }

                runKey.DeleteValue(ServiceValueName, false);
                message = "服务卸载成功（已移除开机启动）";
                return true;
            }
            catch (Exception ex)
            {
                message = $"服务卸载失败: {ex.Message}";
                return false;
            }
        }
    }
}
