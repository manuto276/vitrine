using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Vitrine.Engine.SystemInfo;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Core;

internal class ThemeHost : IDisposable
{
    private ThemeWindow? _window;
    private NotifyIcon? _trayIcon;
    private ToolStripMenuItem? _themesMenu;
    private readonly SystemInfoProvider _systemInfo = new();
    private Configuration _config = null!;

    internal void Start()
    {
        Log.Info("ThemeHost.Start — begin parallel init");

        // 1. Fire WebView2 environment creation immediately (runs on thread pool)
        var envTask = CoreWebView2Environment.CreateAsync();
        Log.Info("WebView2 environment creation started (background)");

        // 2. Acquire desktop handle (blocks, but WebView2 env runs in parallel)
        var desktopHandle = GetDesktopHandleWithRetry(maxAttempts: 3, delayMs: 500);

        if (desktopHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                "Failed to obtain the desktop handle.\n\n"
                + "Please verify that:\n"
                + "  • explorer.exe is running\n"
                + "  • the Windows desktop is visible\n"
                + "  • no third-party wallpaper engine is active"
            );

        Log.Info($"Desktop handle acquired: 0x{desktopHandle:X}");

        // 3. Config + first run (fast)
        _config = Configuration.Load();
        Log.Info($"Config loaded — ActiveTheme={_config.ActiveTheme}");
        EnsureFirstRun();

        // 4. Pre-read theme JS in background while we set up the window
        var themeJsTask = Task.Run(PreReadActiveThemeJs);

        // 5. Create and show window
        _window = new ThemeWindow(desktopHandle);
        _window.MessageReceived += OnThemeMessage;
        _window.Show();
        Log.Info($"ThemeWindow shown — Handle=0x{_window.Handle:X}");

        // 6. Async: await env + theme JS, then init WebView2 and load theme
        _window.Invoke(async () =>
        {
            try
            {
                var env = await envTask;
                Log.Info("WebView2 environment ready");

                await _window.InitAsync(env);

                var themeJs = await themeJsTask;
                if (themeJs != null)
                    _window.LoadThemeContent(themeJs);
                else
                    LoadActiveTheme(); // fallback: read from disk
            }
            catch (Exception ex)
            {
                Log.Error("Failed during WebView2 init or theme load", ex);
            }
        });

        SetupTrayIcon();
        StartSystemInfoBroadcast();
        Log.Info("ThemeHost started successfully");
    }

    private string? PreReadActiveThemeJs()
    {
        try
        {
            var themePath = Path.Combine(Configuration.ThemesPath, _config.ActiveTheme);
            var manifestPath = Path.Combine(themePath, "theme.json");
            if (!File.Exists(manifestPath)) return null;

            var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
            if (manifest == null) return null;

            var entryPath = Path.Combine(themePath, manifest.Entry);
            if (!File.Exists(entryPath)) return null;

            var content = File.ReadAllText(entryPath);
            Log.Info($"Theme JS pre-read — {content.Length} chars");
            return content;
        }
        catch (Exception ex)
        {
            Log.Warn($"Pre-read failed, will load on main thread: {ex.Message}");
            return null;
        }
    }

