using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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
            var isDefault = id == "default";
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
                    Margin = new Thickness(0, 0, 8, 0),
                };
                var capturedId = id;
                applyBtn.Click += (_, _) =>
                {
                    Log.Info($"Applying theme '{capturedId}' from Control Panel");
                    _host.SetActiveTheme(capturedId);
                    LoadThemes();
                };
                actions.Children.Add(applyBtn);
            }

            // Delete button — not for default theme, not for active theme
            if (!isDefault && !isActive)
            {
                var deleteBtn = new Wpf.Ui.Controls.Button
                {
                    Content = "Remove",
                    Appearance = Wpf.Ui.Controls.ControlAppearance.Danger,
                };
                var capturedId = id;
                var capturedName = manifest.Name.Length > 0 ? manifest.Name : id;
                deleteBtn.Click += (_, _) => RemoveTheme(capturedId, capturedName);
                actions.Children.Add(deleteBtn);
            }

            card.Content = actions;
            ThemeList.Children.Add(card);
        }
    }

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Install Theme",
            Filter = "Theme archive (*.zip)|*.zip",
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            InstallThemeFromZip(dialog.FileName);
            LoadThemes();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to install theme", ex);
            System.Windows.MessageBox.Show(
                $"Failed to install theme:\n\n{ex.Message}",
                "Vitrine", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void InstallThemeFromZip(string zipPath)
    {
        Log.Info($"Installing theme from {zipPath}");

        using var archive = ZipFile.OpenRead(zipPath);

        // Check for required files — theme.json and theme.js must exist
        // They can be at the root or inside a single subfolder
        var prefix = DetectZipPrefix(archive);

        var manifestEntry = archive.GetEntry(prefix + "theme.json");
        if (manifestEntry == null)
            throw new FileNotFoundException("The zip file does not contain a theme.json file.");

        // Read manifest to get the theme name for the folder
        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<ThemeManifest>(manifestStream);
        if (manifest == null)
            throw new InvalidOperationException("Invalid theme.json.");

        var entryFile = prefix + manifest.Entry;
        if (archive.GetEntry(entryFile) == null)
            throw new FileNotFoundException($"The zip file does not contain the entry file: {manifest.Entry}");

        // Determine folder name from zip filename
        var themeDirName = Path.GetFileNameWithoutExtension(zipPath)
            .ToLowerInvariant()
            .Replace(' ', '-');

        var targetPath = Path.Combine(Configuration.ThemesPath, themeDirName);

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.CreateDirectory(targetPath);

        // Extract all files, stripping the prefix
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // skip directories

            var relativePath = entry.FullName;
            if (prefix.Length > 0 && relativePath.StartsWith(prefix))
                relativePath = relativePath[prefix.Length..];

            var destPath = Path.Combine(targetPath, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null) Directory.CreateDirectory(destDir);

            entry.ExtractToFile(destPath, overwrite: true);
        }

        Log.Info($"Theme installed to {targetPath}");
    }

    /// <summary>
    /// Detects if zip contents are in a subfolder (e.g. "my-theme/theme.json")
    /// or at the root ("theme.json"). Returns the prefix to strip.
    /// </summary>
    private static string DetectZipPrefix(ZipArchive archive)
    {
        // Check if theme.json is at the root
        if (archive.GetEntry("theme.json") != null)
            return "";

        // Check if it's inside a single subfolder
        var dirs = archive.Entries
            .Select(e => e.FullName.Split('/')[0])
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();

        if (dirs.Count == 1)
        {
            var candidate = dirs[0] + "/";
            if (archive.GetEntry(candidate + "theme.json") != null)
                return candidate;
        }

        return "";
    }

    private void RemoveTheme(string themeId, string themeName)
    {
        var result = System.Windows.MessageBox.Show(
            $"Remove theme \"{themeName}\"?\n\nThis will delete the theme folder and cannot be undone.",
            "Vitrine", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var themePath = Path.Combine(Configuration.ThemesPath, themeId);
            if (Directory.Exists(themePath))
            {
                Directory.Delete(themePath, true);
                Log.Info($"Theme '{themeId}' removed");
            }
            LoadThemes();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to remove theme '{themeId}'", ex);
            System.Windows.MessageBox.Show(
                $"Failed to remove theme:\n\n{ex.Message}",
                "Vitrine", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
