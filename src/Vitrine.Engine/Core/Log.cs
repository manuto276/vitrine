using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Vitrine.Engine.Core;

internal static class Log
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vitrine", "logs");

    private static readonly string LogFile = Path.Combine(
        LogDir, $"vitrine-{DateTime.Now:yyyy-MM-dd}.log");

    private static readonly object Lock = new();

    [Conditional("DEBUG")]
    internal static void Info(string message, [CallerMemberName] string? caller = null)
        => Write("INF", caller, message);

    [Conditional("DEBUG")]
    internal static void Error(string message, Exception? ex = null, [CallerMemberName] string? caller = null)
        => Write("ERR", caller, ex != null ? $"{message} → {ex}" : message);

    [Conditional("DEBUG")]
    internal static void Warn(string message, [CallerMemberName] string? caller = null)
        => Write("WRN", caller, message);

    private static void Write(string level, string? caller, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{caller}] {message}";
            lock (Lock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
        }
        catch { /* logging must never crash the app */ }
    }
}
