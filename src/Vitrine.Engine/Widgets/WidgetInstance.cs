using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Vitrine.Engine.Widgets;

internal class WidgetInstance : Form
{
    private readonly WebView2 _webView;
    private readonly WidgetManifest _manifest;
    private readonly string _widgetPath;
    private readonly IntPtr _desktopHandle;
    private readonly string _userDataFolder;
    private bool _ready;

    internal WidgetInstance(
        WidgetManifest manifest,
        string widgetPath,
        IntPtr desktopHandle,
        string userDataFolder)
    {
        _manifest = manifest;
        _widgetPath = widgetPath;
        _desktopHandle = desktopHandle;
        _userDataFolder = userDataFolder;

        Text = manifest.Name;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        Left = manifest.X;
        Top = manifest.Y;
        Width = manifest.Width;
        Height = manifest.Height;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += async (_, _) => await InitAsync();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (_desktopHandle != IntPtr.Zero)
                cp.Parent = _desktopHandle;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return cp;
        }
    }

    private async Task InitAsync()
    {
        var env = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
        await _webView.EnsureCoreWebView2Async(env);

        _webView.DefaultBackgroundColor = Color.Transparent;

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);

        _webView.CoreWebView2.WebMessageReceived += OnMessageReceived;

        var entryPath = Path.Combine(_widgetPath, _manifest.Entry);
        _webView.CoreWebView2.Navigate(new Uri(entryPath).AbsoluteUri);

        _ready = true;
    }

    private void OnMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        MessageReceived?.Invoke(this, e.WebMessageAsJson);
    }

    internal void PostSystemInfo(string json, int? requestId = null)
    {
        if (!_ready || _webView.CoreWebView2 == null) return;

        try
        {
            string message = requestId.HasValue
                ? $$"""{"type":"systemInfo","id":{{requestId.Value}},"data":{{json}}}"""
                : $$"""{"type":"systemInfo","data":{{json}}}""";

            _webView.CoreWebView2.PostWebMessageAsJson(message);
        }
        catch (ObjectDisposedException) { }
    }

    internal event EventHandler<string>? MessageReceived;

    private const string BridgeScript = """
        (function() {
            const _callbacks = [];
            const _pending = {};
            let _rid = 0;

            window.vitrine = {
                system: {
                    onUpdate(fn) { _callbacks.push(fn); },
                    getInfo() {
                        return new Promise(resolve => {
                            const id = ++_rid;
                            _pending[id] = resolve;
                            window.chrome.webview.postMessage({ type: 'getSystemInfo', id: id });
                        });
                    }
                }
            };

            window.chrome.webview.addEventListener('message', e => {
                const msg = e.data;
                if (msg.type === 'systemInfo') {
                    const data = msg.data;
                    _callbacks.forEach(fn => fn(data));
                    if (msg.id && _pending[msg.id]) {
                        _pending[msg.id](data);
                        delete _pending[msg.id];
                    }
                }
            });
        })();
        """;
}
