using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vitrine.Engine.Core;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Panel.Pages;

internal partial class ThemesPage : System.Windows.Controls.Page
{
    private static readonly string[] PreviewNames = ["preview.png", "preview.jpg", "preview.jpeg", "preview.webp"];

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
        ThemeList.Items.Clear();
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

            ThemeList.Items.Add(BuildThemeCard(id, manifest, dir, isActive, isDefault, hasSettings));
        }
    }

    private UIElement BuildThemeCard(string id, ThemeManifest manifest, string themePath,
        bool isActive, bool isDefault, bool hasSettings)
    {
        var card = new Border
        {
            Width = 260,
            Margin = new Thickness(0, 0, 12, 12),
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = isActive
                ? (System.Windows.Media.Brush)FindResource("SystemAccentColorPrimaryBrush")
                : (System.Windows.Media.Brush)FindResource("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(isActive ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
        };

        var stack = new StackPanel();

        // Preview image area
        var previewPath = FindPreviewImage(themePath);
        if (previewPath != null)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(previewPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 520;
            bitmap.EndInit();

            stack.Children.Add(new System.Windows.Controls.Image
            {
                Source = bitmap,
                Height = 140,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            });
        }
        else
        {
            stack.Children.Add(new Border
            {
                Height = 140,
                Background = (System.Windows.Media.Brush)FindResource("ControlFillColorDefaultBrush"),
                Child = new TextBlock
                {
                    Text = "No preview available",
                    Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorTertiaryBrush"),
                    FontSize = 12,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        // Info area
        var info = new StackPanel { Margin = new Thickness(14, 10, 14, 12) };

        var namePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        namePanel.Children.Add(new TextBlock
        {
            Text = manifest.Name.Length > 0 ? manifest.Name : id,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorPrimaryBrush"),
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
                Margin = new Thickness(6, 0, 0, 0),
            });
        }
        info.Children.Add(namePanel);

        if (manifest.Description.Length > 0)
        {
            info.Children.Add(new TextBlock
            {
                Text = manifest.Description,
                FontSize = 12,
                Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        // Author and version
        info.Children.Add(new TextBlock
        {
            Text = $"{manifest.Author} · v{manifest.Version}",
            FontSize = 11,
            Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorTertiaryBrush"),
            Margin = new Thickness(0, 4, 0, 0),
        });

        // Action buttons
        var actions = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0),
        };

        if (!isActive)
        {
            var applyBtn = new Wpf.Ui.Controls.Button
            {
                Content = "Apply",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                Margin = new Thickness(0, 0, 6, 0),
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

        if (hasSettings)
        {
            var settingsBtn = new Wpf.Ui.Controls.Button
            {
                Content = "Settings",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Margin = new Thickness(0, 0, 6, 0),
            };
            var capturedId = id;
            settingsBtn.Click += (_, _) => _window.NavigateToSettings(capturedId);
            actions.Children.Add(settingsBtn);
        }

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

        info.Children.Add(actions);
        stack.Children.Add(info);
        card.Child = stack;

        return card;
    }

    private static string? FindPreviewImage(string themePath)
    {
        foreach (var name in PreviewNames)
        {
            var path = Path.Combine(themePath, name);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private async void OnInstallClick(object sender, RoutedEventArgs e)
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
            await ShowErrorAsync("Failed to install theme", ex.Message);
        }
    }

    private static void InstallThemeFromZip(string zipPath)
    {
        Log.Info($"Installing theme from {zipPath}");

        using var archive = ZipFile.OpenRead(zipPath);

        var prefix = DetectZipPrefix(archive);

        var manifestEntry = archive.GetEntry(prefix + "theme.json");
        if (manifestEntry == null)
            throw new FileNotFoundException("The zip file does not contain a theme.json file.");

        using var manifestStream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<ThemeManifest>(manifestStream);
        if (manifest == null)
            throw new InvalidOperationException("Invalid theme.json.");

        var entryFile = prefix + manifest.Entry;
        if (archive.GetEntry(entryFile) == null)
            throw new FileNotFoundException($"The zip file does not contain the entry file: {manifest.Entry}");

        var themeDirName = Path.GetFileNameWithoutExtension(zipPath)
            .ToLowerInvariant()
            .Replace(' ', '-');

        var targetPath = Path.Combine(Configuration.ThemesPath, themeDirName);

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.CreateDirectory(targetPath);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

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

    private static string DetectZipPrefix(ZipArchive archive)
    {
        if (archive.GetEntry("theme.json") != null)
            return "";

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

    private async void RemoveTheme(string themeId, string themeName)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Remove Theme",
            Content = $"Remove theme \"{themeName}\"?\n\nThis will delete the theme folder and cannot be undone.",
            PrimaryButtonText = "Remove",
            PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger,
            CloseButtonText = "Cancel",
        };

        var result = await dialog.ShowDialogAsync();
        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary) return;

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
            await ShowErrorAsync("Failed to remove theme", ex.Message);
        }
    }

    private static async System.Threading.Tasks.Task ShowErrorAsync(string title, string message)
    {
        var dialog = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
        };
        await dialog.ShowDialogAsync();
    }
}
