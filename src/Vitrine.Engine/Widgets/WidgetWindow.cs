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
    private readonly IntPtr _parentHandle;

    internal WidgetWindow(string htmlPath, IntPtr parentHandle)
    {
        _htmlPath = htmlPath;
        _parentHandle = parentHandle;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += async (_, _) => await InitWebViewAsync();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Embed inside WorkerW so the widget draws behind desktop icons
            if (_parentHandle != IntPtr.Zero)
                cp.Parent = _parentHandle;
            // WS_EX_TOOLWINDOW — hide from Alt+Tab
            cp.ExStyle |= 0x00000080;
            // WS_EX_NOACTIVATE — don't steal focus
            cp.ExStyle |= 0x08000000;
            return cp;
        }
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
