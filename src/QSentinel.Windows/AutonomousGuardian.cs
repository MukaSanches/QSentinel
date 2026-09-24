namespace QSentinel;

public sealed class AutonomousGuardian
{
    private int pressureTicks;
    private int calmTicks;

    public string Mode { get; private set; } = "APRENDENDO";
    public int Intensity { get; private set; }
    public long AutomaticDecisions { get; private set; }

    public void Tick(SystemMetrics metrics, OptimizationEngine optimizer, NetworkIntelligence network, HardwareIntelligence hardware)
    {
        bool pressure = metrics.Pressure >= 55 ||
                        optimizer.CommitPercent >= 78 ||
                        network.HealthScore < 65;

        if (pressure) { pressureTicks++; calmTicks = 0; }
        else { calmTicks++; pressureTicks = Math.Max(0, pressureTicks - 1); }

        int previous = Intensity;
        Intensity = pressureTicks >= 8 ? 3 :
                    pressureTicks >= 4 ? 2 :
                    pressureTicks >= 2 ? 1 : 0;

        if (hardware.ThermalState != "NORMAL" && Intensity < 2)
            Intensity = 2;

        Mode = Intensity switch
        {
            3 => "PROTEÇÃO INTENSA",
            2 => "OTIMIZAÇÃO ATIVA",
            1 => "PREVENÇÃO",
            _ => calmTicks >= 5 ? "VIGILÂNCIA" : "APRENDENDO"
        };

        if (previous != Intensity) AutomaticDecisions++;
    }
}
