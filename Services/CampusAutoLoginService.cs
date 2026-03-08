using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class CampusAutoLoginService
    {
        private readonly Action<string> _log;
        private readonly Func<string, Exception, Task>? _logNetworkError;

        public CampusAutoLoginService(
            Action<string> log,
            Func<string, Exception, Task>? logNetworkError = null)
        {
            _log = log;
            _logNetworkError = logNetworkError;
        }

        public async Task<AutoLoginResult> AuthenticateWithRetryAsync(
            AutoLoginOptions options,
            CancellationToken token,
            Func<bool> shouldContinue)
        {
            int attemptCount = 0;
            int maxAttempts = options.RetryCount + 1;
            bool success = false;
            string lastErrorMessage = "";

            _log($"开始自动登录校园网（最多尝试{maxAttempts}次）...");

            while (attemptCount < maxAttempts && !success && !token.IsCancellationRequested && shouldContinue())
            {
                attemptCount++;

                if (attemptCount > 1)
                {
                    _log($"\n第{attemptCount}次尝试登录...");
                }

                try
                {
                    if (token.IsCancellationRequested || !shouldContinue())
                    {
                        _log("登录任务已取消");
                        return new AutoLoginResult
                        {
                            Canceled = true,
                            AttemptCount = attemptCount,
                            LastErrorMessage = lastErrorMessage
                        };
                    }

                    var authenticator = new CampusNetworkAuthenticator(
                        options.LoginUrl,
                        options.Username,
                        options.Password
                    );

                    authenticator.LogMessage += _log;
                    authenticator.OnNetworkError += (ex) =>
                    {
                        if (_logNetworkError != null)
                        {
                            _ = _logNetworkError("自动登录时网络错误", ex);
                        }
                    };

                    var result = await authenticator.AuthenticateAsync();

                    if (result.Success)
                    {
                        success = true;
                        _log($"✓ 自动登录成功！（第{attemptCount}次尝试）");
                    }
                    else
                    {
                        lastErrorMessage = result.Message;
                        _log($"✗ 第{attemptCount}次登录失败: {result.Message}");

                        if (attemptCount < maxAttempts && !token.IsCancellationRequested && shouldContinue())
                        {
                            _log($"等待{options.RetryDelaySeconds}秒后重试...");
                            await Task.Delay(options.RetryDelaySeconds * 1000, token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _log("登录任务被取消");
                    return new AutoLoginResult
                    {
                        Canceled = true,
                        AttemptCount = attemptCount,
                        LastErrorMessage = lastErrorMessage
                    };
                }
                catch (Exception ex)
                {
                    lastErrorMessage = ex.Message;
                    _log($"✗ 第{attemptCount}次登录异常: {ex.Message}");

                    if (attemptCount < maxAttempts && !token.IsCancellationRequested && shouldContinue())
                    {
                        _log($"等待{options.RetryDelaySeconds}秒后重试...");
                        try
                        {
                            await Task.Delay(options.RetryDelaySeconds * 1000, token);
                        }
                        catch (OperationCanceledException)
                        {
                            _log("登录任务被取消");
                            return new AutoLoginResult
                            {
                                Canceled = true,
                                AttemptCount = attemptCount,
                                LastErrorMessage = lastErrorMessage
                            };
                        }
                    }
                }
            }

            if (token.IsCancellationRequested || !shouldContinue())
            {
                _log("登录任务已停止");
                return new AutoLoginResult
                {
                    Canceled = true,
                    AttemptCount = attemptCount,
                    LastErrorMessage = lastErrorMessage
                };
            }

            return new AutoLoginResult
            {
                Success = success,
                Canceled = false,
                AttemptCount = attemptCount,
                LastErrorMessage = lastErrorMessage
            };
        }
    }
}
