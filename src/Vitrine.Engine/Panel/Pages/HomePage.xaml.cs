using System.Windows;
using System.Windows.Controls;
using Vitrine.Engine.Core;

namespace Vitrine.Engine.Panel.Pages;

internal partial class HomePage : System.Windows.Controls.UserControl
{
    private readonly ThemeHost _host;
    private readonly ControlPanelWindow _window;

    public HomePage(ThemeHost host, ControlPanelWindow window)
    {
        _host = host;
        _window = window;
        InitializeComponent();

        var config = Configuration.Load();
        ActiveThemeText.Text = config.ActiveTheme;
    }

    private void OnReloadClick(object sender, RoutedEventArgs e)
    {
        _host.ReloadActiveTheme();
    }

    private void OnChangeThemeClick(object sender, RoutedEventArgs e)
    {
        _window.NavigateTo("themes");
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var config = Configuration.Load();
        _window.NavigateTo("settings", config.ActiveTheme);
    }
}
