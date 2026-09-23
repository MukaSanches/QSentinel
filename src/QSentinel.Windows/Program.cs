using System.Diagnostics;

namespace QSentinel;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        bool created;
        using var mutex = new Mutex(true, @"Local\QSentinel.SingleInstance", out created);

        if (!created)
        {
            MessageBox.Show(
                "QSentinel já está em execução.",
                "QSentinel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
    }
}
