using System;
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
        private CheckBox showNotificationCheckBox = null!;
        private CheckBox showTrayNotificationCheckBox = null!;
        private CheckBox showRecoveryNotificationCheckBox = null!;
        private CheckBox autoStartCheckBox = null!;
        private CheckBox saveTestResultCheckBox = null!;
        private TextBox testResultPathTextBox = null!;
        private Button browseButton = null!;
        private CheckBox enableTimeRangeCheckBox = null!;
        private DateTimePicker startTimePicker = null!;
        private DateTimePicker endTimePicker = null!;
        private Button saveButton = null!;
        private Button cancelButton = null!;

        public string LoginUrl { get; private set; } = "http://2.2.2.2";
        public string PrimaryDns { get; private set; } = "8.8.8.8";
        public string SecondaryDns { get; private set; } = "114.114.114.114";
        public int Timeout { get; private set; } = 10000;
        public bool ShowNotification { get; private set; } = true;
        public bool ShowTrayNotification { get; private set; } = true;
        public bool ShowRecoveryNotification { get; private set; } = true;
        public bool AutoStart { get; private set; } = false;
        public bool SaveTestResult { get; private set; } = false;
        public string TestResultPath { get; private set; } = "test_results";
        public bool EnableTimeRange { get; private set; } = false;
        public TimeSpan StartTime { get; private set; } = new TimeSpan(6, 0, 0);  // 06:00
        public TimeSpan EndTime { get; private set; } = new TimeSpan(23, 0, 0);  // 23:00

        public SettingsForm()
        {
            InitializeComponents();
        }

        public SettingsForm(string loginUrl, string primaryDns, string secondaryDns, int timeout, bool showNotification, bool showTrayNotification, bool showRecoveryNotification, bool autoStart, bool saveTestResult, string testResultPath, bool enableTimeRange, TimeSpan startTime, TimeSpan endTime) : this()
        {
            LoginUrl = loginUrl;
            PrimaryDns = primaryDns;
            SecondaryDns = secondaryDns;
            Timeout = timeout;
            ShowNotification = showNotification;
            ShowTrayNotification = showTrayNotification;
            ShowRecoveryNotification = showRecoveryNotification;
            AutoStart = autoStart;
            SaveTestResult = saveTestResult;
            TestResultPath = testResultPath;
            EnableTimeRange = enableTimeRange;
            StartTime = startTime;
            EndTime = endTime;

            loginUrlTextBox.Text = loginUrl;
            primaryDnsTextBox.Text = primaryDns;
            secondaryDnsTextBox.Text = secondaryDns;
            timeoutInput.Value = timeout;
            showNotificationCheckBox.Checked = showNotification;
            showTrayNotificationCheckBox.Checked = showTrayNotification;
            showRecoveryNotificationCheckBox.Checked = showRecoveryNotification;
            autoStartCheckBox.Checked = autoStart;
            saveTestResultCheckBox.Checked = saveTestResult;
            testResultPathTextBox.Text = testResultPath;
            enableTimeRangeCheckBox.Checked = enableTimeRange;
            startTimePicker.Value = DateTime.Today.Add(startTime);
            endTimePicker.Value = DateTime.Today.Add(endTime);
        }

        private void InitializeComponents()
        {
            this.Text = "设置";
            this.Size = new Size(450, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

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

            // 通知设置区域
            Label notificationLabel = new Label
            {
                Text = "通知设置:",
                Location = new Point(20, 175),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            showNotificationCheckBox = new CheckBox
            {
                Text = "显示弹窗通知",
                Location = new Point(20, 205),
                Size = new Size(380, 25),
                Checked = true
            };

            showTrayNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（断网时）",
                Location = new Point(20, 235),
                Size = new Size(380, 25),
                Checked = true
            };

            showRecoveryNotificationCheckBox = new CheckBox
            {
                Text = "显示托盘气泡通知（恢复时）",
                Location = new Point(20, 265),
                Size = new Size(380, 25),
                Checked = true
            };

            autoStartCheckBox = new CheckBox
            {
                Text = "开机自动启动监控",
                Location = new Point(20, 295),
                Size = new Size(380, 25),
                Checked = false
            };

            // 保存测试结果
            saveTestResultCheckBox = new CheckBox
            {
                Text = "保存测试结果",
                Location = new Point(20, 330),
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
                Location = new Point(20, 370),
                Size = new Size(120, 25)
            };

            testResultPathTextBox = new TextBox
            {
                Location = new Point(150, 370),
                Size = new Size(180, 25),
                Text = "test_results",
                Enabled = false
            };

            browseButton = new Button
            {
                Text = "浏览...",
                Location = new Point(340, 368),
                Size = new Size(70, 28),
                Enabled = false
            };
            browseButton.Click += BrowseButton_Click;

            // 自动连接时间段
            enableTimeRangeCheckBox = new CheckBox
            {
                Text = "启用自动连接时间段",
                Location = new Point(20, 410),
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
                Location = new Point(20, 450),
                Size = new Size(120, 25)
            };

            startTimePicker = new DateTimePicker
            {
                Location = new Point(150, 450),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(6),
                Enabled = false
            };

            Label toLabel = new Label
            {
                Text = "至",
                Location = new Point(260, 450),
                Size = new Size(30, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            endTimePicker = new DateTimePicker
            {
                Location = new Point(300, 450),
                Size = new Size(100, 25),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(23),
                Enabled = false
            };

            Label timeRangeHint = new Label
            {
                Text = "（只在此时间段内自动打开登录页）",
                Location = new Point(40, 480),
                Size = new Size(380, 20),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.Gray
            };

            // 分隔线
            Label separatorLabel = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(20, 520),
                Size = new Size(390, 2)
            };

            // 保存按钮
            saveButton = new Button
            {
                Text = "保存",
                Location = new Point(230, 540),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;

            // 取消按钮
            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(330, 540),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(loginUrlLabel);
            this.Controls.Add(loginUrlTextBox);
            this.Controls.Add(primaryDnsLabel);
            this.Controls.Add(primaryDnsTextBox);
            this.Controls.Add(secondaryDnsLabel);
            this.Controls.Add(secondaryDnsTextBox);
            this.Controls.Add(timeoutLabel);
            this.Controls.Add(timeoutInput);
            this.Controls.Add(notificationLabel);
            this.Controls.Add(showNotificationCheckBox);
            this.Controls.Add(showTrayNotificationCheckBox);
            this.Controls.Add(showRecoveryNotificationCheckBox);
            this.Controls.Add(autoStartCheckBox);
            this.Controls.Add(saveTestResultCheckBox);
            this.Controls.Add(testPathLabel);
            this.Controls.Add(testResultPathTextBox);
            this.Controls.Add(browseButton);
            this.Controls.Add(enableTimeRangeCheckBox);
            this.Controls.Add(timeRangeLabel);
            this.Controls.Add(startTimePicker);
            this.Controls.Add(toLabel);
            this.Controls.Add(endTimePicker);
            this.Controls.Add(timeRangeHint);
            this.Controls.Add(separatorLabel);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
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

            LoginUrl = loginUrlTextBox.Text.Trim();
            PrimaryDns = primaryDnsTextBox.Text.Trim();
            SecondaryDns = secondaryDnsTextBox.Text.Trim();
            Timeout = (int)timeoutInput.Value;
            ShowNotification = showNotificationCheckBox.Checked;
            ShowTrayNotification = showTrayNotificationCheckBox.Checked;
            ShowRecoveryNotification = showRecoveryNotificationCheckBox.Checked;
            AutoStart = autoStartCheckBox.Checked;
            SaveTestResult = saveTestResultCheckBox.Checked;
            TestResultPath = testResultPathTextBox.Text.Trim();
            EnableTimeRange = enableTimeRangeCheckBox.Checked;
            StartTime = startTimePicker.Value.TimeOfDay;
            EndTime = endTimePicker.Value.TimeOfDay;
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
