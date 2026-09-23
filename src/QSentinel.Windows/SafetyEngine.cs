namespace QSentinel;

public static class SafetyEngine
{
    private static readonly HashSet<string> Protected =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "System",
            "Idle",
            "Registry",
            "Memory Compression",
            "smss",
            "csrss",
            "wininit",
            "winlogon",
            "services",
            "lsass",
            "svchost",
            "dwm",
            "explorer",
            "fontdrvhost",
            "sihost",
            "taskhostw",
            "SecurityHealthService",
            "MsMpEng",
            "NisSrv",
            "QSentinel"
        };

    public static bool IsProtected(string name, int pid)
    {
        if (pid <= 4)
            return true;

        return Protected.Contains(name);
    }

    public static string Classify(ProcessInfo p)
    {
        if (p.Protected)
            return "PROTEGIDO";

        if (p.MemoryMb >= 1500)
            return "ALTO USO";

        if (p.MemoryMb >= 700)
            return "OBSERVAR";

        return "NORMAL";
    }

    public static bool MayTerminate(ProcessInfo p)
    {
        return !p.Protected &&
               p.Id > 4 &&
               !string.Equals(
                   p.Name,
                   "QSentinel",
                   StringComparison.OrdinalIgnoreCase);
    }
}
