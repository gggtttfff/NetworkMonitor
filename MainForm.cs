using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkMonitor
{
    public class MainForm : Form
    {
        // Static HttpClient to prevent socket exhaustion
        private static readonly HttpClient sharedHttpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        
        private System.Windows.Forms.Timer networkCheckTimer = null!;
        private Label statusLabel = null!;
        private Button startButton = null!;
        private Button stopButton = null!;
        private Button settingsButton = null!;
        private Button testButton = null!;
        private Button loginTestButton = null!;
        private NumericUpDown intervalInput = null!;
        private NotifyIcon notifyIcon = null!;
        private TextBox logTextBox = null!;
        private bool isMonitoring = false;
        private bool wasConnected = true;
        private CancellationTokenSource? monitoringCts = null;
        private Task? monitoringTask = null;
        private CancellationTokenSource? loginCts = null;
        private Task? loginTask = null;
        
        // 日志文件 - 使用新的DiagnosticLogger
        private DiagnosticLogger? diagnosticLogger = null;
        
        // 设置项
        private string loginUrl = "http://2.2.2.2";
        private string primaryDns = "www.baidu.com";
        private string secondaryDns = "baidu.com";
        private int pingTimeout = 10000;
        private bool showNotification = true;
        private bool showTrayNotification = true;
        private bool showRecoveryNotification = true;
        private bool autoStart = false;
        private bool saveTestResult = false;
        private string testResultPath = "test_results";
        private bool enableTimeRange = false;
        private TimeSpan startTime = new TimeSpan(6, 0, 0);   // 06:00
        private TimeSpan endTime = new TimeSpan(23, 0, 0);    // 23:00
        private string username = "23325024026";  // 校园网用户名
        private string password = "17881936070";  // 校园网密码
        private bool enableMonitorTimeRange = false;
        private TimeSpan monitorStartTime = new TimeSpan(0, 0, 0);
        private TimeSpan monitorEndTime = new TimeSpan(23, 59, 59);
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

        private uint originalVolume = 0;

        public MainForm()
        {
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
                autoStart = settings.AutoStart;
                saveTestResult = settings.SaveTestResult;
                testResultPath = settings.TestResultPath;
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
                    SaveTestResult = saveTestResult,
                    TestResultPath = testResultPath,
                    EnableTimeRange = enableTimeRange,
                    StartTime = startTime.ToString(@"hh\:mm\:ss"),
                    EndTime = endTime.ToString(@"hh\:mm\:ss"),
                    CheckInterval = (int)intervalInput.Value,
                    Username = username,
                    Password = password,
                    EnableMonitorTimeRange = enableMonitorTimeRange,
                    MonitorStartTime = monitorStartTime.ToString(@"hh\:mm\:ss"),
                    MonitorEndTime = monitorEndTime.ToString(@"hh\:mm\:ss"),
                    LoginStrategy = loginStrategy,
                    LoginRetryCount = loginRetryCount,
                    LoginRetryDelay = loginRetryDelay
                };
                
                if (SettingsManager.Save(settings))
                {
                    AddLog($"设置已保存到: {SettingsManager.GetSettingsFilePath()}");
                    
                    // 如果正在监控中且启用了监测时间段且当前在监测时间段内，立即进行一次网络检测
                    if (isMonitoring && enableMonitorTimeRange && IsInMonitorTimeRange())
                    {
                        AddLog("设置已更新且当前在监测时间段内，立即执行网络检测...");
                        bool isConnected = await CheckNetworkConnectionAsync();
                        
                        if (isConnected)
                        {
                            statusLabel.Text = "状态: 网络正常";
                            statusLabel.ForeColor = System.Drawing.Color.Green;
                            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                            notifyIcon.Text = "网络监控工具 - 网络正常";
                        }
                        else
                        {
                            statusLabel.Text = "状态: 网络断开!";
                            statusLabel.ForeColor = System.Drawing.Color.Red;
                            notifyIcon.Icon = System.Drawing.SystemIcons.Warning;
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
                // 创建诊断日志记录器
                diagnosticLogger = new DiagnosticLogger();
                
                // 订阅日志事件以更新UI
                diagnosticLogger.OnLogMessage += (message) =>
                {
                    if (logTextBox.InvokeRequired)
                    {
                        logTextBox.Invoke(new Action(() =>
                        {
                            logTextBox.AppendText($"{message}\r\n");
                            logTextBox.SelectionStart = logTextBox.Text.Length;
                            logTextBox.ScrollToCaret();
                        }));
                    }
                    else
                    {
                        logTextBox.AppendText($"{message}\r\n");
                        logTextBox.SelectionStart = logTextBox.Text.Length;
                        logTextBox.ScrollToCaret();
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

        private void InitializeComponents()
        {
            this.Text = "网络监控工具";
            this.Size = new System.Drawing.Size(620, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 初始化系统托盘图标
            notifyIcon = new NotifyIcon
            {
                Text = "网络监控工具",
                Visible = true,
                Icon = System.Drawing.SystemIcons.Application
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // 创建托盘菜单
            var contextMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("显示主窗口");
            showMenuItem.Click += (s, e) => ShowMainWindow();
            var exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip = contextMenu;

            Label titleLabel = new Label
            {
                Text = "网络状态监控",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(350, 25),
                Font = new System.Drawing.Font("微软雅黑", 12, System.Drawing.FontStyle.Bold)
            };

            statusLabel = new Label
            {
                Text = "状态: 未启动",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(350, 30),
                Font = new System.Drawing.Font("微软雅黑", 10)
            };
            
            // 上次断开时间
            lastDisconnectLabel = new Label
            {
                Text = "上次断开: 无",
                Location = new System.Drawing.Point(380, 60),
                Size = new System.Drawing.Size(220, 20),
                Font = new System.Drawing.Font("微软雅黑", 8.5f)
            };
            
            // 上次登录时间
            lastLoginAttemptLabel = new Label
            {
                Text = "上次登录: 无",
                Location = new System.Drawing.Point(380, 80),
                Size = new System.Drawing.Size(220, 20),
                Font = new System.Drawing.Font("微软雅黑", 8.5f)
            };

            Label intervalLabel = new Label
            {
                Text = "检查间隔(秒):",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(100, 25)
            };

            intervalInput = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 300,
                Value = 5
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

            startButton = new Button
            {
                Text = "启动监控",
                Location = new System.Drawing.Point(20, 150),
                Size = new System.Drawing.Size(100, 35)
            };
            startButton.Click += StartButton_Click;

            stopButton = new Button
            {
                Text = "停止监控",
                Location = new System.Drawing.Point(140, 150),
                Size = new System.Drawing.Size(100, 35),
                Enabled = false
            };
            stopButton.Click += StopButton_Click;

            settingsButton = new Button
            {
                Text = "设置",
                Location = new System.Drawing.Point(260, 150),
                Size = new System.Drawing.Size(100, 35)
            };
            settingsButton.Click += SettingsButton_Click;

            testButton = new Button
            {
                Text = "测试访问",
                Location = new System.Drawing.Point(380, 150),
                Size = new System.Drawing.Size(100, 35)
            };
            testButton.Click += TestButton_Click;

            loginTestButton = new Button
            {
                Text = "测试登录",
                Location = new System.Drawing.Point(500, 150),
                Size = new System.Drawing.Size(100, 35)
            };
            loginTestButton.Click += LoginTestButton_Click;

            // 系统网络状态按钮
            Button systemStatusButton = new Button
            {
                Text = "系统网络状态",
                Location = new System.Drawing.Point(380, 195),
                Size = new System.Drawing.Size(110, 30),
                Font = new System.Drawing.Font("微软雅黑", 9)
            };
            systemStatusButton.Click += SystemStatusButton_Click;
            
            // 异常进程检测按钮
            Button abnormalProcessButton = new Button
            {
                Text = "检测异常进程",
                Location = new System.Drawing.Point(500, 195),
                Size = new System.Drawing.Size(110, 30),
                Font = new System.Drawing.Font("微软雅黑", 9)
            };
            abnormalProcessButton.Click += AbnormalProcessButton_Click;

            // 日志显示区域
            Label logLabel = new Label
            {
                Text = "运行日志:",
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(100, 25),
                Font = new System.Drawing.Font("微软雅黑", 10, System.Drawing.FontStyle.Bold)
            };

            logTextBox = new TextBox
            {
                Location = new System.Drawing.Point(20, 230),
                Size = new System.Drawing.Size(540, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("微软雅黑", 9)
            };

            this.Controls.AddRange(new Control[] {
                titleLabel,
                statusLabel,
                lastDisconnectLabel,
                lastLoginAttemptLabel,
                intervalLabel,
                intervalInput,
                startButton,
                stopButton,
                settingsButton,
                testButton,
                loginTestButton,
                systemStatusButton,
                abnormalProcessButton,
                logLabel,
                logTextBox
            });

            // 初始化定时器（保留以便向后兼容）
            networkCheckTimer = new System.Windows.Forms.Timer();
            timeRangeCheckTimer = new System.Windows.Forms.Timer();

            // 窗口事件
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;
            this.Load += MainForm_Load;

            AddLog("程序启动完成");
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            // 如果启用了监测时间段且当前在时间段内，自动启动监控
            if (enableMonitorTimeRange && IsInMonitorTimeRange())
            {
                AddLog("当前在监测时间段内，自动启动监控...");
                await AutoStartMonitoring();
            }
        }
        
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                if (showTrayNotification)
                {
                    ShowBalloonTipWithSound(1000, "网络监控工具", "程序已最小化到系统托盘", ToolTipIcon.Info);
                }
            }
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
        }

        private void ExitApplication()
        {
            isMonitoring = false;
            networkCheckTimer.Stop();
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
            statusLabel.ForeColor = System.Drawing.Color.Green;
            
            AddLog("监控已自动启动，检查间隔: " + intervalInput.Value + "秒");
            
            // 创建新的取消令牌
            monitoringCts = new CancellationTokenSource();
            
            // 立即进行一次网络检测
            AddLog("立即执行首次检测...");
            await CheckNetworkAndUpdateStatus();
            
            // 启动后台监控任务
            monitoringTask = RunMonitoringLoopAsync(monitoringCts.Token);
        }

        private async void StartButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = true;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            intervalInput.Enabled = false;

            statusLabel.Text = "状态: 监控中...";
            statusLabel.ForeColor = System.Drawing.Color.Green;
            
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
                statusLabel.ForeColor = System.Drawing.Color.Gray;
            }
            
            // 启动后台监控任务
            monitoringTask = RunMonitoringLoopAsync(monitoringCts.Token);
        }

        private async void StopButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = false;
            
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
            statusLabel.ForeColor = System.Drawing.Color.Gray;
            
            AddLog("监控已停止");
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            // 使用新的构造函数传递AppSettings对象
            var currentSettings = new AppSettings
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
                SaveTestResult = saveTestResult,
                TestResultPath = testResultPath,
                EnableTimeRange = enableTimeRange,
                StartTime = startTime.ToString(@"hh\:mm\:ss"),
                EndTime = endTime.ToString(@"hh\:mm\:ss"),
                CheckInterval = (int)intervalInput.Value,
                EnableMonitorTimeRange = enableMonitorTimeRange,
                MonitorStartTime = monitorStartTime.ToString(@"hh\:mm\:ss"),
                MonitorEndTime = monitorEndTime.ToString(@"hh\:mm\:ss"),
                LoginStrategy = loginStrategy,
                LoginRetryCount = loginRetryCount,
                LoginRetryDelay = loginRetryDelay
            };
            
            var settingsForm = new SettingsForm(currentSettings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
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
                saveTestResult = settingsForm.SaveTestResult;
                testResultPath = settingsForm.TestResultPath;
                enableTimeRange = settingsForm.EnableTimeRange;
                startTime = settingsForm.StartTime;
                endTime = settingsForm.EndTime;
                enableMonitorTimeRange = settingsForm.EnableMonitorTimeRange;
                monitorStartTime = settingsForm.MonitorStartTime;
                monitorEndTime = settingsForm.MonitorEndTime;
                loginStrategy = settingsForm.LoginStrategy;
                loginRetryCount = settingsForm.LoginRetryCount;
                loginRetryDelay = settingsForm.LoginRetryDelay;

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
                        statusLabel.ForeColor = System.Drawing.Color.Green;
                    }
                    else
                    {
                        AddLog("未连接校园网 - 需要认证");
                        statusLabel.Text = "状态: 未认证";
                        statusLabel.ForeColor = System.Drawing.Color.Orange;
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
            int interval = (int)intervalInput.Value * 1000;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 等待指定间隔
                    await Task.Delay(interval, cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    // 检查是否在监测时间段内
                    if (enableMonitorTimeRange && !IsInMonitorTimeRange())
                    {
                        // 如果之前在时间段内，现在离开了
                        if (statusLabel.Text != "状态: 监控中(时间段外)")
                        {
                            AddLog("离开监测时间段，暂停网络检测");
                            statusLabel.BeginInvoke(new Action(() => 
                            {
                                statusLabel.Text = "状态: 监控中(时间段外)";
                                statusLabel.ForeColor = System.Drawing.Color.Gray;
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
        
        private async Task CheckNetworkAndUpdateStatus()
        {
            bool isConnected = await CheckNetworkConnectionAsync();

            if (isConnected)
            {
                statusLabel.BeginInvoke(new Action(() => 
                {
                    statusLabel.Text = "状态: 网络正常";
                    statusLabel.ForeColor = System.Drawing.Color.Green;
                }));
                notifyIcon.Icon = System.Drawing.SystemIcons.Application;
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
                    statusLabel.ForeColor = System.Drawing.Color.Red;
                }));
                notifyIcon.Icon = System.Drawing.SystemIcons.Warning;
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
                if (IsInAllowedTimeRange())
                {
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
                await diagnosticLogger?.LogErrorAsync("检测异常进程失败", ex)!;
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
                await diagnosticLogger?.LogInfoAsync($"用户手动关闭异常进程: {warning.ProcessName} (句柄数: {warning.HandleCount:N0})")!;
                
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
                await diagnosticLogger?.LogErrorAsync($"关闭异常进程失败: {warning.ProcessName}", ex)!;
                
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
                
                // 格式化报告
                string reportText = NetworkDiagnostics.FormatReport(diagnosticReport);
                
                // 显示到日志
                foreach (var line in reportText.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        AddLog(line.TrimEnd('\r'));
                    }
                }
                
                // 检查是否有异常进程警告
                if (diagnosticReport.ProcessWarnings.Any())
                {
                    var criticalWarnings = diagnosticReport.ProcessWarnings
                        .Where(w => w.WarningLevel == "Critical")
                        .ToList();
                    
                    if (criticalWarnings.Any())
                    {
                        var warning = criticalWarnings.First();
                        string message = $"检测到异常进程！\n\n" +
                                       $"进程: {warning.ProcessName} (PID: {warning.ProcessId})\n" +
                                       $"句柄数: {warning.HandleCount:N0}\n" +
                                       $"内存: {warning.WorkingSet / 1024 / 1024:N0} MB\n\n" +
                                       $"{warning.Description}\n\n" +
                                       $"建议:\n" +
                                       string.Join("\n", warning.Suggestions.Select(s => $"  • {s}"));
                        
                        var result = MessageBox.Show(message + "\n\n是否立即关闭此进程？", 
                            "⚠️ 异常进程警告", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Warning);
                        
                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                AddLog($"正在强制关闭进程: {warning.ProcessName} (PID: {warning.ProcessId})...");
                                
                                var process = Process.GetProcessById(warning.ProcessId);
                                process.Kill(true); // true = 强制结束包括子进程
                                process.WaitForExit(5000); // 等待最多5秒
                                
                                AddLog($"✓ 进程 {warning.ProcessName} 已成功关闭");
                                await diagnosticLogger?.LogInfoAsync($"用户手动关闭异常进程: {warning.ProcessName} (句柄数: {warning.HandleCount:N0})")!;
                                
                                MessageBox.Show($"进程 {warning.ProcessName} 已成功关闭。\n\n建议：\n1. 测试网络是否恢复正常\n2. 如果问题仍存在，请重启计算机", 
                                    "操作成功", 
                                    MessageBoxButtons.OK, 
                                    MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                AddLog($"✗ 关闭进程失败: {ex.Message}");
                                await diagnosticLogger?.LogErrorAsync($"关闭异常进程失败: {warning.ProcessName}", ex)!;
                                
                                MessageBox.Show($"无法关闭进程: {ex.Message}\n\n请尝试：\n1. 使用任务管理器手动结束\n2. 以管理员身份运行此程序\n3. 重启计算机", 
                                    "错误", 
                                    MessageBoxButtons.OK, 
                                    MessageBoxIcon.Error);
                            }
                        }
                        
                        if (showTrayNotification)
                        {
                            ShowBalloonTipWithSound(5000, "异常进程警告", 
                                $"{warning.ProcessName} 占用了 {warning.HandleCount:N0} 个句柄，可能导致网络问题", 
                                ToolTipIcon.Warning);
                        }
                    }
                }
                
                // 保存详细诊断报告到文件
                await diagnosticLogger?.LogDiagnosticAsync("手动触发的系统网络诊断", diagnosticReport)!;
                
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
            try
            {
                // 先尝试ping测试
                using (var ping = new Ping())
                {
                    try
                    {
                        AddLog("检查网络连接...");
                        
                        // 尝试ping百度
                        if (!string.IsNullOrEmpty(primaryDns))
                        {
                            AddLog($"Ping {primaryDns}...");
                            PingReply reply = await ping.SendPingAsync(primaryDns, pingTimeout);
                            if (reply.Status == IPStatus.Success)
                            {
                                // 检测异常的0ms延迟 - 这通常意味着DNS缓存响应，实际没有真正联网
                                if (reply.RoundtripTime == 0)
                                {
                                    AddLog($"⚠️ Ping {primaryDns} 返回0ms延迟(异常) - IP: {reply.Address}");
                                    AddLog($"   这通常表示DNS缓存响应，网络可能未真正连接");
                                    AddLog($"   将视为网络断开，尝试重新认证");
                                    // 不返回true，继续检查其他方式
                                }
                                else
                                {
                                    AddLog($"Ping {primaryDns} 成功，延迟: {reply.RoundtripTime}ms (IP: {reply.Address})");
                                    return true;
                                }
                            }
                            else
                            {
                                AddLog($"Ping {primaryDns} 失败: {reply.Status}");
                            }
                        }
                        
                        // 尝试ping备用DNS
                        if (!string.IsNullOrEmpty(secondaryDns))
                        {
                            AddLog($"Ping {secondaryDns}...");
                            PingReply reply = await ping.SendPingAsync(secondaryDns, pingTimeout);
                            if (reply.Status == IPStatus.Success)
                            {
                                // 检测异常的0ms延迟
                                if (reply.RoundtripTime == 0)
                                {
                                    AddLog($"⚠️ Ping {secondaryDns} 返回0ms延迟(异常) - IP: {reply.Address}");
                                    AddLog($"   这通常表示DNS缓存响应，网络可能未真正连接");
                                    AddLog($"   将视为网络断开，尝试重新认证");
                                    // 不返回true，继续检查其他方式
                                }
                                else
                                {
                                    AddLog($"Ping {secondaryDns} 成功，延迟: {reply.RoundtripTime}ms (IP: {reply.Address})");
                                    return true;
                                }
                            }
                            else
                            {
                                AddLog($"Ping {secondaryDns} 失败: {reply.Status}");
                            }
                        }
                        
                        // 尝试ping公共DNS服务器 (IP地址)
                        AddLog("尝试Ping公共DNS...");
                        PingReply publicReply = await ping.SendPingAsync("223.5.5.5", 3000); // 阿里DNS
                        if (publicReply.Status == IPStatus.Success)
                        {
                            if (publicReply.RoundtripTime == 0)
                            {
                                AddLog($"⚠️ Ping 223.5.5.5 返回0ms延迟(异常)");
                                AddLog($"   IP地址也返回0ms，网络状态异常，将视为断开");
                                // 不返回true
                            }
                            else
                            {
                                AddLog($"Ping 223.5.5.5 成功，延迟: {publicReply.RoundtripTime}ms");
                                return true;
                            }
                        }
                        
                        // 尝试ping百度DNS
                        publicReply = await ping.SendPingAsync("180.76.76.76", 3000);
                        if (publicReply.Status == IPStatus.Success)
                        {
                            if (publicReply.RoundtripTime == 0)
                            {
                                AddLog($"⚠️ Ping 180.76.76.76 返回0ms延迟(异常)");
                                AddLog($"   IP地址也返回0ms，网络状态异常，将视为断开");
                                // 不返回true
                            }
                            else
                            {
                                AddLog($"Ping 180.76.76.76 成功，延迟: {publicReply.RoundtripTime}ms");
                                return true;
                            }
                        }
                    }
                    catch (PingException ex)
                    {
                        AddLog($"Ping测试异常: {ex.Message}");
                    }
                }
                
                // Ping失败，通过HTTP请求检查是否需要认证
                AddLog("Ping测试失败，检查是否需要认证...");
                
                try
                {
                    AddLog($"检查认证网关 {loginUrl}...");
                    
                    using var request = new HttpRequestMessage(HttpMethod.Get, loginUrl);
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    using var gatewayResponse = await sharedHttpClient.SendAsync(request, cts.Token);
                    var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync();
                    
                    // 检查是否已认证
                    bool isAuthenticated = gatewayContent.Contains("认证成功") || 
                                         gatewayContent.Contains("您已经成功登录") ||
                                         gatewayContent.Contains("disconnconfig") ||
                                         gatewayContent.Contains("连接网络") ||
                                         gatewayContent.Contains("您可以关闭该页面");
                    
                    if (isAuthenticated)
                    {
                        AddLog("检测到已认证页面，但网络不通");
                        return false;
                    }
                    else if (gatewayContent.Contains("login") || gatewayContent.Contains("认证") || 
                            gatewayContent.Contains("用户名") || gatewayContent.Contains("密码"))
                    {
                        AddLog("检测到认证页面，需要登录");
                        return false;
                    }
                    else
                    {
                        AddLog("无法确定认证状态");
                        return false;
                    }
                }
                catch (HttpRequestException ex)
                {
                    AddLog($"无法连接到认证网关: {ex.Message}");
                    AddLog("可能网线未插或网络故障");
                    statusLabel.Text = "状态: 网络故障";
                    statusLabel.ForeColor = System.Drawing.Color.Red;
                    
                    // 记录详细的连接错误诊断
                    _ = diagnosticLogger?.LogNetworkErrorAsync($"无法连接到认证网关 {loginUrl}", ex);
                    
                    return false;
                }
                catch (TaskCanceledException)
                {
                    AddLog("连接认证网关超时");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLog($"网络检测异常: {ex.Message}");
                
                // 记录详细的网络错误诊断
                _ = diagnosticLogger?.LogNetworkErrorAsync("网络连接检测失败", ex);
                
                // 检查是否有异常进程影响网络
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var warnings = NetworkDiagnostics.DetectAbnormalProcesses();
                        if (warnings.Any(w => w.WarningLevel == "Critical"))
                        {
                            var critical = warnings.First(w => w.WarningLevel == "Critical");
                            await diagnosticLogger?.LogAsync(LogLevel.Warning, 
                                $"检测到异常进程 {critical.ProcessName} (句柄数: {critical.HandleCount:N0})，可能影响网络连接")!;
                        }
                    }
                    catch { }
                });
                
                return false;
            }
        }

        private async Task AutoLoginAsync()
        {
            // 如果已有登录任务在运行，先取消它
            if (loginCts != null && !loginCts.Token.IsCancellationRequested)
            {
                AddLog("已有登录任务在运行，先取消之前的任务");
                loginCts.Cancel();
                try 
                { 
                    if (loginTask != null)
                        await loginTask; 
                } 
                catch { }
                loginCts.Dispose();
            }
            
            // 创建新的取消令牌
            loginCts = new CancellationTokenSource();
            var token = loginCts.Token;
            
            // 在新任务中执行登录
            loginTask = Task.Run(async () => 
            {
                // 记录登录尝试时间
                lastLoginAttemptTime = DateTime.Now;
                UpdateTimeLabels();
                
                int attemptCount = 0;
                int maxAttempts = loginRetryCount + 1; // 总尝试次数 = 重试次数 + 1
                bool success = false;
                string lastErrorMessage = "";
                
                AddLog($"开始自动登录校园网（最多尝试{maxAttempts}次）...");
                
                while (attemptCount < maxAttempts && !success && !token.IsCancellationRequested && isMonitoring)
                {
                    attemptCount++;
                    
                    if (attemptCount > 1)
                    {
                        AddLog($"\n第{attemptCount}次尝试登录...");
                    }
                    
                    try
                    {
                        // 检查是否应该继续
                        if (token.IsCancellationRequested || !isMonitoring)
                        {
                            AddLog("登录任务已取消");
                            return;
                        }
                        
                        // 使用封装的认证类
                        var authenticator = new CampusNetworkAuthenticator(
                            loginUrl, 
                            username,
                            password
                        );
                        
                        // 订阅日志事件
                        authenticator.LogMessage += AddLog;
                        
                        // 订阅网络错误事件
                        authenticator.OnNetworkError += (ex) =>
                        {
                            _ = diagnosticLogger?.LogNetworkErrorAsync("自动登录时网络错误", ex);
                        };
                        
                        // 执行认证
                        var result = await authenticator.AuthenticateAsync();
                        
                        // 检查结果
                        if (result.Success)
                        {
                            success = true;
                            AddLog($"✓ 自动登录成功！（第{attemptCount}次尝试）");
                            
                            if (showTrayNotification)
                            {
                                ShowBalloonTipWithSound(3000, "登录成功", 
                                    $"校园网自动登录成功！\n尝试{attemptCount}次", 
                                    ToolTipIcon.Info);
                            }
                        }
                        else
                        {
                            lastErrorMessage = result.Message;
                            AddLog($"✗ 第{attemptCount}次登录失败: {result.Message}");
                            
                            // 如果还有重试机会，等待一段时间
                            if (attemptCount < maxAttempts && !token.IsCancellationRequested && isMonitoring)
                            {
                                AddLog($"等待{loginRetryDelay}秒后重试...");
                                await Task.Delay(loginRetryDelay * 1000, token);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        AddLog("登录任务被取消");
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastErrorMessage = ex.Message;
                        AddLog($"✗ 第{attemptCount}次登录异常: {ex.Message}");
                        
                        // 如果还有重试机会，等待一段时间
                        if (attemptCount < maxAttempts && !token.IsCancellationRequested && isMonitoring)
                        {
                            AddLog($"等待{loginRetryDelay}秒后重试...");
                            try
                            {
                                await Task.Delay(loginRetryDelay * 1000, token);
                            }
                            catch (OperationCanceledException)
                            {
                                AddLog("登录任务被取消");
                                return;
                            }
                        }
                    }
                }
                
                // 检查是否因为取消而退出
                if (token.IsCancellationRequested || !isMonitoring)
                {
                    AddLog("登录任务已停止");
                    return;
                }
                
                // 所有尝试完成后的总结
                if (!success)
                {
                    AddLog($"\n✗ 自动登录最终失败（已尝试{attemptCount}次）");
                    
                    if (showTrayNotification)
                    {
                        ShowBalloonTipWithSound(5000, "登录失败", 
                            $"校园网自动登录失败\n尝试{attemptCount}次后仍然失败\n{lastErrorMessage}", 
                            ToolTipIcon.Warning);
                    }
                }
            }, token);
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
                notifyIcon?.Dispose();
                diagnosticLogger?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
