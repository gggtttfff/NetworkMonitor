using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;

namespace NetworkMonitor
{
    public sealed class AdapterOperationResult
    {
        public bool Success { get; set; }
        public bool RequiresElevation { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class AdapterScheduleEvaluation
    {
        public bool ShouldBeEnabled { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class NetworkAdapterManager
    {
        public IReadOnlyList<NetworkAdapterInfo> GetPhysicalAdapters()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(IsSupportedAdapter)
                .Select(adapter => new NetworkAdapterInfo
                {
                    Id = adapter.Id,
                    Name = adapter.Name,
                    Description = adapter.Description,
                    Type = adapter.NetworkInterfaceType.ToString(),
                    IsEnabled = adapter.OperationalStatus != OperationalStatus.Down
                })
                .OrderBy(adapter => adapter.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public AdapterScheduleEvaluation EvaluateSchedule(IEnumerable<AdapterScheduleEntry>? schedule, DateTime now)
        {
            var entry = NormalizeSchedule(schedule).FirstOrDefault(item => item.Day == now.DayOfWeek);
            if (entry == null || !entry.Enabled)
            {
                return new AdapterScheduleEvaluation
                {
                    ShouldBeEnabled = false,
                    Reason = $"{GetDayName(now.DayOfWeek)}未启用"
                };
            }

            if (entry.AllDay)
            {
                return new AdapterScheduleEvaluation
                {
                    ShouldBeEnabled = true,
                    Reason = $"{GetDayName(now.DayOfWeek)}全天启用"
                };
            }

            var current = now.TimeOfDay;
            var start = entry.GetStartTimeOrDefault();
            var end = entry.GetEndTimeOrDefault();
            bool inRange = IsInRange(current, start, end);
            string rangeText = $"{start:hh\\:mm}-{end:hh\\:mm}";
            return new AdapterScheduleEvaluation
            {
                ShouldBeEnabled = inRange,
                Reason = inRange
                    ? $"{GetDayName(now.DayOfWeek)} {rangeText} 启用"
                    : $"{GetDayName(now.DayOfWeek)} 当前不在 {rangeText} 启用时段"
            };
        }

        public AdapterOperationResult SetAdapterEnabled(string adapterName, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(adapterName))
            {
                return new AdapterOperationResult
                {
                    Success = false,
                    Message = "未提供网卡名称"
                };
            }

            string action = enabled ? "启用" : "禁用";
            string command = enabled
                ? $"Enable-NetAdapter -Name '{EscapePowerShell(adapterName)}' -Confirm:$false"
                : $"Disable-NetAdapter -Name '{EscapePowerShell(adapterName)}' -Confirm:$false";

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                        UseShellExecute = true,
                        Verb = IsRunningAsAdministrator() ? string.Empty : "runas",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                process.Start();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return new AdapterOperationResult
                    {
                        Success = true,
                        Message = $"{action}网卡成功: {adapterName}"
                    };
                }

                return new AdapterOperationResult
                {
                    Success = false,
                    Message = $"{action}网卡失败: {adapterName}，退出码 {process.ExitCode}"
                };
            }
            catch (Exception ex)
            {
                bool isElevation = ex is System.ComponentModel.Win32Exception win32Ex && win32Ex.NativeErrorCode == 1223;
                return new AdapterOperationResult
                {
                    Success = false,
                    RequiresElevation = isElevation,
                    Message = isElevation
                        ? $"{action}网卡已取消，未授予管理员权限"
                        : $"{action}网卡失败: {ex.Message}"
                };
            }
        }

        public List<AdapterScheduleEntry> NormalizeSchedule(IEnumerable<AdapterScheduleEntry>? schedule)
        {
            var source = schedule?.ToDictionary(item => item.Day) ?? new Dictionary<DayOfWeek, AdapterScheduleEntry>();
            var result = new List<AdapterScheduleEntry>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (source.TryGetValue(day, out var entry))
                {
                    result.Add(new AdapterScheduleEntry
                    {
                        Day = day,
                        Enabled = entry.Enabled,
                        AllDay = entry.AllDay,
                        StartTime = entry.StartTime,
                        EndTime = entry.EndTime
                    });
                }
                else
                {
                    result.Add(new AdapterScheduleEntry
                    {
                        Day = day,
                        Enabled = false,
                        AllDay = false,
                        StartTime = "00:00:00",
                        EndTime = "23:59:59"
                    });
                }
            }

            return result;
        }

        private static bool IsSupportedAdapter(NetworkInterface adapter)
        {
            return adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                && adapter.NetworkInterfaceType != NetworkInterfaceType.Unknown;
        }

        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool IsInRange(TimeSpan current, TimeSpan start, TimeSpan end)
        {
            if (start <= end)
            {
                return current >= start && current <= end;
            }

            return current >= start || current <= end;
        }

        private static string EscapePowerShell(string value)
        {
            return value.Replace("'", "''", StringComparison.Ordinal);
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
    }
}
