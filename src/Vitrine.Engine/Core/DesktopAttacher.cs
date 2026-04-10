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

    [DllImport("user32.dll")]
    internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    internal static IntPtr GetWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
            return IntPtr.Zero;

        // Spawn a WorkerW behind the desktop icons.
        // Try both common parameter combinations for compatibility.
        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(0x1),
            0x0, 2000, out _);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            0x0, 2000, out _);

        // Give Explorer time to create the WorkerW
        Thread.Sleep(100);

        IntPtr workerW = IntPtr.Zero;

        EnumWindows((hwnd, _) =>
        {
            IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shell != IntPtr.Zero)
                workerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            return true;
        }, IntPtr.Zero);

        // Fallback: if no WorkerW was found, check if SHELLDLL_DefView
        // is directly under Progman (some Windows configurations)
        if (workerW == IntPtr.Zero)
        {
            IntPtr shell = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shell != IntPtr.Zero)
                workerW = progman;
        }

        return workerW;
    }
}
