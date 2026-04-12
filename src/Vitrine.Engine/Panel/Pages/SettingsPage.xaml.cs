using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Vitrine.Engine.Core;
using Vitrine.Engine.Themes;

namespace Vitrine.Engine.Panel.Pages;

internal partial class SettingsPage : System.Windows.Controls.UserControl
{
    private readonly ThemeHost _host;
    private readonly ControlPanelWindow? _window;
    private readonly string _themeName;
    private List<SettingsSection>? _sections;
    private Dictionary<string, JsonElement>? _settings;
    private readonly Dictionary<string, Func<object>> _controls = new();
    private readonly Dictionary<string, UIElement> _cardElements = new();
    private readonly HashSet<string> _invalidKeys = new();
    private bool _dirty;
    private bool _savedOnce;

    public SettingsPage(ThemeHost host, string? themeName = null, ControlPanelWindow? window = null)
    {
        _host = host;
        _window = window;
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

        try
        {
            var defsJson = File.ReadAllText(defsPath);
            _sections = JsonSerializer.Deserialize<List<SettingsSection>>(defsJson);
        }
        catch (JsonException ex)
        {
            Log.Warn($"Failed to parse definitions (may be old format): {ex.Message}");
            _sections = null;
        }

        _settings = File.Exists(settingsPath)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(settingsPath))
            : new();

        if (_sections == null || _sections.Count == 0)
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
        _invalidKeys.Clear();

        bool isFirst = true;

        foreach (var section in _sections!)
        {
            SettingsPanel.Children.Add(new TextBlock
            {
                Text = section.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, isFirst ? 0 : 20, 0, 8),
            });
            isFirst = false;

            foreach (var (key, def) in section.Settings)
            {
                var currentValue = _settings!.TryGetValue(key, out var val)
                    ? val
                    : (def.Default.HasValue ? def.Default.Value : default);

                var card = new Border
                {
                    Margin = new Thickness(0, 0, 0, 4),
                    Padding = new Thickness(16, 12, 16, 12),
                    Background = (Brush)FindResource("CardBackgroundFillColorDefaultBrush"),
                    BorderBrush = (Brush)FindResource("CardStrokeColorDefaultBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Focusable = false,
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var header = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                header.Children.Add(new TextBlock
                {
                    Text = def.Label.Length > 0 ? def.Label : key,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("TextFillColorPrimaryBrush"),
                });
                if (def.Description.Length > 0)
                {
                    header.Children.Add(new TextBlock
                    {
                        Text = def.Description,
                        FontSize = 12,
                        Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                        TextWrapping = TextWrapping.Wrap,
                    });
                }
                Grid.SetColumn(header, 0);
                grid.Children.Add(header);

                var control = CreateControl(key, def, currentValue);
                if (control is FrameworkElement fe)
                {
                    fe.VerticalAlignment = VerticalAlignment.Center;
                    fe.Margin = new Thickness(16, 0, 0, 0);
                }
                Grid.SetColumn(control, 1);
                grid.Children.Add(control);

                card.Child = grid;

                _cardElements[key] = card;
                SettingsPanel.Children.Add(card);
            }
        }

        UpdateVisibility();
        RefreshSaveButton();
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


        Regex? regex = null;
        if (!string.IsNullOrEmpty(def.Pattern))
        {
            try { regex = new Regex(def.Pattern); }
            catch (ArgumentException ex) { Log.Warn($"Invalid regex for setting '{key}': {ex.Message}"); }
        }

        void Validate()
        {
            if (regex == null) return;
            if (regex.IsMatch(textBox.Text ?? ""))
            {
                _invalidKeys.Remove(key);
                textBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
                textBox.ToolTip = null;
            }
            else
            {
                _invalidKeys.Add(key);
                textBox.BorderBrush = Brushes.IndianRed;
                textBox.ToolTip = !string.IsNullOrEmpty(def.PatternMessage)
                    ? def.PatternMessage
                    : $"Value must match pattern: {def.Pattern}";
            }
            RefreshSaveButton();
        }

        textBox.TextChanged += (_, _) => { MarkDirty(); Validate(); };
        Validate();
        _controls[key] = () => textBox.Text ?? "";
        return textBox;
    }

    private void UpdateVisibility()
    {
        if (_sections == null) return;

        foreach (var section in _sections)
        {
            foreach (var (key, def) in section.Settings)
            {
                if (!_cardElements.TryGetValue(key, out var card))
                    continue;

                if (def.VisibleWhen != null && EvaluateCondition(def.VisibleWhen) is bool visible)
                    card.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

                if (def.DisabledWhen != null && EvaluateCondition(def.DisabledWhen) is bool disabled)
                    ((UIElement)card).IsEnabled = !disabled;
            }
        }
    }

    private bool? EvaluateCondition(VisibilityCondition condition)
    {
        if (!_controls.TryGetValue(condition.Key, out var getter))
            return null;

        var currentValue = getter();
        var expectedValue = condition.Value;

        return expectedValue.ValueKind switch
        {
            JsonValueKind.True => currentValue is true,
            JsonValueKind.False => currentValue is false,
            JsonValueKind.String => currentValue?.ToString() == expectedValue.GetString(),
            JsonValueKind.Number => currentValue is double d && Math.Abs(d - expectedValue.GetDouble()) < 0.001,
            _ => null,
        };
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _window?.RootNavigation.Navigate(typeof(ThemesPage));
    }

    private void MarkDirty()
    {
        _dirty = true;
        RefreshSaveButton();
    }

    private void RefreshSaveButton()
    {
        if (_invalidKeys.Count > 0)
        {
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Fix errors to save";
            return;
        }
        SaveButton.IsEnabled = _dirty;
        SaveButton.Content = _dirty ? "Save & Apply" : (_savedOnce ? "Saved" : "Save & Apply");
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

        _dirty = false;
        _savedOnce = true;
        RefreshSaveButton();
        Log.Info($"Settings saved for theme '{_themeName}'");
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        Log.Info($"Resetting settings to defaults for theme '{_themeName}'");
        if (_sections == null) return;

        _settings = new Dictionary<string, JsonElement>();
        foreach (var section in _sections)
        {
            foreach (var (key, def) in section.Settings)
            {
                if (def.Default.HasValue)
                    _settings[key] = def.Default.Value;
            }
        }

        BuildForm();
        MarkDirty();
    }
}
