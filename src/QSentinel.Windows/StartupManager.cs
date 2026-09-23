using Microsoft.Win32;

namespace QSentinel;

public static class StartupManager
{
    private const string KeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName = "QSentinel";

    public static void Enable()
    {
        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(KeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);

            key?.SetValue(
                ValueName,
                $"\"{Application.ExecutablePath}\" --background");
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);

            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch { }
    }
}
