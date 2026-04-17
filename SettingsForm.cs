using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkMonitor
{
    public class SettingsForm : Form
    {
        private readonly NetworkAdapterManager adapterManager = new NetworkAdapterManager();
        private readonly ToolTip helpToolTip = new ToolTip();
        private readonly List<NetworkAdapterInfo> availableAdapters = new List<NetworkAdapterInfo>();
        private Image? helpIconImage;

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
        private CheckBox silentRunOnAutoStartCheckBox = null!;
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
        private Button testLoginButton = null!;
        private Label loginTestStatusLabel = null!;
        private CheckBox enableAdapterAutoManagementCheckBox = null!;
        private CheckBox showAdapterNotificationCheckBox = null!;
        private CheckedListBox managedAdaptersCheckedListBox = null!;
        private DataGridView adapterScheduleGrid = null!;
        private Button refreshAdaptersButton = null!;
        private Button enableSelectedAdapterButton = null!;
        private Button disableSelectedAdapterButton = null!;
        private Label adapterPermissionLabel = null!;
        private Button saveButton = null!;
        private Button cancelButton = null!;

        private string initialUsername = string.Empty;
        private string initialPassword = string.Empty;

        public string LoginUrl { get; private set; } = "http://2.2.2.2";
        public string PrimaryDns { get; private set; } = "8.8.8.8";
        public string SecondaryDns { get; private set; } = "114.114.114.114";
        public int Timeout { get; private set; } = 10000;
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public bool ShowNotification { get; private set; } = true;
        public bool ShowTrayNotification { get; private set; } = true;
        public bool ShowRecoveryNotification { get; private set; } = true;
        public bool CloseToTrayOnClose { get; private set; } = true;
        public bool AutoStart { get; private set; }
        public bool AutoStartMonitoring { get; private set; }
        public bool SilentRunOnAutoStart { get; private set; }
        public bool SaveTestResult { get; private set; }
        public string TestResultPath { get; private set; } = "test_results";
        public bool SaveLogs { get; private set; } = true;
        public int LogRetentionDays { get; private set; } = 30;
        public bool EnableTimeRange { get; private set; }
        public TimeSpan StartTime { get; private set; } = new TimeSpan(6, 0, 0);
        public TimeSpan EndTime { get; private set; } = new TimeSpan(23, 0, 0);
        public bool EnableMonitorTimeRange { get; private set; }
        public TimeSpan MonitorStartTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan MonitorEndTime { get; private set; } = new TimeSpan(23, 59, 59);
        public bool EnableAllDayDetection { get; private set; }
        public int AllDayDetectionInterval { get; private set; } = 60;
        public bool AllDayAutoLogin { get; private set; }
        public string ThemeMode { get; private set; } = "TechDark";
        public string LoginStrategy { get; private set; } = "OnlyWhenDisconnected";
        public int LoginRetryCount { get; private set; } = 3;
        public int LoginRetryDelay { get; private set; } = 5;
        public bool EnableAdapterAutoManagement { get; private set; }
        public bool ShowAdapterNotification { get; private set; } = true;
        public List<string> ManagedAdapterIds { get; private set; } = new List<string>();
        public List<AdapterScheduleEntry> AdapterSchedule { get; private set; } = AdapterScheduleEntry.CreateDefaultWeek();

        public SettingsForm()
        {
            helpToolTip.AutoPopDelay = 15000;
            helpToolTip.InitialDelay = 250;
            helpToolTip.ReshowDelay = 150;
            InitializeComponents();
        }

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
            SilentRunOnAutoStart = settings.SilentRunOnAutoStart;
            SaveTestResult = settings.SaveTestResult;
            TestResultPath = settings.TestResultPath;
            SaveLogs = settings.SaveLogs;
            LogRetentionDays = Math.Max(1, settings.LogRetentionDays);
            EnableTimeRange = settings.EnableTimeRange;
            EnableMonitorTimeRange = settings.EnableMonitorTimeRange;
            EnableAllDayDetection = settings.EnableAllDayDetection;
            AllDayDetectionInterval = Math.Min(Math.Max(settings.AllDayDetectionInterval, 10), 3600);
            AllDayAutoLogin = settings.AllDayAutoLogin;
            ThemeMode = string.IsNullOrWhiteSpace(settings.ThemeMode) ? "TechDark" : settings.ThemeMode;
            LoginStrategy = settings.LoginStrategy;
            LoginRetryCount = settings.LoginRetryCount;
            LoginRetryDelay = settings.LoginRetryDelay;
            EnableAdapterAutoManagement = settings.EnableAdapterAutoManagement;
            ShowAdapterNotification = settings.ShowAdapterNotification;
            ManagedAdapterIds = settings.ManagedAdapterIds ?? new List<string>();
            AdapterSchedule = adapterManager.NormalizeSchedule(settings.AdapterSchedule);

            if (TimeSpan.TryParse(settings.StartTime, out var parsedStartTime))
            {
                StartTime = parsedStartTime;
            }

            if (TimeSpan.TryParse(settings.EndTime, out var parsedEndTime))
            {
                EndTime = parsedEndTime;
            }

            if (TimeSpan.TryParse(settings.MonitorStartTime, out var parsedMonitorStartTime))
            {
                MonitorStartTime = parsedMonitorStartTime;
            }

            if (TimeSpan.TryParse(settings.MonitorEndTime, out var parsedMonitorEndTime))
            {
                MonitorEndTime = parsedMonitorEndTime;
            }

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
            silentRunOnAutoStartCheckBox.Checked = SilentRunOnAutoStart;
            saveTestResultCheckBox.Checked = SaveTestResult;
            testResultPathTextBox.Text = TestResultPath;
            saveLogsCheckBox.Checked = SaveLogs;
            logRetentionDaysInput.Value = LogRetentionDays;
            enableTimeRangeCheckBox.Checked = EnableTimeRange;
            startTimePicker.Value = DateTime.Today.Add(StartTime);
            endTimePicker.Value = DateTime.Today.Add(EndTime);
            enableMonitorTimeRangeCheckBox.Checked = EnableMonitorTimeRange;
            monitorStartTimePicker.Value = DateTime.Today.Add(MonitorStartTime);
            monitorEndTimePicker.Value = DateTime.Today.Add(MonitorEndTime);
            retryCountInput.Value = Math.Min((int)retryCountInput.Maximum, Math.Max((int)retryCountInput.Minimum, LoginRetryCount));
            retryDelayInput.Value = Math.Min((int)retryDelayInput.Maximum, Math.Max((int)retryDelayInput.Minimum, LoginRetryDelay));
            enableAdapterAutoManagementCheckBox.Checked = EnableAdapterAutoManagement;
            showAdapterNotificationCheckBox.Checked = ShowAdapterNotification;

            switch (LoginStrategy)
            {
                case "AlwaysTry":
                    loginStrategyComboBox.SelectedIndex = 1;
                    break;
                case "Smart":
                    loginStrategyComboBox.SelectedIndex = 2;
                    break;
                default:
                    loginStrategyComboBox.SelectedIndex = 0;
                    break;
            }

            ToggleStorageControls();
            ToggleTimeRangeControls();
            RefreshServiceStatus();
            LoadAdapterItems();
            LoadScheduleGrid();
            ToggleAdapterControls();
            ResetCredentialBaseline();
        }

        private void InitializeComponents()
        {
            Text = "设置";
            Size = new Size(780, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = AppIconProvider.GetIcon();

            helpIconImage = LoadHelpIconImage();

            var tabControl = new TabControl
            {
                Location = new Point(12, 12),
                Size = new Size(742, 598)
            };

            var basicTab = new TabPage("基础设置") { AutoScroll = true };
            var policyTab = new TabPage("时间与策略") { AutoScroll = true };
            var advancedTab = new TabPage("操作与存储") { AutoScroll = true };
            var adapterTab = new TabPage("网卡管理") { AutoScroll = true };
            var aboutTab = new TabPage("关于") { AutoScroll = true };

            BuildBasicTab(basicTab);
            BuildPolicyTab(policyTab);
            BuildAdvancedTab(advancedTab);
            BuildAdapterTab(adapterTab);
            BuildAboutTab(aboutTab);

            tabControl.TabPages.Add(basicTab);
            tabControl.TabPages.Add(policyTab);
            tabControl.TabPages.Add(advancedTab);
            tabControl.TabPages.Add(adapterTab);
            tabControl.TabPages.Add(aboutTab);

            saveButton = new Button
            {
                Text = "保存",
                Location = new Point(564, 622),
                Size = new Size(90, 35)
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(664, 622),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(tabControl);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);

            usernameTextBox.TextChanged += (_, _) => HandleCredentialChanged();
            passwordTextBox.TextChanged += (_, _) => HandleCredentialChanged();
            saveTestResultCheckBox.CheckedChanged += (_, _) => ToggleStorageControls();
            saveLogsCheckBox.CheckedChanged += (_, _) => ToggleStorageControls();
            enableTimeRangeCheckBox.CheckedChanged += (_, _) => ToggleTimeRangeControls();
            enableMonitorTimeRangeCheckBox.CheckedChanged += (_, _) => ToggleTimeRangeControls();
            enableAdapterAutoManagementCheckBox.CheckedChanged += (_, _) => ToggleAdapterControls();

            AcceptButton = saveButton;
            CancelButton = cancelButton;
            RefreshServiceStatus();
            LoadAdapterItems();
            LoadScheduleGrid();
            ToggleStorageControls();
            ToggleTimeRangeControls();
            ToggleAdapterControls();
            ResetCredentialBaseline();
        }

        private void BuildBasicTab(TabPage tab)
        {
            var loginUrlLabel = CreateLabel("登录页面 URL:", 20, 20);
            loginUrlTextBox = CreateTextBox(170, 20, 480, "http://2.2.2.2");
            AddHelpIcon(tab, loginUrlLabel, "校园网认证页地址。主界面的“认证登录”和自动登录都使用这里。");

            var primaryDnsLabel = CreateLabel("主检测目标:", 20, 60);
            primaryDnsTextBox = CreateTextBox(170, 60, 480, "8.8.8.8");
            AddHelpIcon(tab, primaryDnsLabel, "网络检测优先探测的目标。可填域名或 IP。");

            var secondaryDnsLabel = CreateLabel("备用检测目标:", 20, 100);
            secondaryDnsTextBox = CreateTextBox(170, 100, 480, "114.114.114.114");
            AddHelpIcon(tab, secondaryDnsLabel, "主检测失败后并行参与判断的备用目标。");

            var timeoutLabel = CreateLabel("超时时间(毫秒):", 20, 140);
            timeoutInput = new NumericUpDown
            {
                Location = new Point(170, 140),
                Size = new Size(120, 25),
                Minimum = 1000,
                Maximum = 30000,
                Value = 10000,
                Increment = 1000
            };
            AddHelpIcon(tab, timeoutLabel, "Ping 与部分网络探测的超时上限。值越大，误判越少，但断网检测会更慢。");

            var accountLabel = new Label
            {
                Text = "校园网账号",
                Location = new Point(20, 185),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            var usernameLabel = CreateLabel("用户名:", 20, 215);
            usernameTextBox = CreateTextBox(170, 215, 480, string.Empty);
            AddHelpIcon(tab, usernameLabel, "自动登录和“测试登录”使用的校园网用户名。");

            var passwordLabel = CreateLabel("密码:", 20, 255);
            passwordTextBox = CreateTextBox(170, 255, 390, string.Empty);
            passwordTextBox.UseSystemPasswordChar = true;
            AddHelpIcon(tab, passwordLabel, "密码保存到配置文件时会按当前 Windows 用户加密。");

            testLoginButton = new Button
            {
                Text = "测试登录",
                Location = new Point(570, 253),
                Size = new Size(80, 28)
            };
            testLoginButton.Click += TestLoginButton_Click;

            loginTestStatusLabel = new Label
            {
                Text = "登录测试: 未测试",
                Location = new Point(170, 287),
                Size = new Size(420, 20),
                ForeColor = Color.DarkOrange,
                Font = new Font("微软雅黑", 8)
            };

            var notificationLabel = new Label
            {
                Text = "通知设置",
                Location = new Point(20, 325),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            showNotificationCheckBox = CreateCheckBox("显示弹窗通知", 20, 355, 260, true);
            AddHelpIcon(tab, showNotificationCheckBox, "保留给当前代码行为的总开关说明。现阶段主要通知仍通过托盘气泡显示。");

            showTrayNotificationCheckBox = CreateCheckBox("显示托盘气泡通知（断网时）", 20, 388, 320, true);
            AddHelpIcon(tab, showTrayNotificationCheckBox, "检测到断网并准备自动登录时，显示托盘气泡提示。");

            showRecoveryNotificationCheckBox = CreateCheckBox("显示托盘气泡通知（恢复时）", 20, 421, 320, true);
            AddHelpIcon(tab, showRecoveryNotificationCheckBox, "网络恢复正常时，显示托盘气泡提示。");

            var closeBehaviorLabel = CreateLabel("点击关闭按钮时:", 20, 460);
            closeBehaviorComboBox = new ComboBox
            {
                Location = new Point(170, 460),
                Size = new Size(240, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            closeBehaviorComboBox.Items.AddRange(new object[] { "最小化到后台运行", "直接退出程序" });
            closeBehaviorComboBox.SelectedIndex = 0;
            AddHelpIcon(tab, closeBehaviorLabel, "只影响右上角关闭按钮。最小化按钮仍是最小化，不会自动退出。");

            autoStartMonitoringCheckBox = CreateCheckBox("打开程序自动开启监控", 20, 500, 300, false);
            AddHelpIcon(tab, autoStartMonitoringCheckBox, "主窗体加载完成后自动进入监控状态，不代表自动登录。");

            tab.Controls.AddRange(new Control[]
            {
                loginUrlLabel, loginUrlTextBox,
                primaryDnsLabel, primaryDnsTextBox,
                secondaryDnsLabel, secondaryDnsTextBox,
                timeoutLabel, timeoutInput,
                accountLabel,
                usernameLabel, usernameTextBox,
                passwordLabel, passwordTextBox, testLoginButton, loginTestStatusLabel,
                notificationLabel,
                showNotificationCheckBox,
                showTrayNotificationCheckBox,
                showRecoveryNotificationCheckBox,
                closeBehaviorLabel, closeBehaviorComboBox,
                autoStartMonitoringCheckBox
            });
        }

        private void BuildPolicyTab(TabPage tab)
        {
            enableTimeRangeCheckBox = CreateCheckBox("启用自动连接时间段", 20, 20, 240, false);
            AddHelpIcon(tab, enableTimeRangeCheckBox, "只在设定时段内允许自动登录校园网。");

            var timeRangeLabel = CreateLabel("允许连接时间:", 20, 60);
            startTimePicker = CreateTimePicker(170, 60, DateTime.Today.AddHours(6));
            endTimePicker = CreateTimePicker(300, 60, DateTime.Today.AddHours(23));
            var toLabel = CreateLabel("至", 275, 60, 20);
            AddHelpIcon(tab, timeRangeLabel, "不在时间段内时，即使断网也不会自动登录。");

            var timeRangeHint = new Label
            {
                Text = "（只在此时间段内自动登录校园网）",
                Location = new Point(40, 92),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            enableMonitorTimeRangeCheckBox = CreateCheckBox("启用自动监测时间段", 20, 130, 240, false);
            AddHelpIcon(tab, enableMonitorTimeRangeCheckBox, "只在设定时段内执行网络监控循环。");

            var monitorTimeRangeLabel = CreateLabel("监测时间段:", 20, 170);
            monitorStartTimePicker = CreateTimePicker(170, 170, DateTime.Today);
            monitorEndTimePicker = CreateTimePicker(300, 170, DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59));
            var monitorToLabel = CreateLabel("至", 275, 170, 20);
            AddHelpIcon(tab, monitorTimeRangeLabel, "不在时间段内时，状态显示为“时间段外”，监控循环等待。");

            var monitorTimeHint = new Label
            {
                Text = "（只在此时间段内进行网络监控）",
                Location = new Point(40, 202),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            var strategyLabel = new Label
            {
                Text = "登录策略设置",
                Location = new Point(20, 245),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            var loginStrategyLabel = CreateLabel("登录策略:", 20, 278);
            loginStrategyComboBox = new ComboBox
            {
                Location = new Point(170, 278),
                Size = new Size(240, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            loginStrategyComboBox.Items.AddRange(new object[] { "仅在断网时登录", "每次都尝试登录", "智能策略" });
            loginStrategyComboBox.SelectedIndex = 0;
            AddHelpIcon(tab, loginStrategyLabel, "当前代码已保存该策略，但实际自动登录流程主要仍基于断网检测结果执行。");

            var retryCountLabel = CreateLabel("失败重试次数:", 20, 318);
            retryCountInput = new NumericUpDown
            {
                Location = new Point(170, 318),
                Size = new Size(120, 25),
                Minimum = 0,
                Maximum = 1000,
                Value = 3
            };
            AddHelpIcon(tab, retryCountLabel, "自动登录失败后重试的次数。0 表示只尝试一次。");

            var retryDelayLabel = CreateLabel("重试间隔(秒):", 20, 358);
            retryDelayInput = new NumericUpDown
            {
                Location = new Point(170, 358),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 60,
                Value = 5
            };
            AddHelpIcon(tab, retryDelayLabel, "两次自动登录尝试之间的等待时间。");

            tab.Controls.AddRange(new Control[]
            {
                enableTimeRangeCheckBox,
                timeRangeLabel, startTimePicker, toLabel, endTimePicker, timeRangeHint,
                enableMonitorTimeRangeCheckBox,
                monitorTimeRangeLabel, monitorStartTimePicker, monitorToLabel, monitorEndTimePicker, monitorTimeHint,
                strategyLabel, loginStrategyLabel, loginStrategyComboBox,
                retryCountLabel, retryCountInput,
                retryDelayLabel, retryDelayInput
            });
        }

        private void BuildAdvancedTab(TabPage tab)
        {
            var serviceLabel = CreateLabel("服务状态:", 20, 20);
            serviceStatusLabel = new Label
            {
                Text = "未检测",
                Location = new Point(120, 20),
                Size = new Size(350, 25),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            autoStartCheckBox = CreateCheckBox("开机自启动程序（注册表启动项）", 20, 55, 320, false);
            AddHelpIcon(tab, autoStartCheckBox, "写入当前用户的 Run 注册表项，不安装 Windows 服务。");

            silentRunOnAutoStartCheckBox = CreateCheckBox("开机自启动后静默运行", 20, 88, 260, false);
            AddHelpIcon(tab, silentRunOnAutoStartCheckBox, "仅在注册表自启动触发时隐藏主界面，手动双击启动仍显示窗口。");

            installServiceButton = new Button
            {
                Text = "安装启动项",
                Location = new Point(20, 125),
                Size = new Size(110, 32)
            };
            installServiceButton.Click += InstallServiceButton_Click;

            uninstallServiceButton = new Button
            {
                Text = "移除启动项",
                Location = new Point(140, 125),
                Size = new Size(110, 32)
            };
            uninstallServiceButton.Click += UninstallServiceButton_Click;

            saveTestResultCheckBox = CreateCheckBox("保存测试结果", 20, 185, 180, false);
            AddHelpIcon(tab, saveTestResultCheckBox, "保存主界面测试功能产生的输出文件。");

            var testPathLabel = CreateLabel("保存路径:", 20, 220);
            testResultPathTextBox = CreateTextBox(170, 220, 390, "test_results");
            browseButton = new Button
            {
                Text = "浏览...",
                Location = new Point(570, 218),
                Size = new Size(80, 28)
            };
            browseButton.Click += BrowseButton_Click;
            AddHelpIcon(tab, testPathLabel, "测试结果和导出内容的目标目录。");

            saveLogsCheckBox = CreateCheckBox("是否保存日志", 20, 265, 180, true);
            AddHelpIcon(tab, saveLogsCheckBox, "关闭后不再写入诊断日志文件，但界面内即时日志仍会显示。");

            var logRetentionLabel = CreateLabel("日志保存时间(天):", 20, 300);
            logRetentionDaysInput = new NumericUpDown
            {
                Location = new Point(170, 300),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 365,
                Value = 30
            };
            AddHelpIcon(tab, logRetentionLabel, "超过保留天数的日志由诊断日志模块清理。");

            tab.Controls.AddRange(new Control[]
            {
                serviceLabel, serviceStatusLabel,
                autoStartCheckBox, silentRunOnAutoStartCheckBox,
                installServiceButton, uninstallServiceButton,
                saveTestResultCheckBox,
                testPathLabel, testResultPathTextBox, browseButton,
                saveLogsCheckBox,
                logRetentionLabel, logRetentionDaysInput
            });
        }

        private void BuildAdapterTab(TabPage tab)
        {
            enableAdapterAutoManagementCheckBox = CreateCheckBox("启用网卡自动管理", 20, 20, 220, false);
            AddHelpIcon(tab, enableAdapterAutoManagementCheckBox, "按下方周计划自动启用或禁用被选中的网卡。");

            showAdapterNotificationCheckBox = CreateCheckBox("网卡启用/禁用时显示托盘通知", 20, 53, 280, true);
            AddHelpIcon(tab, showAdapterNotificationCheckBox, "计划执行或手动执行网卡操作后，显示托盘通知。");

            adapterPermissionLabel = new Label
            {
                Text = "提示：网卡启用/禁用会触发管理员权限请求。",
                Location = new Point(20, 86),
                Size = new Size(420, 20),
                ForeColor = Color.DarkOrange
            };

            var adapterListLabel = new Label
            {
                Text = "受管理网卡",
                Location = new Point(20, 118),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            refreshAdaptersButton = new Button
            {
                Text = "刷新网卡列表",
                Location = new Point(530, 114),
                Size = new Size(120, 28)
            };
            refreshAdaptersButton.Click += (_, _) => LoadAdapterItems();

            managedAdaptersCheckedListBox = new CheckedListBox
            {
                Location = new Point(20, 148),
                Size = new Size(630, 150),
                CheckOnClick = true
            };

            enableSelectedAdapterButton = new Button
            {
                Text = "手动启用选中网卡",
                Location = new Point(20, 310),
                Size = new Size(150, 30)
            };
            enableSelectedAdapterButton.Click += (_, _) => ToggleSelectedAdapter(true);

            disableSelectedAdapterButton = new Button
            {
                Text = "手动禁用选中网卡",
                Location = new Point(180, 310),
                Size = new Size(150, 30)
            };
            disableSelectedAdapterButton.Click += (_, _) => ToggleSelectedAdapter(false);

            var scheduleLabel = new Label
            {
                Text = "每周启用计划",
                Location = new Point(20, 356),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            adapterScheduleGrid = new DataGridView
            {
                Location = new Point(20, 386),
                Size = new Size(630, 160),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            adapterScheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DayName",
                HeaderText = "星期",
                ReadOnly = true,
                Width = 110
            });
            adapterScheduleGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Enabled",
                HeaderText = "启用",
                Width = 70
            });
            adapterScheduleGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "AllDay",
                HeaderText = "全天",
                Width = 70
            });
            adapterScheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StartTime",
                HeaderText = "开始时间",
                Width = 140
            });
            adapterScheduleGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EndTime",
                HeaderText = "结束时间",
                Width = 140
            });

            var scheduleHintLabel = new Label
            {
                Text = "时间格式为 HH:mm；若勾选“全天”，开始/结束时间仅作为展示。",
                Location = new Point(20, 553),
                Size = new Size(430, 20),
                ForeColor = Color.Gray
            };

            tab.Controls.AddRange(new Control[]
            {
                enableAdapterAutoManagementCheckBox,
                showAdapterNotificationCheckBox,
                adapterPermissionLabel,
                adapterListLabel,
                refreshAdaptersButton,
                managedAdaptersCheckedListBox,
                enableSelectedAdapterButton,
                disableSelectedAdapterButton,
                scheduleLabel,
                adapterScheduleGrid,
                scheduleHintLabel
            });
        }

        private void BuildAboutTab(TabPage tab)
        {
            var aboutTitleLabel = new Label
            {
                Text = "NetworkMonitor",
                Location = new Point(20, 30),
                Size = new Size(320, 30),
                Font = new Font("微软雅黑", 11, FontStyle.Bold)
            };

            var authorLabel = CreateLabel("作者: gggtttfff", 20, 80, 320);
            var versionLabel = CreateLabel($"版本: V{AppVersionProvider.GetDisplayVersion()}", 20, 110, 320);
            var licenseLabel = CreateLabel("开源协议: MIT", 20, 145, 320);

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

            var checkUpdateButton = new Button
            {
                Text = "检查更新",
                Location = new Point(20, 310),
                Size = new Size(100, 28)
            };
            checkUpdateButton.Click += async (_, _) => await CheckForUpdateAsync(tab);

            var updateStatusLabel = new Label
            {
                Text = "",
                Location = new Point(130, 310),
                Size = new Size(400, 28),
                Font = new Font("微软雅黑", 9)
            };
            updateStatusLabel.Name = "updateStatusLabel";

            tab.Controls.AddRange(new Control[]
            {
                aboutTitleLabel,
                authorLabel,
                versionLabel,
                licenseLabel,
                mitLinkLabel,
                githubLinkLabel,
                giteeLinkLabel,
                feedbackLinkLabel,
                checkUpdateButton,
                updateStatusLabel
            });
        }

        private async Task CheckForUpdateAsync(TabPage tab)
        {
            var updateStatusLabel = tab.Controls.Find("updateStatusLabel", true).FirstOrDefault() as Label;
            var checkUpdateButton = tab.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "检查更新");

            if (updateStatusLabel == null || checkUpdateButton == null)
                return;

            checkUpdateButton.Enabled = false;
            updateStatusLabel.Text = "正在检查...";
            updateStatusLabel.ForeColor = SystemColors.ControlText;

            try
            {
                var currentVersion = AppVersionProvider.GetDisplayVersion();
                var result = await UpdateChecker.CheckForUpdateAsync(currentVersion);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    updateStatusLabel.Text = result.ErrorMessage;
                    updateStatusLabel.ForeColor = Color.Red;
                }
                else if (result.HasUpdate)
                {
                    updateStatusLabel.Text = $"发现新版本 V{result.LatestVersion}";
                    updateStatusLabel.ForeColor = Color.Green;

                    var downloadLink = new LinkLabel
                    {
                        Text = "前往下载",
                        Location = new Point(130, 340),
                        Size = new Size(100, 25),
                        Tag = result.ReleaseUrl
                    };
                    downloadLink.LinkClicked += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(result.DownloadUrl))
                            OpenUrl(result.DownloadUrl);
                        else
                            OpenUrl(result.ReleaseUrl);
                    };

                    var existingLink = tab.Controls.OfType<LinkLabel>().FirstOrDefault(l => l.Text == "前往下载");
                    if (existingLink != null)
                        tab.Controls.Remove(existingLink);

                    tab.Controls.Add(downloadLink);
                }
                else
                {
                    updateStatusLabel.Text = "已是最新版本";
                    updateStatusLabel.ForeColor = Color.Green;

                    var existingLink = tab.Controls.OfType<LinkLabel>().FirstOrDefault(l => l.Text == "前往下载");
                    if (existingLink != null)
                        tab.Controls.Remove(existingLink);
                }
            }
            catch (Exception ex)
            {
                updateStatusLabel.Text = $"检查失败: {ex.Message}";
                updateStatusLabel.ForeColor = Color.Red;
            }
            finally
            {
                checkUpdateButton.Enabled = true;
            }
        }

        private Label CreateLabel(string text, int x, int y, int width = 130)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25)
            };
        }

        private TextBox CreateTextBox(int x, int y, int width, string text)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Text = text
            };
        }

        private CheckBox CreateCheckBox(string text, int x, int y, int width, bool checkedValue)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Checked = checkedValue
            };
        }

        private DateTimePicker CreateTimePicker(int x, int y, DateTime value)
        {
            return new DateTimePicker
            {
                Location = new Point(x, y),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = value
            };
        }

        private void AddHelpIcon(Control parent, Control anchor, string text)
        {
            helpToolTip.SetToolTip(anchor, text);
            
            int textWidth = TextRenderer.MeasureText(anchor.Text, anchor.Font).Width;
            int checkboxOffset = anchor is CheckBox ? 18 : 0;
            
            int iconX = anchor.Left + checkboxOffset + textWidth + 4;
            int iconY = anchor.Top + Math.Max(0, (anchor.Height - 12) / 2);
            
            var icon = new PictureBox
            {
                Size = new Size(12, 12),
                Location = new Point(iconX, iconY),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = helpIconImage,
                Cursor = Cursors.Hand
            };
            helpToolTip.SetToolTip(icon, text);
            parent.Controls.Add(icon);
            icon.BringToFront();
        }

        private Image LoadHelpIconImage()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Static", "Icons", "help-question.png");
            if (File.Exists(iconPath))
            {
                return Image.FromFile(iconPath);
            }

            return new Bitmap(16, 16);
        }

        private void ToggleStorageControls()
        {
            testResultPathTextBox.Enabled = saveTestResultCheckBox.Checked;
            browseButton.Enabled = saveTestResultCheckBox.Checked;
            logRetentionDaysInput.Enabled = saveLogsCheckBox.Checked;
        }

        private void ToggleTimeRangeControls()
        {
            startTimePicker.Enabled = enableTimeRangeCheckBox.Checked;
            endTimePicker.Enabled = enableTimeRangeCheckBox.Checked;
            monitorStartTimePicker.Enabled = enableMonitorTimeRangeCheckBox.Checked;
            monitorEndTimePicker.Enabled = enableMonitorTimeRangeCheckBox.Checked;
        }

        private void ToggleAdapterControls()
        {
            bool enabled = enableAdapterAutoManagementCheckBox.Checked;
            managedAdaptersCheckedListBox.Enabled = enabled;
            adapterScheduleGrid.Enabled = enabled;
            showAdapterNotificationCheckBox.Enabled = enabled;
            enableSelectedAdapterButton.Enabled = enabled;
            disableSelectedAdapterButton.Enabled = enabled;
        }

        private void LoadAdapterItems()
        {
            var checkedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < managedAdaptersCheckedListBox.Items.Count; i++)
            {
                if (managedAdaptersCheckedListBox.GetItemChecked(i) && managedAdaptersCheckedListBox.Items[i] is NetworkAdapterInfo checkedAdapter)
                {
                    checkedIds.Add(checkedAdapter.Id);
                }
            }
            if (checkedIds.Count == 0)
            {
                checkedIds = ManagedAdapterIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            availableAdapters.Clear();
            availableAdapters.AddRange(adapterManager.GetPhysicalAdapters());

            managedAdaptersCheckedListBox.Items.Clear();
            foreach (var adapter in availableAdapters)
            {
                int index = managedAdaptersCheckedListBox.Items.Add(adapter);
                managedAdaptersCheckedListBox.SetItemChecked(index, checkedIds.Contains(adapter.Id));
            }
        }

        private void LoadScheduleGrid()
        {
            adapterScheduleGrid.Rows.Clear();
            foreach (var entry in adapterManager.NormalizeSchedule(AdapterSchedule))
            {
                adapterScheduleGrid.Rows.Add(
                    GetDayName(entry.Day),
                    entry.Enabled,
                    entry.AllDay,
                    entry.GetStartTimeOrDefault().ToString(@"hh\:mm"),
                    entry.GetEndTimeOrDefault().ToString(@"hh\:mm"));
            }
        }

        private void ToggleSelectedAdapter(bool enabled)
        {
            if (managedAdaptersCheckedListBox.SelectedItem is not NetworkAdapterInfo adapter)
            {
                MessageBox.Show("请先在网卡列表中选择一个网卡。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = adapterManager.SetAdapterEnabled(adapter.Name, enabled);
            MessageBox.Show(result.Message, result.Success ? "提示" : "失败",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            LoadAdapterItems();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(loginUrlTextBox.Text))
            {
                MessageBox.Show("请输入登录页面 URL", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            if (!TryReadSchedule(out var schedule, out var errorMessage))
            {
                MessageBox.Show(errorMessage, "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoginUrl = loginUrlTextBox.Text.Trim();
            PrimaryDns = primaryDnsTextBox.Text.Trim();
            SecondaryDns = secondaryDnsTextBox.Text.Trim();
            Timeout = (int)timeoutInput.Value;
            Username = usernameTextBox.Text.Trim();
            Password = passwordTextBox.Text;
            ShowNotification = showNotificationCheckBox.Checked;
            ShowTrayNotification = showTrayNotificationCheckBox.Checked;
            ShowRecoveryNotification = showRecoveryNotificationCheckBox.Checked;
            CloseToTrayOnClose = closeBehaviorComboBox.SelectedIndex != 1;
            AutoStart = autoStartCheckBox.Checked;
            AutoStartMonitoring = autoStartMonitoringCheckBox.Checked;
            SilentRunOnAutoStart = silentRunOnAutoStartCheckBox.Checked;
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
            EnableAdapterAutoManagement = enableAdapterAutoManagementCheckBox.Checked;
            ShowAdapterNotification = showAdapterNotificationCheckBox.Checked;
            ManagedAdapterIds = managedAdaptersCheckedListBox.CheckedItems
                .OfType<NetworkAdapterInfo>()
                .Select(item => item.Id)
                .ToList();
            AdapterSchedule = schedule;

            LoginStrategy = loginStrategyComboBox.SelectedIndex switch
            {
                1 => "AlwaysTry",
                2 => "Smart",
                _ => "OnlyWhenDisconnected"
            };
            LoginRetryCount = (int)retryCountInput.Value;
            LoginRetryDelay = (int)retryDelayInput.Value;

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool TryReadSchedule(out List<AdapterScheduleEntry> schedule, out string errorMessage)
        {
            schedule = new List<AdapterScheduleEntry>();
            errorMessage = string.Empty;

            for (int i = 0; i < adapterScheduleGrid.Rows.Count; i++)
            {
                var row = adapterScheduleGrid.Rows[i];
                bool enabled = Convert.ToBoolean(row.Cells["Enabled"].Value ?? false);
                bool allDay = Convert.ToBoolean(row.Cells["AllDay"].Value ?? false);
                string startText = Convert.ToString(row.Cells["StartTime"].Value)?.Trim() ?? "00:00";
                string endText = Convert.ToString(row.Cells["EndTime"].Value)?.Trim() ?? "23:59";

                if (!TimeSpan.TryParse(startText, out var start))
                {
                    errorMessage = $"{GetDayName((DayOfWeek)i)} 的开始时间格式无效，请使用 HH:mm。";
                    return false;
                }

                if (!TimeSpan.TryParse(endText, out var end))
                {
                    errorMessage = $"{GetDayName((DayOfWeek)i)} 的结束时间格式无效，请使用 HH:mm。";
                    return false;
                }

                schedule.Add(new AdapterScheduleEntry
                {
                    Day = (DayOfWeek)i,
                    Enabled = enabled,
                    AllDay = allDay,
                    StartTime = start.ToString(@"hh\:mm\:ss"),
                    EndTime = end.ToString(@"hh\:mm\:ss")
                });
            }

            return true;
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
                    passwordTextBox.Text);

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
            return !string.Equals(usernameTextBox.Text.Trim(), initialUsername, StringComparison.Ordinal)
                || !string.Equals(passwordTextBox.Text, initialPassword, StringComparison.Ordinal);
        }

        private void HandleCredentialChanged()
        {
            if (IsCredentialChanged())
            {
                MarkLoginTestPending("账号或密码已更改，可直接保存，建议先测试");
                return;
            }

            if (loginTestStatusLabel != null)
            {
                loginTestStatusLabel.Text = "登录测试: 账号和密码未变更，可直接保存";
                loginTestStatusLabel.ForeColor = Color.DimGray;
            }
        }

        private void ResetCredentialBaseline()
        {
            initialUsername = usernameTextBox.Text.Trim();
            initialPassword = passwordTextBox.Text;
            HandleCredentialChanged();
        }

        private void MarkLoginTestPending(string reason)
        {
            if (loginTestStatusLabel != null)
            {
                loginTestStatusLabel.Text = $"登录测试: {reason}";
                loginTestStatusLabel.ForeColor = Color.DarkOrange;
            }
        }

        private void MarkLoginTestPassed()
        {
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
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择测试结果保存目录",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(testResultPathTextBox.Text))
            {
                folderDialog.SelectedPath = testResultPathTextBox.Text;
            }

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                testResultPathTextBox.Text = folderDialog.SelectedPath;
            }
        }

        private static string GetDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                DayOfWeek.Sunday => "周日",
                _ => day.ToString()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                helpIconImage?.Dispose();
                helpToolTip.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
