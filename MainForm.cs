using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Icon = System.Drawing.Icon;

namespace NetworkMonitor
{
    public class MainForm : Form
    {
        private System.Windows.Forms.Timer networkCheckTimer = null!;
        private System.Windows.Forms.Timer logUpdateTimer = null!;
        private Label statusLabel = null!;
        private RoundedButton startButton = null!;
        private RoundedButton stopButton = null!;
        private RoundedButton settingsButton = null!;
        private RoundedButton testButton = null!;
        private RoundedButton loginTestButton = null!;
        private NumericUpDown intervalInput = null!;
        private NotifyIcon notifyIcon = null!;
        private TextBox logTextBox = null!;
        private readonly Icon appIcon;
        private bool isMonitoring = false;
        private bool wasConnected = true;
        private CancellationTokenSource? monitoringCts = null;
        private Task? monitoringTask = null;
        private CancellationTokenSource? loginCts = null;
        private Task? loginTask = null;
        private DateTime lastAutoLoginStartTime = DateTime.MinValue;

        // 日志缓冲区，用于批量更新UI避免卡顿
        private readonly Queue<string> logBuffer = new Queue<string>();
        private readonly object logBufferLock = new object();
        private const int MaxLogLines = 1000;
        private const int LogUpdateIntervalMs = 100;
        
        // 日志文件 - 使用新的DiagnosticLogger
        private DiagnosticLogger? diagnosticLogger = null;
        private readonly NetworkConnectionService networkConnectionService;
        private readonly CampusAutoLoginService campusAutoLoginService;
        
        // 设置项
        private string loginUrl = "http://2.2.2.2";
        private string primaryDns = "www.baidu.com";
        private string secondaryDns = "baidu.com";
        private int pingTimeout = 10000;
        private bool showNotification = true;
        private bool showTrayNotification = true;
        private bool showRecoveryNotification = true;
        private bool autoStart = false; // 开机自启动程序（服务安装状态）
        private bool autoStartMonitoring = false; // 打开程序自动开启监控
        private bool lastMonitoringEnabled = false; // 上次关闭程序时是否在监控
        private bool saveTestResult = false;
        private string testResultPath = "test_results";
        private bool saveLogs = true;
        private int logRetentionDays = 30;
        private bool enableTimeRange = false;
        private TimeSpan startTime = new TimeSpan(6, 0, 0);   // 06:00
        private TimeSpan endTime = new TimeSpan(23, 0, 0);    // 23:00
        private string username = "";  // 校园网用户名
        private string password = "";  // 校园网密码
        private bool enableMonitorTimeRange = false;
        private TimeSpan monitorStartTime = new TimeSpan(0, 0, 0);
        private TimeSpan monitorEndTime = new TimeSpan(23, 59, 59);
        private bool enableAllDayDetection = false;
        private int allDayDetectionInterval = 60;
        private bool allDayAutoLogin = false;
        private string themeMode = "TechDark";
        private string loginStrategy = "OnlyWhenDisconnected";
        private int loginRetryCount = 3;
        private int loginRetryDelay = 5;
        private System.Windows.Forms.Timer? timeRangeCheckTimer = null;  // 用于检查时间段的定时器
        
        // 时间记录
        private DateTime? lastDisconnectTime = null;
        private DateTime? lastLoginAttemptTime = null;
        private Label lastDisconnectLabel = null!;
        private Label lastLoginAttemptLabel = null!;

        // 引入Windows API调整系统音量
        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        [DllImport("winmm.dll")]
        private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private uint originalVolume = 0;

        public MainForm()
        {
            appIcon = AppIconProvider.GetIcon();
            networkConnectionService = new NetworkConnectionService(
                AddLog,
                (context, ex) => diagnosticLogger?.LogNetworkErrorAsync(context, ex) ?? Task.CompletedTask,
                (message) => diagnosticLogger?.LogAsync(LogLevel.Warning, message) ?? Task.CompletedTask);
            campusAutoLoginService = new CampusAutoLoginService(
                AddLog,
                (context, ex) => diagnosticLogger?.LogNetworkErrorAsync(context, ex) ?? Task.CompletedTask);

            LoadSettings();
            InitializeDiagnosticLogger();
            InitializeComponents();
        }

        private void LoadSettings()
        {
            try
            {
                var settings = SettingsManager.Load();
                
                loginUrl = settings.LoginUrl;
                primaryDns = settings.PrimaryDns;
                secondaryDns = settings.SecondaryDns;
                pingTimeout = settings.PingTimeout;
                showNotification = settings.ShowNotification;
                showTrayNotification = settings.ShowTrayNotification;
                showRecoveryNotification = settings.ShowRecoveryNotification;
                autoStart = StartupServiceManager.IsInstalled();
                autoStartMonitoring = settings.AutoStartMonitoring;
                lastMonitoringEnabled = settings.LastMonitoringEnabled;
                saveTestResult = settings.SaveTestResult;
                testResultPath = settings.TestResultPath;
                saveLogs = settings.SaveLogs;
                logRetentionDays = Math.Max(1, settings.LogRetentionDays);
                enableTimeRange = settings.EnableTimeRange;
                
                // 解析时间
                if (TimeSpan.TryParse(settings.StartTime, out TimeSpan parsedStartTime))
                {
                    startTime = parsedStartTime;
                }
                
                if (TimeSpan.TryParse(settings.EndTime, out TimeSpan parsedEndTime))
                {
                    endTime = parsedEndTime;
                }
                
                // 加载用户名和密码
                username = settings.Username;
                password = settings.Password;
                
                // 加载监测时间段设置
                enableMonitorTimeRange = settings.EnableMonitorTimeRange;
                if (TimeSpan.TryParse(settings.MonitorStartTime, out TimeSpan parsedMonitorStartTime))
                {
                    monitorStartTime = parsedMonitorStartTime;
                }
                if (TimeSpan.TryParse(settings.MonitorEndTime, out TimeSpan parsedMonitorEndTime))
                {
                    monitorEndTime = parsedMonitorEndTime;
                }
                enableAllDayDetection = settings.EnableAllDayDetection;
                allDayDetectionInterval = Math.Min(Math.Max(settings.AllDayDetectionInterval, 10), 3600);
                allDayAutoLogin = settings.AllDayAutoLogin;
                themeMode = string.IsNullOrWhiteSpace(settings.ThemeMode) ? "TechDark" : settings.ThemeMode;
                UiTheme.Apply(themeMode);
                
                // 加载登录策略设置
                loginStrategy = settings.LoginStrategy;
                loginRetryCount = settings.LoginRetryCount;
                loginRetryDelay = settings.LoginRetryDelay;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置失败: {ex.Message}\n将使用默认设置", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    LoginUrl = loginUrl,
                    PrimaryDns = primaryDns,
                    SecondaryDns = secondaryDns,
                    PingTimeout = pingTimeout,
                    ShowNotification = showNotification,
                    ShowTrayNotification = showTrayNotification,
                    ShowRecoveryNotification = showRecoveryNotification,
                    AutoStart = autoStart,
                    AutoStartMonitoring = autoStartMonitoring,
                    LastMonitoringEnabled = isMonitoring,
                    SaveTestResult = saveTestResult,
                    TestResultPath = testResultPath,
                    SaveLogs = saveLogs,
                    LogRetentionDays = logRetentionDays,
                    EnableTimeRange = enableTimeRange,
                    StartTime = startTime.ToString(@"hh\:mm\:ss"),
                    EndTime = endTime.ToString(@"hh\:mm\:ss"),
                    CheckInterval = (int)intervalInput.Value,
                    Username = username,
                    Password = password,
                    EnableMonitorTimeRange = enableMonitorTimeRange,
                    MonitorStartTime = monitorStartTime.ToString(@"hh\:mm\:ss"),
                    MonitorEndTime = monitorEndTime.ToString(@"hh\:mm\:ss"),
                    EnableAllDayDetection = enableAllDayDetection,
                    AllDayDetectionInterval = allDayDetectionInterval,
                    AllDayAutoLogin = allDayAutoLogin,
                    ThemeMode = themeMode,
                    LoginStrategy = loginStrategy,
                    LoginRetryCount = loginRetryCount,
                    LoginRetryDelay = loginRetryDelay
                };
                
                if (SettingsManager.Save(settings))
                {
                    SyncStartupServiceRegistration();
                    AddLog($"设置已保存到: {SettingsManager.GetSettingsFilePath()}");
                    
                    // 如果正在监控中且启用了监测时间段且当前在监测时间段内，立即进行一次网络检测
                    if (isMonitoring && enableMonitorTimeRange && IsInMonitorTimeRange())
                    {
                        AddLog("设置已更新且当前在监测时间段内，立即执行网络检测...");
                        bool isConnected = await CheckNetworkConnectionAsync();
                        
                        if (isConnected)
                        {
                            statusLabel.Text = "状态: 网络正常";
                            statusLabel.ForeColor = UiTheme.PrimaryGreen;
                            notifyIcon.Icon = appIcon;
                            notifyIcon.Text = "网络监控工具 - 网络正常";
                        }
                        else
                        {
                            statusLabel.Text = "状态: 网络断开!";
                            statusLabel.ForeColor = UiTheme.Error;
                            notifyIcon.Icon = appIcon;
                            notifyIcon.Text = "网络监控工具 - 网络断开";
                        }
                    }
                }
                else
                {
                    AddLog("保存设置失败");
                }
            }
            catch (Exception ex)
            {
                AddLog($"保存设置异常: {ex.Message}");
            }
        }

