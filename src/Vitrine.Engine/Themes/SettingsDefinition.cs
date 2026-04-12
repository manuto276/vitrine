using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitrine.Engine.Themes;

internal class SettingsSection
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("settings")]
    public Dictionary<string, SettingsDefinition> Settings { get; set; } = new();
}

internal class SettingsDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("default")]
    public JsonElement? Default { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("visibleWhen")]
    public VisibilityCondition? VisibleWhen { get; set; }

    [JsonPropertyName("disabledWhen")]
    public VisibilityCondition? DisabledWhen { get; set; }

    [JsonPropertyName("options")]
    public List<SettingsOption>? Options { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("patternMessage")]
    public string? PatternMessage { get; set; }
}

internal class SettingsOption
{
    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}

internal class VisibilityCondition
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}
