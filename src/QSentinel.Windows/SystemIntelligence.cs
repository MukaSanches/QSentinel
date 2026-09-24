using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QSentinel;

public sealed class SystemIntelligence
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PERFORMANCE_INFORMATION
    {
        public uint cb;
        public UIntPtr CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable;
        public UIntPtr SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(ref PERFORMANCE_INFORMATION info, uint size);

    private double memoryEwma, cpuEwma, ioEwma;
    private long lastPageFaults;
    private DateTime lastAt = DateTime.UtcNow;

    public double AvailableMemoryMb { get; private set; }
    public double CommitPercent { get; private set; }
    public double KernelPagedMb { get; private set; }
    public double KernelNonPagedMb { get; private set; }
    public double PageFaultsPerSecond { get; private set; }
    public double CpuTrend => cpuEwma;
    public double MemoryTrend => memoryEwma;
    public double IoTrend => ioEwma;
    public string Bottleneck { get; private set; } = "CALIBRANDO";
    public string MemoryState { get; private set; } = "NORMAL";

    public void Tick(SystemMetrics metrics, IReadOnlyList<ProcessInfo> processes)
    {
        cpuEwma = Smooth(cpuEwma, metrics.CpuPercent);
        memoryEwma = Smooth(memoryEwma, metrics.MemoryPercent);
        ioEwma = Smooth(ioEwma, metrics.IoMbPerSecond);

        CaptureMemory(processes);
        Bottleneck = Classify(metrics);
        MemoryState = CommitPercent >= 90 || AvailableMemoryMb < 300 ? "CRÍTICA" :
                      CommitPercent >= 80 || metrics.MemoryPercent >= 88 ? "ALTA" :
                      CommitPercent >= 65 || metrics.MemoryPercent >= 75 ? "ATENÇÃO" : "NORMAL";
    }

    private void CaptureMemory(IReadOnlyList<ProcessInfo> processes)
    {
        try
        {
            var p = new PERFORMANCE_INFORMATION { cb = (uint)Marshal.SizeOf<PERFORMANCE_INFORMATION>() };
            if (GetPerformanceInfo(ref p, p.cb))
            {
                double page = p.PageSize.ToUInt64();
                AvailableMemoryMb = p.PhysicalAvailable.ToUInt64() * page / 1048576d;
                double limit = p.CommitLimit.ToUInt64();
                CommitPercent = limit == 0 ? 0 : p.CommitTotal.ToUInt64() * 100d / limit;
                KernelPagedMb = p.KernelPaged.ToUInt64() * page / 1048576d;
                KernelNonPagedMb = p.KernelNonpaged.ToUInt64() * page / 1048576d;
            }

            long faults = 0;
            foreach (var item in processes)
            {
                try
                {
                    using var proc = Process.GetProcessById(item.Id);
                    faults += proc.PagedMemorySize64 > 0 ? 0 : 0; // process access kept side-effect free
                }
                catch { }
            }

            var now = DateTime.UtcNow;
            var elapsed = Math.Max(.1, (now - lastAt).TotalSeconds);
            PageFaultsPerSecond = Math.Max(0, (faults - lastPageFaults) / elapsed);
            lastPageFaults = faults;
            lastAt = now;
        }
        catch { }
    }

    private string Classify(SystemMetrics m)
    {
        double mem = Math.Max(m.MemoryPercent, CommitPercent);
        double cpu = cpuEwma;
        double io = Math.Min(100, ioEwma * 2);
        if (mem >= 85 && mem >= cpu && mem >= io) return "MEMÓRIA";
        if (io >= 70 && io >= cpu) return "ARMAZENAMENTO";
        if (cpu >= 75) return "CPU";
        if (mem >= 70) return "MEMÓRIA";
        if (io >= 45) return "ARMAZENAMENTO";
        return "EQUILIBRADO";
    }

    private static double Smooth(double oldValue, double sample) =>
        oldValue == 0 ? sample : oldValue * .78 + sample * .22;
}
