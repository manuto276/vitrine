using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
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
        var desktopHandle = GetDesktopHandleWithRetry(maxAttempts: 3, delayMs: 500);

        if (desktopHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                "Failed to obtain the desktop handle.\n\n"
                + "Please verify that:\n"
                + "  • explorer.exe is running\n"
                + "  • the Windows desktop is visible\n"
                + "  • no third-party wallpaper engine is active"
            );

        _config = Configuration.Load();
        EnsureFirstRun();

        _window = new ThemeWindow(desktopHandle);
        _window.MessageReceived += OnThemeMessage;
        _window.Show();

        // InitAsync must be awaited before loading theme
        _window.Invoke(async () =>
        {
            await _window.InitAsync();
            LoadActiveTheme();
        });

        SetupTrayIcon();
        StartSystemInfoBroadcast();
    }

    private void EnsureFirstRun()
    {
        var themesPath = Configuration.ThemesPath;
        if (Directory.Exists(themesPath))
            return;

        Directory.CreateDirectory(themesPath);

        var bundledPath = Path.Combine(AppContext.BaseDirectory, "Assets", "themes", "default");
        var targetPath = Path.Combine(themesPath, "default");

        if (Directory.Exists(bundledPath))
            CopyDirectory(bundledPath, targetPath);

        _config.ActiveTheme = "default";
        _config.Save();
    }

    private void LoadActiveTheme()
    {
        var themePath = Path.Combine(Configuration.ThemesPath, _config.ActiveTheme);
        var manifestPath = Path.Combine(themePath, "theme.json");

        if (!File.Exists(manifestPath))
        {
            MessageBox.Show(
                $"Theme '{_config.ActiveTheme}' not found.\nExpected: {manifestPath}",
                "Vitrine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
        if (manifest == null) return;

        _window?.LoadTheme(themePath, manifest.Entry);
    }

    private void SwitchTheme(string themeName)
    {
        _config.ActiveTheme = themeName;
        _config.Save();
        LoadActiveTheme();
        UpdateThemeMenuChecks();
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();

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
