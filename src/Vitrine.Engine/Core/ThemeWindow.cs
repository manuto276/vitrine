using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

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

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        var primary = Screen.PrimaryScreen!.Bounds;
        if (Bounds != primary)
        {
            Log.Info($"Display settings changed — repositioning to primary monitor: {primary}");
            Bounds = primary;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.Dispose(disposing);
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

    internal async Task InitAsync(CoreWebView2Environment env)
    {
        if (_initialized) return;

        Log.Info($"Attaching WebView2 — BrowserVersion={env.BrowserVersionString}");
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
        var themeJsPath = Path.Combine(themePath, entry);
        Log.Info($"Loading theme — reading {themeJsPath}");

        if (!File.Exists(themeJsPath))
        {
            Log.Error($"Theme JS not found: {themeJsPath}");
            return;
        }

        var themeJs = File.ReadAllText(themeJsPath);

        var themeCssPath = Path.Combine(themePath, "theme.css");
        string? themeCss = File.Exists(themeCssPath) ? File.ReadAllText(themeCssPath) : null;

        var settingsPath = Path.Combine(themePath, "settings.json");
        string? settings = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null;

        if (themeCss != null)
            Log.Info($"Theme CSS found — {themeCss.Length} chars");
        if (settings != null)
            Log.Info($"Theme settings found — {settings.Length} chars");

        LoadThemeContent(themeJs, themeCss, settings);
    }

    internal void LoadThemeContent(string themeJs, string? themeCss = null, string? settingsJson = null)
    {
        if (!_initialized)
        {
            Log.Warn("LoadThemeContent called but WebView2 not initialized yet");
            return;
        }

        Log.Info($"Loading theme content — JS={themeJs.Length} chars, CSS={themeCss?.Length ?? 0} chars, settings={settingsJson?.Length ?? 0} chars");

        var cssBlock = themeCss != null
            ? $"<style>{themeCss}</style>"
            : "";

        var settingsBlock = $"<script>window.vitrine.settings = {settingsJson ?? "{}"};</script>";

        _webView.CoreWebView2.NavigateToString(
            ShellHtmlPrefix + cssBlock + ShellHtmlMiddle + settingsBlock + ShellHtmlScript + themeJs + ShellHtmlSuffix);
        Log.Info("NavigateToString called");
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
        """;

    private const string ShellHtmlMiddle = """

        </head>
        <body>
          <div id="root"></div>
        """;

    // Injected between settings block and theme JS
    private const string ShellHtmlScript = """
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
