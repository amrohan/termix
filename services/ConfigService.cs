using System.Text.Json;
using System.Text.Json.Serialization;
using termix.models;

namespace termix.Services;

public class ConfigService
{
    private readonly string _configFilePath;

    public ConfigService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "termix");
        Directory.CreateDirectory(dir);
        _configFilePath = Path.Combine(dir, "config.json");
    }

    public TermixConfig Load()
    {
        if (!File.Exists(_configFilePath)) return new TermixConfig();
        try
        {
            var json = File.ReadAllText(_configFilePath);
            return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.TermixConfig) ?? new TermixConfig();
        }
        catch
        {
            return new TermixConfig();
        }
    }

    public void Save(TermixConfig config)
    {
        var json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.TermixConfig);
        File.WriteAllText(_configFilePath, json);
    }
}