using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Vitrine.Engine.SystemInfo;
using Vitrine.Engine.Widgets;

namespace Vitrine.Engine.Core;

internal class WidgetHost : IDisposable
{
    private IntPtr _desktopHandle;
    private NotifyIcon? _trayIcon;
    private readonly List<WidgetInstance> _widgets = new();
    private readonly SystemInfoProvider _systemInfo = new();
    private readonly string _widgetsPath;
    private readonly string _webViewDataPath;

    internal WidgetHost()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vitrine"
        );
        _widgetsPath = Path.Combine(appData, "widgets");
        _webViewDataPath = Path.Combine(appData, "webview2");
    }

    internal void Start()
    {
        _desktopHandle = GetDesktopHandleWithRetry(maxAttempts: 3, delayMs: 500);

        if (_desktopHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                "Failed to obtain the desktop handle.\n\n"
                + "Please verify that:\n"
                + "  • explorer.exe is running\n"
                + "  • the Windows desktop is visible\n"
                + "  • no third-party wallpaper engine is active"
            );

        EnsureFirstRun();
        SetupTrayIcon();
        LoadAllWidgets();
        StartSystemInfoBroadcast();
    }

    private void EnsureFirstRun()
    {
        if (Directory.Exists(_widgetsPath))
            return;

        Directory.CreateDirectory(_widgetsPath);

        // Copy the bundled welcome widget template to APPDATA
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Assets", "widgets", "welcome");
        var targetPath = Path.Combine(_widgetsPath, "welcome");

        if (Directory.Exists(templatePath))
            CopyDirectory(templatePath, targetPath);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));

        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    private void LoadAllWidgets()
    {
        if (!Directory.Exists(_widgetsPath))
            return;

        foreach (var widgetDir in Directory.GetDirectories(_widgetsPath))
        {
            var manifestPath = Path.Combine(widgetDir, "manifest.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<WidgetManifest>(json);
                if (manifest == null) continue;

                var widgetName = Path.GetFileName(widgetDir);
                var userDataFolder = Path.Combine(_webViewDataPath, widgetName);
                Directory.CreateDirectory(userDataFolder);

                var widget = new WidgetInstance(manifest, widgetDir, _desktopHandle, userDataFolder);

                widget.MessageReceived += OnWidgetMessage;
                widget.Show();

                _widgets.Add(widget);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load widget '{Path.GetFileName(widgetDir)}':\n\n{ex.Message}",
                    "Vitrine",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
    }

    private void OnWidgetMessage(object? sender, string messageJson)
    {
        if (sender is not WidgetInstance widget) return;

        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var type = doc.RootElement.GetProperty("type").GetString();

            if (type == "getSystemInfo")
            {
                var id = doc.RootElement.GetProperty("id").GetInt32();
                var info = _systemInfo.Collect();
                widget.PostSystemInfo(info, id);
            }
        }
        catch { /* ignore malformed messages */ }
    }

    private void StartSystemInfoBroadcast()
    {
        _systemInfo.InfoUpdated += json =>
        {
            foreach (var widget in _widgets)
                widget.PostSystemInfo(json);
        };

        _systemInfo.Start(intervalMs: 2000);
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Vitrine",
            Visible = true,
            ContextMenuStrip = menu,
        };
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

        foreach (var widget in _widgets)
            widget.Close();
        _widgets.Clear();

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

    public void Dispose()
    {
        _systemInfo.Dispose();
        _trayIcon?.Dispose();
    }
}