        private void InitializeDiagnosticLogger()
        {
            try
            {
                diagnosticLogger?.Dispose();
                diagnosticLogger = null;

                if (!saveLogs)
                {
                    return;
                }

                // 创建诊断日志记录器
                diagnosticLogger = new DiagnosticLogger(retentionDays: logRetentionDays);
                
                // 订阅日志事件以更新UI（使用缓冲区避免卡顿）
                diagnosticLogger.OnLogMessage += (message) =>
                {
                    // 将消息加入缓冲区，不直接更新UI
                    lock (logBufferLock)
                    {
                        logBuffer.Enqueue(message);
                    }
                };
                
                // 记录启动信息
                _ = diagnosticLogger.LogInfoAsync($"程序启动 - 日志文件: {diagnosticLogger.GetCurrentLogFilePath()}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化诊断日志系统失败: {ex.Message}", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PlayNotificationSound()
        {
            try
            {
                // 获取当前音量
                waveOutGetVolume(IntPtr.Zero, out originalVolume);
                
                // 计算30%音量 (0x0000-0xFFFF 范围)
                uint targetVolume = (uint)(0xFFFF * 0.3); // 30%
                uint newVolume = (targetVolume << 16) | targetVolume;
                
                // 设置为30%音量
                waveOutSetVolume(IntPtr.Zero, newVolume);
                
                // 播放系统通知声音
                SystemSounds.Asterisk.Play();
                
                // 延迟后恢复原始音量
                Task.Delay(500).ContinueWith(_ => 
                {
                    waveOutSetVolume(IntPtr.Zero, originalVolume);
                });
            }
            catch
            {
                // 如果调整音量失败，静默失败
            }
        }

        private void ShowBalloonTipWithSound(int timeout, string title, string text, ToolTipIcon icon)
        {
            notifyIcon.ShowBalloonTip(timeout, title, text, icon);
            PlayNotificationSound();
        }

        private void OpenCampusLoginPage()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = loginUrl,
                    UseShellExecute = true
                });
                AddLog($"已打开校园网登录页: {loginUrl}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开校园网登录页失败: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog($"打开校园网登录页失败: {ex.Message}");
            }
        }

        private void TryApplyBlankCaption()
        {
            try
            {
                int captionColor = ColorTranslator.ToWin32(Color.FromArgb(255, 255, 255));
                int textColor = ColorTranslator.ToWin32(Color.FromArgb(255, 255, 255));
                _ = DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
                _ = DwmSetWindowAttribute(this.Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            catch
            {
                // 如果系统不支持 DWM 标题栏属性，保留默认行为
            }
        }

        private void InitializeComponents()
        {
            this.Text = "网络监控工具";
            this.Size = new System.Drawing.Size(1080, 760);
            this.MinimumSize = new System.Drawing.Size(980, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = appIcon;
            this.BackColor = Color.FromArgb(255, 255, 255);
            this.HandleCreated += (_, _) => TryApplyBlankCaption();

            // 初始化系统托盘图标
            notifyIcon = new NotifyIcon
            {
                Text = "网络监控工具",
                Visible = true,
                Icon = appIcon
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // 创建托盘菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = UiTheme.BgDark;
            contextMenu.ForeColor = UiTheme.TextPrimary;
            var showMenuItem = new ToolStripMenuItem("显示主窗口");
            showMenuItem.Click += (s, e) => ShowMainWindow();
            var exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip = contextMenu;

            var rootPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 255, 255)
            };

            var sideBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            var sideHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(16, 16, 16, 16),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            var sideHeaderIcon = new PictureBox
            {
                Location = new Point(8, 0),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = appIcon.ToBitmap()
            };

            var sideHeaderText = new Label
            {
                Text = "网络监控工具",
                Location = new Point(50, 4),
                Size = new Size(160, 24),
                Font = new System.Drawing.Font("微软雅黑", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                BackColor = Color.Transparent
            };
            sideHeader.Controls.Add(sideHeaderIcon);
            sideHeader.Controls.Add(sideHeaderText);

            Label topTitle = new Label();

            RoundedButton navServerButton = new RoundedButton
            {
                Text = "监控中心",
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = UiTheme.Panel,
                ForeColor = Color.FromArgb(31, 41, 55),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                BorderRadius = 0
            };

            RoundedButton navIntegrationButton = new RoundedButton
            {
                Text = "认证登录",
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = UiTheme.BgDark,
                ForeColor = UiTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                BorderRadius = 0
            };


            settingsButton = new RoundedButton
            {
                Text = "设置",
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = UiTheme.BgDark,
                ForeColor = UiTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                BorderRadius = 0
            };
            settingsButton.Click += SettingsButton_Click;

            var navButtons = new[] { navServerButton, navIntegrationButton, settingsButton };
            RoundedButton activeNavButton = navServerButton;
            Color navNormalColor = Color.FromArgb(250, 250, 250);
            Color navActiveColor = Color.FromArgb(245, 245, 245);

            void SetActiveNav(RoundedButton selectedButton, string title)
            {
                foreach (var btn in navButtons)
                {
                    btn.BackColor = btn == selectedButton ? navActiveColor : navNormalColor;
                }
                activeNavButton = selectedButton;
                topTitle.Text = title;
            }

            foreach (var btn in navButtons)
            {
                btn.MouseEnter += (_, _) =>
                {
                    if (btn != activeNavButton)
                    {
                        btn.BackColor = navActiveColor;
                    }
                };

                btn.MouseLeave += (_, _) =>
                {
                    if (btn != activeNavButton)
                    {
                        btn.BackColor = navNormalColor;
                    }
                };
            }

            navServerButton.Click += (_, _) => SetActiveNav(navServerButton, "监控中心");
            navIntegrationButton.Click += (_, _) =>
            {
                SetActiveNav(navIntegrationButton, "认证登录");
                OpenCampusLoginPage();
            };
            settingsButton.Click += (_, _) => SetActiveNav(settingsButton, "设置");

            sideBar.Controls.Add(settingsButton);
            sideBar.Controls.Add(navIntegrationButton);
            sideBar.Controls.Add(navServerButton);
            sideBar.Controls.Add(sideHeader);

            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 255, 255)
            };

            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(255, 255, 255)
            };

            topTitle = new Label
            {
                Text = "监控中心",
                Location = new Point(16, 18),
                Size = new Size(180, 28),
                Font = new Font("微软雅黑", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            topBar.Controls.Add(topTitle);
            SetActiveNav(navServerButton, "监控中心");

            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 18),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 255, 255),
                Padding = new Padding(16, 14, 16, 16)
            };
            // 添加阴影效果
            cardPanel.Paint += CardPanel_Paint;

