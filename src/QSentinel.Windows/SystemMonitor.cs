using System.Diagnostics;

namespace QSentinel;

public sealed class SystemMonitor
{
    private sealed class Previous
    {
        public double CpuMs;
        public DateTime At;
        public ulong IoBytes;
    }

    private readonly Dictionary<int, Previous> previous = new();

    public MonitorSnapshot Capture()
    {
        var items = new List<ProcessInfo>();
        var seen = new HashSet<int>();

        int foregroundPid = NativeSystem.ForegroundPid();
        DateTime now = DateTime.UtcNow;
        double totalIo = 0;

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                seen.Add(process.Id);

                string name = process.ProcessName;
                double cpuMs = process.TotalProcessorTime.TotalMilliseconds;
                ulong ioBytes = NativeSystem.IoBytes(process);

                double cpu = 0;
                double io = 0;

                if (previous.TryGetValue(process.Id, out var old))
                {
                    double elapsedMs = (now - old.At).TotalMilliseconds;

                    if (elapsedMs > 100)
                    {
                        double deltaCpu =
                            Math.Max(0, cpuMs - old.CpuMs);

                        cpu = Math.Clamp(
                            deltaCpu /
                            elapsedMs /
                            Math.Max(1, Environment.ProcessorCount) *
                            100d,
                            0,
                            100);

                        ulong deltaIo =
                            ioBytes >= old.IoBytes
                                ? ioBytes - old.IoBytes
                                : 0;

                        io =
                            deltaIo /
                            1024d /
                            1024d /
                            (elapsedMs / 1000d);
                    }
                }

                previous[process.Id] =
                    new Previous
                    {
                        CpuMs = cpuMs,
                        At = now,
                        IoBytes = ioBytes
                    };

                var item = new ProcessInfo
                {
                    Id = process.Id,
                    Name = name,
                    CpuPercent = cpu,
                    MemoryMb =
                        process.WorkingSet64 /
                        1024d /
                        1024d,
                    IoMbPerSecond = io,
                    Protected =
                        SafetyEngine.IsProtected(
                            name,
                            process.Id),
                    Foreground =
                        process.Id == foregroundPid
                };

                item.Status =
                    SafetyEngine.Classify(item);

                totalIo += io;
                items.Add(item);
            }
            catch
            {
                // O processo terminou ou o Windows negou acesso.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (int stale in
            previous.Keys
                .Where(x => !seen.Contains(x))
                .ToList())
        {
            previous.Remove(stale);
        }

        items = items
            .OrderByDescending(
                x =>
                    x.CpuPercent * 4 +
                    x.MemoryMb / 200d +
                    x.IoMbPerSecond * 3)
            .ToList();

        return new MonitorSnapshot
        {
            Metrics = new SystemMetrics
            {
                CpuPercent = NativeSystem.CpuPercent(),
                MemoryPercent = NativeSystem.MemoryPercent(),
                IoMbPerSecond = totalIo,
                ProcessCount = items.Count
            },
            Processes = items
        };
    }
}
