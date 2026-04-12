using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Vitrine.Engine.Panel.Pages;

internal partial class AboutPage : System.Windows.Controls.Page
{
    public AboutPage()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
    }

    private void OnDonateClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://paypal.me/manuto08") { UseShellExecute = true });
    }
}
