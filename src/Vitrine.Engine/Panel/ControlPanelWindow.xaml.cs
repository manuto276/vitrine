using System;
using System.Windows;
using Vitrine.Engine.Core;
using Vitrine.Engine.Panel.Pages;
using Wpf.Ui.Controls;
using WpfApplication = System.Windows.Application;

namespace Vitrine.Engine.Panel;

internal partial class ControlPanelWindow : FluentWindow
{
    private readonly ThemeHost _host;
    private HomePage? _homePage;
    private ThemesPage? _themesPage;
    private SettingsPage? _settingsPage;
    private AboutPage? _aboutPage;

    public ControlPanelWindow(ThemeHost host)
    {
        _host = host;
        Log.Info("Control Panel opening");
        InitializeComponent();

        Loaded += (_, _) =>
        {
            _loaded = true;
            Log.Info("Control Panel loaded");
            NavigateTo("home");
        };
    }

    private bool _loaded;

    private void OnNavChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        if (NavList.SelectedItem is System.Windows.Controls.ListBoxItem item)
            NavigateTo(item.Tag?.ToString() ?? "home");
    }

    internal void NavigateTo(string page, string? themeId = null)
    {
        Log.Info($"Navigating to '{page}'" + (themeId != null ? $" (theme={themeId})" : ""));
        PageContent.Content = page switch
        {
            "home" => _homePage ??= new HomePage(_host, this),
            "themes" => _themesPage ??= new ThemesPage(_host, this),
            "settings" => GetSettingsPage(themeId),
            "about" => _aboutPage ??= new AboutPage(),
            _ => _homePage ??= new HomePage(_host, this),
        };
    }

    private SettingsPage GetSettingsPage(string? themeId)
    {
        if (themeId != null || _settingsPage == null)
            _settingsPage = new SettingsPage(_host, themeId);
        return _settingsPage;
    }
}
