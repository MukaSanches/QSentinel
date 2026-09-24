using System.Diagnostics;
using System.Net.NetworkInformation;

namespace QSentinel;

public sealed class NetworkIntelligence
{
    private long lastRx;
    private long lastTx;
    private DateTime lastSample = DateTime.UtcNow;
    private int ticks;
    private bool tcpChecked;
    private double latencyEwma;

    public double ReceiveMbps { get; private set; }
    public double SendMbps { get; private set; }
    public double GatewayLatencyMs { get; private set; }
    public string State { get; private set; } = "INICIANDO";
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

            long rx = 0, tx = 0;
            foreach (var nic in adapters)
            {
                try
                {
                    var s = nic.GetIPv4Statistics();
                    rx += s.BytesReceived;
                    tx += s.BytesSent;
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
            lastRx = rx;
            lastTx = tx;
            lastSample = now;

            if (!tcpChecked)
            {
                tcpChecked = true;
                EnsureSafeTcpDefaults();
            }

            // Latency probing is intentionally sparse: diagnostics must not become network load.
            if (++ticks % 20 == 0)
                ProbeGateway(adapters);

            State = adapters.Count == 0 ? "SEM REDE" :
                    GatewayLatencyMs >= 120 ? "LATÊNCIA ALTA" :
                    GatewayLatencyMs >= 60 ? "ATENÇÃO" : "ESTÁVEL";
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
            var reply = ping.Send(gateway, 300);
            DiagnosticRuns++;
            if (reply?.Status != IPStatus.Success) return;

            double sample = reply.RoundtripTime;
            latencyEwma = latencyEwma == 0 ? sample : latencyEwma * 0.75 + sample * 0.25;
            GatewayLatencyMs = latencyEwma;
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
