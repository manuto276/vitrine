using System.Text.Json.Serialization;

namespace Vitrine.Engine.Themes;

internal class ThemeManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Unknown";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "Unknown";

    [JsonPropertyName("entry")]
    public string Entry { get; set; } = "theme.js";
}
