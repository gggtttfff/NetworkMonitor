using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class CampusNetworkAuthenticator : IDisposable
    {
        private readonly string _portalUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _authServer;
        private readonly string _fallbackAuthServer = "http://172.16.253.121";
        private HttpClient? _httpClient;
        private static readonly HttpClient _sharedHttpClient = CreateDirectHttpClient();

        public event Action<string>? LogMessage;
        public event Action<Exception>? OnNetworkError;

        public CampusNetworkAuthenticator(string portalUrl, string username, string password, string authServer = "http://172.16.253.121")
        {
            _portalUrl = portalUrl;
            _username = username;
            _password = password;
            _authServer = authServer;
        }

        /// <summary>
        /// 执行校园网认证
        /// </summary>
        /// <returns>认证结果</returns>
        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            var result = new AuthenticationResult();
            
            try
            {
                // 首先检查是否已经认证
                Log("检查当前认证状态...");
                if (await IsAuthenticatedAsync())
                {
                    Log("✓ 已经认证成功，无需重复登录");
                    result.Success = true;
                    result.Message = "已经认证成功，无需重复登录";
                    result.StatusCode = System.Net.HttpStatusCode.OK;
                    return result;
                }
                
                Log("未认证，开始登录流程...");
                
                using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false, Proxy = null };
                using var httpClient = new HttpClient(handler);
                _httpClient = httpClient;
                
                ConfigureHttpClient(httpClient);

                // 准备登录数据
                var loginData = PrepareLoginData();

                // 第一步: 获取网络参数
                Log("第1步: 获取网络参数");
                var initialResponse = await GetInitialParametersAsync(httpClient, loginData);
                
                if (!initialResponse.Success)
                {
                    // 如果获取参数失败,尝试使用备用登录方法
                    Log("\n标准登录流程失败,尝试备用登录方法...");
                    result = await TryFallbackAuthenticationAsync(httpClient);
                    if (result.Success)
                    {
                        return result;
                    }
                    
                    result.Success = false;
                    result.Message = initialResponse.Message;
                    return result;
                }

                // 显示提取到的参数
                LogParameters(loginData);

                // 第二步: 发送认证请求
                Log("\n第2步: 发送认证请求到服务器");
                result = await PerformAuthenticationAsync(httpClient, loginData);

                // 如果标准认证失败,尝试备用方法
                if (!result.Success)
                {
                    Log("\n标准认证失败,尝试备用登录方法...");
                    var fallbackResult = await TryFallbackAuthenticationAsync(httpClient);
                    if (fallbackResult.Success)
                    {
                        return fallbackResult;
                    }
                }

                // 保存调试信息
                if (!string.IsNullOrEmpty(result.ResponseContent))
                {
                    await SaveDebugResponseAsync(result.ResponseContent, "final");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"认证失败: {ex.Message}";
                result.Exception = ex;
                Log($"认证异常: {ex}");
                
                // 触发网络错误事件以便记录详细诊断
                OnNetworkError?.Invoke(ex);
            }
            finally
            {
                _httpClient = null;
            }

            return result;
        }

        /// <summary>
        /// 检查当前是否已认证
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                Log($"访问 {_portalUrl} 检查认证状态...");
                
                // 使用共享的 HttpClient 实例
                var request = new HttpRequestMessage(HttpMethod.Get, _portalUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                using var response = await _sharedHttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                Log($"响应状态码: {(int)response.StatusCode}");
                Log($"响应大小: {content.Length} 字节");
                
                // 检查各种成功标志
                bool isAuthenticated = content.Contains("认证成功") || 
                                     content.Contains("您已经成功登录") ||
                                     content.Contains("disconnconfig") ||
                                     content.Contains("连接网络") ||
                                     content.Contains("您可以关闭该页面");
                
                if (isAuthenticated)
                {
                    Log("✓ 检测到认证成功标志");
                }
                else
                {
                    Log("未检测到认证成功标志");
                    // 保存响应以便调试
                    await SaveDebugResponseAsync(content, "check_auth");
                }
                
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                Log($"检查认证状态失败: {ex.Message}");
                
                // 触发网络错误事件
                OnNetworkError?.Invoke(ex);
                
                return false;
            }
        }

        private void ConfigureHttpClient(HttpClient httpClient)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        private Dictionary<string, string> PrepareLoginData()
        {
            return new Dictionary<string, string>
            {
                { "userid", _username },
                { "passwd", _password },
                { "wlanuserip", "" },
                { "wlanacname", "" },
                { "wlanacIp", "" },
                { "ssid", "" },
                { "vlan", "" },
                { "mac", "" },
                { "version", "0" },
                { "portalpageid", "2" },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                { "uuid", Guid.NewGuid().ToString() },
                { "portaltype", "0" },
                { "hostname", "" },
                { "bindCtrlId", "" },
                { "validateType", "0" },
                { "bindOperatorType", "2" },
                { "sendFttrNotice", "0" }
            };
        }

        private async Task<(bool Success, string Message)> GetInitialParametersAsync(HttpClient httpClient, Dictionary<string, string> loginData)
        {
            try
            {
                var queryString = BuildQueryString(loginData);
                var initialUrl = $"{_portalUrl}/quickauth.do?{queryString}";
                
                var response = await httpClient.GetAsync(initialUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                Log($"初始响应状态码: {(int)response.StatusCode}");
                await SaveDebugResponseAsync(content, "initial");

                // 从响应中提取参数
                if (response.Headers.Location != null)
                {
                    var redirectUrl = response.Headers.Location.ToString();
                    Log($"检测到重定向: {redirectUrl}");
                    ExtractParametersFromUrl(redirectUrl, loginData);
                }
                else if (content.Contains("location.replace"))
                {
                    ExtractParametersFromJavaScript(content, loginData);
                }

                return (true, "参数提取成功");
            }
            catch (HttpRequestException httpEx)
            {
                // HTTP请求异常，可能是Socket错误
                Log($"获取参数时HTTP请求失败: {httpEx.Message}");
                
                // 检查是否是Socket错误
                if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
                {
                    Log($"Socket错误: {socketEx.ErrorCode} ({socketEx.SocketErrorCode})");
                    if (socketEx.ErrorCode == 10055)
                    {
                        Log("检测到Socket缓冲区耗尽错误 (10055)");
                    }
                }
                
                OnNetworkError?.Invoke(httpEx);
                return (false, $"获取参数失败: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Log($"获取参数时发生异常: {ex.Message}");
                OnNetworkError?.Invoke(ex);
                return (false, $"获取参数失败: {ex.Message}");
            }
        }

        private async Task<AuthenticationResult> PerformAuthenticationAsync(HttpClient httpClient, Dictionary<string, string> loginData)
        {
            var result = new AuthenticationResult();
            
            try
            {
                // 确定认证服务器地址
                string authServer = !string.IsNullOrEmpty(loginData["wlanacIp"]) ? 
                    $"http://{loginData["wlanacIp"]}" : _authServer;
                
                Log($"用户名: {loginData["userid"]}");
                Log($"认证服务器: {authServer}");

                // 设置 Referer 头
                SetRefererHeader(httpClient, authServer, loginData);
                
                // 构建请求URL
                var queryString = BuildQueryString(loginData);
                var requestUrl = $"{authServer}/quickauth.do?{queryString}";
                
                // 发送认证请求
                var response = await httpClient.GetAsync(requestUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                Log($"响应状态码: {(int)response.StatusCode} {response.StatusCode}");
                Log($"响应大小: {content.Length} 字节");
                
                result.StatusCode = response.StatusCode;
                result.ResponseContent = content;
                
                // 检查响应类型和内容
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                Log($"响应类型: {contentType}");
                
                // 解析响应
                if (contentType.Contains("json"))
                {
                    ParseJsonResponse(content, result);
                }
                else if (content.Contains("<!DOCTYPE html") || content.Contains("<html"))
                {
                    ParseHtmlResponse(content, result);
                }
                else
                {
                    result.Success = false;
                    result.Message = "响应格式未知";
                }
            }
            catch (HttpRequestException httpEx)
            {
                Log($"认证请求HTTP异常: {httpEx.Message}");
                
                // 检查内部异常
                if (httpEx.InnerException is System.Net.Sockets.SocketException socketEx)
                {
                    Log($"Socket错误: {socketEx.ErrorCode} ({socketEx.SocketErrorCode})");
                    if (socketEx.ErrorCode == 10055)
                    {
                        Log("检测到Socket缓冲区耗尽错误 (10055 WSAENOBUFS)");
                        Log("建议: 检查TCP连接数、系统资源和进程句柄");
                    }
                }
                
                OnNetworkError?.Invoke(httpEx);
                
                result.Success = false;
                result.Message = $"认证请求失败: {httpEx.Message}";
                result.Exception = httpEx;
            }
            catch (Exception ex)
            {
                Log($"认证请求异常: {ex.Message}");
                OnNetworkError?.Invoke(ex);
                
                result.Success = false;
                result.Message = $"认证请求异常: {ex.Message}";
                result.Exception = ex;
            }
            
            return result;
        }

        private void ParseJsonResponse(string content, AuthenticationResult result)
        {
            Log("响应是 JSON 格式");
            try
            {
                var jsonResponse = JsonDocument.Parse(content);
                if (jsonResponse.RootElement.TryGetProperty("code", out var codeElement))
                {
                    string code = codeElement.GetString() ?? "";
                    
                    if (jsonResponse.RootElement.TryGetProperty("message", out var msgElement))
                    {
                        result.Message = msgElement.GetString() ?? "";
                    }
                    
                    Log($"JSON 返回代码: {code}");
                    Log($"JSON 返回消息: {result.Message}");
                    
                    result.Success = (code == "0" || code == "200");
                    if (result.Success && string.IsNullOrEmpty(result.Message))
                    {
                        result.Message = "认证成功！";
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"JSON 解析失败: {ex.Message}");
                result.Success = false;
                result.Message = "JSON 解析失败";
            }
        }

        private void ParseHtmlResponse(string content, AuthenticationResult result)
        {
            Log("响应是 HTML 页面");
            
            if (content.Contains("认证成功") || 
                content.Contains("连接网络") || 
                content.Contains("您可以关闭该页面") ||
                content.Contains("断线 LOOUT") ||
                content.Contains("disconnconfig") ||
                content.Contains("您已经成功登录"))
            {
                result.Success = true;
                result.Message = "认证成功！已连接到校园网";
                Log("✓ 检测到成功页面标志");
            }
            else
            {
                result.Success = false;
                result.Message = "未检测到认证成功标志";
            }
        }

        private void ExtractParametersFromUrl(string url, Dictionary<string, string> loginData)
        {
            try
            {
                var uri = new Uri(url.StartsWith("http") ? url : "http://" + url);
                var query = uri.Query.TrimStart('?');
                var queryParams = query.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0], p => WebUtility.UrlDecode(p[1]));

                foreach (var param in queryParams)
                {
                    if (loginData.ContainsKey(param.Key))
                    {
                        loginData[param.Key] = param.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"提取参数失败: {ex.Message}");
            }
        }

        private void ExtractParametersFromJavaScript(string content, Dictionary<string, string> loginData)
        {
            var pattern = @"location\.replace\(\""([^\""]+?)\""(?:\+encodeURIComponent\(\""([^\""]+?)\""\))?\)";
            var match = Regex.Match(content, pattern);
            
            if (match.Success)
            {
                var mainUrl = match.Groups[1].Value;
                Log($"提取到主 URL: {mainUrl}");
                ExtractParametersFromUrl(mainUrl, loginData);
                
                if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    var encodedUrl = match.Groups[2].Value;
                    Log($"提取到编码URL: {encodedUrl.Substring(0, Math.Min(100, encodedUrl.Length))}...");
                }
            }
        }

        private void SetRefererHeader(HttpClient httpClient, string authServer, Dictionary<string, string> loginData)
        {
            if (httpClient.DefaultRequestHeaders.Contains("Referer"))
                httpClient.DefaultRequestHeaders.Remove("Referer");
                
            var referer = $"{authServer}/portal.do?wlanuserip={loginData["wlanuserip"]}&wlanacname={loginData["wlanacname"]}&mac={loginData["mac"]}&vlan={loginData["vlan"]}&hostname={loginData["hostname"]}";
            httpClient.DefaultRequestHeaders.Add("Referer", referer);
        }

        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
        }

        private void LogParameters(Dictionary<string, string> loginData)
        {
            Log($"→ wlanuserip: {loginData["wlanuserip"]}");
            Log($"→ wlanacname: {loginData["wlanacname"]}");
            Log($"→ wlanacIp: {loginData["wlanacIp"]}");
            Log($"→ mac: {loginData["mac"]}");
            Log($"→ vlan: {loginData["vlan"]}");
            Log($"→ hostname: {loginData["hostname"]}");
        }

        private async Task SaveDebugResponseAsync(string content, string suffix)
        {
            try
            {
                string debugDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug");
                if (!Directory.Exists(debugDir))
                {
                    Directory.CreateDirectory(debugDir);
                }
                
                string debugFile = Path.Combine(debugDir, $"auth_response_{suffix}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                await File.WriteAllTextAsync(debugFile, content);
                Log($"响应已保存到: {debugFile}");
            }
            catch
            {
                // 忽略保存失败
            }
        }

        /// <summary>
        /// 备用登录方法: 当2.2.2.2无法访问时,直接使用172.16.253.121登录
        /// </summary>
        private async Task<AuthenticationResult> TryFallbackAuthenticationAsync(HttpClient httpClient)
        {
            var result = new AuthenticationResult();
            
            try
            {
                Log($"使用备用认证服务器: {_fallbackAuthServer}");
                
                // 构建备用登录URL,使用您提供的URL模式
                // 注意: 这些参数通常是动态获取的,但在备用模式下我们尝试使用常见默认值
                var fallbackUrl = $"{_fallbackAuthServer}/portal/usertemp_computer/shaoguanxy-pc/logout.html" +
                    $"?wlanacip=172.16.253.113" +
                    $"&wlanuserip=" +  // 留空,让服务器自动检测
                    $"&wlanacname=NFV-BASE-SGYD" +
                    $"&mac=" +  // 留空,让服务器自动检测
                    $"&version=0" +
                    $"&msg=%E8%AE%A4%E8%AF%81%E6%88%90%E5%8A%9F" +
                    $"&selfTicket=" +
                    $"&macChange=false" +
                    $"&dropLogCheck=-1" +
                    $"&vlan=199" +
                    $"&groupId=1" +
                    $"&userId={_username}";
                
                Log($"备用请求URL: {fallbackUrl}");
                
                // 发送请求
                var response = await httpClient.GetAsync(fallbackUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                Log($"备用认证响应状态码: {(int)response.StatusCode}");
                Log($"响应大小: {content.Length} 字节");
                
                result.StatusCode = response.StatusCode;
                result.ResponseContent = content;
                
                // 保存调试信息
                await SaveDebugResponseAsync(content, "fallback");
                
                // 检查是否成功
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 检查成功标志
                    if (content.Contains("认证成功") || 
                        content.Contains("您已经成功登录") ||
                        content.Contains("连接网络") ||
                        content.Contains("您可以关闭该页面"))
                    {
                        result.Success = true;
                        result.Message = "备用登录成功！";
                        Log("✓ 备用登录成功");
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "备用登录响应异常";
                        Log("备用登录失败: 未检测到成功标志");
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = $"备用登录失败: HTTP {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Log($"备用登录异常: {ex.Message}");
                result.Success = false;
                result.Message = $"备用登录失败: {ex.Message}";
                result.Exception = ex;
            }
            
            return result;
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(message);
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
                Timeout = TimeSpan.FromSeconds(10)
            };
        }
        
        public void Dispose()
        {
            // 不要释放静态的 _sharedHttpClient
            // 它应该在应用程序的整个生命周期内存在
        }
    }

    /// <summary>
    /// 认证结果
    /// </summary>
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public HttpStatusCode StatusCode { get; set; }
        public string? ResponseContent { get; set; }
        public Exception? Exception { get; set; }
    }
}
