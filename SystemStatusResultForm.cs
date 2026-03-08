using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NetworkMonitor
{
    public class SystemStatusResultForm : Form
    {
        private readonly NetworkDiagnostics.DiagnosticReport _report;

        public SystemStatusResultForm(NetworkDiagnostics.DiagnosticReport report)
        {
            _report = report;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "系统网络状态";
            Width = 900;
            Height = 680;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            Icon = AppIconProvider.GetIcon();

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            tabControl.TabPages.Add(CreateTextTab("概览", BuildSummaryText()));

            for (int i = 0; i < _report.Adapters.Count; i++)
            {
                var adapter = _report.Adapters[i];
                string tabTitle = $"适配器{i + 1}";
                tabControl.TabPages.Add(CreateTextTab(tabTitle, BuildAdapterText(adapter)));
            }

            for (int i = 0; i < _report.GatewayTests.Count; i++)
            {
                var gateway = _report.GatewayTests[i];
                string tabTitle = $"网关{i + 1}";
                tabControl.TabPages.Add(CreateTextTab(tabTitle, BuildGatewayText(gateway)));
            }

            tabControl.TabPages.Add(CreateTextTab("TCP统计", BuildTcpText()));
            tabControl.TabPages.Add(CreateTextTab("Top进程", BuildProcessesText()));
            tabControl.TabPages.Add(CreateTextTab("异常进程", BuildWarningsText()));

            Controls.Add(tabControl);
        }

        private TabPage CreateTextTab(string title, string content)
        {
            var tab = new TabPage(title);
            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Consolas", 10f),
                Text = content
            };
            tab.Controls.Add(box);
            return tab;
        }

        private string BuildSummaryText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("系统网络诊断结果");
            sb.AppendLine("==============================");
            sb.AppendLine($"生成时间: {_report.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"网络可用: {(_report.IsNetworkAvailable ? "是" : "否")}");
            sb.AppendLine($"网络适配器数量: {_report.Adapters.Count}");
            sb.AppendLine($"网关测试数量: {_report.GatewayTests.Count}");
            sb.AppendLine($"异常进程数量: {_report.ProcessWarnings.Count}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(_report.ErrorMessage))
            {
                sb.AppendLine("错误信息:");
                sb.AppendLine(_report.ErrorMessage);
                sb.AppendLine();
            }

            sb.AppendLine("说明:");
            sb.AppendLine("- 每个标签页对应一个测试结果或结果分组");
            sb.AppendLine("- 适配器和网关测试按序号拆分为独立标签页");
            return sb.ToString();
        }

        private string BuildAdapterText(NetworkDiagnostics.AdapterInfo adapter)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"名称: {adapter.Name}");
            sb.AppendLine($"描述: {adapter.Description}");
            sb.AppendLine($"类型: {adapter.Type}");
            sb.AppendLine($"状态: {adapter.Status}");
            if (adapter.Speed > 0)
            {
                sb.AppendLine($"速度: {adapter.Speed / 1000000} Mbps");
            }
            sb.AppendLine();

            sb.AppendLine("IPv4:");
            if (adapter.IPv4Addresses.Any())
            {
                foreach (var ip in adapter.IPv4Addresses) sb.AppendLine($"- {ip}");
            }
            else
            {
                sb.AppendLine("- 无");
            }
            sb.AppendLine();

            sb.AppendLine("IPv6:");
            if (adapter.IPv6Addresses.Any())
            {
                foreach (var ip in adapter.IPv6Addresses) sb.AppendLine($"- {ip}");
            }
            else
            {
                sb.AppendLine("- 无");
            }
            sb.AppendLine();

            sb.AppendLine("网关:");
            if (adapter.Gateways.Any())
            {
                foreach (var gw in adapter.Gateways) sb.AppendLine($"- {gw}");
            }
            else
            {
                sb.AppendLine("- 无");
            }
            sb.AppendLine();

            sb.AppendLine("DNS:");
            if (adapter.DnsServers.Any())
            {
                foreach (var dns in adapter.DnsServers) sb.AppendLine($"- {dns}");
            }
            else
            {
                sb.AppendLine("- 无");
            }
            sb.AppendLine();

            sb.AppendLine("统计:");
            sb.AppendLine($"接收字节: {adapter.BytesReceived:N0}");
            sb.AppendLine($"发送字节: {adapter.BytesSent:N0}");
            sb.AppendLine($"接收包: {adapter.PacketsReceived:N0}");
            sb.AppendLine($"发送包: {adapter.PacketsSent:N0}");
            sb.AppendLine($"丢弃包: {adapter.PacketsDiscarded:N0}");
            sb.AppendLine($"错误包: {adapter.PacketsWithErrors:N0}");
            return sb.ToString();
        }

        private string BuildGatewayText(NetworkDiagnostics.GatewayPingResult gateway)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"网关: {gateway.Gateway}");
            sb.AppendLine($"是否成功: {(gateway.Success ? "是" : "否")}");
            sb.AppendLine($"状态码: {gateway.Status}");
            sb.AppendLine($"延迟: {gateway.RoundtripTime} ms");
            if (!string.IsNullOrWhiteSpace(gateway.ErrorMessage))
            {
                sb.AppendLine($"错误: {gateway.ErrorMessage}");
            }
            return sb.ToString();
        }

        private string BuildTcpText()
        {
            var tcp = _report.TcpStats;
            var sb = new StringBuilder();
            sb.AppendLine($"总连接数: {tcp.TotalConnections}");
            sb.AppendLine($"已建立: {tcp.Established}");
            sb.AppendLine($"TIME_WAIT: {tcp.TimeWait}");
            sb.AppendLine($"CLOSE_WAIT: {tcp.CloseWait}");
            sb.AppendLine($"LISTEN: {tcp.Listen}");
            sb.AppendLine($"其他: {tcp.Other}");
            return sb.ToString();
        }

        private string BuildProcessesText()
        {
            var sb = new StringBuilder();
            if (!_report.TopProcesses.Any())
            {
                sb.AppendLine("无进程数据");
                return sb.ToString();
            }

            foreach (var p in _report.TopProcesses)
            {
                sb.AppendLine($"{p.ProcessName} (PID: {p.ProcessId})");
                sb.AppendLine($"- 句柄数: {p.HandleCount:N0}");
                sb.AppendLine($"- 内存: {p.WorkingSet / 1024 / 1024:N0} MB");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string BuildWarningsText()
        {
            var sb = new StringBuilder();
            if (!_report.ProcessWarnings.Any())
            {
                sb.AppendLine("未检测到异常进程");
                return sb.ToString();
            }

            foreach (var w in _report.ProcessWarnings)
            {
                sb.AppendLine($"[{w.WarningLevel}] {w.ProcessName} (PID: {w.ProcessId})");
                sb.AppendLine($"- 句柄数: {w.HandleCount:N0}");
                sb.AppendLine($"- 内存: {w.WorkingSet / 1024 / 1024:N0} MB");
                sb.AppendLine($"- 描述: {w.Description}");
                if (w.Suggestions.Any())
                {
                    sb.AppendLine("- 建议:");
                    foreach (var s in w.Suggestions)
                    {
                        sb.AppendLine($"  * {s}");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
