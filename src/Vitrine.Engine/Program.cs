using System;
using System.Windows.Forms;
using Vitrine.Engine.Core;

namespace Vitrine.Engine;

static class Program
{
    [STAThread]
    static void Main()
    {
        Log.Info("Vitrine starting");
        ApplicationConfiguration.Initialize();

        try
        {
            using var host = new ThemeHost();
            host.Start();
            Log.Info("Entering message loop");
            Application.Run();
        }
        catch (Exception ex)
        {
            Log.Error("Fatal error during startup", ex);
            MessageBox.Show(
                $"Failed to start Vitrine:\n\n{ex.Message}",
                "Vitrine",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        Log.Info("Vitrine exiting");
    }
}
