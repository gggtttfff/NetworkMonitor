using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace NetworkMonitor
{
    public class SettingsForm : Form
    {
        private TextBox loginUrlTextBox = null!;
        private TextBox primaryDnsTextBox = null!;
        private TextBox secondaryDnsTextBox = null!;
        private NumericUpDown timeoutInput = null!;
        private TextBox usernameTextBox = null!;
        private TextBox passwordTextBox = null!;
        private CheckBox showNotificationCheckBox = null!;
        private CheckBox showTrayNotificationCheckBox = null!;
        private CheckBox showRecoveryNotificationCheckBox = null!;
        private CheckBox autoStartCheckBox = null!;
        private CheckBox autoStartMonitoringCheckBox = null!;
        private CheckBox saveTestResultCheckBox = null!;
        private TextBox testResultPathTextBox = null!;
        private Button browseButton = null!;
        private Button installServiceButton = null!;
        private Button uninstallServiceButton = null!;
        private Label serviceStatusLabel = null!;
        private CheckBox enableTimeRangeCheckBox = null!;
        private DateTimePicker startTimePicker = null!;
        private DateTimePicker endTimePicker = null!;
        private CheckBox enableMonitorTimeRangeCheckBox = null!;
        private DateTimePicker monitorStartTimePicker = null!;
        private DateTimePicker monitorEndTimePicker = null!;
        private CheckBox enableAllDayDetectionCheckBox = null!;
        private NumericUpDown allDayDetectionIntervalInput = null!;
        private CheckBox allDayAutoLoginCheckBox = null!;
        private ComboBox loginStrategyComboBox = null!;
        private NumericUpDown retryCountInput = null!;
        private NumericUpDown retryDelayInput = null!;
        private ComboBox themeModeComboBox = null!;
        private Button saveButton = null!;
        private Button cancelButton = null!;

        public string LoginUrl { get; private set; } = "http://2.2.2.2";
        public string PrimaryDns { get; private set; } = "8.8.8.8";
        public string SecondaryDns { get; private set; } = "114.114.114.114";
        public int Timeout { get; private set; } = 10000;
        public string Username { get; private set; } = "23325024026";
        public string Password { get; private set; } = "17881936070";
        public bool ShowNotification { get; private set; } = true;
        public bool ShowTrayNotification { get; private set; } = true;
        public bool ShowRecoveryNotification { get; private set; } = true;
        public bool AutoStart { get; private set; } = false;
        public bool AutoStartMonitoring { get; private set; } = false;
        public bool SaveTestResult { get; private set; } = false;
        public string TestResultPath { get; private set; } = "test_results";
        public bool EnableTimeRange { get; private set; } = false;
        public TimeSpan StartTime { get; private set; } = new TimeSpan(6, 0, 0);  // 06:00
        public TimeSpan EndTime { get; private set; } = new TimeSpan(23, 0, 0);  // 23:00
        public bool EnableMonitorTimeRange { get; private set; } = false;
        public TimeSpan MonitorStartTime { get; private set; } = new TimeSpan(0, 0, 0);  // 00:00
        public TimeSpan MonitorEndTime { get; private set; } = new TimeSpan(23, 59, 59);  // 23:59:59
        public bool EnableAllDayDetection { get; private set; } = false;
        public int AllDayDetectionInterval { get; private set; } = 60;
        public bool AllDayAutoLogin { get; private set; } = false;
        public string ThemeMode { get; private set; } = "TechDark";
        public string LoginStrategy { get; private set; } = "OnlyWhenDisconnected";
        public int LoginRetryCount { get; private set; } = 3;
        public int LoginRetryDelay { get; private set; } = 5;

        public SettingsForm()
        {
            InitializeComponents();
        }

        // 新的构造函数：接收AppSettings对象
        public SettingsForm(AppSettings settings) : this()
        {
            LoadFromSettings(settings);
        }
        
        private void LoadFromSettings(AppSettings settings)
        {
            LoginUrl = settings.LoginUrl;
            PrimaryDns = settings.PrimaryDns;
            SecondaryDns = settings.SecondaryDns;
            Timeout = settings.PingTimeout;
            Username = settings.Username;
            Password = settings.Password;
            ShowNotification = settings.ShowNotification;
            ShowTrayNotification = settings.ShowTrayNotification;
            ShowRecoveryNotification = settings.ShowRecoveryNotification;
            AutoStart = StartupServiceManager.IsInstalled();
            AutoStartMonitoring = settings.AutoStartMonitoring;
            SaveTestResult = settings.SaveTestResult;
            TestResultPath = settings.TestResultPath;
            EnableTimeRange = settings.EnableTimeRange;
            
            if (TimeSpan.TryParse(settings.StartTime, out TimeSpan parsedStartTime))
                StartTime = parsedStartTime;
            if (TimeSpan.TryParse(settings.EndTime, out TimeSpan parsedEndTime))
                EndTime = parsedEndTime;
                
            EnableMonitorTimeRange = settings.EnableMonitorTimeRange;
            if (TimeSpan.TryParse(settings.MonitorStartTime, out TimeSpan parsedMonitorStartTime))
                MonitorStartTime = parsedMonitorStartTime;
            if (TimeSpan.TryParse(settings.MonitorEndTime, out TimeSpan parsedMonitorEndTime))
                MonitorEndTime = parsedMonitorEndTime;
            EnableAllDayDetection = settings.EnableAllDayDetection;
            AllDayDetectionInterval = Math.Min(Math.Max(settings.AllDayDetectionInterval, 10), 3600);
            AllDayAutoLogin = settings.AllDayAutoLogin;
            ThemeMode = string.IsNullOrWhiteSpace(settings.ThemeMode) ? "TechDark" : settings.ThemeMode;
                
            LoginStrategy = settings.LoginStrategy;
            LoginRetryCount = settings.LoginRetryCount;
            LoginRetryDelay = settings.LoginRetryDelay;

            // 设置UI控件
            loginUrlTextBox.Text = LoginUrl;
            primaryDnsTextBox.Text = PrimaryDns;
            secondaryDnsTextBox.Text = SecondaryDns;
            timeoutInput.Value = Timeout;
            usernameTextBox.Text = Username;
            passwordTextBox.Text = Password;
            showNotificationCheckBox.Checked = ShowNotification;
            showTrayNotificationCheckBox.Checked = ShowTrayNotification;
            showRecoveryNotificationCheckBox.Checked = ShowRecoveryNotification;
            autoStartCheckBox.Checked = AutoStart;
            autoStartMonitoringCheckBox.Checked = AutoStartMonitoring;
            saveTestResultCheckBox.Checked = SaveTestResult;
            testResultPathTextBox.Text = TestResultPath;
            enableTimeRangeCheckBox.Checked = EnableTimeRange;
            startTimePicker.Value = DateTime.Today.Add(StartTime);
            endTimePicker.Value = DateTime.Today.Add(EndTime);
            enableMonitorTimeRangeCheckBox.Checked = EnableMonitorTimeRange;
            monitorStartTimePicker.Value = DateTime.Today.Add(MonitorStartTime);
            monitorEndTimePicker.Value = DateTime.Today.Add(MonitorEndTime);
            enableAllDayDetectionCheckBox.Checked = EnableAllDayDetection;
            allDayDetectionIntervalInput.Value = AllDayDetectionInterval;
            allDayAutoLoginCheckBox.Checked = AllDayAutoLogin;
            themeModeComboBox.SelectedIndex = ThemeMode == "MintLight" ? 1 : 0;
            
            // 设置登录策略
            switch (LoginStrategy)
            {
                case "OnlyWhenDisconnected": loginStrategyComboBox.SelectedIndex = 0; break;
                case "AlwaysTry": loginStrategyComboBox.SelectedIndex = 1; break;
                case "Smart": loginStrategyComboBox.SelectedIndex = 2; break;
                default: loginStrategyComboBox.SelectedIndex = 0; break;
            }
            
            retryCountInput.Value = LoginRetryCount;
            retryDelayInput.Value = LoginRetryDelay;
            RefreshServiceStatus();
        }

        private void InitializeComponents()
        {
            this.Text = "设置";
            this.Size = new Size(510, 590);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoScroll = false;
            this.Icon = AppIconProvider.GetIcon();

            // 登录页面 URL
            Label loginUrlLabel = new Label
            {
                Text = "登录页面 URL:",
                Location = new Point(20, 20),
                Size = new Size(120, 25)
            };

            loginUrlTextBox = new TextBox
            {
                Location = new Point(150, 20),
                Size = new Size(260, 25),
                Text = "http://2.2.2.2"
            };

            // 主 DNS
            Label primaryDnsLabel = new Label
            {
                Text = "主 DNS 服务器:",
                Location = new Point(20, 60),
                Size = new Size(120, 25)
            };

            primaryDnsTextBox = new TextBox
            {
                Location = new Point(150, 60),
                Size = new Size(260, 25),
                Text = "8.8.8.8"
            };

            // 备用 DNS
            Label secondaryDnsLabel = new Label
            {
                Text = "备用 DNS 服务器:",
                Location = new Point(20, 100),
                Size = new Size(120, 25)
            };

            secondaryDnsTextBox = new TextBox
            {
                Location = new Point(150, 100),
                Size = new Size(260, 25),
                Text = "114.114.114.114"
            };

            // 超时时间
            Label timeoutLabel = new Label
            {
                Text = "超时时间(毫秒):",
                Location = new Point(20, 140),
                Size = new Size(120, 25)
            };

            timeoutInput = new NumericUpDown
            {
                Location = new Point(150, 140),
                Size = new Size(260, 25),
                Minimum = 1000,
                Maximum = 30000,
                Value = 10000,
                Increment = 1000
            };

            // 校园网账号设置
            Label accountLabel = new Label
            {
                Text = "校园网账号:",
                Location = new Point(20, 175),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            Label usernameLabel = new Label
            {
                Text = "用户名:",
                Location = new Point(20, 205),
                Size = new Size(120, 25)
            };

            usernameTextBox = new TextBox
            {
                Location = new Point(150, 205),
                Size = new Size(260, 25),
                Text = "23325024026"
            };

            Label passwordLabel = new Label
            {
                Text = "密码:",
                Location = new Point(20, 245),
                Size = new Size(120, 25)
            };

            passwordTextBox = new TextBox
            {
                Location = new Point(150, 245),
                Size = new Size(260, 25),
                Text = "17881936070",
                UseSystemPasswordChar = true
            };

            // 通知设置区域
            Label notificationLabel = new Label
            {
                Text = "通知设置:",
                Location = new Point(20, 285),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            showNotificationCheckBox = new CheckBox
            {
                Text = "显示弹窗通知",
                Location = new Point(20, 315),
                Size = new Size(380, 25),
                Checked = true
            };

            showTrayNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（断网时）",
                Location = new Point(20, 345),
                Size = new Size(380, 25),
                Checked = true
            };

            showRecoveryNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（恢复时）",
                Location = new Point(20, 375),
                Size = new Size(380, 25),
                Checked = true
            };

            autoStartCheckBox = new CheckBox
            {
                Text = "开机自启动程序（服务）",
                Location = new Point(20, 50),
                Size = new Size(380, 25),
                Checked = false
            };
            
            autoStartMonitoringCheckBox = new CheckBox
            {
                Text = "打开程序自动开启监控",
                Location = new Point(20, 435),
                Size = new Size(380, 25),
                Checked = false
            };

            Label themeModeLabel = new Label
            {
                Text = "主题模式:",
                Location = new Point(20, 465),
                Size = new Size(120, 25)
            };

            themeModeComboBox = new ComboBox
            {
                Location = new Point(150, 465),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeModeComboBox.Items.AddRange(new object[]
            {
                "深色科技绿",
                "浅色薄荷绿"
            });
            themeModeComboBox.SelectedIndex = 0;

            // 保存测试结果
            saveTestResultCheckBox = new CheckBox
            {
                Text = "保存测试结果",
                Location = new Point(20, 180),
                Size = new Size(380, 25),
                Checked = false
            };
            saveTestResultCheckBox.CheckedChanged += (s, e) =>
            {
                testResultPathTextBox.Enabled = saveTestResultCheckBox.Checked;
                browseButton.Enabled = saveTestResultCheckBox.Checked;
            };

            // 测试结果路径
            Label testPathLabel = new Label
            {
                Text = "保存路径:",
                Location = new Point(20, 220),
                Size = new Size(120, 25)
            };

            testResultPathTextBox = new TextBox
            {
                Location = new Point(150, 220),
                Size = new Size(180, 25),
                Text = "test_results",
                Enabled = false
            };

            browseButton = new Button
            {
                Text = "浏览...",
                Location = new Point(340, 218),
                Size = new Size(70, 28),
                Enabled = false
            };
            browseButton.Click += BrowseButton_Click;

            var serviceLabel = new Label
            {
                Text = "服务状态:",
                Location = new Point(20, 20),
                Size = new Size(80, 25)
            };

            serviceStatusLabel = new Label
            {
                Text = "未检测",
                Location = new Point(105, 20),
                Size = new Size(220, 25),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            installServiceButton = new Button
            {
                Text = "安装服务",
                Location = new Point(20, 85),
                Size = new Size(110, 32)
            };
            installServiceButton.Click += InstallServiceButton_Click;

            uninstallServiceButton = new Button
            {
                Text = "卸载服务",
                Location = new Point(140, 85),
                Size = new Size(110, 32)
            };
            uninstallServiceButton.Click += UninstallServiceButton_Click;

            // 自动连接时间段
            enableTimeRangeCheckBox = new CheckBox
            {
                Text = "启用自动连接时间段",
                Location = new Point(20, 20),
                Size = new Size(380, 25),
                Checked = false
            };
            enableTimeRangeCheckBox.CheckedChanged += (s, e) =>
            {
                startTimePicker.Enabled = enableTimeRangeCheckBox.Checked;
                endTimePicker.Enabled = enableTimeRangeCheckBox.Checked;
            };

            Label timeRangeLabel = new Label
            {
                Text = "允许连接时间:",
                Location = new Point(20, 60),
                Size = new Size(120, 25)
            };

            startTimePicker = new DateTimePicker
            {
                Location = new Point(150, 60),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(6),
                Enabled = false
            };

            Label toLabel = new Label
            {
                Text = "至",
                Location = new Point(260, 60),
                Size = new Size(30, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            endTimePicker = new DateTimePicker
            {
                Location = new Point(300, 60),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(23),
                Enabled = false
            };

            Label timeRangeHint = new Label
            {
                Text = "（只在此时间段内自动登录校园网）",
                Location = new Point(40, 90),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            // 监测时间段设置
            enableMonitorTimeRangeCheckBox = new CheckBox
            {
                Text = "启用自动监测时间段",
                Location = new Point(20, 125),
                Size = new Size(380, 25),
                Checked = false
            };
            enableMonitorTimeRangeCheckBox.CheckedChanged += (s, e) =>
            {
                monitorStartTimePicker.Enabled = enableMonitorTimeRangeCheckBox.Checked;
                monitorEndTimePicker.Enabled = enableMonitorTimeRangeCheckBox.Checked;
            };

            Label monitorTimeRangeLabel = new Label
            {
                Text = "监测时间段:",
                Location = new Point(20, 165),
                Size = new Size(120, 25)
            };

            monitorStartTimePicker = new DateTimePicker
            {
                Location = new Point(150, 165),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today,
                Enabled = false
            };

            Label monitorToLabel = new Label
            {
                Text = "至",
                Location = new Point(260, 165),
                Size = new Size(30, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            monitorEndTimePicker = new DateTimePicker
            {
                Location = new Point(300, 165),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59),
                Enabled = false
            };

            Label monitorTimeHint = new Label
            {
                Text = "（只在此时间段内进行网络监控）",
                Location = new Point(40, 195),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            enableAllDayDetectionCheckBox = new CheckBox
            {
                Text = "启用监控时间外全天检测",
                Location = new Point(20, 230),
                Size = new Size(380, 25),
                Checked = false
            };

            Label allDayIntervalLabel = new Label
            {
                Text = "时间外检测间隔(秒):",
                Location = new Point(20, 265),
                Size = new Size(130, 25)
            };

            allDayDetectionIntervalInput = new NumericUpDown
            {
                Location = new Point(150, 265),
                Size = new Size(120, 25),
                Minimum = 10,
                Maximum = 3600,
                Value = 60
            };

            allDayAutoLoginCheckBox = new CheckBox
            {
                Text = "时间外检测到断网时自动登录",
                Location = new Point(20, 300),
                Size = new Size(380, 25),
                Checked = false
            };

            // 登录策略设置
            Label strategyLabel = new Label
            {
                Text = "登录策略设置:",
                Location = new Point(20, 335),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            Label loginStrategyLabel = new Label
            {
                Text = "登录策略:",
                Location = new Point(20, 365),
                Size = new Size(120, 25)
            };

            loginStrategyComboBox = new ComboBox
            {
                Location = new Point(150, 365),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            loginStrategyComboBox.Items.AddRange(new object[] {
                "仅在断网时登录",
                "每次都尝试登录",
                "智能策略"
            });
            loginStrategyComboBox.SelectedIndex = 0;

            Label retryCountLabel = new Label
            {
                Text = "失败重试次数:",
                Location = new Point(20, 405),
                Size = new Size(120, 25)
            };

            retryCountInput = new NumericUpDown
            {
                Location = new Point(150, 405),
                Size = new Size(260, 25),
                Minimum = 0,
                Maximum = 1000,
                Value = 3
            };

            Label retryDelayLabel = new Label
            {
                Text = "重试间隔(秒):",
                Location = new Point(20, 445),
                Size = new Size(120, 25)
            };

            retryDelayInput = new NumericUpDown
            {
                Location = new Point(150, 445),
                Size = new Size(260, 25),
                Minimum = 1,
                Maximum = 60,
                Value = 5
            };

            var tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(470, 480)
            };

            var basicTab = new TabPage("基础设置") { AutoScroll = true };
            var policyTab = new TabPage("时间与策略") { AutoScroll = true };
            var advancedTab = new TabPage("操作与存储") { AutoScroll = true };
            var aboutTab = new TabPage("关于") { AutoScroll = true };

            var aboutTitleLabel = new Label
            {
                Text = "NetworkMonitor",
                Location = new Point(20, 30),
                Size = new Size(320, 30),
                Font = new Font("微软雅黑", 11, FontStyle.Bold)
            };

            var authorLabel = new Label
            {
                Text = "作者: gggtttfff",
                Location = new Point(20, 80),
                Size = new Size(320, 25)
            };

            var versionLabel = new Label
            {
                Text = "版本: V1.0.0",
                Location = new Point(20, 110),
                Size = new Size(320, 25)
            };

            var licenseLabel = new Label
            {
                Text = "开源协议: MIT",
                Location = new Point(20, 145),
                Size = new Size(320, 25)
            };

            var mitLinkLabel = new LinkLabel
            {
                Text = "查看 MIT 协议全文",
                Location = new Point(20, 180),
                Size = new Size(200, 25)
            };
            mitLinkLabel.LinkClicked += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://opensource.org/licenses/MIT",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开链接: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            basicTab.Controls.Add(loginUrlLabel);
            basicTab.Controls.Add(loginUrlTextBox);
            basicTab.Controls.Add(primaryDnsLabel);
            basicTab.Controls.Add(primaryDnsTextBox);
            basicTab.Controls.Add(secondaryDnsLabel);
            basicTab.Controls.Add(secondaryDnsTextBox);
            basicTab.Controls.Add(timeoutLabel);
            basicTab.Controls.Add(timeoutInput);
            basicTab.Controls.Add(accountLabel);
            basicTab.Controls.Add(usernameLabel);
            basicTab.Controls.Add(usernameTextBox);
            basicTab.Controls.Add(passwordLabel);
            basicTab.Controls.Add(passwordTextBox);
            basicTab.Controls.Add(notificationLabel);
            basicTab.Controls.Add(showNotificationCheckBox);
            basicTab.Controls.Add(showTrayNotificationCheckBox);
            basicTab.Controls.Add(showRecoveryNotificationCheckBox);
            basicTab.Controls.Add(autoStartMonitoringCheckBox);
            basicTab.Controls.Add(themeModeLabel);
            basicTab.Controls.Add(themeModeComboBox);

            policyTab.Controls.Add(enableTimeRangeCheckBox);
            policyTab.Controls.Add(timeRangeLabel);
            policyTab.Controls.Add(startTimePicker);
            policyTab.Controls.Add(toLabel);
            policyTab.Controls.Add(endTimePicker);
            policyTab.Controls.Add(timeRangeHint);
            policyTab.Controls.Add(enableMonitorTimeRangeCheckBox);
            policyTab.Controls.Add(monitorTimeRangeLabel);
            policyTab.Controls.Add(monitorStartTimePicker);
            policyTab.Controls.Add(monitorToLabel);
            policyTab.Controls.Add(monitorEndTimePicker);
            policyTab.Controls.Add(monitorTimeHint);
            policyTab.Controls.Add(enableAllDayDetectionCheckBox);
            policyTab.Controls.Add(allDayIntervalLabel);
            policyTab.Controls.Add(allDayDetectionIntervalInput);
            policyTab.Controls.Add(allDayAutoLoginCheckBox);
            policyTab.Controls.Add(strategyLabel);
            policyTab.Controls.Add(loginStrategyLabel);
            policyTab.Controls.Add(loginStrategyComboBox);
            policyTab.Controls.Add(retryCountLabel);
            policyTab.Controls.Add(retryCountInput);
            policyTab.Controls.Add(retryDelayLabel);
            policyTab.Controls.Add(retryDelayInput);

            advancedTab.Controls.Add(serviceLabel);
            advancedTab.Controls.Add(serviceStatusLabel);
            advancedTab.Controls.Add(autoStartCheckBox);
            advancedTab.Controls.Add(installServiceButton);
            advancedTab.Controls.Add(uninstallServiceButton);
            advancedTab.Controls.Add(saveTestResultCheckBox);
            advancedTab.Controls.Add(testPathLabel);
            advancedTab.Controls.Add(testResultPathTextBox);
            advancedTab.Controls.Add(browseButton);
            aboutTab.Controls.Add(aboutTitleLabel);
            aboutTab.Controls.Add(authorLabel);
            aboutTab.Controls.Add(versionLabel);
            aboutTab.Controls.Add(licenseLabel);
            aboutTab.Controls.Add(mitLinkLabel);

            tabControl.TabPages.Add(basicTab);
            tabControl.TabPages.Add(policyTab);
            tabControl.TabPages.Add(advancedTab);
            tabControl.TabPages.Add(aboutTab);

            // 保存按钮
            saveButton = new Button
            {
                Text = "保存",
                Location = new Point(292, 505),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;

            // 取消按钮
            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(392, 505),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(tabControl);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
            RefreshServiceStatus();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // 验证 URL
            if (string.IsNullOrWhiteSpace(loginUrlTextBox.Text))
            {
                MessageBox.Show("请输入登录页面 URL", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 验证 DNS 地址
            if (string.IsNullOrWhiteSpace(primaryDnsTextBox.Text))
            {
                MessageBox.Show("请输入主 DNS 服务器地址", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(secondaryDnsTextBox.Text))
            {
                MessageBox.Show("请输入备用 DNS 服务器地址", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // 验证用户名和密码
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                MessageBox.Show("请输入校园网用户名", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show("请输入校园网密码", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoginUrl = loginUrlTextBox.Text.Trim();
            PrimaryDns = primaryDnsTextBox.Text.Trim();
            SecondaryDns = secondaryDnsTextBox.Text.Trim();
            Timeout = (int)timeoutInput.Value;
            Username = usernameTextBox.Text.Trim();
            Password = passwordTextBox.Text.Trim();
            ShowNotification = showNotificationCheckBox.Checked;
            ShowTrayNotification = showTrayNotificationCheckBox.Checked;
            ShowRecoveryNotification = showRecoveryNotificationCheckBox.Checked;
            AutoStart = autoStartCheckBox.Checked;
            AutoStartMonitoring = autoStartMonitoringCheckBox.Checked;
            SaveTestResult = saveTestResultCheckBox.Checked;
            TestResultPath = testResultPathTextBox.Text.Trim();
            EnableTimeRange = enableTimeRangeCheckBox.Checked;
            StartTime = startTimePicker.Value.TimeOfDay;
            EndTime = endTimePicker.Value.TimeOfDay;
            EnableMonitorTimeRange = enableMonitorTimeRangeCheckBox.Checked;
            MonitorStartTime = monitorStartTimePicker.Value.TimeOfDay;
            MonitorEndTime = monitorEndTimePicker.Value.TimeOfDay;
            EnableAllDayDetection = enableAllDayDetectionCheckBox.Checked;
            AllDayDetectionInterval = (int)allDayDetectionIntervalInput.Value;
            AllDayAutoLogin = allDayAutoLoginCheckBox.Checked;
            ThemeMode = themeModeComboBox.SelectedIndex == 1 ? "MintLight" : "TechDark";
            
            // 登录策略映射
            switch (loginStrategyComboBox.SelectedIndex)
            {
                case 0: LoginStrategy = "OnlyWhenDisconnected"; break;
                case 1: LoginStrategy = "AlwaysTry"; break;
                case 2: LoginStrategy = "Smart"; break;
                default: LoginStrategy = "OnlyWhenDisconnected"; break;
            }
            
            LoginRetryCount = (int)retryCountInput.Value;
            LoginRetryDelay = (int)retryDelayInput.Value;
        }

        private void InstallServiceButton_Click(object? sender, EventArgs e)
        {
            bool ok = StartupServiceManager.Install(Application.ExecutablePath, out string message);
            MessageBox.Show(message, ok ? "成功" : "失败",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            RefreshServiceStatus();
        }

        private void UninstallServiceButton_Click(object? sender, EventArgs e)
        {
            bool ok = StartupServiceManager.Uninstall(out string message);
            MessageBox.Show(message, ok ? "成功" : "失败",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            RefreshServiceStatus();
        }

        private void RefreshServiceStatus()
        {
            bool installed = StartupServiceManager.IsInstalled();
            autoStartCheckBox.Checked = installed;
            serviceStatusLabel.Text = installed ? "已安装（开机启动已启用）" : "未安装";
            serviceStatusLabel.ForeColor = installed ? Color.Green : Color.DarkRed;
        }

        private void BrowseButton_Click(object? sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "选择测试结果保存目录";
                folderDialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(testResultPathTextBox.Text))
                {
                    folderDialog.SelectedPath = testResultPathTextBox.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    testResultPathTextBox.Text = folderDialog.SelectedPath;
                }
            }
        }
    }
}
