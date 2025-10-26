using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace NetworkMonitor
{
    public class AutoLoginBrowser : Form
    {
        private WebView2 webView = null!;
        private string loginUrl;
        private bool isAutoLogin;

        public AutoLoginBrowser(string url, bool autoLogin = false)
        {
            loginUrl = url;
            isAutoLogin = autoLogin;
            InitializeComponents();
        }

        private async void InitializeComponents()
        {
            this.Text = "校园网登录";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(webView);

            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                webView.Source = new Uri(loginUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化浏览器失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess || !isAutoLogin) return;

            try
            {
                // 等待页面完全加载
                await System.Threading.Tasks.Task.Delay(1000);

                // 执行自动点击登录按钮的JavaScript
                string script = @"
                    (function() {
                        var loginButton = document.getElementById('formSumbit');
                        if (loginButton) {
                            loginButton.click();
                            return true;
                        }
                        return false;
                    })();
                ";

                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                
                if (result == "true")
                {
                    // 登录按钮已点击,等待几秒后关闭
                    await System.Threading.Tasks.Task.Delay(3000);
                    this.BeginInvoke(new Action(() => this.Close()));
                }
            }
            catch
            {
                // 忽略脚本执行错误
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (webView != null)
            {
                webView.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
