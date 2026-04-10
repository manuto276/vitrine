using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitrine.Engine.Core;

internal class Configuration
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vitrine");

    private static string ConfigPath => Path.Combine(AppDataDir, "config.json");

    [JsonPropertyName("activeTheme")]
    public string ActiveTheme { get; set; } = "default";

    internal static string ThemesPath => Path.Combine(AppDataDir, "themes");

    internal static Configuration Load()
    {
        if (!File.Exists(ConfigPath))
            return new Configuration();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
        }
        catch
        {
            return new Configuration();
        }
    }

    internal void Save()
    {
        Directory.CreateDirectory(AppDataDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
