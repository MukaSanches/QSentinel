using System.Diagnostics;
using System.Net.NetworkInformation;

namespace QSentinel;

public sealed class NetworkIntelligence
{
    private long lastRx;
    private long lastTx;
    private DateTime lastSample = DateTime.UtcNow;
    private int ticks;
    private double lossEwma;
    private long lastErrors;
    private long lastDiscards;
    private bool tcpChecked;
    private double latencyEwma;

    public double ReceiveMbps { get; private set; }
    public double SendMbps { get; private set; }
    public double GatewayLatencyMs { get; private set; }
    public string State { get; private set; } = "INICIANDO";
    public double PacketLossPercent { get; private set; }
    public long InterfaceErrorsDelta { get; private set; }
    public long InterfaceDiscardsDelta { get; private set; }
    public int HealthScore { get; private set; } = 100;
    public long DiagnosticRuns { get; private set; }

    public void Tick()
    {
        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            long rx = 0, tx = 0, errors = 0, discards = 0;
            foreach (var nic in adapters)
            {
                try
                {
                    var s = nic.GetIPv4Statistics();
                    rx += s.BytesReceived;
                    tx += s.BytesSent;
                    errors += s.IncomingPacketsWithErrors + s.OutgoingPacketsWithErrors;
                    discards += s.IncomingPacketsDiscarded + s.OutgoingPacketsDiscarded;
                }
                catch { }
            }

            var now = DateTime.UtcNow;
            double seconds = Math.Max(0.1, (now - lastSample).TotalSeconds);
            if (lastRx > 0)
            {
                ReceiveMbps = Math.Max(0, (rx - lastRx) * 8d / 1_000_000d / seconds);
                SendMbps = Math.Max(0, (tx - lastTx) * 8d / 1_000_000d / seconds);
            }
            InterfaceErrorsDelta = Math.Max(0, errors - lastErrors);
            InterfaceDiscardsDelta = Math.Max(0, discards - lastDiscards);
            lastRx = rx;
            lastTx = tx;
            lastErrors = errors;
            lastDiscards = discards;
            lastSample = now;

            if (!tcpChecked)
            {
                tcpChecked = true;
                EnsureSafeTcpDefaults();
            }

            // Latency probing is intentionally sparse: diagnostics must not become network load.
            if (++ticks % 12 == 0)
                ProbeGateway(adapters);

            int penalty = (int)Math.Min(100, Math.Max(0, GatewayLatencyMs - 20) / 2 + PacketLossPercent * 7 + Math.Min(25, InterfaceErrorsDelta * 5) + Math.Min(20, InterfaceDiscardsDelta * 2));
            HealthScore = Math.Clamp(100 - penalty, 0, 100);
            State = adapters.Count == 0 ? "SEM REDE" :
                    PacketLossPercent >= 10 ? "PERDA DE PACOTES" :
                    GatewayLatencyMs >= 120 ? "LATÊNCIA ALTA" :
                    InterfaceErrorsDelta > 0 ? "ERROS NO LINK" :
                    HealthScore < 70 ? "DEGRADADA" : "ESTÁVEL";
        }
        catch
        {
            State = "MONITORANDO";
        }
    }

    private void ProbeGateway(List<NetworkInterface> adapters)
    {
        try
        {
            var gateway = adapters
                .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                .Select(g => g.Address)
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (gateway is null) return;

            using var ping = new Ping();
            int sent = 3, lost = 0, ok = 0; double total = 0;
            for (int i = 0; i < sent; i++)
            {
                var reply = ping.Send(gateway, 350); DiagnosticRuns++;
                if (reply?.Status == IPStatus.Success) { total += reply.RoundtripTime; ok++; } else lost++;
            }
            if (ok > 0)
            {
                double sample = total / ok;
                latencyEwma = latencyEwma == 0 ? sample : latencyEwma * 0.78 + sample * 0.22;
                GatewayLatencyMs = latencyEwma;
            }
            double loss = lost * 100d / sent;
            lossEwma = lossEwma == 0 ? loss : lossEwma * 0.75 + loss * 0.25;
            PacketLossPercent = lossEwma;
        }
        catch { }
    }

    private static void EnsureSafeTcpDefaults()
    {
        // Microsoft documents receive-window auto-tuning "normal" as the standard
        // adaptive mode. QSentinel deliberately avoids MTU/DNS/registry "gaming tweaks",
        // forced congestion algorithms and NIC advanced-property changes.
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = "int tcp set global autotuninglevel=normal",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            p?.WaitForExit(1500);
        }
        catch { }
    }
}
