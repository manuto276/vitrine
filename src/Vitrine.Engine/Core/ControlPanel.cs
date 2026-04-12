using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Core;

internal class ControlPanel : Form
{
    private readonly WebView2 _webView;
    private readonly ThemeHost _host;
    private bool _initialized;

    internal ControlPanel(ThemeHost host)
    {
        _host = host;

        Text = "Vitrine";
        Size = new Size(960, 640);
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(243, 243, 243);
        Icon = LoadIcon();

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        if (_initialized) return;

        var env = await CoreWebView2Environment.CreateAsync();
        await _webView.EnsureCoreWebView2Async(env);

        _webView.DefaultBackgroundColor = Color.FromArgb(243, 243, 243);

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);
        _webView.CoreWebView2.WebMessageReceived += OnMessage;

        _initialized = true;
        LoadPanel();
    }

    private void LoadPanel()
    {
        var panelJsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "panel", "panel.js");
        var panelCssPath = Path.Combine(AppContext.BaseDirectory, "Assets", "panel", "panel.css");

        if (!File.Exists(panelJsPath))
        {
            Log.Error($"Panel JS not found: {panelJsPath}");
            return;
        }

        var js = File.ReadAllText(panelJsPath);
        var css = File.Exists(panelCssPath) ? "<style>" + File.ReadAllText(panelCssPath) + "</style>" : "";

        var html = ShellPrefix + css + ShellMiddle + js + ShellSuffix;
        _webView.CoreWebView2.NavigateToString(html);
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;

            if (root.GetProperty("type").GetString() != "panelInvoke") return;

            var id = root.GetProperty("id").GetInt32();
            var method = root.GetProperty("method").GetString() ?? "";
            var args = root.TryGetProperty("args", out var a) ? a : default;

            try
            {
                var result = HandleMethod(method, args);
                Respond(id, result);
            }
            catch (Exception ex)
            {
                RespondError(id, ex.Message);
            }
        }
        catch { /* malformed message */ }
    }

    private string HandleMethod(string method, JsonElement args)
    {
        switch (method)
        {
            case "getThemes":
                return GetThemesList();

            case "getActiveTheme":
                var config = Configuration.Load();
                return JsonSerializer.Serialize(new { name = config.ActiveTheme });

            case "setActiveTheme":
                var themeName = args.GetProperty("name").GetString()!;
                _host.SetActiveTheme(themeName);
                return "{}";

            case "getSettings":
                return GetThemeFile(args.GetProperty("theme").GetString()!, "settings.json");

            case "saveSettings":
                SaveThemeSettings(
                    args.GetProperty("theme").GetString()!,
                    args.GetProperty("settings"));
                _host.ReloadActiveTheme();
                return "{}";

            case "getDefinitions":
                return GetThemeFile(args.GetProperty("theme").GetString()!, "settings.definitions.json");

            case "reloadTheme":
                _host.ReloadActiveTheme();
                return "{}";

            default:
                throw new InvalidOperationException($"Unknown method: {method}");
        }
    }

    private static string GetThemesList()
    {
        var themesPath = Configuration.ThemesPath;
        if (!Directory.Exists(themesPath)) return "[]";

        var themes = new List<object>();
        foreach (var dir in Directory.GetDirectories(themesPath).OrderBy(d => d))
        {
            var manifestPath = Path.Combine(dir, "theme.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
                themes.Add(new
                {
                    id = Path.GetFileName(dir),
                    name = manifest?.Name ?? Path.GetFileName(dir),
                    description = manifest?.Description ?? "",
                    hasSettings = File.Exists(Path.Combine(dir, "settings.definitions.json")),
                });
            }
            catch { /* skip invalid themes */ }
        }

        return JsonSerializer.Serialize(themes);
    }

    private static string GetThemeFile(string theme, string fileName)
    {
        var path = Path.Combine(Configuration.ThemesPath, theme, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : "{}";
    }

    private static void SaveThemeSettings(string theme, JsonElement settings)
    {
        var path = Path.Combine(Configuration.ThemesPath, theme, "settings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        Log.Info($"Settings saved for theme '{theme}'");
    }

    private void Respond(int id, string dataJson)
    {
        _webView.CoreWebView2.PostWebMessageAsString(
            $$"""{"type":"panelResponse","id":{{id}},"data":{{dataJson}}}""");
    }

    private void RespondError(int id, string error)
    {
        var escapedError = JsonSerializer.Serialize(error);
        _webView.CoreWebView2.PostWebMessageAsString(
            $$"""{"type":"panelResponse","id":{{id}},"error":{{escapedError}}}""");
    }

    private static Icon? LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "vitrine.ico");
        return File.Exists(iconPath) ? new Icon(iconPath) : null;
    }

    private const string ShellPrefix = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif; background: #f3f3f3; color: #1a1a1a; }
            #root { width: 100%; height: 100vh; }
          </style>
        """;

    private const string ShellMiddle = """

        </head>
        <body>
          <div id="root"></div>
          <script>
        """;

    private const string ShellSuffix = """

          </script>
        </body>
        </html>
        """;

    private const string BridgeScript = """
        (function() {
            const _pending = {};
            let _rid = 0;

            window.vitrine = {
                panel: {
                    invoke(method, args) {
                        return new Promise((resolve, reject) => {
                            const id = ++_rid;
                            _pending[id] = { resolve, reject };
                            window.chrome.webview.postMessage({ type: 'panelInvoke', id, method, args });
                        });
                    },
                    getThemes() { return this.invoke('getThemes'); },
                    getActiveTheme() { return this.invoke('getActiveTheme'); },
                    setActiveTheme(name) { return this.invoke('setActiveTheme', { name }); },
                    getSettings(theme) { return this.invoke('getSettings', { theme }); },
                    saveSettings(theme, settings) { return this.invoke('saveSettings', { theme, settings }); },
                    getDefinitions(theme) { return this.invoke('getDefinitions', { theme }); },
                    reloadTheme() { return this.invoke('reloadTheme'); },
                }
            };

            window.chrome.webview.addEventListener('message', e => {
                let msg = e.data;
                if (typeof msg === 'string') msg = JSON.parse(msg);
                if (msg.type === 'panelResponse' && msg.id && _pending[msg.id]) {
                    if (msg.error) _pending[msg.id].reject(new Error(msg.error));
                    else _pending[msg.id].resolve(msg.data);
                    delete _pending[msg.id];
                }
            });
        })();
        """;
}
