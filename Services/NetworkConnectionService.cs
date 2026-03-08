using System;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class NetworkConnectionService
    {
        private static readonly HttpClient SharedHttpClient = CreateDirectHttpClient();

        private readonly Action<string> _log;
        private readonly Func<string, Exception, Task>? _logNetworkError;
        private readonly Func<string, Task>? _logWarning;

        public NetworkConnectionService(
            Action<string> log,
            Func<string, Exception, Task>? logNetworkError = null,
            Func<string, Task>? logWarning = null)
        {
            _log = log;
            _logNetworkError = logNetworkError;
            _logWarning = logWarning;
        }

        public async Task<NetworkCheckResult> CheckConnectionAsync(NetworkCheckOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                _log("检查网络连接...");

                // 快速并行探测，降低串行等待带来的检测延迟
                var pingResult = await ProbeInternetWithPingAsync(options, cancellationToken);
                if (pingResult.IsConnected)
                {
                    return pingResult;
                }

                _log("Ping测试失败，检查是否需要认证...");

                try
                {
                    _log($"检查认证网关 {options.LoginUrl}...");

                    using var request = new HttpRequestMessage(HttpMethod.Get, options.LoginUrl);
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(3));
                    using var gatewayResponse = await SharedHttpClient.SendAsync(request, cts.Token);
                    var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync();

                    bool isAuthenticated = gatewayContent.Contains("认证成功") ||
                                           gatewayContent.Contains("您已经成功登录") ||
                                           gatewayContent.Contains("disconnconfig") ||
                                           gatewayContent.Contains("连接网络") ||
                                           gatewayContent.Contains("您可以关闭该页面");

                    if (isAuthenticated)
                    {
                        _log("检测到已认证页面，但网络不通");
                    }
                    else if (gatewayContent.Contains("login") || gatewayContent.Contains("认证") ||
                             gatewayContent.Contains("用户名") || gatewayContent.Contains("密码"))
                    {
                        _log("检测到认证页面，需要登录");
                    }
                    else
                    {
                        _log("无法确定认证状态");
                    }

                    return new NetworkCheckResult { IsConnected = false };
                }
                catch (HttpRequestException ex)
                {
                    _log($"无法连接到认证网关: {ex.Message}");
                    _log("可能网线未插或网络故障");

                    if (_logNetworkError != null)
                    {
                        await _logNetworkError($"无法连接到认证网关 {options.LoginUrl}", ex);
                    }

                    return new NetworkCheckResult
                    {
                        IsConnected = false,
                        GatewayUnreachable = true
                    };
                }
                catch (TaskCanceledException)
                {
                    _log("连接认证网关超时");
                    return new NetworkCheckResult { IsConnected = false };
                }
            }
            catch (Exception ex)
            {
                _log($"网络检测异常: {ex.Message}");

                if (_logNetworkError != null)
                {
                    await _logNetworkError("网络连接检测失败", ex);
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var warnings = NetworkDiagnostics.DetectAbnormalProcesses();
                        if (warnings.Any(w => w.WarningLevel == "Critical"))
                        {
                            var critical = warnings.First(w => w.WarningLevel == "Critical");
                            if (_logWarning != null)
                            {
                                await _logWarning($"检测到异常进程 {critical.ProcessName} (句柄数: {critical.HandleCount:N0})，可能影响网络连接");
                            }
                        }
                    }
                    catch
                    {
                    }
                });

                return new NetworkCheckResult { IsConnected = false };
            }
        }

        private static HttpClient CreateDirectHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                Proxy = null
            };
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        private async Task<NetworkCheckResult> ProbeInternetWithPingAsync(NetworkCheckOptions options, CancellationToken cancellationToken)
        {
            int timeout = Math.Min(Math.Max(options.PingTimeout, 800), 2500);
            var targets = new[]
            {
                options.PrimaryDns,
                options.SecondaryDns,
                "223.5.5.5",
                "180.76.76.76"
            }
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToArray();

            var tasks = targets.Select(t => PingOneAsync(t!, timeout, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);

            foreach (var r in results)
            {
                if (r.Success && r.Roundtrip > 0)
                {
                    _log($"Ping {r.Target} 成功，延迟: {r.Roundtrip}ms");
                    return new NetworkCheckResult { IsConnected = true };
                }
            }

            foreach (var r in results.Where(x => x.Success && x.Roundtrip == 0))
            {
                _log($"⚠️ Ping {r.Target} 返回0ms延迟(异常)，将继续按断网流程处理");
            }

            foreach (var r in results.Where(x => !x.Success))
            {
                _log($"Ping {r.Target} 失败: {r.Error}");
            }

            return new NetworkCheckResult { IsConnected = false };
        }

        private static async Task<(string Target, bool Success, long Roundtrip, string Error)> PingOneAsync(string target, int timeout, CancellationToken cancellationToken)
        {
            try
            {
                using var ping = new Ping();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                var reply = await ping.SendPingAsync(target, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    return (target, true, reply.RoundtripTime, string.Empty);
                }

                return (target, false, 0, reply.Status.ToString());
            }
            catch (Exception ex)
            {
                return (target, false, 0, ex.Message);
            }
        }
    }
}
