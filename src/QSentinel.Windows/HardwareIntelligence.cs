using System.Diagnostics;
using System.Management;

namespace QSentinel;

public sealed class HardwareIntelligence
{
    private int ticks;
    public string StorageKind { get; private set; } = "DESCONHECIDO";
    public double DiskTemperatureC { get; private set; }
    public string DiskHealth { get; private set; } = "MONITORANDO";
    public double CpuClockMhz { get; private set; }
    public double CpuMaxClockMhz { get; private set; }
    public int CpuClockPercent { get; private set; }
    public string ThermalState { get; private set; } = "NORMAL";

    public void Tick(SystemMetrics metrics)
    {
        if (++ticks % 20 != 1) return;
        DetectStorage();
        ReadCpuClock(metrics);
    }

    private void DetectStorage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "root\\Microsoft\\Windows\\Storage",
                "SELECT MediaType, HealthStatus, Temperature FROM MSFT_PhysicalDisk");
            foreach (ManagementObject disk in searcher.Get())
            {
                uint media = Convert.ToUInt32(disk["MediaType"] ?? 0u);
                StorageKind = media switch { 3 => "HDD", 4 => "SSD", 5 => "SCM", _ => StorageKind };
                uint health = Convert.ToUInt32(disk["HealthStatus"] ?? 5u);
                DiskHealth = health switch { 0 => "SAUDÁVEL", 1 => "ATENÇÃO", 2 => "NÃO SAUDÁVEL", _ => "DESCONHECIDO" };
                if (double.TryParse(disk["Temperature"]?.ToString(), out double temp) && temp > 0)
                    DiskTemperatureC = Math.Max(DiskTemperatureC, temp);
            }
        }
        catch { }
    }

    private void ReadCpuClock(SystemMetrics metrics)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject cpu in searcher.Get())
            {
                CpuClockMhz = Convert.ToDouble(cpu["CurrentClockSpeed"] ?? 0d);
                CpuMaxClockMhz = Convert.ToDouble(cpu["MaxClockSpeed"] ?? 0d);
                CpuClockPercent = CpuMaxClockMhz <= 0 ? 0 : (int)Math.Clamp(CpuClockMhz * 100d / CpuMaxClockMhz, 0, 200);
            }

            // Clock collapse during sustained high load is a useful symptom, not proof, of throttling.
            ThermalState = metrics.CpuPercent >= 80 && CpuClockPercent > 0 && CpuClockPercent < 65
                ? "POSSÍVEL THROTTLING"
                : DiskTemperatureC >= 70 ? "DISCO QUENTE" : "NORMAL";
        }
        catch { }
    }
}
