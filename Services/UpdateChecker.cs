using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class UpdateChecker
    {
        private const string GitHubRepo = "gggtttfff/NetworkMonitor";
        private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
        private const string GitHubReleasesUrl = $"https://github.com/{GitHubRepo}/releases";

        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                Proxy = null
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "NetworkMonitor-UpdateChecker");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            return client;
        }

        public static async Task<UpdateCheckResult> CheckForUpdateAsync(string currentVersion)
        {
            var result = new UpdateCheckResult();

            try
            {
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"GitHub API 返回错误: {(int)response.StatusCode}";
                    return result;
                }

                var content = await response.Content.ReadAsStringAsync();
                var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (releaseInfo == null)
                {
                    result.ErrorMessage = "无法解析版本信息";
                    return result;
                }

                string latestVersion = releaseInfo.TagName?.TrimStart('v') ?? "";
                result.LatestVersion = latestVersion;
                result.ReleaseUrl = releaseInfo.HtmlUrl ?? GitHubReleasesUrl;

                if (!string.IsNullOrEmpty(latestVersion))
                {
                    result.HasUpdate = IsNewerVersion(currentVersion, latestVersion);

                    if (releaseInfo.Assets != null)
                    {
                        foreach (var asset in releaseInfo.Assets)
                        {
                            if (asset.Name?.Contains("Setup") == true && asset.Name?.EndsWith(".exe") == true)
                            {
                                result.DownloadUrl = asset.BrowserDownloadUrl ?? "";
                                break;
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                result.ErrorMessage = $"网络请求失败: {httpEx.Message}";
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "检查更新超时";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检查更新失败: {ex.Message}";
            }

            return result;
        }

        private static bool IsNewerVersion(string current, string latest)
        {
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(latest))
                return false;

            var currentParts = current.Split('.');
            var latestParts = latest.Split('.');

            int maxParts = Math.Max(currentParts.Length, latestParts.Length);

            for (int i = 0; i < maxParts; i++)
            {
                int currentPart = i < currentParts.Length && int.TryParse(currentParts[i], out var c) ? c : 0;
                int latestPart = i < latestParts.Length && int.TryParse(latestParts[i], out var l) ? l : 0;

                if (latestPart > currentPart)
                    return true;
                if (latestPart < currentPart)
                    return false;
            }

            return false;
        }

        public static string GetReleasesUrl()
        {
            return GitHubReleasesUrl;
        }
    }

    internal class GitHubReleaseInfo
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
        public GitHubAsset[]? Assets { get; set; }
    }

    internal class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}