using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Vitrine.Engine.Core;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Panel.Pages;

internal partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly ThemeHost _host;
    private readonly string _themeName;
    private Dictionary<string, SettingsDefinition>? _definitions;
    private Dictionary<string, JsonElement>? _settings;
    private readonly Dictionary<string, Func<object>> _controls = new();
    private readonly Dictionary<string, UIElement> _cardElements = new();

    public SettingsPage(ThemeHost host, string? themeName = null)
    {
        _host = host;
        _themeName = themeName ?? Configuration.Load().ActiveTheme;
        InitializeComponent();
        SubtitleText.Text = $"Configure \"{_themeName}\" theme";
        LoadSettings();
    }

    private void LoadSettings()
    {
        var themePath = Path.Combine(Configuration.ThemesPath, _themeName);
        var defsPath = Path.Combine(themePath, "settings.definitions.json");
        var settingsPath = Path.Combine(themePath, "settings.json");

        if (!File.Exists(defsPath))
        {
            Log.Info($"No definitions found for theme '{_themeName}'");
            EmptyText.Visibility = Visibility.Visible;
            ButtonBar.Visibility = Visibility.Collapsed;
            return;
        }

        Log.Info($"Loading settings for theme '{_themeName}'");

        _definitions = JsonSerializer.Deserialize<Dictionary<string, SettingsDefinition>>(
            File.ReadAllText(defsPath));
        _settings = File.Exists(settingsPath)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(settingsPath))
            : new();

        if (_definitions == null || _definitions.Count == 0)
        {
            EmptyText.Visibility = Visibility.Visible;
            ButtonBar.Visibility = Visibility.Collapsed;
            return;
        }

        BuildForm();
    }

    private void BuildForm()
    {
        SettingsPanel.Children.Clear();
        _controls.Clear();
        _cardElements.Clear();

        // Group by category
        var grouped = _definitions!
            .GroupBy(kv => kv.Value.Category.Length > 0 ? kv.Value.Category : "General")
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            // Category header
            SettingsPanel.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, SettingsPanel.Children.Count > 0 ? 20 : 0, 0, 8),
            });

            foreach (var (key, def) in group)
            {
                var currentValue = _settings!.TryGetValue(key, out var val)
                    ? val
                    : (def.Default.HasValue ? def.Default.Value : default);

                var card = new Wpf.Ui.Controls.CardControl { Margin = new Thickness(0, 0, 0, 4) };

                var header = new StackPanel();
                header.Children.Add(new TextBlock
                {
                    Text = def.Label.Length > 0 ? def.Label : key,
                    FontWeight = FontWeights.SemiBold,
                });
                if (def.Description.Length > 0)
                {
                    header.Children.Add(new TextBlock
                    {
                        Text = def.Description,
                        FontSize = 12,
                        Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 400,
                    });
                }
                card.Header = header;
                card.Content = CreateControl(key, def, currentValue);

                _cardElements[key] = card;
                SettingsPanel.Children.Add(card);
            }
        }

        UpdateVisibility();
    }

    private UIElement CreateControl(string key, SettingsDefinition def, JsonElement value)
    {
        if (def.Type == "boolean")
        {
            var toggle = new Wpf.Ui.Controls.ToggleSwitch
            {
                IsChecked = value.ValueKind == JsonValueKind.True,
            };
            toggle.Click += (_, _) => { MarkDirty(); UpdateVisibility(); };
            _controls[key] = () => toggle.IsChecked == true;
            return toggle;
        }

        if (def.Options is { Count: > 0 })
        {
            var combo = new System.Windows.Controls.ComboBox { MinWidth = 140 };
            foreach (var opt in def.Options)
                combo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = opt.Label, Tag = opt.Value });

            var currentStr = value.ToString();
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is System.Windows.Controls.ComboBoxItem ci && ci.Tag.ToString() == currentStr)
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }

            combo.SelectionChanged += (_, _) => { MarkDirty(); UpdateVisibility(); };
            _controls[key] = () =>
            {
                if (combo.SelectedItem is System.Windows.Controls.ComboBoxItem ci)
                {
                    var tagVal = ci.Tag;
                    if (def.Type == "number" && tagVal is JsonElement je)
                        return je.GetDouble();
                    return tagVal is JsonElement je2 ? je2.GetString()! : tagVal;
                }
                return def.Default.HasValue ? def.Default.Value : "";
            };
            return combo;
        }

        if (def.Type == "number")
        {
            var numBox = new Wpf.Ui.Controls.NumberBox
            {
                Value = value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0,
                MinWidth = 100,
                SpinButtonPlacementMode = Wpf.Ui.Controls.NumberBoxSpinButtonPlacementMode.Compact,
            };
            numBox.ValueChanged += (_, _) => MarkDirty();
            _controls[key] = () => numBox.Value;
            return numBox;
        }

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString(),
            MinWidth = 200,
        };
        textBox.TextChanged += (_, _) => MarkDirty();
        _controls[key] = () => textBox.Text ?? "";
        return textBox;
    }

    private void UpdateVisibility()
    {
        if (_definitions == null) return;

        foreach (var (key, def) in _definitions)
        {
            if (def.VisibleWhen == null || !_cardElements.TryGetValue(key, out var card))
                continue;

            var conditionKey = def.VisibleWhen.Key;
            if (!_controls.TryGetValue(conditionKey, out var getter))
                continue;

            var currentValue = getter();
            var expectedValue = def.VisibleWhen.Value;

            bool visible = expectedValue.ValueKind switch
            {
                JsonValueKind.True => currentValue is true,
                JsonValueKind.False => currentValue is false,
                JsonValueKind.String => currentValue?.ToString() == expectedValue.GetString(),
                JsonValueKind.Number => currentValue is double d && Math.Abs(d - expectedValue.GetDouble()) < 0.001,
                _ => true,
            };

            card.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void MarkDirty()
    {
        SaveButton.IsEnabled = true;
        SaveButton.Content = "Save & Apply";
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var result = new Dictionary<string, object>();
        foreach (var (key, getter) in _controls)
            result[key] = getter();

        var themePath = Path.Combine(Configuration.ThemesPath, _themeName);
        var settingsPath = Path.Combine(themePath, "settings.json");
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);

        _host.ReloadActiveTheme();

        SaveButton.IsEnabled = false;
        SaveButton.Content = "Saved";
        Log.Info($"Settings saved for theme '{_themeName}'");
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        Log.Info($"Resetting settings to defaults for theme '{_themeName}'");
        if (_definitions == null) return;

        _settings = new Dictionary<string, JsonElement>();
        foreach (var (key, def) in _definitions)
        {
            if (def.Default.HasValue)
                _settings[key] = def.Default.Value;
        }

        BuildForm();
        MarkDirty();
    }
}
