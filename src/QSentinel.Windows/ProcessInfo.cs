namespace QSentinel;

public sealed class ProcessInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public double CpuPercent { get; set; }
    public double MemoryMb { get; init; }
    public string Status { get; set; } = "NORMAL";
    public bool Protected { get; set; }
}
