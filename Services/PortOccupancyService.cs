using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace NetworkMonitor
{
    public sealed class PortOccupancyService
    {
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        public IReadOnlyList<PortOccupancyEntry> QueryAll()
        {
            var entries = new List<PortOccupancyEntry>();
            entries.AddRange(QueryProtocol("tcp"));
            entries.AddRange(QueryProtocol("udp"));

            return entries
                .OrderBy(entry => entry.Port)
                .ThenBy(entry => entry.Protocol, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.ProcessId)
                .ToList();
        }

        public IReadOnlyList<PortOccupancyEntry> QueryByPort(int port)
        {
            return QueryAll()
                .Where(entry => entry.Port == port)
                .ToList();
        }

        public ProcessTerminationResult KillProcessTreeElevated(int processId)
        {
            if (processId <= 0)
            {
                return new ProcessTerminationResult
                {
                    Success = false,
                    Message = "PID 无效"
                };
            }

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill.exe",
                        Arguments = $"/PID {processId} /T /F",
                        UseShellExecute = true,
                        Verb = IsRunningAsAdministrator() ? string.Empty : "runas",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0
                    ? new ProcessTerminationResult
                    {
                        Success = true,
                        Message = $"已结束 PID {processId} 的进程树"
                    }
                    : new ProcessTerminationResult
                    {
                        Success = false,
                        Message = $"结束 PID {processId} 失败，退出码 {process.ExitCode}"
                    };
            }
            catch (Exception ex)
            {
                bool isCanceled = ex is System.ComponentModel.Win32Exception win32Ex && win32Ex.NativeErrorCode == 1223;
                return new ProcessTerminationResult
                {
                    Success = false,
                    RequiresElevation = isCanceled,
                    UserCanceledElevation = isCanceled,
                    Message = isCanceled
                        ? "用户取消了管理员权限请求"
                        : $"结束 PID {processId} 失败: {ex.Message}"
                };
            }
        }

        private IReadOnlyList<PortOccupancyEntry> QueryProtocol(string protocol)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = $"-ano -p {protocol}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var entries = new List<PortOccupancyEntry>();
            foreach (string rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith("UDP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var entry = ParseNetstatLine(line);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private PortOccupancyEntry? ParseNetstatLine(string line)
        {
            string[] parts = WhitespaceRegex.Split(line);
            if (parts.Length < 4)
            {
                return null;
            }

            string protocol = parts[0];
            string localEndpoint = parts[1];
            string state = string.Empty;
            string pidText;

            if (protocol.Equals("TCP", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 5)
                {
                    return null;
                }

                state = parts[3];
                pidText = parts[4];
            }
            else
            {
                pidText = parts[3];
            }

            if (!int.TryParse(pidText, out int pid))
            {
                return null;
            }

            if (!TryParseEndpoint(localEndpoint, out string localAddress, out int port))
            {
                return null;
            }

            ResolveProcessInfo(pid, out string processName, out string executablePath);
            return new PortOccupancyEntry
            {
                Protocol = protocol,
                LocalAddress = localAddress,
                Port = port,
                State = state,
                ProcessId = pid,
                ProcessName = processName,
                ExecutablePath = executablePath
            };
        }

        private static bool TryParseEndpoint(string endpoint, out string address, out int port)
        {
            address = endpoint;
            port = 0;

            int separatorIndex = endpoint.LastIndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= endpoint.Length - 1)
            {
                return false;
            }

            string rawAddress = endpoint.Substring(0, separatorIndex).Trim('[', ']');
            string rawPort = endpoint.Substring(separatorIndex + 1);
            if (!int.TryParse(rawPort, out port))
            {
                return false;
            }

            address = rawAddress;
            return true;
        }

        private static void ResolveProcessInfo(int pid, out string processName, out string executablePath)
        {
            processName = "未知";
            executablePath = string.Empty;

            try
            {
                using var process = Process.GetProcessById(pid);
                processName = process.ProcessName;
                try
                {
                    executablePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    executablePath = string.Empty;
                }
            }
            catch
            {
                processName = "已退出";
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
