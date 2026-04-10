using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Vitrine.Engine.Widgets;

namespace Vitrine.Engine.Core;

internal class WidgetHost
{
    private IntPtr _desktopHandle;
    private NotifyIcon? _trayIcon;

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

        SetupTrayIcon();
        LoadWidget("welcome", x: 40, y: 40, width: 320, height: 130);
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

    private void LoadWidget(string name, int x, int y, int width, int height)
    {
        var htmlPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets", "widgets", name, "index.html"
        );

        if (!File.Exists(htmlPath))
            throw new FileNotFoundException($"Widget '{name}' not found: {htmlPath}");

        var widget = new WidgetWindow(htmlPath, _desktopHandle)
        {
            Left = x,
            Top = y,
            Width = width,
            Height = height,
        };

        widget.Show();
    }
}
