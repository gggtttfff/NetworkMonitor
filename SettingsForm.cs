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
        private ComboBox closeBehaviorComboBox = null!;
        private CheckBox autoStartCheckBox = null!;
        private CheckBox autoStartMonitoringCheckBox = null!;
        private CheckBox saveTestResultCheckBox = null!;
        private TextBox testResultPathTextBox = null!;
        private Button browseButton = null!;
        private CheckBox saveLogsCheckBox = null!;
        private NumericUpDown logRetentionDaysInput = null!;
        private Button installServiceButton = null!;
        private Button uninstallServiceButton = null!;
        private Label serviceStatusLabel = null!;
        private CheckBox enableTimeRangeCheckBox = null!;
        private DateTimePicker startTimePicker = null!;
        private DateTimePicker endTimePicker = null!;
        private CheckBox enableMonitorTimeRangeCheckBox = null!;
        private DateTimePicker monitorStartTimePicker = null!;
        private DateTimePicker monitorEndTimePicker = null!;
        private ComboBox loginStrategyComboBox = null!;
        private NumericUpDown retryCountInput = null!;
        private NumericUpDown retryDelayInput = null!;
        private ComboBox? themeModeComboBox = null;
        private Button testLoginButton = null!;
        private Label loginTestStatusLabel = null!;
        private string initialUsername = string.Empty;
        private string initialPassword = string.Empty;
        private Button saveButton = null!;
        private Button cancelButton = null!;

        public string LoginUrl { get; private set; } = "http://2.2.2.2";
        public string PrimaryDns { get; private set; } = "8.8.8.8";
        public string SecondaryDns { get; private set; } = "114.114.114.114";
        public int Timeout { get; private set; } = 10000;
        public string Username { get; private set; } = "";
        public string Password { get; private set; } = "";
        public bool ShowNotification { get; private set; } = true;
        public bool ShowTrayNotification { get; private set; } = true;
        public bool ShowRecoveryNotification { get; private set; } = true;
        public bool CloseToTrayOnClose { get; private set; } = true;
        public bool AutoStart { get; private set; } = false;
        public bool AutoStartMonitoring { get; private set; } = false;
        public bool SaveTestResult { get; private set; } = false;
        public string TestResultPath { get; private set; } = "test_results";
        public bool SaveLogs { get; private set; } = true;
        public int LogRetentionDays { get; private set; } = 30;
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
            CloseToTrayOnClose = settings.CloseToTrayOnClose;
            AutoStart = StartupServiceManager.IsInstalled();
            AutoStartMonitoring = settings.AutoStartMonitoring;
            SaveTestResult = settings.SaveTestResult;
            TestResultPath = settings.TestResultPath;
            SaveLogs = settings.SaveLogs;
            LogRetentionDays = Math.Max(1, settings.LogRetentionDays);
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
            closeBehaviorComboBox.SelectedIndex = CloseToTrayOnClose ? 0 : 1;
            autoStartCheckBox.Checked = AutoStart;
            autoStartMonitoringCheckBox.Checked = AutoStartMonitoring;
            saveTestResultCheckBox.Checked = SaveTestResult;
            testResultPathTextBox.Text = TestResultPath;
            saveLogsCheckBox.Checked = SaveLogs;
            logRetentionDaysInput.Value = LogRetentionDays;
            logRetentionDaysInput.Enabled = SaveLogs;
            enableTimeRangeCheckBox.Checked = EnableTimeRange;
            startTimePicker.Value = DateTime.Today.Add(StartTime);
            endTimePicker.Value = DateTime.Today.Add(EndTime);
            enableMonitorTimeRangeCheckBox.Checked = EnableMonitorTimeRange;
            monitorStartTimePicker.Value = DateTime.Today.Add(MonitorStartTime);
            monitorEndTimePicker.Value = DateTime.Today.Add(MonitorEndTime);

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
            ResetCredentialBaseline();
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

            // 主检测目标
            Label primaryDnsLabel = new Label
            {
                Text = "主检测目标:",
                Location = new Point(20, 60),
                Size = new Size(120, 25)
            };

            primaryDnsTextBox = new TextBox
            {
                Location = new Point(150, 60),
                Size = new Size(260, 25),
                Text = "8.8.8.8"
            };

            // 备用检测目标
            Label secondaryDnsLabel = new Label
            {
                Text = "备用检测目标:",
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
                Text = ""
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
                Size = new Size(180, 25),
                Text = "",
                UseSystemPasswordChar = true
            };

            testLoginButton = new Button
            {
                Text = "测试登录",
                Location = new Point(340, 243),
                Size = new Size(70, 28)
            };
            testLoginButton.Click += TestLoginButton_Click;

            loginTestStatusLabel = new Label
            {
                Text = "登录测试: 未测试",
                Location = new Point(150, 275),
                Size = new Size(260, 20),
                ForeColor = Color.DarkOrange,
                Font = new Font("微软雅黑", 8)
            };

            // 通知设置区域
            Label notificationLabel = new Label
            {
                Text = "通知设置:",
                Location = new Point(20, 305),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            showNotificationCheckBox = new CheckBox
            {
                Text = "显示弹窗通知",
                Location = new Point(20, 335),
                Size = new Size(380, 25),
                Checked = true
            };

            showTrayNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（断网时）",
                Location = new Point(20, 365),
                Size = new Size(380, 25),
                Checked = true
            };

            showRecoveryNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（恢复时）",
                Location = new Point(20, 395),
                Size = new Size(380, 25),
                Checked = true
            };

            var closeBehaviorLabel = new Label
            {
                Text = "点击关闭按钮时:",
                Location = new Point(20, 430),
                Size = new Size(120, 25)
            };

            closeBehaviorComboBox = new ComboBox
            {
                Location = new Point(150, 430),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            closeBehaviorComboBox.Items.AddRange(new object[]
            {
                "最小化到后台运行",
                "直接退出程序"
            });
            closeBehaviorComboBox.SelectedIndex = 0;

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
                Location = new Point(20, 485),
                Size = new Size(380, 25),
                Checked = false
            };

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

            saveLogsCheckBox = new CheckBox
            {
                Text = "是否保存日志",
                Location = new Point(20, 265),
                Size = new Size(380, 25),
                Checked = true
            };
            saveLogsCheckBox.CheckedChanged += (s, e) =>
            {
                logRetentionDaysInput.Enabled = saveLogsCheckBox.Checked;
            };

            var logRetentionLabel = new Label
            {
                Text = "日志保存时间(天):",
                Location = new Point(20, 300),
                Size = new Size(120, 25)
            };

            logRetentionDaysInput = new NumericUpDown
            {
                Location = new Point(150, 300),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 365,
                Value = 30,
                Enabled = true
            };

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

            // 登录策略设置
            Label strategyLabel = new Label
            {
                Text = "登录策略设置:",
                Location = new Point(20, 230),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            Label loginStrategyLabel = new Label
            {
                Text = "登录策略:",
                Location = new Point(20, 260),
                Size = new Size(120, 25)
            };

            loginStrategyComboBox = new ComboBox
            {
                Location = new Point(150, 260),
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
                Location = new Point(20, 300),
                Size = new Size(120, 25)
            };

            retryCountInput = new NumericUpDown
            {
                Location = new Point(150, 300),
                Size = new Size(260, 25),
                Minimum = 0,
                Maximum = 1000,
                Value = 3
            };

            Label retryDelayLabel = new Label
            {
                Text = "重试间隔(秒):",
                Location = new Point(20, 340),
                Size = new Size(120, 25)
            };

            retryDelayInput = new NumericUpDown
            {
                Location = new Point(150, 340),
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
                Text = $"版本: V{AppVersionProvider.GetDisplayVersion()}",
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
                Size = new Size(220, 25)
            };
            mitLinkLabel.LinkClicked += (_, _) => OpenUrl("https://opensource.org/licenses/MIT");

            var githubLinkLabel = new LinkLabel
            {
                Text = "GitHub 页面",
                Location = new Point(20, 210),
                Size = new Size(220, 25)
            };
            githubLinkLabel.LinkClicked += (_, _) => OpenUrl("https://github.com/gggtttfff/NetworkMonitor");

            var giteeLinkLabel = new LinkLabel
            {
                Text = "Gitee 页面",
                Location = new Point(20, 240),
                Size = new Size(220, 25)
            };
            giteeLinkLabel.LinkClicked += (_, _) => OpenUrl("https://gitee.com/pieory/NetworkMonitor");

            var feedbackLinkLabel = new LinkLabel
            {
                Text = "反馈 (GitHub)",
                Location = new Point(20, 270),
                Size = new Size(220, 25)
            };
            feedbackLinkLabel.LinkClicked += (_, _) => OpenUrl("https://github.com/gggtttfff/NetworkMonitor/issues");

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
            basicTab.Controls.Add(testLoginButton);
            basicTab.Controls.Add(loginTestStatusLabel);
            basicTab.Controls.Add(notificationLabel);
            basicTab.Controls.Add(showNotificationCheckBox);
            basicTab.Controls.Add(showTrayNotificationCheckBox);
            basicTab.Controls.Add(showRecoveryNotificationCheckBox);
            basicTab.Controls.Add(closeBehaviorLabel);
            basicTab.Controls.Add(closeBehaviorComboBox);
            basicTab.Controls.Add(autoStartMonitoringCheckBox);

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
            advancedTab.Controls.Add(saveLogsCheckBox);
            advancedTab.Controls.Add(logRetentionLabel);
            advancedTab.Controls.Add(logRetentionDaysInput);
            aboutTab.Controls.Add(aboutTitleLabel);
            aboutTab.Controls.Add(authorLabel);
            aboutTab.Controls.Add(versionLabel);
            aboutTab.Controls.Add(licenseLabel);
            aboutTab.Controls.Add(mitLinkLabel);
            aboutTab.Controls.Add(githubLinkLabel);
            aboutTab.Controls.Add(giteeLinkLabel);
            aboutTab.Controls.Add(feedbackLinkLabel);

            tabControl.TabPages.Add(basicTab);
            tabControl.TabPages.Add(policyTab);
            tabControl.TabPages.Add(advancedTab);
            tabControl.TabPages.Add(aboutTab);

            // 保存按钮
            saveButton = new Button
            {
                Text = "保存",
                Location = new Point(292, 505),
                Size = new Size(90, 35)
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

            usernameTextBox.TextChanged += (_, _) => HandleCredentialChanged();
            passwordTextBox.TextChanged += (_, _) => HandleCredentialChanged();

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
            ResetCredentialBaseline();
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

            // 验证检测目标
            if (string.IsNullOrWhiteSpace(primaryDnsTextBox.Text))
            {
                MessageBox.Show("请输入主检测目标", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(secondaryDnsTextBox.Text))
            {
                MessageBox.Show("请输入备用检测目标", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            CloseToTrayOnClose = closeBehaviorComboBox.SelectedIndex != 1;
            AutoStart = autoStartCheckBox.Checked;
            AutoStartMonitoring = autoStartMonitoringCheckBox.Checked;
            SaveTestResult = saveTestResultCheckBox.Checked;
            TestResultPath = testResultPathTextBox.Text.Trim();
            SaveLogs = saveLogsCheckBox.Checked;
            LogRetentionDays = (int)logRetentionDaysInput.Value;
            EnableTimeRange = enableTimeRangeCheckBox.Checked;
            StartTime = startTimePicker.Value.TimeOfDay;
            EndTime = endTimePicker.Value.TimeOfDay;
            EnableMonitorTimeRange = enableMonitorTimeRangeCheckBox.Checked;
            MonitorStartTime = monitorStartTimePicker.Value.TimeOfDay;
            MonitorEndTime = monitorEndTimePicker.Value.TimeOfDay;
            ThemeMode = "TechDark";

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
            DialogResult = DialogResult.OK;
            Close();
        }

        private async void TestLoginButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(loginUrlTextBox.Text) ||
                string.IsNullOrWhiteSpace(usernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                MessageBox.Show("请先填写登录地址、用户名和密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            testLoginButton.Enabled = false;
            loginTestStatusLabel.Text = "登录测试: 测试中...";
            loginTestStatusLabel.ForeColor = Color.DodgerBlue;

            try
            {
                var authenticator = new CampusNetworkAuthenticator(
                    loginUrlTextBox.Text.Trim(),
                    usernameTextBox.Text.Trim(),
                    passwordTextBox.Text.Trim());

                var result = await authenticator.AuthenticateAsync();
                if (result.Success)
                {
                    MarkLoginTestPassed();
                    MessageBox.Show("登录测试通过", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MarkLoginTestPending("失败，请检查配置，仍可直接保存");
                    MessageBox.Show($"登录测试失败: {result.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MarkLoginTestPending("失败，请检查网络，仍可直接保存");
                MessageBox.Show($"登录测试异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                testLoginButton.Enabled = true;
            }
        }

        private bool IsCredentialChanged()
        {
            if (usernameTextBox == null || passwordTextBox == null)
            {
                return false;
            }

            return !string.Equals(usernameTextBox.Text.Trim(), initialUsername, StringComparison.Ordinal)
                || !string.Equals(passwordTextBox.Text, initialPassword, StringComparison.Ordinal);
        }

        private void UpdateSaveButtonState()
        {
            if (saveButton == null)
            {
                return;
            }

            saveButton.Enabled = true;
        }

        private void HandleCredentialChanged()
        {
            if (IsCredentialChanged())
            {
                MarkLoginTestPending("账号或密码已更改，可直接保存，建议先测试");
                return;
            }

            UpdateSaveButtonState();
            if (loginTestStatusLabel != null)
            {
                loginTestStatusLabel.Text = "登录测试: 账号和密码未变更，可直接保存";
                loginTestStatusLabel.ForeColor = Color.DimGray;
            }
        }

        private void ResetCredentialBaseline()
        {
            initialUsername = usernameTextBox?.Text.Trim() ?? string.Empty;
            initialPassword = passwordTextBox?.Text ?? string.Empty;
            HandleCredentialChanged();
        }

        private void MarkLoginTestPending(string reason)
        {
            UpdateSaveButtonState();
            if (loginTestStatusLabel != null)
            {
                loginTestStatusLabel.Text = $"登录测试: {reason}";
                loginTestStatusLabel.ForeColor = Color.DarkOrange;
            }
        }

        private void MarkLoginTestPassed()
        {
            UpdateSaveButtonState();
            if (loginTestStatusLabel != null)
            {
                loginTestStatusLabel.Text = "登录测试: 已通过";
                loginTestStatusLabel.ForeColor = Color.Green;
            }
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

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
