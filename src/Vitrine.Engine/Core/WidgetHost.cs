using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Vitrine.Engine.Widgets;

namespace Vitrine.Engine.Core;

internal class WidgetHost
{
    private IntPtr _workerW;

    internal void Start()
    {
        _workerW = GetWorkerWWithRetry(maxAttempts: 3, delayMs: 500);

        if (_workerW == IntPtr.Zero)
            throw new InvalidOperationException(
                "Impossibile ottenere il WorkerW handle.\n\n"
                + "Verifica che:\n"
                + "  • explorer.exe sia in esecuzione\n"
                + "  • il desktop di Windows sia visibile\n"
                + "  • non sia attivo un wallpaper engine di terze parti"
            );

        LoadWidget("welcome", x: 40, y: 40, width: 320, height: 130);
    }

    private static IntPtr GetWorkerWWithRetry(int maxAttempts, int delayMs)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            IntPtr handle = DesktopAttacher.GetWorkerW();
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
            throw new FileNotFoundException($"Widget '{name}' non trovato: {htmlPath}");

        var widget = new WidgetWindow(htmlPath)
        {
            Left = x,
            Top = y,
            Width = width,
            Height = height,
        };

        widget.Show();
        DesktopAttacher.SetParent(widget.Handle, _workerW);
    }
}
