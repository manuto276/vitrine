using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vitrine.Engine.Core;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Panel.Pages;

internal partial class ThemesPage : System.Windows.Controls.UserControl
{
    private readonly ThemeHost _host;
    private readonly ControlPanelWindow _window;

    public ThemesPage(ThemeHost host, ControlPanelWindow window)
    {
        _host = host;
        _window = window;
        InitializeComponent();
        LoadThemes();
    }

    private void LoadThemes()
    {
        ThemeList.Children.Clear();
        var config = Configuration.Load();
        var themesPath = Configuration.ThemesPath;

        if (!Directory.Exists(themesPath)) return;

        foreach (var dir in Directory.GetDirectories(themesPath).OrderBy(d => d))
        {
            var manifestPath = Path.Combine(dir, "theme.json");
            if (!File.Exists(manifestPath)) continue;

            ThemeManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ThemeManifest>(File.ReadAllText(manifestPath));
                if (manifest == null) continue;
            }
            catch { continue; }

            var id = Path.GetFileName(dir);
            var isActive = id == config.ActiveTheme;
            var hasSettings = File.Exists(Path.Combine(dir, "settings.definitions.json"));

            var card = new Wpf.Ui.Controls.CardControl { Margin = new Thickness(0, 0, 0, 4) };

            var header = new StackPanel();
            var namePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            namePanel.Children.Add(new TextBlock
            {
                Text = manifest.Name.Length > 0 ? manifest.Name : id,
                FontWeight = FontWeights.SemiBold,
            });

            if (isActive)
            {
                namePanel.Children.Add(new TextBlock
                {
                    Text = " Active",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 123, 15)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                });
            }

            header.Children.Add(namePanel);

            if (manifest.Description.Length > 0)
            {
                header.Children.Add(new TextBlock
                {
                    Text = manifest.Description,
                    FontSize = 12,
                    Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                });
            }

            card.Header = header;

            var actions = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            if (hasSettings)
            {
                var settingsBtn = new Wpf.Ui.Controls.Button
                {
                    Content = "Settings",
                    Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                var capturedId = id;
                settingsBtn.Click += (_, _) => _window.NavigateTo("settings", capturedId);
                actions.Children.Add(settingsBtn);
            }

            if (!isActive)
            {
                var applyBtn = new Wpf.Ui.Controls.Button
                {
                    Content = "Apply",
                    Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                };
                var capturedId = id;
                applyBtn.Click += (_, _) =>
                {
                    _host.SetActiveTheme(capturedId);
                    LoadThemes();
                };
                actions.Children.Add(applyBtn);
            }

            card.Content = actions;
            ThemeList.Children.Add(card);
        }
    }
}
