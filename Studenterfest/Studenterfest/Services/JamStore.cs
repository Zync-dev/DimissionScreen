namespace DimissionScreen.Services;

public class JamStore
{
    private readonly IConfiguration _cfg;
    private readonly string _file;

    public JamStore(IConfiguration cfg)
    {
        _cfg = cfg;
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        Directory.CreateDirectory(dataDir);
        _file = Path.Combine(dataDir, "jam.txt");
    }

    public string Get()
    {
        try
        {
            if (File.Exists(_file))
            {
                var v = File.ReadAllText(_file).Trim();
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        catch { /* falder tilbage til config */ }
        return _cfg["Party:JamUrl"] ?? "";
    }

    public void Set(string? url)
    {
        File.WriteAllText(_file, (url ?? "").Trim());
    }
}
