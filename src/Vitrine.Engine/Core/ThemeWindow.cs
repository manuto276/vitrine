using System;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Vitrine.Engine.Core;

internal class ThemeWindow : Form
{
    private readonly WebView2 _webView;
    private readonly IntPtr _desktopHandle;
    private bool _initialized;

    internal event EventHandler<string>? MessageReceived;

    internal ThemeWindow(IntPtr desktopHandle)
    {
        _desktopHandle = desktopHandle;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        Bounds = Screen.PrimaryScreen!.Bounds;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);
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

    internal async Task InitAsync()
    {
        if (_initialized) return;

        var env = await CoreWebView2Environment.CreateAsync();
        await _webView.EnsureCoreWebView2Async(env);

        _webView.DefaultBackgroundColor = Color.Transparent;

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);
        _webView.CoreWebView2.WebMessageReceived += (_, e) => MessageReceived?.Invoke(this, e.WebMessageAsJson);

        _initialized = true;
    }

    internal void LoadTheme(string themePath, string entry)
    {
        if (!_initialized) return;

        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "vitrine.theme", themePath, CoreWebView2HostResourceAccessKind.Allow);

        _webView.CoreWebView2.NavigateToString($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { background: transparent; width: 100vw; height: 100vh; overflow: hidden; }
                #root { width: 100%; height: 100%; }
              </style>
            </head>
            <body>
              <div id="root"></div>
              <script src="https://vitrine.theme/{{entry}}"></script>
            </body>
            </html>
            """);
    }

    internal void PostMessage(string json)
    {
        if (!_initialized) return;

        try
        {
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
        catch (ObjectDisposedException) { }
    }

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
                    _callbacks.forEach(fn => fn(msg.data));
                    if (msg.id && _pending[msg.id]) {
                        _pending[msg.id](msg.data);
                        delete _pending[msg.id];
                    }
                }
            });
        })();
        """;
}
