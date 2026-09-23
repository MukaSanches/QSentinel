using System.Diagnostics;

namespace QSentinel;

public sealed class OptimizationEngine
{
    private readonly AppSettings settings;

    private readonly Dictionary<int, ProcessPriorityClass> originalPriority =
        new();

    private readonly HashSet<int> optimized =
        new();

    public long InterventionCount { get; private set; }
    public long BlockedLaunches { get; private set; }

    public int OptimizedCount => optimized.Count;

    public OptimizationEngine(AppSettings settings)
    {
        this.settings = settings;
    }

    public bool IsOptimized(int pid) =>
        optimized.Contains(pid);

    public void Tick(
        IReadOnlyList<ProcessInfo> processes,
        SystemMetrics metrics,
        bool force = false)
    {
        EnforceBlockList(processes);

        if (!settings.OptimizationEnabled)
        {
            RestoreAll();
            return;
        }

        double pressure =
            force
                ? Math.Max(metrics.Pressure, 70)
                : metrics.Pressure;

        if (pressure < 35)
        {
            RestoreAll();
            return;
        }

        int limit =
            pressure >= 80 ? 8 :
            pressure >= 65 ? 5 :
            3;

        var candidates =
            processes
                .Where(SafetyEngine.CanOptimize)
                .Where(x => !settings.IsBlocked(x.Name))
                .Where(x =>
                    x.CpuPercent >= 1 ||
                    x.MemoryMb >= 400 ||
                    x.IoMbPerSecond >= 1)
                .OrderByDescending(
                    x =>
                        x.CpuPercent * 4 +
                        x.MemoryMb / 200d +
                        x.IoMbPerSecond * 3)
                .Take(limit)
                .ToList();

        var targets =
            candidates
                .Select(x => x.Id)
                .ToHashSet();

        foreach (int pid in optimized.ToList())
        {
            if (!targets.Contains(pid))
                RestoreProcess(pid);
        }

        foreach (var candidate in candidates)
            Optimize(candidate, metrics);
    }

    private void Optimize(
        ProcessInfo info,
        SystemMetrics metrics)
    {
        try
        {
            using var process =
                Process.GetProcessById(info.Id);

            ProcessPriorityClass original;

            try
            {
                original = process.PriorityClass;
            }
            catch
            {
                return;
            }

            if (original is
                ProcessPriorityClass.High or
                ProcessPriorityClass.RealTime or
                ProcessPriorityClass.AboveNormal)
                return;

            if (!originalPriority.ContainsKey(info.Id))
                originalPriority[info.Id] = original;

            try
            {
                process.PriorityClass =
                    ProcessPriorityClass.BelowNormal;
            }
            catch { }

            NativeSystem.SetEco(process, true);

            if (metrics.MemoryPercent >= 72 &&
                info.MemoryMb >= 400)
            {
                NativeSystem.SetMemoryPriority(
                    process,
                    3);
            }

            if (optimized.Add(info.Id))
                InterventionCount++;
        }
        catch
        {
            optimized.Remove(info.Id);
            originalPriority.Remove(info.Id);
        }
    }

    public void RestoreAll()
    {
        foreach (int pid in optimized.ToList())
            RestoreProcess(pid);
    }

    private void RestoreProcess(int pid)
    {
        try
        {
            using var process =
                Process.GetProcessById(pid);

            if (originalPriority.TryGetValue(
                pid,
                out var priority))
            {
                try
                {
                    process.PriorityClass = priority;
                }
                catch { }
            }

            NativeSystem.SetEco(process, false);
            NativeSystem.SetMemoryPriority(process, 5);
        }
        catch { }

        optimized.Remove(pid);
        originalPriority.Remove(pid);
    }

    private void EnforceBlockList(
        IReadOnlyList<ProcessInfo> processes)
    {
        foreach (var info in processes)
        {
            if (!settings.IsBlocked(info.Name))
                continue;

            if (!SafetyEngine.CanOptimize(info))
                continue;

            try
            {
                using var process =
                    Process.GetProcessById(info.Id);

                process.Kill(
                    entireProcessTree: false);

                BlockedLaunches++;
            }
            catch { }
        }
    }
}
