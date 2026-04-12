using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitrine.Engine.Themes;

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

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("visibleWhen")]
    public VisibilityCondition? VisibleWhen { get; set; }

    [JsonPropertyName("options")]
    public List<SettingsOption>? Options { get; set; }
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
