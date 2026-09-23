using System.Text.Json;

namespace QSentinel;

public sealed class AppSettings
{
    public bool AutoStart { get; set; } = true;
    public bool OptimizationEnabled { get; set; } = true;
    public int RefreshMilliseconds { get; set; } = 1500;
    public List<string> BlockedProcesses { get; set; } = new();

    private static string Folder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QSentinel");

    private static string FilePath => Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { }

        var settings = new AppSettings();
        settings.Save();
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(
                    this,
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public bool IsBlocked(string name) =>
        BlockedProcesses.Any(x =>
            string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
}
