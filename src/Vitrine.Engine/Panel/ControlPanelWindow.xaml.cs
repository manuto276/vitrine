using System;
using System.Windows;
using Vitrine.Engine.Core;
using Vitrine.Engine.Panel.Pages;
using Wpf.Ui.Controls;

namespace Vitrine.Engine.Panel;

internal partial class ControlPanelWindow : FluentWindow
{
    private readonly ThemeHost _host;
    private readonly PageService _pageService;
    private HomePage? _homePage;
    private ThemesPage? _themesPage;
    private AboutPage? _aboutPage;
    private string? _pendingSettingsTheme;

    public ControlPanelWindow(ThemeHost host)
    {
        _host = host;
        Log.Info("Control Panel opening");
        InitializeComponent();

        // Window-level input diagnostics
        this.PreviewKeyDown += (_, e) =>
            Log.Info($"Window PreviewKeyDown: key={e.Key}, source={e.OriginalSource?.GetType().Name}, handled={e.Handled}");
        this.PreviewTextInput += (_, e) =>
            Log.Info($"Window PreviewTextInput: text='{e.Text}', source={e.OriginalSource?.GetType().Name}, handled={e.Handled}");

        _pageService = new PageService();
        _pageService.Register(() => _homePage ??= new HomePage(_host, this));
        _pageService.Register(() => _themesPage ??= new ThemesPage(_host, this));
        _pageService.Register(() =>
        {
            // SettingsPage is always created fresh with the pending theme id
            return new SettingsPage(_host, _pendingSettingsTheme, this);
        });
        _pageService.Register(() => _aboutPage ??= new AboutPage());

        RootNavigation.SetPageService(_pageService);

        Loaded += (_, _) =>
        {
            Log.Info("Control Panel loaded");
            RootNavigation.Navigate(typeof(HomePage));
        };
    }

    internal void NavigateToSettings(string themeId)
    {
        Log.Info($"Navigating to settings (theme={themeId})");
        _pendingSettingsTheme = themeId;
        RootNavigation.Navigate(typeof(SettingsPage));
    }
}
