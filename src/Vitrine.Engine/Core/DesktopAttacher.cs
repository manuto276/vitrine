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

        // Tight poll: check every 10ms for up to 200ms instead of sleeping 100ms
        if (PollForSplit(progman, maxAttempts: 20, intervalMs: 10))
        {
            Log.Info("SHELLDLL_DefView split detected");
        }
        else
        {
            Log.Warn("Split not detected, retrying with alt params");
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                0x0, 2000, out _);
            PollForSplit(progman, maxAttempts: 10, intervalMs: 10);
        }

        Log.Info($"Returning Progman handle 0x{progman:X}");
        return progman;
    }

    private static bool PollForSplit(IntPtr progman, int maxAttempts, int intervalMs)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Thread.Sleep(intervalMs);
            if (IsSplitDone(progman))
                return true;
        }
        return false;
    }

    private static bool IsSplitDone(IntPtr progman)
    {
        bool found = false;
        EnumWindows((hwnd, _) =>
        {
            IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shell != IntPtr.Zero && hwnd != progman)
            {
                found = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
