using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Vitrine.Engine.SystemInfo;

internal class SystemInfoProvider : IDisposable
{
    private global::System.Threading.Timer? _timer;
    private long _prevIdleTime, _prevKernelTime, _prevUserTime;

    internal event Action<string>? InfoUpdated;

    internal void Start(int intervalMs = 2000)
    {
        GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdleTime = FileTimeToLong(idle);
        _prevKernelTime = FileTimeToLong(kernel);
        _prevUserTime = FileTimeToLong(user);

        _timer = new global::System.Threading.Timer(_ => Collect(), null, intervalMs, intervalMs);
    }

    internal string Collect()
    {
        var json = JsonSerializer.Serialize(new
        {
            system = GetSystemDetails(),
            cpu = GetCpuInfo(),
            memory = GetMemoryInfo(),
            battery = GetBatteryInfo(),
            drives = GetDriveInfo(),
            processes = GetTopProcesses()
        });

        InfoUpdated?.Invoke(json);
        return json;
    }

    private static object GetSystemDetails()
    {
        return new
        {
            hostname = Environment.MachineName,
            os = RuntimeInformation.OSDescription,
            uptime = Environment.TickCount64 / 1000
        };
    }

    private object GetCpuInfo()
    {
        GetSystemTimes(out var idle, out var kernel, out var user);

        long idleTime = FileTimeToLong(idle);
        long kernelTime = FileTimeToLong(kernel);
        long userTime = FileTimeToLong(user);

        long idleDiff = idleTime - _prevIdleTime;
        long kernelDiff = kernelTime - _prevKernelTime;
        long userDiff = userTime - _prevUserTime;

        long totalDiff = kernelDiff + userDiff;
        double usage = totalDiff > 0
            ? Math.Round((1.0 - (double)idleDiff / totalDiff) * 100.0, 1)
            : 0;

        _prevIdleTime = idleTime;
        _prevKernelTime = kernelTime;
        _prevUserTime = userTime;

        return new { usage, cores = Environment.ProcessorCount };
    }

    private static object GetMemoryInfo()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref status);

        return new
        {
            total = status.ullTotalPhys,
            available = status.ullAvailPhys,
            used = status.ullTotalPhys - status.ullAvailPhys,
            load = status.dwMemoryLoad
        };
    }

    private static object[] GetDriveInfo()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new
            {
                name = d.Name,
                label = d.VolumeLabel,
                total = d.TotalSize,
                free = d.AvailableFreeSpace,
                used = d.TotalSize - d.AvailableFreeSpace
            })
            .ToArray<object>();
    }

    private static object GetBatteryInfo()
    {
        var status = new SYSTEM_POWER_STATUS();
        if (!GetSystemPowerStatus(ref status))
            return new { hasBattery = false };

        // BatteryFlag 128 = no system battery
        bool hasBattery = (status.BatteryFlag & 128) == 0;
        if (!hasBattery)
            return new { hasBattery = false };

        return new
        {
            hasBattery = true,
            charging = status.ACLineStatus == 1,
            level = (int)status.BatteryLifePercent > 100 ? -1 : (int)status.BatteryLifePercent,
            // -1 = unknown
            remainingSeconds = status.BatteryLifeTime == unchecked((uint)-1) ? -1 : (int)status.BatteryLifeTime,
            powerSource = status.ACLineStatus == 1 ? "ac" : "battery"
        };
    }

    private static object[] GetTopProcesses(int count = 5)
    {
        try
        {
            return Process.GetProcesses()
                .Select(p =>
                {
                    try { return new { p.ProcessName, p.Id, Mem = p.WorkingSet64 }; }
                    catch { return null; }
                })
                .Where(p => p != null && p.Mem > 0)
                .OrderByDescending(p => p!.Mem)
                .Take(count)
                .Select(p => (object)new { name = p!.ProcessName, pid = p.Id, memory = p.Mem })
                .ToArray();
        }
        catch { return []; }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    #region Native

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;       // 0=offline, 1=online, 255=unknown
        public byte BatteryFlag;        // 1=high, 2=low, 4=critical, 8=charging, 128=no battery
        public byte BatteryLifePercent; // 0-100, 255=unknown
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;    // seconds remaining, -1=unknown
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);

    private static long FileTimeToLong(FILETIME ft) =>
        ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    #endregion
}
