using System.Diagnostics;

namespace QSentinel;

public sealed class OptimizationEngine
{
    private readonly AppSettings settings;
    private readonly Dictionary<int, ProcessPriorityClass> originalPriority = new();
    private readonly HashSet<int> optimized = new();
    private double adaptivePressure;
    private int calmTicks;
    private readonly AdaptivePolicy policy = new();
    private readonly SystemIntelligence intelligence = new();

    public string DominantBottleneck => policy.DominantBottleneck;
    public int BottleneckConfidence => policy.Confidence;
    public double AvailableMemoryMb => intelligence.AvailableMemoryMb;
    public double CommitPercent => intelligence.CommitPercent;
    public string MemoryState => intelligence.MemoryState;

    public long InterventionCount { get; private set; }
    public long BlockedLaunches { get; private set; }
    public int OptimizedCount => optimized.Count;
    public double AdaptivePressure => adaptivePressure;

    public OptimizationEngine(AppSettings settings) => this.settings = settings;

    public bool IsOptimized(int pid) => optimized.Contains(pid);

    public void Tick(IReadOnlyList<ProcessInfo> processes, SystemMetrics metrics, bool force = false)
    {
        EnforceBlockList(processes);
        intelligence.Tick(metrics, processes);
        policy.Tick(intelligence);

        if (!settings.OptimizationEnabled)
        {
            RestoreAll();
            adaptivePressure = 0;
            calmTicks = 0;
            return;
        }

        // EWMA prevents one short spike from causing process churn.
        double observed = force ? Math.Max(metrics.Pressure, 75) : metrics.Pressure;
        adaptivePressure = adaptivePressure == 0
            ? observed
            : adaptivePressure * 0.72 + observed * 0.28;

        // Hysteresis: enter optimization under pressure, leave only after sustained calm.
        if (!force && adaptivePressure < 32)
        {
            calmTicks++;
            if (calmTicks >= 4) RestoreAll();
            return;
        }
        calmTicks = 0;

        if (!force && adaptivePressure < 42 && optimized.Count == 0)
            return;

        int limit =
            adaptivePressure >= 85 ? 8 :
            adaptivePressure >= 70 ? 5 :
            adaptivePressure >= 52 ? 3 : 2;

        var candidates = processes
            .Where(SafetyEngine.CanOptimize)
            .Where(x => !settings.IsBlocked(x.Name))
            .Where(x => x.CpuPercent >= 1 || x.MemoryMb >= 400 || x.IoMbPerSecond >= 1)
            .Select(x => new
            {
                Process = x,
                Score = Score(x, metrics, policy)
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.Process)
            .ToList();

        var targets = candidates.Select(x => x.Id).ToHashSet();

        foreach (int pid in optimized.ToList())
            if (!targets.Contains(pid)) RestoreProcess(pid);

        foreach (var candidate in candidates)
            Optimize(candidate, metrics);
    }

    private static double Score(ProcessInfo p, SystemMetrics metrics, AdaptivePolicy policy)
    {
        // Weight the resource that is actually under pressure instead of applying
        // the same fixed rule to every machine and workload.
        double cpuWeight = Math.Max(policy.CpuWeight, metrics.CpuPercent >= 70 ? 5.0 : 2.5);
        double memWeight = Math.Max(policy.MemoryWeight, metrics.MemoryPercent >= 75 ? 1.5 : 0.7);
        double ioWeight = Math.Max(policy.IoWeight, metrics.IoMbPerSecond >= 20 ? 5.0 : 2.0);

        return p.CpuPercent * cpuWeight
             + (p.MemoryMb / 200d) * memWeight
             + p.IoMbPerSecond * ioWeight;
    }

    private void Optimize(ProcessInfo info, SystemMetrics metrics)
    {
        try
        {
            using var process = Process.GetProcessById(info.Id);
            ProcessPriorityClass original;
            try { original = process.PriorityClass; }
            catch { return; }

            if (original is ProcessPriorityClass.High or ProcessPriorityClass.RealTime or ProcessPriorityClass.AboveNormal)
                return;

            if (!originalPriority.ContainsKey(info.Id))
                originalPriority[info.Id] = original;

            // Only lower scheduler priority when CPU pressure is material.
            if (metrics.CpuPercent >= 65)
            {
                try { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
                catch { }
            }

            // EcoQoS is the documented Windows mechanism for non-foreground work.
            NativeSystem.SetEco(process, true);

            // Lower memory priority only under genuine memory pressure.
            if ((metrics.MemoryPercent >= 82 || intelligence.CommitPercent >= 80 || intelligence.AvailableMemoryMb < 600) && info.MemoryMb >= 400)
                NativeSystem.SetMemoryPriority(process, 3);

            if (optimized.Add(info.Id)) InterventionCount++;
        }
        catch
        {
            optimized.Remove(info.Id);
            originalPriority.Remove(info.Id);
        }
    }

    public void RestoreAll()
    {
        foreach (int pid in optimized.ToList()) RestoreProcess(pid);
    }

    private void RestoreProcess(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (originalPriority.TryGetValue(pid, out var priority))
            {
                try { process.PriorityClass = priority; }
                catch { }
            }

            NativeSystem.ResetEco(process);
            NativeSystem.SetMemoryPriority(process, 5);
        }
        catch { }

        optimized.Remove(pid);
        originalPriority.Remove(pid);
    }

    private void EnforceBlockList(IReadOnlyList<ProcessInfo> processes)
    {
        foreach (var info in processes)
        {
            if (!settings.IsBlocked(info.Name) || !SafetyEngine.CanOptimize(info)) continue;
            try
            {
                using var process = Process.GetProcessById(info.Id);
                process.Kill(entireProcessTree: false);
                BlockedLaunches++;
            }
            catch { }
        }
    }
}
