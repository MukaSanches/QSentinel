namespace QSentinel;

public sealed class ProcessInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public double CpuPercent { get; set; }
    public double MemoryMb { get; init; }
    public double IoMbPerSecond { get; set; }
    public string Status { get; set; } = "NORMAL";
    public bool Protected { get; set; }
    public bool Foreground { get; set; }
}

public sealed class SystemMetrics
{
    public double CpuPercent { get; init; }
    public double MemoryPercent { get; init; }
    public double IoMbPerSecond { get; init; }
    public int ProcessCount { get; init; }

    public double Pressure =>
        Math.Clamp(
            CpuPercent * 0.45 +
            MemoryPercent * 0.40 +
            Math.Min(IoMbPerSecond * 2.0, 100.0) * 0.15,
            0,
            100);
}

public sealed class MonitorSnapshot
{
    public required SystemMetrics Metrics { get; init; }
    public required List<ProcessInfo> Processes { get; init; }
}
