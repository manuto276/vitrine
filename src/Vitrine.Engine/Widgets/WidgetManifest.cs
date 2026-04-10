using System.Text.Json.Serialization;

namespace Vitrine.Engine.Widgets;

internal class WidgetManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("entry")]
    public string Entry { get; set; } = "index.html";

    [JsonPropertyName("width")]
    public int Width { get; set; } = 300;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 200;

    [JsonPropertyName("x")]
    public int X { get; set; } = 40;

    [JsonPropertyName("y")]
    public int Y { get; set; } = 40;
}
