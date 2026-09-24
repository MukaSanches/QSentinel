namespace QSentinel;

public sealed class AdaptivePolicy
{
    private string lastBottleneck = "CALIBRANDO";
    private int stableTicks;

    public string DominantBottleneck { get; private set; } = "CALIBRANDO";
    public int Confidence { get; private set; }

    public void Tick(SystemIntelligence intelligence)
    {
        string current = intelligence.Bottleneck;
        if (current == lastBottleneck) stableTicks++;
        else { lastBottleneck = current; stableTicks = 1; }

        Confidence = Math.Clamp(stableTicks * 12, 12, 100);
        if (stableTicks >= 3 || DominantBottleneck == "CALIBRANDO")
            DominantBottleneck = current;
    }

    public double CpuWeight => DominantBottleneck == "CPU" ? 6.0 : 2.4;
    public double MemoryWeight => DominantBottleneck == "MEMÓRIA" ? 2.2 : .75;
    public double IoWeight => DominantBottleneck == "ARMAZENAMENTO" ? 6.5 : 2.0;
}
