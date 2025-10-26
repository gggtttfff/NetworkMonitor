using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkMonitor
{
    public class MainForm : Form
    {
        private Timer networkCheckTimer = null!;
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
        private bool hasOpenedBrowser = false;
        
        // 日志文件
        private string logFilePath = "";
        private StreamWriter? logFileWriter = null;
        
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

        public MainForm()
        {
            LoadSettings();
            InitializeLogFile();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置失败: {ex.Message}\n将使用默认设置", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveSettings()
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
                    Password = password
                };
                
                if (SettingsManager.Save(settings))
                {
                    AddLog($"设置已保存到: {SettingsManager.GetSettingsFilePath()}");
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

        private void InitializeLogFile()
        {
            try
            {
                // 创建日志目录
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 按日期创建日志文件
                string logFileName = $"network_monitor_{DateTime.Now:yyyyMMdd}.log";
                logFilePath = Path.Combine(logDir, logFileName);

                // 打开文件流 (追加模式)
                logFileWriter = new StreamWriter(logFilePath, append: true);
                logFileWriter.AutoFlush = true; // 自动刷新

                // 写入启动日志
                logFileWriter.WriteLine($"\n========== 程序启动 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化日志文件失败: {ex.Message}", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                intervalLabel,
                intervalInput,
                startButton,
                stopButton,
                settingsButton,
                testButton,
                loginTestButton,
                systemStatusButton,
                logLabel,
                logTextBox
            });

            // 初始化定时器
            networkCheckTimer = new Timer();
            networkCheckTimer.Tick += NetworkCheckTimer_Tick;

            // 窗口事件
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;

            AddLog("程序启动完成");
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.ShowBalloonTip(1000, "网络监控工具", "程序已最小化到系统托盘", ToolTipIcon.Info);
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
            logFileWriter?.Close();
            Application.Exit();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                notifyIcon.ShowBalloonTip(1000, "网络监控工具", "程序仍在后台运行", ToolTipIcon.Info);
            }
        }

        private void AddLog(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AddLog(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string fullTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{timestamp}] {message}";
            string fileLogMessage = $"[{fullTimestamp}] {message}";

            // 显示到界面
            logTextBox.AppendText($"{logMessage}\r\n");
            
            // 自动滚动到最后
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();

            // 写入文件
            try
            {
                logFileWriter?.WriteLine(fileLogMessage);
            }
            catch
            {
                // 静默失败,不影响主功能
            }
        }

        private void StartButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = true;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            intervalInput.Enabled = false;

            int interval = (int)intervalInput.Value * 1000;
            networkCheckTimer.Interval = interval;
            networkCheckTimer.Start();

            statusLabel.Text = "状态: 监控中...";
            statusLabel.ForeColor = System.Drawing.Color.Green;
            
            // 重置标志
            hasOpenedBrowser = false;
            
            AddLog("监控已启动，检查间隔: " + intervalInput.Value + "秒");
        }

        private void StopButton_Click(object? sender, EventArgs e)
        {
            isMonitoring = false;
            startButton.Enabled = true;
            stopButton.Enabled = false;
            intervalInput.Enabled = true;

            networkCheckTimer.Stop();

            statusLabel.Text = "状态: 已停止";
            statusLabel.ForeColor = System.Drawing.Color.Gray;
            
            AddLog("监控已停止");
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(loginUrl, primaryDns, secondaryDns, pingTimeout, showNotification, showTrayNotification, showRecoveryNotification, autoStart, saveTestResult, testResultPath, enableTimeRange, startTime, endTime);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                loginUrl = settingsForm.LoginUrl;
                primaryDns = settingsForm.PrimaryDns;
                secondaryDns = settingsForm.SecondaryDns;
                pingTimeout = settingsForm.Timeout;
                showNotification = settingsForm.ShowNotification;
                showTrayNotification = settingsForm.ShowTrayNotification;
                showRecoveryNotification = settingsForm.ShowRecoveryNotification;
                autoStart = settingsForm.AutoStart;
                saveTestResult = settingsForm.SaveTestResult;
                testResultPath = settingsForm.TestResultPath;
                enableTimeRange = settingsForm.EnableTimeRange;
                startTime = settingsForm.StartTime;
                endTime = settingsForm.EndTime;

                // 保存设置到文件
                SaveSettings();
                
                MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AddLog($"设置已更新: {loginUrl}, DNS: {primaryDns}/{secondaryDns}");
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

                            // 询问是否打开文件
                            var result = MessageBox.Show("测试完成！\n\n是否打开结果文件？", "测试结果",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                            if (result == DialogResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = filePath,
                                    UseShellExecute = true
                                });
                            }
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

        private async void NetworkCheckTimer_Tick(object? sender, EventArgs e)
        {
            // 禁用定时器避免重复执行
            networkCheckTimer.Stop();

            bool isConnected = await CheckNetworkConnectionAsync();

            if (isConnected)
            {
                statusLabel.Text = "状态: 网络正常";
                statusLabel.ForeColor = System.Drawing.Color.Green;
                notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                notifyIcon.Text = "网络监控工具 - 网络正常";
                
                // 网络恢复后重置标志
                if (!wasConnected)
                {
                    hasOpenedBrowser = false;
                    if (showTrayNotification && showRecoveryNotification)
                    {
                        notifyIcon.ShowBalloonTip(3000, "网络恢复", "网络连接已恢复正常", ToolTipIcon.Info);
                    }
                    AddLog("网络已恢复正常");
                }
                wasConnected = true;
            }
            else
            {
                statusLabel.Text = "状态: 网络断开!";
                statusLabel.ForeColor = System.Drawing.Color.Red;
                notifyIcon.Icon = System.Drawing.SystemIcons.Warning;
                notifyIcon.Text = "网络监控工具 - 网络断开";

                // 只在网络从连接变为断开时自动登录一次
                if (wasConnected && !hasOpenedBrowser)
                {
                    AddLog("检测到网络断开");
                    
                    // 检查是否在允许的时间段内
                    if (IsInAllowedTimeRange())
                    {
                        // 自动登录校园网
                        _ = AutoLoginAsync(); // 异步执行，不阻塞
                        hasOpenedBrowser = true;
                        if (showTrayNotification)
                        {
                            notifyIcon.ShowBalloonTip(3000, "网络断开", "检测到网络断开，正在尝试自动登录...", ToolTipIcon.Warning);
                        }
                    }
                    else
                    {
                        AddLog("当前不在允许的连接时间段内，跳过自动连接");
                        hasOpenedBrowser = true; // 设置为true以避免重复提示
                    }
                }
                wasConnected = false;
            }

            // 重新启动定时器
            if (isMonitoring)
            {
                networkCheckTimer.Start();
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

        private void SystemStatusButton_Click(object? sender, EventArgs e)
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

        private async Task<bool> CheckNetworkConnectionAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    // 先ping主DNS
                    AddLog($"Ping {primaryDns}...");
                    PingReply reply = await ping.SendPingAsync(primaryDns, pingTimeout);
                    
                    if (reply.Status == IPStatus.Success)
                    {
                        AddLog($"Ping {primaryDns} 成功, 延迟: {reply.RoundtripTime}ms");
                        return true;
                    }
                    
                    AddLog($"Ping {primaryDns} 失败: {reply.Status}");
                    
                    // 如果主DNS失败，试试备用DNS
                    AddLog($"Ping {secondaryDns}...");
                    reply = await ping.SendPingAsync(secondaryDns, pingTimeout);
                    
                    if (reply.Status == IPStatus.Success)
                    {
                        AddLog($"Ping {secondaryDns} 成功, 延迟: {reply.RoundtripTime}ms");
                        return true;
                    }
                    
                    AddLog($"Ping {secondaryDns} 失败: {reply.Status}");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddLog($"网络检测异常: {ex.Message}");
                return false;
            }
        }

        private async Task AutoLoginAsync()
        {
            try
            {
                AddLog("开始自动登录校园网...");
                
                // 使用封装的认证类
                var authenticator = new CampusNetworkAuthenticator(
                    loginUrl, 
                    username,
                    password
                );
                
                // 订阅日志事件
                authenticator.LogMessage += AddLog;
                
                // 执行认证
                var result = await authenticator.AuthenticateAsync();
                
                // 显示结果
                if (result.Success)
                {
                    AddLog("✓ 自动登录成功！");
                    
                    if (showNotification)
                    {
                        notifyIcon.ShowBalloonTip(3000, "登录成功", "校园网自动登录成功！", ToolTipIcon.Info);
                    }
                    
                    // 登录成功后重置标志，允许下次断开时再次尝试
                    hasOpenedBrowser = false;
                }
                else
                {
                    AddLog($"✗ 自动登录失败: {result.Message}");
                    
                    if (showNotification)
                    {
                        notifyIcon.ShowBalloonTip(5000, "登录失败", 
                            $"校园网自动登录失败\n{result.Message}", 
                            ToolTipIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"自动登录异常: {ex.Message}");
                
                if (showNotification)
                {
                    notifyIcon.ShowBalloonTip(5000, "登录错误", 
                        $"自动登录发生错误\n{ex.Message}", 
                        ToolTipIcon.Error);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                networkCheckTimer?.Dispose();
                notifyIcon?.Dispose();
                logFileWriter?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}