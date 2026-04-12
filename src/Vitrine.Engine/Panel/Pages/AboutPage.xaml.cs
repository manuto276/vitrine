using System.Reflection;

namespace Vitrine.Engine.Panel.Pages;

internal partial class AboutPage : System.Windows.Controls.Page
{
    public AboutPage()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
    }
}
