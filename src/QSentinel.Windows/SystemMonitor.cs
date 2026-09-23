using System.Diagnostics;

namespace QSentinel;

public sealed class SystemMonitor
{
    public List<ProcessInfo> Snapshot()
    {
        var result = new List<ProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;

                var item = new ProcessInfo
                {
                    Id = process.Id,
                    Name = name,
                    MemoryMb = process.WorkingSet64 / 1024d / 1024d,
                    Protected = SafetyEngine.IsProtected(name, process.Id)
                };

                item.Status = SafetyEngine.Classify(item);

                result.Add(item);
            }
            catch
            {
                // Process exited or access denied.
            }
            finally
            {
                process.Dispose();
            }
        }

        return result
            .OrderByDescending(x => x.MemoryMb)
            .ToList();
    }

    public static double MemoryUsagePercent()
    {
        var info = GC.GetGCMemoryInfo();

        try
        {
            var computerInfo =
                new Microsoft.VisualBasic.Devices.ComputerInfo();

            ulong total = computerInfo.TotalPhysicalMemory;
            ulong available = computerInfo.AvailablePhysicalMemory;

            if (total == 0)
                return 0;

            return (total - available) * 100d / total;
        }
        catch
        {
            return 0;
        }
    }
}