    private void EnsureFirstRun()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, "Assets", "themes", "default");
        var targetPath = Path.Combine(Configuration.ThemesPath, "default");
        var targetManifest = Path.Combine(targetPath, "theme.json");

        if (!File.Exists(targetManifest) || !ThemeIsComplete(targetPath))
        {
            Log.Info("Installing/repairing default theme");
            Directory.CreateDirectory(Configuration.ThemesPath);

            if (Directory.Exists(bundledPath))
            {
                if (Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);
                CopyDirectory(bundledPath, targetPath);
                Log.Info($"Default theme copied to {targetPath}");
            }
            else
            {
                Log.Warn($"Bundled theme not found: {bundledPath}");
            }

            _config.ActiveTheme = "default";
            _config.Save();
        }
        else
        {
            Log.Info($"Default theme OK: {targetPath}");
        }
    }

    private static bool ThemeIsComplete(string themePath)
    {
        var manifestPath = Path.Combine(themePath, "theme.json");
        if (!File.Exists(manifestPath)) return false;

        try
        {
            var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
            if (manifest == null) return false;
            return File.Exists(Path.Combine(themePath, manifest.Entry));
        }
        catch { return false; }
    }

    private void LoadActiveTheme()
    {
        var themePath = Path.Combine(Configuration.ThemesPath, _config.ActiveTheme);
        var manifestPath = Path.Combine(themePath, "theme.json");

        Log.Info($"Loading theme '{_config.ActiveTheme}' — manifest={manifestPath}");

        if (!File.Exists(manifestPath))
        {
            Log.Error($"Manifest not found: {manifestPath}");
            MessageBox.Show(
                $"Theme '{_config.ActiveTheme}' not found.\nExpected: {manifestPath}",
                "Vitrine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
        if (manifest == null)
        {
            Log.Error("Failed to deserialize theme manifest");
            return;
        }

        var entryPath = Path.Combine(themePath, manifest.Entry);
        Log.Info($"Theme entry: {entryPath} (exists={File.Exists(entryPath)})");

        _window?.LoadTheme(themePath, manifest.Entry);
    }

    private void SwitchTheme(string themeName)
    {
        Log.Info($"Switching theme to '{themeName}'");
        _config.ActiveTheme = themeName;
        _config.Save();
        LoadActiveTheme();
        UpdateThemeMenuChecks();
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => RefreshThemeList();

        _themesMenu = new ToolStripMenuItem("Themes");
        RefreshThemeList();
        menu.Items.Add(_themesMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Vitrine",
            Visible = true,
            ContextMenuStrip = menu,
        };
    }

    private void RefreshThemeList()
    {
        if (_themesMenu == null) return;
        _themesMenu.DropDownItems.Clear();

        var themesPath = Configuration.ThemesPath;
        if (!Directory.Exists(themesPath)) return;

        foreach (var themeDir in Directory.GetDirectories(themesPath).OrderBy(d => d))
        {
            var manifestPath = Path.Combine(themeDir, "theme.json");
            if (!File.Exists(manifestPath)) continue;

            var name = Path.GetFileName(themeDir);
            string displayName = name;

            try
            {
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
                if (manifest?.Name is { Length: > 0 } n) displayName = n;
            }
            catch { /* use folder name */ }

            var item = new ToolStripMenuItem(displayName)
            {
                Tag = name,
                Checked = name == _config.ActiveTheme,
            };
            item.Click += (_, _) => SwitchTheme(name);
            _themesMenu.DropDownItems.Add(item);
        }
    }

    private void UpdateThemeMenuChecks()
    {
        if (_themesMenu == null) return;
        foreach (ToolStripMenuItem item in _themesMenu.DropDownItems)
            item.Checked = (string?)item.Tag == _config.ActiveTheme;
    }

    private void OnThemeMessage(object? sender, string messageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var type = doc.RootElement.GetProperty("type").GetString();

            if (type == "getSystemInfo")
            {
                var id = doc.RootElement.GetProperty("id").GetInt32();
                var info = _systemInfo.Collect();
                _window?.PostMessage($$"""{"type":"systemInfo","id":{{id}},"data":{{info}}}""");
            }
        }
        catch { /* ignore malformed messages */ }
    }

    private void StartSystemInfoBroadcast()
    {
        _systemInfo.InfoUpdated += json =>
        {
            try
            {
                _window?.Invoke(() =>
                    _window.PostMessage($$"""{"type":"systemInfo","data":{{json}}}"""));
            }
            catch (ObjectDisposedException) { }
        };

        _systemInfo.Start(intervalMs: 2000);
    }

    private static Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "vitrine.ico");
        if (File.Exists(iconPath))
            return new Icon(iconPath);

        return SystemIcons.Application;
    }

    private void Shutdown()
    {
        Log.Info("Shutting down");
        _systemInfo.Dispose();
        _window?.Close();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        Application.Exit();
    }

    private static IntPtr GetDesktopHandleWithRetry(int maxAttempts, int delayMs)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Log.Info($"GetDesktopHandle attempt {i + 1}/{maxAttempts}");
            IntPtr handle = DesktopAttacher.GetDesktopHandle();
            if (handle != IntPtr.Zero)
                return handle;

            if (i < maxAttempts - 1)
                Thread.Sleep(delayMs);
        }
        return IntPtr.Zero;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    public void Dispose()
    {
        _systemInfo.Dispose();
        _trayIcon?.Dispose();
    }
}
