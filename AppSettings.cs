using System;
using System.IO;
using System.Text.Json;

namespace NetworkMonitor
{
    /// <summary>
    /// 应用程序设置数据类
    /// </summary>
    public class AppSettings
    {
        public string LoginUrl { get; set; } = "http://2.2.2.2";
        public string PrimaryDns { get; set; } = "www.baidu.com";
        public string SecondaryDns { get; set; } = "baidu.com";
        public int PingTimeout { get; set; } = 10000;
        public bool ShowNotification { get; set; } = true;
        public bool ShowTrayNotification { get; set; } = true;
        public bool ShowRecoveryNotification { get; set; } = true;
        public bool AutoStart { get; set; } = false; // 开机自启动程序（服务安装状态）
        public bool AutoStartMonitoring { get; set; } = false; // 打开程序自动开启监控
        public bool LastMonitoringEnabled { get; set; } = false; // 程序关闭时是否处于监控中
        public bool SaveTestResult { get; set; } = false;
        public string TestResultPath { get; set; } = "test_results";
        public bool EnableTimeRange { get; set; } = false;
        public string StartTime { get; set; } = "06:00:00";
        public string EndTime { get; set; } = "23:00:00";
        public int CheckInterval { get; set; } = 5; // 检查间隔（秒）
        public string Username { get; set; } = "23325024026"; // 校园网用户名
        public string Password { get; set; } = "17881936070"; // 校园网密码
        
        // 自动监测时间段设置
        public bool EnableMonitorTimeRange { get; set; } = false; // 启用监测时间段
        public string MonitorStartTime { get; set; } = "00:00:00"; // 监测开始时间
        public string MonitorEndTime { get; set; } = "23:59:59"; // 监测结束时间
        public bool EnableAllDayDetection { get; set; } = false; // 监控时间外全天检测开关
        public int AllDayDetectionInterval { get; set; } = 60; // 监控时间外检测间隔（秒）
        public bool AllDayAutoLogin { get; set; } = false; // 监控时间外是否自动登录
        public string ThemeMode { get; set; } = "TechDark"; // 主题模式：TechDark, MintLight
        
        // 登录请求策略
        public string LoginStrategy { get; set; } = "OnlyWhenDisconnected"; // 登录策略：OnlyWhenDisconnected, AlwaysTry, Smart
        public int LoginRetryCount { get; set; } = 3; // 登录失败重试次数
        public int LoginRetryDelay { get; set; } = 5; // 重试间隔（秒）
    }

    /// <summary>
    /// 设置持久化管理器
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "appsettings.json"
        );

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // 格式化输出
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 使用驼峰命名
        };

        /// <summary>
        /// 加载设置
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                // 加载失败时记录错误但不中断程序
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
            }

            // 返回默认设置
            return new AppSettings();
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public static bool Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取设置文件路径
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return SettingsFilePath;
        }
    }
}
