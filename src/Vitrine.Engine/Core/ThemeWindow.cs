using System;
using System.Drawing;
using System.IO;
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

        Log.Info($"ThemeWindow created — Bounds={Bounds}, DesktopHandle=0x{desktopHandle:X}");

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

        Log.Info("Initializing WebView2 environment");
        var env = await CoreWebView2Environment.CreateAsync();
        Log.Info($"WebView2 environment created — BrowserVersion={env.BrowserVersionString}");

        await _webView.EnsureCoreWebView2Async(env);
        Log.Info("CoreWebView2 initialized");

        _webView.DefaultBackgroundColor = Color.Transparent;

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);
        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(ErrorCaptureScript);
        Log.Info("Bridge + error capture scripts injected");

        _webView.CoreWebView2.WebMessageReceived += OnWebMessage;

        _webView.CoreWebView2.NavigationCompleted += (_, e) =>
            Log.Info($"Navigation completed — Success={e.IsSuccess}, HttpStatus={e.HttpStatusCode}");

        _webView.CoreWebView2.ProcessFailed += (_, e) =>
            Log.Error($"WebView2 process failed — Kind={e.ProcessFailedKind}, Reason={e.Reason}");

        _initialized = true;
    }

    internal void LoadTheme(string themePath, string entry)
    {
        if (!_initialized)
        {
            Log.Warn("LoadTheme called but WebView2 not initialized yet");
            return;
        }

        var themeJsPath = Path.Combine(themePath, entry);
        Log.Info($"Loading theme — reading {themeJsPath}");

        if (!File.Exists(themeJsPath))
        {
            Log.Error($"Theme JS not found: {themeJsPath}");
            return;
        }

        var themeJs = File.ReadAllText(themeJsPath);
        Log.Info($"Theme JS loaded — {themeJs.Length} chars");

        // Inline the theme JS to avoid CORS issues with NavigateToString + virtual hosts
        _webView.CoreWebView2.NavigateToString(
            ShellHtmlPrefix + themeJs + ShellHtmlSuffix);

        Log.Info("NavigateToString called with inline theme JS");
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

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var json = e.WebMessageAsJson;

        // Log JS errors captured by the error script
        if (json.Contains("\"jsError\""))
            Log.Error($"JS error from theme: {json}");

        MessageReceived?.Invoke(this, json);
    }

    private const string ShellHtmlPrefix = """
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
          <script>
        """;

    private const string ShellHtmlSuffix = """

          </script>
        </body>
        </html>
        """;

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

    private const string ErrorCaptureScript = """
        window.onerror = function(msg, url, line, col, err) {
            window.chrome.webview.postMessage({
                type: 'jsError',
                message: msg,
                source: url,
                line: line,
                col: col,
                stack: err && err.stack
            });
        };
        window.addEventListener('unhandledrejection', function(e) {
            window.chrome.webview.postMessage({
                type: 'jsError',
                message: 'Unhandled rejection: ' + (e.reason && e.reason.message || String(e.reason)),
                stack: e.reason && e.reason.stack
            });
        });
        """;
}
