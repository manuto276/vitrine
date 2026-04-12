using System;
using System.Windows;
using System.Windows.Controls;
using Vitrine.Engine.Core;
using Vitrine.Engine.Panel.Pages;
using Wpf.Ui.Controls;

namespace Vitrine.Engine.Panel;

internal partial class ControlPanelWindow : FluentWindow
{
    private readonly ThemeHost _host;
    private HomePage? _homePage;
    private ThemesPage? _themesPage;
    private AboutPage? _aboutPage;
    private bool _loaded;

    public ControlPanelWindow(ThemeHost host)
    {
        _host = host;
        Log.Info("Control Panel opening");
        InitializeComponent();

        Loaded += (_, _) =>
        {
            _loaded = true;
            Log.Info("Control Panel loaded");
            NavList.SelectedIndex = 0;
        };
    }

    private void OnNavChanged(object sender, SelectionChangedEventArgs e)
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
            "settings" => new SettingsPage(_host, themeId),
            "about" => _aboutPage ??= new AboutPage(),
            _ => _homePage ??= new HomePage(_host, this),
        };
    }
}