            var cardTitle = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.FromArgb(255, 255, 255)
            };

            statusLabel = new Label
            {
                Text = "状态: 未启动",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            lastDisconnectLabel = new Label
            {
                Text = "上次断开: 无",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.FromArgb(107, 114, 128),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            lastLoginAttemptLabel = new Label
            {
                Text = "上次登录: 无",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.FromArgb(107, 114, 128),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                AutoSize = false,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            Label intervalLabel = new Label
            {
                Text = "检查间隔(秒):",
                AutoSize = false,
                Size = new Size(100, 34),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(75, 85, 99),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            intervalInput = new NumericUpDown
            {
                Size = new Size(80, 34),
                Minimum = 1,
                Maximum = 300,
                Value = 5,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            // 应用已保存的检查间隔
            try
            {
                var settings = SettingsManager.Load();
                if (settings.CheckInterval > 0)
                {
                    intervalInput.Value = Math.Min(Math.Max(settings.CheckInterval, 1), 300);
                }
            }
            catch { /* 忽略错误，使用默认值 */ }

            startButton = new RoundedButton
            {
                Text = "启动监控",
                Size = new Size(110, 34),
                BackColor = UiTheme.PrimaryGreen,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            startButton.Click += StartButton_Click;

            stopButton = new RoundedButton
            {
                Text = "停止监控",
                Size = new Size(110, 34),
                Enabled = false,
                BackColor = UiTheme.Error,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            stopButton.Click += StopButton_Click;

            testButton = new RoundedButton
            {
                Text = "测试访问",
                Size = new Size(110, 34),
                BackColor = UiTheme.Info,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            testButton.Click += TestButton_Click;

            loginTestButton = new RoundedButton
            {
                Text = "测试登录",
                Size = new Size(110, 34),
                BackColor = UiTheme.DeepGreen,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            loginTestButton.Click += LoginTestButton_Click;

            RoundedButton systemStatusButton = new RoundedButton
            {
                Text = "系统网络状态",
                Size = new Size(130, 34),
                BackColor = UiTheme.BgDark,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            systemStatusButton.Click += SystemStatusButton_Click;

            RoundedButton abnormalProcessButton = new RoundedButton
            {
                Text = "检测异常进程",
                Size = new Size(130, 34),
                BackColor = UiTheme.Warning,
                ForeColor = UiTheme.TextPrimary,
                BorderRadius = 6
            };
            abnormalProcessButton.Click += AbnormalProcessButton_Click;

            actionPanel.Controls.Add(intervalLabel);
            actionPanel.Controls.Add(intervalInput);
            actionPanel.Controls.Add(startButton);
            actionPanel.Controls.Add(stopButton);
            actionPanel.Controls.Add(testButton);
            actionPanel.Controls.Add(loginTestButton);
            actionPanel.Controls.Add(systemStatusButton);
            actionPanel.Controls.Add(abnormalProcessButton);

            Label logLabel = new Label
            {
                Text = "运行日志",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                BackColor = Color.FromArgb(255, 255, 255)
            };

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(31, 41, 55),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9)
            };

            cardPanel.Controls.Add(logTextBox);
            cardPanel.Controls.Add(logLabel);
            cardPanel.Controls.Add(actionPanel);
            cardPanel.Controls.Add(lastLoginAttemptLabel);
            cardPanel.Controls.Add(lastDisconnectLabel);
            cardPanel.Controls.Add(statusLabel);
            cardPanel.Controls.Add(cardTitle);

            contentHost.Controls.Add(cardPanel);
            rightPanel.Controls.Add(contentHost);
            rightPanel.Controls.Add(topBar);

            rootPanel.Controls.Add(rightPanel);
            rootPanel.Controls.Add(sideBar);
            this.Controls.Add(rootPanel);

            // 初始化定时器（保留以便向后兼容）
            networkCheckTimer = new System.Windows.Forms.Timer();
            timeRangeCheckTimer = new System.Windows.Forms.Timer();

            // 初始化日志更新定时器（用于批量更新UI避免卡顿）
            logUpdateTimer = new System.Windows.Forms.Timer();
            logUpdateTimer.Interval = LogUpdateIntervalMs;
            logUpdateTimer.Tick += LogUpdateTimer_Tick;
            logUpdateTimer.Start();

            // 窗口事件
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;
            this.Load += MainForm_Load;

            AddLog("程序启动完成");
        }

        private void CardPanel_Paint(object? sender, PaintEventArgs e)
        {
            // 绘制阴影效果
            if (sender is not Panel panel) return;

            var graphics = e.Graphics;
            var bounds = panel.ClientRectangle;

            // 阴影参数
            int shadowSize = 4;
            int shadowOpacity = 25;

            // 绘制底部阴影
            using (var shadowBrush = new SolidBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)))
            {
                // 底部阴影
                graphics.FillRectangle(shadowBrush,
                    shadowSize, bounds.Height - shadowSize,
                    bounds.Width - shadowSize * 2, shadowSize);

                // 右侧阴影
                graphics.FillRectangle(shadowBrush,
                    bounds.Width - shadowSize, shadowSize,
                    shadowSize, bounds.Height - shadowSize * 2);

                // 右下角圆角阴影
                graphics.FillRectangle(shadowBrush,
                    bounds.Width - shadowSize, bounds.Height - shadowSize,
                    shadowSize, shadowSize);
            }
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (!EnsureInitialRequiredSettings())
            {
                return;
            }

            bool shouldAutoStartMonitoring = autoStartMonitoring || lastMonitoringEnabled;
            if (shouldAutoStartMonitoring)
            {
                string reason = autoStartMonitoring ? "已启用“打开程序自动开启监控”" : "恢复上次关闭前的监控状态";
                AddLog($"{reason}，自动启动监控...");
                await AutoStartMonitoring();
            }
        }

        private void SyncStartupServiceRegistration()
        {
            try
            {
                bool installed = StartupServiceManager.IsInstalled();
                if (autoStart && !installed)
                {
                    if (StartupServiceManager.Install(Application.ExecutablePath, out string installMsg))
                    {
                        AddLog(installMsg);
                    }
                    else
                    {
                        AddLog(installMsg);
                        autoStart = false;
                    }
                }
                else if (!autoStart && installed)
                {
                    if (StartupServiceManager.Uninstall(out string uninstallMsg))
                    {
                        AddLog(uninstallMsg);
                    }
                    else
                    {
                        AddLog(uninstallMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"同步开机启动服务失败: {ex.Message}");
            }
        }
        
        private void LogUpdateTimer_Tick(object? sender, EventArgs e)
        {
            // 窗口隐藏时暂停UI更新，避免不必要的性能开销
            if (!this.Visible || this.WindowState == FormWindowState.Minimized)
            {
                // 清空缓冲区，只保留文件日志
                lock (logBufferLock)
                {
                    logBuffer.Clear();
                }
                return;
            }

            List<string> messagesToProcess = new List<string>();
            lock (logBufferLock)
            {
                while (logBuffer.Count > 0 && messagesToProcess.Count < 100)
                {
                    messagesToProcess.Add(logBuffer.Dequeue());
                }
            }

            if (messagesToProcess.Count == 0)
            {
                return;
            }

            // 批量更新UI
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() =>
                {
                    BatchAppendLogMessages(messagesToProcess);
                }));
            }
            else
            {
                BatchAppendLogMessages(messagesToProcess);
            }
        }

        private void BatchAppendLogMessages(List<string> messages)
        {
            // 暂停重绘以提高性能
            logTextBox.SuspendLayout();

            try
            {
                // 批量添加消息
                StringBuilder sb = new StringBuilder();
                foreach (var message in messages)
                {
                    sb.AppendLine(message);
                }
                logTextBox.AppendText(sb.ToString());

                // 限制行数，避免内存无限增长
                int currentLines = logTextBox.Lines.Length;
                if (currentLines > MaxLogLines)
                {
                    int linesToRemove = currentLines - MaxLogLines;
                    int index = logTextBox.GetFirstCharIndexFromLine(linesToRemove);
                    logTextBox.Select(0, index);
                    logTextBox.SelectedText = "";
                }

                // 滚动到底部
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
            }
            finally
            {
                logTextBox.ResumeLayout();
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // 最小化到任务栏时，不再自动隐藏到托盘
            // 只有点击关闭按钮（X）时才会隐藏到托盘
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void ExitApplication()
        {
            PersistMonitoringStateForNextLaunch();
            isMonitoring = false;
            networkCheckTimer.Stop();
            logUpdateTimer.Stop();
            notifyIcon.Visible = false;
            diagnosticLogger?.Dispose();
            Application.Exit();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                if (showTrayNotification)
                {
                    ShowBalloonTipWithSound(1000, "网络监控工具", "程序仍在后台运行", ToolTipIcon.Info);
                }
            }
            else
            {
                PersistMonitoringStateForNextLaunch();
            }
        }

        private void PersistMonitoringStateForNextLaunch()
        {
            try
            {
                var settings = SettingsManager.Load();
                settings.LastMonitoringEnabled = isMonitoring;
                if (!SettingsManager.Save(settings))
                {
                    AddLog("保存上次监控状态失败");
                }
            }
            catch (Exception ex)
            {
                AddLog($"保存上次监控状态异常: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            // 使用新的诊断日志系统
            _ = diagnosticLogger?.LogInfoAsync(message);
        }

        private async Task AutoStartMonitoring()
        {
            isMonitoring = true;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            intervalInput.Enabled = false;

            statusLabel.Text = "状态: 监控中...";
            statusLabel.ForeColor = UiTheme.PrimaryGreen;
            
            AddLog("监控已自动启动，检查间隔: " + intervalInput.Value + "秒");
            
            // 创建新的取消令牌
            monitoringCts = new CancellationTokenSource();
            
            // 检查是否在监测时间段内并执行首次检测
            if (!enableMonitorTimeRange || IsInMonitorTimeRange())
            {
                AddLog("立即执行首次检测...");
                await CheckNetworkAndUpdateStatus();
            }
            else
            {
                AddLog("当前不在监测时间段内，等待进入时间段");
                statusLabel.Text = "状态: 监控中(时间段外)";
                statusLabel.ForeColor = UiTheme.TextSecondary;
            }
            
            // 启动后台监控任务
            monitoringTask = RunMonitoringLoopAsync(monitoringCts.Token);
        }

        private async void StartButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = true;
            lastMonitoringEnabled = true;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            intervalInput.Enabled = false;

            statusLabel.Text = "状态: 监控中...";
            statusLabel.ForeColor = UiTheme.PrimaryGreen;
            
            AddLog("监控已启动，检查间隔: " + intervalInput.Value + "秒");
            
            // 创建新的取消令牌
            monitoringCts = new CancellationTokenSource();
            
            // 检查是否在监测时间段内并执行首次检测
            if (!enableMonitorTimeRange || IsInMonitorTimeRange())
            {
                AddLog("立即执行首次检测...");
                await CheckNetworkAndUpdateStatus();
            }
            else
            {
                AddLog("当前不在监测时间段内，等待进入时间段");
                statusLabel.Text = "状态: 监控中(时间段外)";
                statusLabel.ForeColor = UiTheme.TextSecondary;
            }
            
            // 启动后台监控任务
            monitoringTask = RunMonitoringLoopAsync(monitoringCts.Token);
        }

        private async void StopButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = false;
            lastMonitoringEnabled = false;
            
            // 取消登录任务
            if (loginCts != null && !loginCts.Token.IsCancellationRequested)
            {
                AddLog("正在停止登录任务...");
                loginCts.Cancel();
                try 
                { 
                    if (loginTask != null)
                        await loginTask; 
                } 
                catch (OperationCanceledException) 
                { 
                    // 正常取消
                }
                loginCts.Dispose();
                loginCts = null;
                loginTask = null;
            }
            
            // 取消后台监控任务
            monitoringCts?.Cancel();
            
            // 等待任务完成
            if (monitoringTask != null)
            {
                try
                {
                    await monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，忽略异常
                }
            }
            
            monitoringCts?.Dispose();
            monitoringCts = null;
            monitoringTask = null;
            
            startButton.Enabled = true;
            stopButton.Enabled = false;
            intervalInput.Enabled = true;

            networkCheckTimer.Stop();
            timeRangeCheckTimer?.Stop();

            statusLabel.Text = "状态: 已停止";
            statusLabel.ForeColor = UiTheme.TextSecondary;
            
            AddLog("监控已停止");
        }

        private AppSettings BuildCurrentSettings()
        {
            return new AppSettings
            {
                LoginUrl = loginUrl,
                PrimaryDns = primaryDns,
                SecondaryDns = secondaryDns,
                PingTimeout = pingTimeout,
                Username = username,
                Password = password,
                ShowNotification = showNotification,
                ShowTrayNotification = showTrayNotification,
                ShowRecoveryNotification = showRecoveryNotification,
                AutoStart = autoStart,
                AutoStartMonitoring = autoStartMonitoring,
                LastMonitoringEnabled = isMonitoring,
                SaveTestResult = saveTestResult,
                TestResultPath = testResultPath,
                SaveLogs = saveLogs,
                LogRetentionDays = logRetentionDays,
                EnableTimeRange = enableTimeRange,
                StartTime = startTime.ToString(@"hh\:mm\:ss"),
                EndTime = endTime.ToString(@"hh\:mm\:ss"),
                CheckInterval = (int)intervalInput.Value,
                EnableMonitorTimeRange = enableMonitorTimeRange,
                MonitorStartTime = monitorStartTime.ToString(@"hh\:mm\:ss"),
                MonitorEndTime = monitorEndTime.ToString(@"hh\:mm\:ss"),
                EnableAllDayDetection = enableAllDayDetection,
                AllDayDetectionInterval = allDayDetectionInterval,
                AllDayAutoLogin = allDayAutoLogin,
                ThemeMode = themeMode,
                LoginStrategy = loginStrategy,
                LoginRetryCount = loginRetryCount,
                LoginRetryDelay = loginRetryDelay
            };
        }

        private void ApplySettingsFromForm(SettingsForm settingsForm)
        {
            loginUrl = settingsForm.LoginUrl;
            primaryDns = settingsForm.PrimaryDns;
            secondaryDns = settingsForm.SecondaryDns;
            pingTimeout = settingsForm.Timeout;
            username = settingsForm.Username;
            password = settingsForm.Password;
            showNotification = settingsForm.ShowNotification;
            showTrayNotification = settingsForm.ShowTrayNotification;
            showRecoveryNotification = settingsForm.ShowRecoveryNotification;
            autoStart = settingsForm.AutoStart;
            autoStartMonitoring = settingsForm.AutoStartMonitoring;
            saveTestResult = settingsForm.SaveTestResult;
            testResultPath = settingsForm.TestResultPath;
            saveLogs = settingsForm.SaveLogs;
            logRetentionDays = settingsForm.LogRetentionDays;
            enableTimeRange = settingsForm.EnableTimeRange;
            startTime = settingsForm.StartTime;
            endTime = settingsForm.EndTime;
            enableMonitorTimeRange = settingsForm.EnableMonitorTimeRange;
            monitorStartTime = settingsForm.MonitorStartTime;
            monitorEndTime = settingsForm.MonitorEndTime;
            enableAllDayDetection = settingsForm.EnableAllDayDetection;
            allDayDetectionInterval = settingsForm.AllDayDetectionInterval;
            allDayAutoLogin = settingsForm.AllDayAutoLogin;
            loginStrategy = settingsForm.LoginStrategy;
            loginRetryCount = settingsForm.LoginRetryCount;
            loginRetryDelay = settingsForm.LoginRetryDelay;
        }

        private bool EnsureInitialRequiredSettings()
        {
            bool firstRun = !File.Exists(SettingsManager.GetSettingsFilePath());
            bool missingRequired = string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password);
            if (!firstRun && !missingRequired)
            {
                return true;
            }

            MessageBox.Show("首次使用请先完成必要配置（账号、密码等）。", "首次配置", MessageBoxButtons.OK, MessageBoxIcon.Information);

            while (true)
            {
                using var settingsForm = new SettingsForm(BuildCurrentSettings());
                var result = settingsForm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    ApplySettingsFromForm(settingsForm);
                    InitializeDiagnosticLogger();
                    SaveSettings();
                    AddLog("首次配置完成");
                    return true;
                }

                var exitResult = MessageBox.Show("必须完成基础配置后才能使用软件，是否退出？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (exitResult == DialogResult.Yes)
                {
                    ExitApplication();
                    return false;
                }
            }
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(BuildCurrentSettings());
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                ApplySettingsFromForm(settingsForm);

                // 应用日志设置变更
                InitializeDiagnosticLogger();

                // 保存设置到文件
                SaveSettings();
                MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AddLog($"设置已更新: {loginUrl}, DNS: {primaryDns}/{secondaryDns}, 用户名: {username}, 重试{loginRetryCount}次");
            }
        }

        private async void LoginTestButton_Click(object? sender, EventArgs e)
        {
            loginTestButton.Enabled = false;
            AddLog("开始测试校园网登录...");

            try
            {
                // 使用封装的认证类
                var authenticator = new CampusNetworkAuthenticator(
                    loginUrl, 
                    username,
                    password
                );
                
                // 订阅日志事件
                authenticator.LogMessage += AddLog;
                
                // 订阅网络错误事件以记录详细诊断
                authenticator.OnNetworkError += (ex) =>
                {
                    _ = diagnosticLogger?.LogNetworkErrorAsync("测试登录时网络错误", ex);
                };
                
                // 执行认证
                var result = await authenticator.AuthenticateAsync();
                
                // 显示结果
                if (result.Success)
                {
                    AddLog("✓✓✓ 校园网登录成功！");
                    MessageBox.Show($"校园网登录成功！\n\n{result.Message}", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AddLog($"✗ 登录未成功: {result.Message}");
                    MessageBox.Show($"校园网登录未成功\n\n{result.Message}\n\n请查看 debug 文件夹中的响应文件", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                AddLog($"登录测试失败: {ex.Message}");
                MessageBox.Show($"登录测试失败: {ex.Message}\n\n{ex.GetType().Name}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loginTestButton.Enabled = true;
            }
        }

        private async void TestButton_Click(object? sender, EventArgs e)
        {
            testButton.Enabled = false;
            AddLog($"开始测试访问: {loginUrl}");

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);

                    // 发送 GET 请求
                    var response = await httpClient.GetAsync(loginUrl);
                    var content = await response.Content.ReadAsStringAsync();

                    // 检测认证状态
                    bool isAuthenticated = content.Contains("认证成功") || 
                                         content.Contains("您已经成功登录") ||
                                         content.Contains("disconnconfig") ||
                                         content.Contains("连接网络") ||
                                         content.Contains("您可以关闭该页面");
                    
                    if (isAuthenticated)
                    {
                        AddLog("已连接校园网 - 认证成功");
                        statusLabel.Text = "状态: 已连接校园网";
                        statusLabel.ForeColor = UiTheme.PrimaryGreen;
                    }
                    else
                    {
                        AddLog("未连接校园网 - 需要认证");
                        statusLabel.Text = "状态: 未认证";
                        statusLabel.ForeColor = UiTheme.Warning;
                    }

                    AddLog($"测试完成 - 状态码: {(int)response.StatusCode} {response.StatusCode}");
                    AddLog($"响应大小: {content.Length} 字节");

                    // 如果启用了保存功能,则保存结果
                    if (saveTestResult)
                    {
                        try
                        {
                            string testDir = testResultPath;
                            // 如果是相对路径,转为绝对路径
                            if (!Path.IsPathRooted(testDir))
                            {
                                testDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, testDir);
                            }

                            if (!Directory.Exists(testDir))
                            {
                                Directory.CreateDirectory(testDir);
                            }

                            string fileName = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                            string filePath = Path.Combine(testDir, fileName);

                            await File.WriteAllTextAsync(filePath, content);

                            AddLog($"结果已保存: {filePath}");
                            MessageBox.Show($"测试完成！\n\n结果已保存到: {filePath}", "测试结果",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception saveEx)
                        {
                            AddLog($"保存文件失败: {saveEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"测试失败: {ex.Message}");
                MessageBox.Show($"访问失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                testButton.Enabled = true;
            }
        }

        
        private async Task RunMonitoringLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool outsideMonitorRangeBeforeDelay = enableMonitorTimeRange && !IsInMonitorTimeRange();
                    int intervalMs = (int)intervalInput.Value * 1000;
                    if (outsideMonitorRangeBeforeDelay && enableAllDayDetection)
                    {
                        intervalMs = allDayDetectionInterval * 1000;
                    }

                    // 等待指定间隔
                    await Task.Delay(intervalMs, cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    // 检查是否在监测时间段内
                    if (enableMonitorTimeRange && !IsInMonitorTimeRange())
                    {
                        if (enableAllDayDetection)
                        {
                            if (statusLabel.Text == "状态: 监控中(时间段外)")
                            {
                                AddLog($"时间段外全天检测中，间隔: {allDayDetectionInterval}秒");
                            }

                            await CheckNetworkAndUpdateStatus(allDayAutoLogin);
                            continue;
                        }

                        // 如果之前在时间段内，现在离开了
                        if (statusLabel.Text != "状态: 监控中(时间段外)")
                        {
                            AddLog("离开监测时间段，暂停网络检测");
                            statusLabel.BeginInvoke(new Action(() => 
                            {
                                statusLabel.Text = "状态: 监控中(时间段外)";
                                statusLabel.ForeColor = UiTheme.TextSecondary;
                            }));
                        }
                        continue;
                    }
                    else if (enableMonitorTimeRange && statusLabel.Text == "状态: 监控中(时间段外)")
                    {
                        // 刚进入时间段
                        AddLog("进入监测时间段，恢复网络检测");
                    }
                    
                    // 执行网络检测
                    await CheckNetworkAndUpdateStatus();
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                    break;
                }
                catch (Exception ex)
                {
                    AddLog($"监控循环异常: {ex.Message}");
                    // 继续运行，不要因为单次错误而停止监控
                }
            }
        }
        
        private async Task CheckNetworkAndUpdateStatus(bool allowAutoLogin = true)
        {
            bool isConnected = await CheckNetworkConnectionAsync();

            if (isConnected)
            {
                statusLabel.BeginInvoke(new Action(() => 
                {
                    statusLabel.Text = "状态: 网络正常";
                    statusLabel.ForeColor = UiTheme.PrimaryGreen;
                }));
                notifyIcon.Icon = appIcon;
                notifyIcon.Text = "网络监控工具 - 网络正常";
                
                // 网络恢复
                if (!wasConnected)
                {
                    if (showTrayNotification && showRecoveryNotification)
                    {
                        ShowBalloonTipWithSound(3000, "网络恢复", "网络连接已恢复正常", ToolTipIcon.Info);
                    }
                    AddLog("网络已恢复正常");
                }
                wasConnected = true;
            }
            else
            {
                statusLabel.BeginInvoke(new Action(() => 
                {
                    statusLabel.Text = "状态: 网络断开!";
                    statusLabel.ForeColor = UiTheme.Error;
                }));
                notifyIcon.Icon = appIcon;
                notifyIcon.Text = "网络监控工具 - 网络断开";

                // 网络断开处理
                bool justDisconnected = wasConnected;
                wasConnected = false;
                
                if (justDisconnected)
                {
                    // 记录断开时间（仅在刚断开时记录）
                    lastDisconnectTime = DateTime.Now;
                    UpdateTimeLabels();
                    
                    AddLog("检测到网络断开");
                    
                    if (showTrayNotification)
                    {
                        ShowBalloonTipWithSound(3000, "网络断开", "检测到网络断开，正在尝试自动登录...", ToolTipIcon.Warning);
                    }
                }
                
                // 检查是否在允许的时间段内，并尝试自动登录
                if (allowAutoLogin && IsInAllowedTimeRange())
                {
                    var blockingWarnings = NetworkDiagnostics.DetectAbnormalProcesses()
                        .Where(w => w.WarningLevel == "Critical" ||
                                    ((w.WarningLevel == "High" || w.WarningLevel == "Critical") &&
                                     (string.Equals(w.ProcessName, "WeChat", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(w.ProcessName, "cloudmusic", StringComparison.OrdinalIgnoreCase))))
                        .ToList();
                    if (blockingWarnings.Any())
                    {
                        var top = blockingWarnings.First();
                        AddLog($"检测到关键资源占用进程: {top.ProcessName} (PID:{top.ProcessId}, 句柄:{top.HandleCount:N0})，暂缓自动登录");
                        if (showTrayNotification)
                        {
                            ShowBalloonTipWithSound(4000, "资源占用过高", 
                                $"{top.ProcessName} 占用较高，建议先关闭后再认证", 
                                ToolTipIcon.Warning);
                        }
                        return;
                    }

                    // 自动登录校园网（每次检测到断开都尝试）
                    _ = AutoLoginAsync(); // 异步执行，不阻塞
                }
                else
                {
                    if (justDisconnected)
                    {
                        AddLog("当前不在允许的连接时间段内，跳过自动连接");
                    }
                }
            }
        }


        private bool IsInAllowedTimeRange()
        {
            // 如果未启用时间段限制,始终返回true
            if (!enableTimeRange)
            {
                return true;
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            // 处理跨夜情况 (例如: 22:00 - 06:00)
            if (startTime > endTime)
            {
                return currentTime >= startTime || currentTime <= endTime;
            }
            else
            {
                return currentTime >= startTime && currentTime <= endTime;
            }
        }
        
        private bool IsInMonitorTimeRange()
        {
            // 如果未启用监测时间段限制,始终返回true
            if (!enableMonitorTimeRange)
            {
                return true;
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            // 处理跨夜情况
            if (monitorStartTime > monitorEndTime)
            {
                return currentTime >= monitorStartTime || currentTime <= monitorEndTime;
            }
            else
            {
                return currentTime >= monitorStartTime && currentTime <= monitorEndTime;
            }
        }

        private async void AbnormalProcessButton_Click(object? sender, EventArgs e)
        {
            AddLog("\n===== 开始检测异常进程 =====");
            
            try
            {
                var warnings = NetworkDiagnostics.DetectAbnormalProcesses();
                
                if (!warnings.Any())
                {
                    AddLog("✓ 未检测到异常进程");
                    MessageBox.Show("系统中所有进程的资源使用都在正常范围内。", 
                        "检测结果", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                    return;
                }
                
                AddLog($"\n检测到 {warnings.Count} 个异常进程:");
                foreach (var warning in warnings)
                {
                    string levelIcon = warning.WarningLevel switch
                    {
                        "Critical" => "🔴",
                        "High" => "🟠",
                        "Medium" => "🟡",
                        _ => "ℹ️"
                    };
                    
                    AddLog($"  {levelIcon} [{warning.WarningLevel}] {warning.ProcessName} (PID: {warning.ProcessId})");
                    AddLog($"      句柄数: {warning.HandleCount:N0}, 内存: {warning.WorkingSet / 1024 / 1024:N0} MB");
                    AddLog($"      {warning.Description}");
                }
                
                // 显示最严重的警告
                var mostCritical = warnings.First();
                string message = $"检测到 {warnings.Count} 个异常进程！\n\n" +
                               $"最严重的问题：\n" +
                               $"进程: {mostCritical.ProcessName} (PID: {mostCritical.ProcessId})\n" +
                               $"级别: {mostCritical.WarningLevel}\n" +
                               $"句柄数: {mostCritical.HandleCount:N0}\n" +
                               $"内存: {mostCritical.WorkingSet / 1024 / 1024:N0} MB\n\n" +
                               $"{mostCritical.Description}\n\n" +
                               $"建议：\n" +
                               string.Join("\n", mostCritical.Suggestions.Select(s => $"  • {s}"));
                
                var result = MessageBox.Show(message + "\n\n是否立即关闭此进程？", 
                    "异常进程警告", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    await KillProcessAsync(mostCritical);
                }
            }
            catch (Exception ex)
            {
                AddLog($"检测异常进程失败: {ex.Message}");
                if (diagnosticLogger != null)
                {
                    await diagnosticLogger.LogErrorAsync("检测异常进程失败", ex);
                }
            }
            
            AddLog("===== 检测完成 =====");
        }
        
        private async Task KillProcessAsync(NetworkDiagnostics.AbnormalProcessWarning warning)
        {
            try
            {
                AddLog($"\n正在强制关闭进程: {warning.ProcessName} (PID: {warning.ProcessId})...");
                
                var process = Process.GetProcessById(warning.ProcessId);
                process.Kill(true); // true = 强制结束包括子进程和后台服务
                process.WaitForExit(5000); // 等待最多5秒
                
                AddLog($"✓ 进程 {warning.ProcessName} 已成功关闭（包括所有后台服务）");
                if (diagnosticLogger != null)
                {
                    await diagnosticLogger.LogInfoAsync($"用户手动关闭异常进程: {warning.ProcessName} (句柄数: {warning.HandleCount:N0})");
                }
                
                // 等待一下让系统释放资源
                await Task.Delay(1000);
                
                // 再次检测是否还有其他异常进程
                var remainingWarnings = NetworkDiagnostics.DetectAbnormalProcesses();
                
                string successMsg = $"进程 {warning.ProcessName} 已成功关闭。\n\n";
                
                if (remainingWarnings.Any())
                {
                    successMsg += $"⚠️ 仍有 {remainingWarnings.Count} 个异常进程需要关注。\n\n";
                }
                
                successMsg += "建议：\n" +
                            "1. 测试网络是否恢复正常\n" +
                            "2. 再次点击\"检测异常进程\"验证\n" +
                            "3. 如果问题仍存在，请重启计算机";
                
                MessageBox.Show(successMsg, 
                    "操作成功", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
            }
            catch (ArgumentException)
            {
                // 进程已经不存在
                AddLog($"ℹ️ 进程 {warning.ProcessName} 已经不存在");
                MessageBox.Show($"进程 {warning.ProcessName} 已经结束。", 
                    "信息", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"✗ 关闭进程失败: {ex.Message}");
                if (diagnosticLogger != null)
                {
                    await diagnosticLogger.LogErrorAsync($"关闭异常进程失败: {warning.ProcessName}", ex);
                }
                
                string errorMsg = $"无法关闭进程: {ex.Message}\n\n" +
                                "请尝试：\n" +
                                "1. 使用任务管理器手动结束（Ctrl+Shift+Esc）\n" +
                                "2. 以管理员身份运行此程序\n" +
                                "3. 重启计算机以完全清除";
                
                MessageBox.Show(errorMsg, 
                    "错误", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
        
        private async void SystemStatusButton_Click(object? sender, EventArgs e)
        {
            AddLog("\n===== 开始系统网络诊断 =====");
            
            try
            {
                // 生成完整的诊断报告
                var diagnosticReport = await NetworkDiagnostics.GenerateFullReportAsync();
                using var statusForm = new SystemStatusResultForm(diagnosticReport);
                statusForm.ShowDialog(this);
                
                // 保存详细诊断报告到文件
                if (diagnosticLogger != null)
                {
                    await diagnosticLogger.LogDiagnosticAsync("手动触发的系统网络诊断", diagnosticReport);
                }
                
                AddLog("===== 诊断完成 =====");
            }
            catch (Exception ex)
            {
                AddLog($"诊断失败: {ex.Message}");
                if (diagnosticLogger != null)
                    await diagnosticLogger.LogErrorAsync("系统网络诊断失败", ex);
            }
        }

        /* 以下代码已被新诊断系统替代，保留作为参考
        private void SystemStatusButton_Click_Old(object? sender, EventArgs e)
        {
            AddLog("\n===== 系统网络状态检测 =====");
            
            try
            {
                // 获取所有网络接口
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                
                bool hasActiveConnection = false;
                
                foreach (NetworkInterface adapter in adapters)
                {
                    // 跳过非活动和环回接口
                    if (adapter.OperationalStatus != OperationalStatus.Up || 
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    
                    hasActiveConnection = true;
                    
                    AddLog($"\n网络适配器: {adapter.Name}");
                    AddLog($"  描述: {adapter.Description}");
                    AddLog($"  类型: {adapter.NetworkInterfaceType}");
                    AddLog($"  状态: {adapter.OperationalStatus}");
                    AddLog($"  速度: {adapter.Speed / 1000000} Mbps");
                    
                    // 获取IP配置
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();
                    
                    // 显示IPv4地址
                    var ipv4Addresses = ipProperties.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .ToList();
                    
                    if (ipv4Addresses.Any())
                    {
                        foreach (var addr in ipv4Addresses)
                        {
                            AddLog($"  IPv4地址: {addr.Address}");
                            AddLog($"  子网掩码: {addr.IPv4Mask}");
                        }
                    }
                    
                    // 显示IPv6地址
                    var ipv6Addresses = ipProperties.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        .ToList();
                    
                    if (ipv6Addresses.Any())
                    {
                        foreach (var addr in ipv6Addresses)
                        {
                            AddLog($"  IPv6地址: {addr.Address}");
                        }
                    }
                    
                    // 显示网关信息
                    var gateways = ipProperties.GatewayAddresses;
                    if (gateways.Any())
                    {
                        foreach (var gateway in gateways)
                        {
                            AddLog($"  网关: {gateway.Address}");
                        }
                    }
                    
                    // 显示DNS服务器
                    var dnsServers = ipProperties.DnsAddresses;
                    if (dnsServers.Any())
                    {
                        foreach (var dns in dnsServers)
                        {
                            AddLog($"  DNS服务器: {dns}");
                        }
                    }
                    
                    // 显示DHCP服务器
                    if (ipProperties.DhcpServerAddresses.Any())
                    {
                        foreach (var dhcp in ipProperties.DhcpServerAddresses)
                        {
                            AddLog($"  DHCP服务器: {dhcp}");
                        }
                    }
                    
                    // 获取统计信息
                    IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                    AddLog($"  接收字节数: {stats.BytesReceived:N0}");
                    AddLog($"  发送字节数: {stats.BytesSent:N0}");
                    AddLog($"  接收包数: {stats.UnicastPacketsReceived:N0}");
                    AddLog($"  发送包数: {stats.UnicastPacketsSent:N0}");
                    AddLog($"  丢弃包数: {stats.IncomingPacketsDiscarded + stats.OutgoingPacketsDiscarded:N0}");
                    AddLog($"  错误包数: {stats.IncomingPacketsWithErrors + stats.OutgoingPacketsWithErrors:N0}");
                }
                
                if (!hasActiveConnection)
                {
                    AddLog("\n没有检测到活动的网络连接！");
                }
                
                // 测试网络连通性
                AddLog("\n===== 网络连通性测试 =====");
                
                // 检查是否可以解析DNS
                try
                {
                    AddLog("正在测试DNS解析...");
                    IPAddress[] addresses = Dns.GetHostAddresses("www.baidu.com");
                    AddLog($"DNS解析成功: www.baidu.com => {string.Join(", ", addresses.Select(a => a.ToString()))}");
                }
                catch (Exception ex)
                {
                    AddLog($"DNS解析失败: {ex.Message}");
                }
                
                // 检查Internet连接状态（使用Windows API）
                AddLog($"\nInternet连接状态: {(NetworkInterface.GetIsNetworkAvailable() ? "已连接" : "未连接")}");
                
            AddLog("\n===== 检测完成 =====");
            }
            catch (Exception ex)
            {
                AddLog($"获取系统网络状态失败: {ex.Message}");
            }
        }
        */

        private async Task<bool> CheckNetworkConnectionAsync()
        {
            var options = new NetworkCheckOptions
            {
                LoginUrl = loginUrl,
                PrimaryDns = primaryDns,
                SecondaryDns = secondaryDns,
                PingTimeout = pingTimeout
            };

            var result = await networkConnectionService.CheckConnectionAsync(options);
            if (result.GatewayUnreachable)
            {
                statusLabel.BeginInvoke(new Action(() =>
                {
                    statusLabel.Text = "状态: 网络故障";
                    statusLabel.ForeColor = UiTheme.Error;
                }));
            }

            return result.IsConnected;
        }

        private Task AutoLoginAsync()
        {
            // 避免在断网期间频繁重启登录任务，造成连接和句柄风暴
            if (loginTask != null && !loginTask.IsCompleted)
            {
                AddLog("登录任务正在运行中，跳过本次重复触发");
                return Task.CompletedTask;
            }

            int cooldownSeconds = Math.Max(20, loginRetryDelay);
            if (lastAutoLoginStartTime != DateTime.MinValue &&
                DateTime.Now - lastAutoLoginStartTime < TimeSpan.FromSeconds(cooldownSeconds))
            {
                var remain = cooldownSeconds - (int)(DateTime.Now - lastAutoLoginStartTime).TotalSeconds;
                AddLog($"自动登录冷却中，{Math.Max(remain, 1)}秒后再尝试");
                return Task.CompletedTask;
            }
            
            // 创建新的取消令牌
            loginCts?.Dispose();
            loginCts = new CancellationTokenSource();
            var token = loginCts.Token;
            lastAutoLoginStartTime = DateTime.Now;
            
            // 在新任务中执行登录
            loginTask = Task.Run(async () => 
            {
                // 记录登录尝试时间
                lastLoginAttemptTime = DateTime.Now;
                UpdateTimeLabels();

                var loginOptions = new AutoLoginOptions
                {
                    LoginUrl = loginUrl,
                    Username = username,
                    Password = password,
                    RetryCount = loginRetryCount,
                    RetryDelaySeconds = loginRetryDelay
                };

                var result = await campusAutoLoginService.AuthenticateWithRetryAsync(
                    loginOptions,
                    token,
                    () => isMonitoring);

                if (result.Canceled)
                {
                    return;
                }

                if (result.Success)
                {
                    if (showTrayNotification)
                    {
                        ShowBalloonTipWithSound(3000, "登录成功",
                            $"校园网自动登录成功！\n尝试{result.AttemptCount}次",
                            ToolTipIcon.Info);
                    }
                }
                else
                {
                    AddLog($"\n✗ 自动登录最终失败（已尝试{result.AttemptCount}次）");
                    if (showTrayNotification)
                    {
                        ShowBalloonTipWithSound(5000, "登录失败",
                            $"校园网自动登录失败\n尝试{result.AttemptCount}次后仍然失败\n{result.LastErrorMessage}",
                            ToolTipIcon.Warning);
                    }
                }
            }, token);

            return Task.CompletedTask;
        }

        private void UpdateTimeLabels()
        {
            if (lastDisconnectLabel.InvokeRequired)
            {
                lastDisconnectLabel.Invoke(new Action(() => UpdateTimeLabels()));
                return;
            }
            
            // 更新断开时间
            if (lastDisconnectTime.HasValue)
            {
                lastDisconnectLabel.Text = $"上次断开: {lastDisconnectTime.Value:HH:mm:ss}";
            }
            else
            {
                lastDisconnectLabel.Text = "上次断开: 无";
            }
            
            // 更新登录时间
            if (lastLoginAttemptTime.HasValue)
            {
                lastLoginAttemptLabel.Text = $"上次登录: {lastLoginAttemptTime.Value:HH:mm:ss}";
            }
            else
            {
                lastLoginAttemptLabel.Text = "上次登录: 无";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                networkCheckTimer?.Dispose();
                logUpdateTimer?.Dispose();
                notifyIcon?.Dispose();
                diagnosticLogger?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
