using System;
using System.Windows.Forms;
using Vitrine.Engine.Core;

namespace Vitrine.Engine;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var host = new WidgetHost();
            host.Start();
            Application.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start Vitrine:\n\n{ex.Message}",
                "Vitrine",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
