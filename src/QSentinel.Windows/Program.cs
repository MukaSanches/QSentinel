namespace QSentinel;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        bool created;
        using var mutex = new Mutex(true, @"Local\QSentinel.SingleInstance", out created);
        if (!created) return;

        var settings = AppSettings.Load();

        if (settings.AutoStart) StartupManager.Enable();
        else StartupManager.Disable();

        bool background = args.Any(x =>
            string.Equals(x, "--background", StringComparison.OrdinalIgnoreCase));

        Application.Run(new MainForm(settings, background));
    }
}
