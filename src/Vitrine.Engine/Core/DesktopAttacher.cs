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

    internal static IntPtr GetDesktopHandle()
    {
        IntPtr progman = FindWindow("Progman", null);
        Log.Info($"FindWindow('Progman') = 0x{progman:X}");

        if (progman == IntPtr.Zero)
            return IntPtr.Zero;

        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(0x1),
            0x0, 2000, out _);
        Log.Info("Sent 0x052C to Progman");

        Thread.Sleep(100);

        bool splitSucceeded = false;
        EnumWindows((hwnd, _) =>
        {
            IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shell != IntPtr.Zero && hwnd != progman)
            {
                Log.Info($"SHELLDLL_DefView found in WorkerW 0x{hwnd:X}");
                splitSucceeded = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (!splitSucceeded)
        {
            Log.Warn("Split not detected, retrying with alt params");
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                0x0, 2000, out _);
            Thread.Sleep(100);
        }

        Log.Info($"Returning Progman handle 0x{progman:X}");
        return progman;
    }
}
