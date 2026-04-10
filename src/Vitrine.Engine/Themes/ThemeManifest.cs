using System.Text.Json.Serialization;

namespace Vitrine.Engine.Themes;

internal class ThemeManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("entry")]
    public string Entry { get; set; } = "theme.js";
}
