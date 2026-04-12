using System;
using System.Windows;
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

    public ControlPanelWindow(ThemeHost host)
    {
        _host = host;
        Log.Info("Control Panel opening");
        InitializeComponent();

        SetupNavigation();

        Loaded += (_, _) =>
        {
            _loaded = true;
            Log.Info("Control Panel loaded");
            NavigateTo("home");
        };
    }

    private bool _loaded;

    private void SetupNavigation()
    {
        NavView.MenuItems.Add(new NavigationViewItem
        {
            Content = "Home",
            Tag = "home",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
        });
        NavView.MenuItems.Add(new NavigationViewItem
        {
            Content = "Themes",
            Tag = "themes",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Color24 },
        });

        NavView.FooterMenuItems.Add(new NavigationViewItem
        {
            Content = "About",
            Tag = "about",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
        });
    }

    private void OnNavChanged(NavigationView sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (NavView.SelectedItem is NavigationViewItem item)
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
