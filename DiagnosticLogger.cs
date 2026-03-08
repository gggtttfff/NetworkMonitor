using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Info,       // 一般信息
        Warning,    // 警告
        Error,      // 错误
        Diagnostic  // 诊断信息（包含详细的系统信息）
    }

    /// <summary>
    /// 增强型诊断日志记录器
    /// </summary>
    public class DiagnosticLogger : IDisposable
    {
        private readonly string _logDirectory;
        private readonly long _maxLogFileSizeBytes;
        private readonly int _maxLogFileCount;
        private readonly int _retentionDays;
        private StreamWriter? _currentWriter;
        private string _currentLogFilePath = "";
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        /// <summary>
        /// 日志消息事件（用于同时输出到UI）
        /// </summary>
        public event Action<string>? OnLogMessage;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logDirectory">日志目录</param>
        /// <param name="maxLogFileSizeMB">单个日志文件最大大小（MB），默认10MB</param>
        /// <param name="maxLogFileCount">最多保留日志文件数量，默认30个</param>
        /// <param name="retentionDays">日志保留天数，默认30天</param>
        public DiagnosticLogger(string? logDirectory = null, int maxLogFileSizeMB = 10, int maxLogFileCount = 30, int retentionDays = 30)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            _maxLogFileSizeBytes = maxLogFileSizeMB * 1024 * 1024;
            _maxLogFileCount = maxLogFileCount;
            _retentionDays = Math.Max(1, retentionDays);

            // 创建日志目录
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // 初始化日志文件
            InitializeLogFile();

            // 清理旧日志
            CleanupOldLogs();
        }

        /// <summary>
        /// 初始化日志文件
        /// </summary>
        private void InitializeLogFile()
        {
            try
            {
                // 按日期和序号创建日志文件
                string dateStr = DateTime.Now.ToString("yyyyMMdd");
                int fileIndex = 1;
                string logFileName;

                // 查找今天的最新日志文件
                do
                {
                    logFileName = $"network_monitor_{dateStr}_{fileIndex:D3}.log";
                    _currentLogFilePath = Path.Combine(_logDirectory, logFileName);

                    // 如果文件存在且未超过大小限制，使用该文件
                    if (File.Exists(_currentLogFilePath))
                    {
                        var fileInfo = new FileInfo(_currentLogFilePath);
                        if (fileInfo.Length < _maxLogFileSizeBytes)
                        {
                            break;
                        }
                        fileIndex++;
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                // 打开文件流（追加模式）
                _currentWriter = new StreamWriter(_currentLogFilePath, append: true, Encoding.UTF8);
                _currentWriter.AutoFlush = true;

                // 写入启动标记
                _currentWriter.WriteLine();
                _currentWriter.WriteLine($"========== 日志会话开始 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            }
            catch (Exception ex)
            {
                // 静默失败，但输出调试信息
                System.Diagnostics.Debug.WriteLine($"初始化日志文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "network_monitor_*.log");
                var expireBefore = DateTime.Now.AddDays(-_retentionDays);

                // 先按天数清理
                foreach (var logFile in logFiles)
                {
                    try
                    {
                        var info = new FileInfo(logFile);
                        if (info.LastWriteTime < expireBefore)
                        {
                            File.Delete(logFile);
                        }
                    }
                    catch
                    {
                        // 忽略删除失败
                    }
                }

                // 再按数量上限清理
                logFiles = Directory.GetFiles(_logDirectory, "network_monitor_*.log");
                Array.Sort(logFiles, (a, b) =>
                {
                    var fileA = new FileInfo(a);
                    var fileB = new FileInfo(b);
                    return fileB.LastWriteTime.CompareTo(fileA.LastWriteTime);
                });

                if (logFiles.Length > _maxLogFileCount)
                {
                    for (int i = _maxLogFileCount; i < logFiles.Length; i++)
                    {
                        try
                        {
                            File.Delete(logFiles[i]);
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理旧日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查并轮转日志文件
        /// </summary>
        private void RotateLogFileIfNeeded()
        {
            try
            {
                if (_currentWriter != null && File.Exists(_currentLogFilePath))
                {
                    var fileInfo = new FileInfo(_currentLogFilePath);

                    // 如果文件大小超过限制，创建新文件
                    if (fileInfo.Length >= _maxLogFileSizeBytes)
                    {
                        _currentWriter.WriteLine($"========== 日志文件达到大小限制，轮转到新文件 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
                        _currentWriter.Close();
                        _currentWriter.Dispose();
                        _currentWriter = null;

                        InitializeLogFile();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"轮转日志文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public async Task LogAsync(LogLevel level, string message)
        {
            if (_disposed) return;

            await _writeLock.WaitAsync();
            try
            {
                // 检查是否需要轮转
                RotateLogFileIfNeeded();

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string levelStr = level.ToString().ToUpper().PadRight(10);
                string logLine = $"[{timestamp}] [{levelStr}] {message}";

                // 写入文件
                _currentWriter?.WriteLine(logLine);

                // 触发UI事件（不包含完整时间戳，保持UI简洁）
                string uiMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                OnLogMessage?.Invoke(uiMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入日志失败: {ex.Message}");
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public async Task LogInfoAsync(string message)
        {
            await LogAsync(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public async Task LogWarningAsync(string message)
        {
            await LogAsync(LogLevel.Warning, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public async Task LogErrorAsync(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                await LogAsync(LogLevel.Error, $"{message}\n异常类型: {exception.GetType().Name}\n异常消息: {exception.Message}\n堆栈跟踪: {exception.StackTrace}");
            }
            else
            {
                await LogAsync(LogLevel.Error, message);
            }
        }

        /// <summary>
        /// 记录诊断日志（包含完整的网络诊断报告）
        /// </summary>
        public async Task LogDiagnosticAsync(string message, NetworkDiagnostics.DiagnosticReport? diagnosticReport = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(message);

            if (diagnosticReport != null)
            {
                sb.AppendLine("========== 网络诊断报告 ==========");
                sb.AppendLine(NetworkDiagnostics.FormatReport(diagnosticReport));
            }

            await LogAsync(LogLevel.Diagnostic, sb.ToString());
        }

        /// <summary>
        /// 记录网络错误并自动收集诊断信息
        /// </summary>
        public async Task LogNetworkErrorAsync(string errorMessage, Exception? exception = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"网络错误: {errorMessage}");

            if (exception != null)
            {
                sb.AppendLine($"异常类型: {exception.GetType().Name}");
                sb.AppendLine($"异常消息: {exception.Message}");

                // 检查是否是Socket错误
                if (exception is System.Net.Sockets.SocketException socketEx)
                {
                    sb.AppendLine($"Socket错误代码: {socketEx.ErrorCode} ({socketEx.SocketErrorCode})");
                    
                    // Socket错误10055 (WSAENOBUFS) - 缓冲区空间不足
                    if (socketEx.ErrorCode == 10055)
                    {
                        sb.AppendLine("** 检测到Socket缓冲区耗尽错误 (10055 WSAENOBUFS) **");
                        sb.AppendLine("可能原因:");
                        sb.AppendLine("  1. TCP连接数过多");
                        sb.AppendLine("  2. 系统资源不足");
                        sb.AppendLine("  3. 进程句柄数过多");
                        sb.AppendLine("  4. 未正确释放网络连接");
                    }
                }
                else if (exception is HttpRequestException httpEx)
                {
                    sb.AppendLine($"HTTP请求异常");
                    if (httpEx.InnerException != null)
                    {
                        sb.AppendLine($"内部异常: {httpEx.InnerException.GetType().Name}");
                        sb.AppendLine($"内部消息: {httpEx.InnerException.Message}");
                    }
                }

                sb.AppendLine($"\n堆栈跟踪:\n{exception.StackTrace}");
            }

            await LogAsync(LogLevel.Error, sb.ToString());

            // 自动收集诊断信息
            try
            {
                var diagnosticReport = await NetworkDiagnostics.GenerateFullReportAsync();
                await LogDiagnosticAsync("自动收集的网络诊断信息", diagnosticReport);
            }
            catch (Exception ex)
            {
                await LogAsync(LogLevel.Warning, $"无法收集诊断信息: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录Socket错误（特别处理）
        /// </summary>
        public async Task LogSocketErrorAsync(System.Net.Sockets.SocketException socketException, string context)
        {
            await LogNetworkErrorAsync($"Socket错误 - {context}", socketException);
        }

        /// <summary>
        /// 同步记录日志（用于无法使用async的场景）
        /// </summary>
        public void Log(LogLevel level, string message)
        {
            Task.Run(async () => await LogAsync(level, message)).Wait();
        }

        /// <summary>
        /// 同步记录信息日志
        /// </summary>
        public void LogInfo(string message)
        {
            Task.Run(async () => await LogInfoAsync(message)).Wait();
        }

        /// <summary>
        /// 同步记录错误日志
        /// </summary>
        public void LogError(string message, Exception? exception = null)
        {
            Task.Run(async () => await LogErrorAsync(message, exception)).Wait();
        }

        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        public string GetCurrentLogFilePath()
        {
            return _currentLogFilePath;
        }

        /// <summary>
        /// 获取日志目录
        /// </summary>
        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _writeLock.Wait();
            try
            {
                if (_currentWriter != null)
                {
                    _currentWriter.WriteLine($"========== 日志会话结束 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
                    _currentWriter.Close();
                    _currentWriter.Dispose();
                    _currentWriter = null;
                }
            }
            finally
            {
                _writeLock.Release();
            }

            _writeLock.Dispose();
        }
    }
}
