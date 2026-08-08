using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ActionOrbit.App.Services.MiniTools;

internal sealed class SystemMetricsReader
{
    private ulong _previousIdle;
    private ulong _previousTotal;
    private bool _hasCpuBaseline;

    public SystemMetricsSnapshot Read()
    {
        var cpu = ReadCpuPercentage();
        var (memoryPercent, usedMemory, totalMemory) = ReadMemory();
        var (batteryPercent, isCharging, hasBattery) = ReadPower();
        return new SystemMetricsSnapshot(
            cpu,
            memoryPercent,
            usedMemory,
            totalMemory,
            batteryPercent,
            isCharging,
            hasBattery);
    }

    private double ReadCpuPercentage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0;
        }

        var idle = ToUInt64(idleTime);
        var total = ToUInt64(kernelTime) + ToUInt64(userTime);
        if (!_hasCpuBaseline)
        {
            _previousIdle = idle;
            _previousTotal = total;
            _hasCpuBaseline = true;
            return 0;
        }

        var totalDelta = total - _previousTotal;
        var idleDelta = idle - _previousIdle;
        _previousIdle = idle;
        _previousTotal = total;
        return totalDelta == 0
            ? 0
            : Math.Clamp(100d * (totalDelta - Math.Min(idleDelta, totalDelta)) / totalDelta, 0, 100);
    }

    private static (double Percent, ulong Used, ulong Total) ReadMemory()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };
        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhysical == 0)
        {
            return (0, 0, 0);
        }

        var used = status.TotalPhysical - status.AvailablePhysical;
        return (100d * used / status.TotalPhysical, used, status.TotalPhysical);
    }

    private static (int Percent, bool IsCharging, bool HasBattery) ReadPower()
    {
        if (!GetSystemPowerStatus(out var status) || status.BatteryLifePercent == byte.MaxValue)
        {
            return (0, false, false);
        }

        var hasBattery = (status.BatteryFlag & 128) == 0;
        var isCharging = status.AcLineStatus == 1 || (status.BatteryFlag & 8) != 0;
        return (status.BatteryLifePercent, isCharging, hasBattery);
    }

    private static ulong ToUInt64(FILETIME value) =>
        ((ulong)(uint)value.dwHighDateTime << 32) | (uint)value.dwLowDateTime;

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
}

internal sealed record SystemMetricsSnapshot(
    double CpuPercent,
    double MemoryPercent,
    ulong UsedMemoryBytes,
    ulong TotalMemoryBytes,
    int BatteryPercent,
    bool IsCharging,
    bool HasBattery);
