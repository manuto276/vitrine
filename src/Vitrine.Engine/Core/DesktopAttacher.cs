using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Vitrine.Engine.Core;

internal static class DesktopAttacher
{
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    /// <summary>
    /// Returns the window handle to embed widgets behind desktop icons.
    /// After sending 0x052C, SHELLDLL_DefView (icons) moves to a WorkerW,
    /// and Progman becomes the background layer — that's where we draw.
    /// </summary>
    internal static IntPtr GetDesktopHandle()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
            return IntPtr.Zero;

        // Tell Progman to spawn a WorkerW and move SHELLDLL_DefView into it.
        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(0x1),
            0x0, 2000, out _);

        Thread.Sleep(100);

        // Verify the split happened: SHELLDLL_DefView should now be in a WorkerW,
        // not directly under Progman.
        bool splitSucceeded = false;
        EnumWindows((hwnd, _) =>
        {
            IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shell != IntPtr.Zero && hwnd != progman)
            {
                splitSucceeded = true;
                return false; // stop enumerating
            }
            return true;
        }, IntPtr.Zero);

        if (!splitSucceeded)
        {
            // Retry with alternative parameters
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                0x0, 2000, out _);
            Thread.Sleep(100);
        }

        // Progman is now the background layer behind desktop icons
        return progman;
    }
}
