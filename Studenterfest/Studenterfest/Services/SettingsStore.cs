using System.Text.Json;

namespace DimissionScreen.Services;

public class PartySettings
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? JamUrl { get; set; }
    public string Theme { get; set; } = "light";
    public bool Lyrics { get; set; } = true;
}

public class SettingsStore
{
    private readonly IConfiguration _cfg;
    private readonly string _file;
    private readonly object _lock = new();

    public SettingsStore(IConfiguration cfg)
    {
        _cfg = cfg;
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        Directory.CreateDirectory(dataDir);
        _file = Path.Combine(dataDir, "settings.json");
    }

    public PartySettings Get()
    {
        var s = new PartySettings();
        lock (_lock)
        {
            try
            {
                if (File.Exists(_file))
                {
                    var loaded = JsonSerializer.Deserialize<PartySettings>(File.ReadAllText(_file));
                    if (loaded != null) s = loaded;
                }
            }
            catch { /* falder tilbage til standard + config */ }
        }

        if (string.IsNullOrWhiteSpace(s.Title)) s.Title = _cfg["Party:Title"] ?? "Dimission";
        if (string.IsNullOrWhiteSpace(s.Subtitle)) s.Subtitle = _cfg["Party:Subtitle"] ?? "";
        if (string.IsNullOrWhiteSpace(s.JamUrl)) s.JamUrl = _cfg["Party:JamUrl"] ?? "";
        if (string.IsNullOrWhiteSpace(s.Theme)) s.Theme = "light";
        return s;
    }

    public void Save(PartySettings s)
    {
        lock (_lock)
        {
            try { File.WriteAllText(_file, JsonSerializer.Serialize(s)); }
            catch { /* ignorér – uden volume kan vi ikke gemme */ }
        }
    }
}
