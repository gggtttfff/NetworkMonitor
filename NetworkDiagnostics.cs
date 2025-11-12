using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    /// <summary>
    /// 网络诊断工具类，收集系统网络状态信息
    /// </summary>
    public class NetworkDiagnostics
    {
        /// <summary>
        /// 网络适配器诊断信息
        /// </summary>
        public class AdapterInfo
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public NetworkInterfaceType Type { get; set; }
            public OperationalStatus Status { get; set; }
            public long Speed { get; set; }
            public List<string> IPv4Addresses { get; set; } = new List<string>();
            public List<string> IPv6Addresses { get; set; } = new List<string>();
            public List<string> Gateways { get; set; } = new List<string>();
            public List<string> DnsServers { get; set; } = new List<string>();
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public long PacketsReceived { get; set; }
            public long PacketsSent { get; set; }
            public long PacketsDiscarded { get; set; }
            public long PacketsWithErrors { get; set; }
        }

        /// <summary>
        /// 网关连通性信息
        /// </summary>
        public class GatewayPingResult
        {
            public string Gateway { get; set; } = "";
            public bool Success { get; set; }
            public long RoundtripTime { get; set; }
            public IPStatus Status { get; set; }
            public string ErrorMessage { get; set; } = "";
        }

        /// <summary>
        /// TCP连接统计信息
        /// </summary>
        public class TcpConnectionStats
        {
            public int TotalConnections { get; set; }
            public int Established { get; set; }
            public int TimeWait { get; set; }
            public int CloseWait { get; set; }
            public int Listen { get; set; }
            public int Other { get; set; }
        }

        /// <summary>
        /// 进程资源信息
        /// </summary>
        public class ProcessResourceInfo
        {
            public string ProcessName { get; set; } = "";
            public int ProcessId { get; set; }
            public int HandleCount { get; set; }
            public long WorkingSet { get; set; }
        }

        /// <summary>
        /// 异常进程警告信息
        /// </summary>
        public class AbnormalProcessWarning
        {
            public string ProcessName { get; set; } = "";
            public int ProcessId { get; set; }
            public int HandleCount { get; set; }
            public long WorkingSet { get; set; }
            public string WarningLevel { get; set; } = ""; // Critical, High, Medium
            public string Description { get; set; } = "";
            public List<string> Suggestions { get; set; } = new List<string>();
        }

        /// <summary>
        /// 完整的诊断报告
        /// </summary>
        public class DiagnosticReport
        {
            public DateTime Timestamp { get; set; }
            public List<AdapterInfo> Adapters { get; set; } = new List<AdapterInfo>();
            public List<GatewayPingResult> GatewayTests { get; set; } = new List<GatewayPingResult>();
            public TcpConnectionStats TcpStats { get; set; } = new TcpConnectionStats();
            public List<ProcessResourceInfo> TopProcesses { get; set; } = new List<ProcessResourceInfo>();
            public List<AbnormalProcessWarning> ProcessWarnings { get; set; } = new List<AbnormalProcessWarning>();
            public bool IsNetworkAvailable { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// 收集所有网络适配器信息
        /// </summary>
        public static List<AdapterInfo> GetNetworkAdapters()
        {
            var adapters = new List<AdapterInfo>();

            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface adapter in interfaces)
                {
                    // 跳过环回和非活动接口
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var adapterInfo = new AdapterInfo
                    {
                        Name = adapter.Name,
                        Description = adapter.Description,
                        Type = adapter.NetworkInterfaceType,
                        Status = adapter.OperationalStatus,
                        Speed = adapter.Speed
                    };

                    // 只对活动接口获取详细信息
                    if (adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        try
                        {
                            IPInterfaceProperties ipProps = adapter.GetIPProperties();

                            // 获取IPv4地址
                            var ipv4Addrs = ipProps.UnicastAddresses
                                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                                .Select(addr => $"{addr.Address} / {addr.IPv4Mask}")
                                .ToList();
                            adapterInfo.IPv4Addresses.AddRange(ipv4Addrs);

                            // 获取IPv6地址
                            var ipv6Addrs = ipProps.UnicastAddresses
                                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                .Select(addr => addr.Address.ToString())
                                .ToList();
                            adapterInfo.IPv6Addresses.AddRange(ipv6Addrs);

                            // 获取网关
                            var gateways = ipProps.GatewayAddresses
                                .Select(gw => gw.Address.ToString())
                                .ToList();
                            adapterInfo.Gateways.AddRange(gateways);

                            // 获取DNS服务器
                            var dnsServers = ipProps.DnsAddresses
                                .Select(dns => dns.ToString())
                                .ToList();
                            adapterInfo.DnsServers.AddRange(dnsServers);

                            // 获取统计信息
                            IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                            adapterInfo.BytesReceived = stats.BytesReceived;
                            adapterInfo.BytesSent = stats.BytesSent;
                            adapterInfo.PacketsReceived = stats.UnicastPacketsReceived;
                            adapterInfo.PacketsSent = stats.UnicastPacketsSent;
                            adapterInfo.PacketsDiscarded = stats.IncomingPacketsDiscarded + stats.OutgoingPacketsDiscarded;
                            adapterInfo.PacketsWithErrors = stats.IncomingPacketsWithErrors + stats.OutgoingPacketsWithErrors;
                        }
                        catch
                        {
                            // 某些适配器可能无法获取详细信息，跳过
                        }
                    }

                    adapters.Add(adapterInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取网络适配器信息失败: {ex.Message}");
            }

            return adapters;
        }

        /// <summary>
        /// 测试网关连通性
        /// </summary>
        public static async Task<List<GatewayPingResult>> TestGatewayConnectivityAsync(int timeout = 3000)
        {
            var results = new List<GatewayPingResult>();

            try
            {
                // 收集所有网关地址
                var gateways = new HashSet<string>();
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface adapter in interfaces)
                {
                    if (adapter.OperationalStatus == OperationalStatus.Up &&
                        adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        try
                        {
                            IPInterfaceProperties ipProps = adapter.GetIPProperties();
                            foreach (var gateway in ipProps.GatewayAddresses)
                            {
                                gateways.Add(gateway.Address.ToString());
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                    }
                }

                // Ping每个网关
                using (var ping = new Ping())
                {
                    foreach (var gateway in gateways)
                    {
                        var result = new GatewayPingResult { Gateway = gateway };

                        try
                        {
                            PingReply reply = await ping.SendPingAsync(gateway, timeout);
                            result.Success = (reply.Status == IPStatus.Success);
                            result.Status = reply.Status;
                            result.RoundtripTime = reply.RoundtripTime;
                        }
                        catch (Exception ex)
                        {
                            result.Success = false;
                            result.ErrorMessage = ex.Message;
                        }

                        results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"测试网关连通性失败: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 获取TCP连接统计信息
        /// </summary>
        public static TcpConnectionStats GetTcpConnectionStats()
        {
            var stats = new TcpConnectionStats();

            try
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] connections = ipProperties.GetActiveTcpConnections();

                stats.TotalConnections = connections.Length;

                foreach (var conn in connections)
                {
                    switch (conn.State)
                    {
                        case TcpState.Established:
                            stats.Established++;
                            break;
                        case TcpState.TimeWait:
                            stats.TimeWait++;
                            break;
                        case TcpState.CloseWait:
                            stats.CloseWait++;
                            break;
                        case TcpState.Listen:
                            stats.Listen++;
                            break;
                        default:
                            stats.Other++;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取TCP连接统计信息失败: {ex.Message}");
            }

            return stats;
        }

        /// <summary>
        /// 获取占用资源最多的进程（按句柄数排序）
        /// </summary>
        public static List<ProcessResourceInfo> GetTopProcessesByHandles(int topCount = 10)
        {
            var processes = new List<ProcessResourceInfo>();

            try
            {
                Process[] allProcesses = Process.GetProcesses();

                var processInfos = allProcesses
                    .Select(p =>
                    {
                        try
                        {
                            return new ProcessResourceInfo
                            {
                                ProcessName = p.ProcessName,
                                ProcessId = p.Id,
                                HandleCount = p.HandleCount,
                                WorkingSet = p.WorkingSet64
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(p => p != null)
                    .OrderByDescending(p => p!.HandleCount)
                    .Take(topCount)
                    .ToList();

                processes.AddRange(processInfos!);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取进程信息失败: {ex.Message}");
            }

            return processes;
        }

        /// <summary>
        /// 检测异常进程（高句柄占用、可能的资源泄漏）
        /// </summary>
        public static List<AbnormalProcessWarning> DetectAbnormalProcesses()
        {
            var warnings = new List<AbnormalProcessWarning>();

            try
            {
                Process[] allProcesses = Process.GetProcesses();

                foreach (Process process in allProcesses)
                {
                    try
                    {
                        int handleCount = process.HandleCount;
                        long workingSet = process.WorkingSet64;
                        string processName = process.ProcessName;

                        AbnormalProcessWarning? warning = null;

                        // 检测极高句柄数（可能是资源泄漏）
                        if (handleCount > 100000) // 10万+句柄
                        {
                            warning = new AbnormalProcessWarning
                            {
                                ProcessName = processName,
                                ProcessId = process.Id,
                                HandleCount = handleCount,
                                WorkingSet = workingSet,
                                WarningLevel = "Critical",
                                Description = $"进程占用了异常高的句柄数量 ({handleCount:N0})，可能存在严重的资源泄漏"
                            };

                            if (handleCount > 500000) // 50万+
                            {
                                warning.Suggestions.Add("立即关闭此进程以释放系统资源");
                                warning.Suggestions.Add("检查此进程是否在网络中断时持续重试连接");
                                warning.Suggestions.Add("考虑重启计算机以完全释放资源");
                                warning.Suggestions.Add("联系软件厂商报告此资源泄漏问题");
                            }
                            else
                            {
                                warning.Suggestions.Add("考虑重启此进程");
                                warning.Suggestions.Add("监控进程行为，检查是否有异常网络活动");
                                warning.Suggestions.Add("如果问题持续，考虑卸载或更新此软件");
                            }
                        }
                        else if (handleCount > 50000) // 5万+句柄
                        {
                            warning = new AbnormalProcessWarning
                            {
                                ProcessName = processName,
                                ProcessId = process.Id,
                                HandleCount = handleCount,
                                WorkingSet = workingSet,
                                WarningLevel = "High",
                                Description = $"进程占用了较高的句柄数量 ({handleCount:N0})，需要关注"
                            };
                            warning.Suggestions.Add("监控此进程的句柄使用情况");
                            warning.Suggestions.Add("检查是否因为网络问题导致连接堆积");
                            warning.Suggestions.Add("如果句柄数持续增长，考虑重启进程");
                        }
                        else if (handleCount > 20000) // 2万+句柄
                        {
                            warning = new AbnormalProcessWarning
                            {
                                ProcessName = processName,
                                ProcessId = process.Id,
                                HandleCount = handleCount,
                                WorkingSet = workingSet,
                                WarningLevel = "Medium",
                                Description = $"进程占用了中等偏高的句柄数量 ({handleCount:N0})"
                            };
                            warning.Suggestions.Add("定期监控此进程的资源使用");
                            warning.Suggestions.Add("注意是否与网络连接问题相关");
                        }

                        if (warning != null)
                        {
                            warnings.Add(warning);
                        }
                    }
                    catch
                    {
                        // 跳过无法访问的进程
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检测异常进程失败: {ex.Message}");
            }

            // 按句柄数排序
            return warnings.OrderByDescending(w => w.HandleCount).ToList();
        }

        /// <summary>
        /// 生成完整的诊断报告
        /// </summary>
        public static async Task<DiagnosticReport> GenerateFullReportAsync()
        {
            var report = new DiagnosticReport
            {
                Timestamp = DateTime.Now
            };

            try
            {
                // 收集网络适配器信息
                report.Adapters = GetNetworkAdapters();

                // 测试网关连通性
                report.GatewayTests = await TestGatewayConnectivityAsync();

                // 获取TCP连接统计
                report.TcpStats = GetTcpConnectionStats();

                // 获取进程资源信息
                report.TopProcesses = GetTopProcessesByHandles();

                // 检测异常进程
                report.ProcessWarnings = DetectAbnormalProcesses();

                // 检查网络可用性
                report.IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                report.ErrorMessage = $"生成诊断报告失败: {ex.Message}";
                Debug.WriteLine(report.ErrorMessage);
            }

            return report;
        }

        /// <summary>
        /// 将诊断报告格式化为字符串
        /// </summary>
        public static string FormatReport(DiagnosticReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"========== 网络诊断报告 ==========");
            sb.AppendLine($"生成时间: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"网络可用: {(report.IsNetworkAvailable ? "是" : "否")}");
            sb.AppendLine();

            // 网络适配器信息
            sb.AppendLine("===== 网络适配器 =====");
            foreach (var adapter in report.Adapters)
            {
                sb.AppendLine($"\n[{adapter.Name}]");
                sb.AppendLine($"  描述: {adapter.Description}");
                sb.AppendLine($"  类型: {adapter.Type}");
                sb.AppendLine($"  状态: {adapter.Status}");
                if (adapter.Speed > 0)
                    sb.AppendLine($"  速度: {adapter.Speed / 1000000} Mbps");

                if (adapter.IPv4Addresses.Any())
                {
                    sb.AppendLine("  IPv4地址:");
                    foreach (var ip in adapter.IPv4Addresses)
                        sb.AppendLine($"    - {ip}");
                }

                if (adapter.Gateways.Any())
                {
                    sb.AppendLine("  网关:");
                    foreach (var gw in adapter.Gateways)
                        sb.AppendLine($"    - {gw}");
                }

                if (adapter.DnsServers.Any())
                {
                    sb.AppendLine("  DNS服务器:");
                    foreach (var dns in adapter.DnsServers)
                        sb.AppendLine($"    - {dns}");
                }

                if (adapter.Status == OperationalStatus.Up)
                {
                    sb.AppendLine($"  统计:");
                    sb.AppendLine($"    接收: {adapter.BytesReceived:N0} 字节 ({adapter.PacketsReceived:N0} 包)");
                    sb.AppendLine($"    发送: {adapter.BytesSent:N0} 字节 ({adapter.PacketsSent:N0} 包)");
                    sb.AppendLine($"    丢弃: {adapter.PacketsDiscarded:N0} 包");
                    sb.AppendLine($"    错误: {adapter.PacketsWithErrors:N0} 包");
                }
            }

            // 网关测试结果
            if (report.GatewayTests.Any())
            {
                sb.AppendLine("\n===== 网关连通性测试 =====");
                foreach (var test in report.GatewayTests)
                {
                    if (test.Success)
                    {
                        sb.AppendLine($"  {test.Gateway}: 成功 ({test.RoundtripTime}ms)");
                    }
                    else
                    {
                        sb.AppendLine($"  {test.Gateway}: 失败 ({test.Status})");
                        if (!string.IsNullOrEmpty(test.ErrorMessage))
                            sb.AppendLine($"    错误: {test.ErrorMessage}");
                    }
                }
            }

            // TCP连接统计
            sb.AppendLine("\n===== TCP连接统计 =====");
            sb.AppendLine($"  总连接数: {report.TcpStats.TotalConnections}");
            sb.AppendLine($"  已建立: {report.TcpStats.Established}");
            sb.AppendLine($"  等待关闭: {report.TcpStats.TimeWait}");
            sb.AppendLine($"  等待远程关闭: {report.TcpStats.CloseWait}");
            sb.AppendLine($"  监听: {report.TcpStats.Listen}");
            sb.AppendLine($"  其他: {report.TcpStats.Other}");

            // 异常进程警告
            if (report.ProcessWarnings.Any())
            {
                sb.AppendLine("\n⚠️  ===== 异常进程警告 =====");
                foreach (var warning in report.ProcessWarnings)
                {
                    string levelIcon = warning.WarningLevel switch
                    {
                        "Critical" => "🔴",
                        "High" => "🟠",
                        "Medium" => "🟡",
                        _ => "ℹ️"
                    };

                    sb.AppendLine($"\n  {levelIcon} 【{warning.WarningLevel.ToUpper()}】{warning.ProcessName} (PID: {warning.ProcessId})");
                    sb.AppendLine($"    句柄数: {warning.HandleCount:N0}");
                    sb.AppendLine($"    内存: {warning.WorkingSet / 1024 / 1024:N0} MB");
                    sb.AppendLine($"    问题: {warning.Description}");
                    
                    if (warning.Suggestions.Any())
                    {
                        sb.AppendLine($"    建议:");
                        foreach (var suggestion in warning.Suggestions)
                        {
                            sb.AppendLine($"      • {suggestion}");
                        }
                    }
                }
            }

            // 进程资源信息
            if (report.TopProcesses.Any())
            {
                sb.AppendLine("\n===== 占用句柄最多的进程 (Top 10) =====");
                foreach (var proc in report.TopProcesses)
                {
                    sb.AppendLine($"  {proc.ProcessName} (PID: {proc.ProcessId})");
                    sb.AppendLine($"    句柄数: {proc.HandleCount:N0}");
                    sb.AppendLine($"    内存: {proc.WorkingSet / 1024 / 1024:N0} MB");
                }
            }

            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                sb.AppendLine($"\n错误: {report.ErrorMessage}");
            }

            sb.AppendLine("\n========== 报告结束 ==========");

            return sb.ToString();
        }
    }
}
