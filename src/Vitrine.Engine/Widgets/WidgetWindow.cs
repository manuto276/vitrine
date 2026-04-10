using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Vitrine.Engine.Widgets;

internal class WidgetWindow : Form
{
    private readonly WebView2 _webView;
    private readonly string _htmlPath;

    internal WidgetWindow(string htmlPath)
    {
        _htmlPath = htmlPath;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += async (_, _) => await InitWebViewAsync();
    }

    private async Task InitWebViewAsync()
    {
        var options = new CoreWebView2EnvironmentOptions();
        var env = await CoreWebView2Environment.CreateAsync(null, null, options);
        await _webView.EnsureCoreWebView2Async(env);

        _webView.DefaultBackgroundColor = Color.Transparent;

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        _webView.CoreWebView2.Navigate(new Uri(_htmlPath).AbsoluteUri);
    }
}
