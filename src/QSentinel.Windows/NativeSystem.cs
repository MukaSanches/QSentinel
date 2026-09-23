using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QSentinel;

internal static class NativeSystem
{
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

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint Low;
        public uint High;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_POWER_THROTTLING_STATE
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_PRIORITY_INFORMATION
    {
        public uint MemoryPriority;
    }

    private enum PROCESS_INFORMATION_CLASS
    {
        ProcessMemoryPriority = 0,
        ProcessPowerThrottling = 4
    }

    private const uint PowerVersion = 1;
    private const uint ExecutionSpeed = 0x1;

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(
        out FILETIME idleTime,
        out FILETIME kernelTime,
        out FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessIoCounters(
        IntPtr processHandle,
        out IO_COUNTERS ioCounters);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "SetProcessInformation",
        SetLastError = true)]
    private static extern bool SetProcessInformationPower(
        IntPtr hProcess,
        PROCESS_INFORMATION_CLASS infoClass,
        ref PROCESS_POWER_THROTTLING_STATE state,
        uint size);

    [DllImport(
        "kernel32.dll",
        EntryPoint = "SetProcessInformation",
        SetLastError = true)]
    private static extern bool SetProcessInformationMemory(
        IntPtr hProcess,
        PROCESS_INFORMATION_CLASS infoClass,
        ref MEMORY_PRIORITY_INFORMATION state,
        uint size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId);

    private static ulong? lastIdle;
    private static ulong? lastKernel;
    private static ulong? lastUser;

    private static ulong ToUInt64(FILETIME x) =>
        ((ulong)x.High << 32) | x.Low;

    public static double MemoryPercent()
    {
        try
        {
            var status = new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            return GlobalMemoryStatusEx(ref status)
                ? status.dwMemoryLoad
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static double CpuPercent()
    {
        try
        {
            if (!GetSystemTimes(
                out var idle,
                out var kernel,
                out var user))
                return 0;

            ulong i = ToUInt64(idle);
            ulong k = ToUInt64(kernel);
            ulong u = ToUInt64(user);

            if (lastIdle is null)
            {
                lastIdle = i;
                lastKernel = k;
                lastUser = u;
                return 0;
            }

            ulong idleDelta = i - lastIdle.Value;
            ulong kernelDelta = k - lastKernel!.Value;
            ulong userDelta = u - lastUser!.Value;

            lastIdle = i;
            lastKernel = k;
            lastUser = u;

            ulong total = kernelDelta + userDelta;

            return total == 0
                ? 0
                : Math.Clamp(
                    (total - idleDelta) * 100d / total,
                    0,
                    100);
        }
        catch
        {
            return 0;
        }
    }

    public static int ForegroundPid()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return -1;

            GetWindowThreadProcessId(hwnd, out uint pid);
            return (int)pid;
        }
        catch
        {
            return -1;
        }
    }

    public static ulong IoBytes(Process process)
    {
        try
        {
            if (GetProcessIoCounters(process.Handle, out var io))
            {
                return
                    io.ReadTransferCount +
                    io.WriteTransferCount +
                    io.OtherTransferCount;
            }
        }
        catch { }

        return 0;
    }

    public static void SetEco(Process process, bool enabled)
    {
        try
        {
            var state = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PowerVersion,
                ControlMask = ExecutionSpeed,
                StateMask = enabled ? ExecutionSpeed : 0
            };

            SetProcessInformationPower(
                process.Handle,
                PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                ref state,
                (uint)Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>());
        }
        catch { }
    }

    public static void SetMemoryPriority(Process process, uint priority)
    {
        try
        {
            var state = new MEMORY_PRIORITY_INFORMATION
            {
                MemoryPriority = priority
            };

            SetProcessInformationMemory(
                process.Handle,
                PROCESS_INFORMATION_CLASS.ProcessMemoryPriority,
                ref state,
                (uint)Marshal.SizeOf<MEMORY_PRIORITY_INFORMATION>());
        }
        catch { }
    }
}
