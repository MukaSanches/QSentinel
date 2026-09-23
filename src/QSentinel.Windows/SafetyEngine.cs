namespace QSentinel;

public static class SafetyEngine
{
    private static readonly HashSet<string> Protected =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "System","Idle","Registry","Memory Compression",
            "smss","csrss","wininit","winlogon","services","lsass","svchost",
            "dwm","explorer","fontdrvhost","sihost","taskhostw","RuntimeBroker",
            "ShellExperienceHost","StartMenuExperienceHost","SearchHost","SearchIndexer",
            "ctfmon","WmiPrvSE","audiodg","spoolsv",
            "SecurityHealthService","SecurityHealthSystray","MsMpEng","NisSrv",
            "QSentinel"
        };

    public static bool IsProtected(string name, int pid) =>
        pid <= 4 ||
        pid == Environment.ProcessId ||
        Protected.Contains(name);

    public static bool CanOptimize(ProcessInfo p) =>
        !p.Protected &&
        !p.Foreground &&
        p.Id > 4;

    public static string Classify(ProcessInfo p)
    {
        if (p.Protected) return "PROTEGIDO";
        if (p.Foreground) return "EM USO";

        if (p.CpuPercent >= 10 ||
            p.MemoryMb >= 1500 ||
            p.IoMbPerSecond >= 10)
            return "ALTO IMPACTO";

        if (p.CpuPercent >= 3 ||
            p.MemoryMb >= 700 ||
            p.IoMbPerSecond >= 3)
            return "OBSERVAR";

        return "NORMAL";
    }
}
