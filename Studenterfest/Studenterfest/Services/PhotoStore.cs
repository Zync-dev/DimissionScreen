namespace DimissionScreen.Services;

public class PhotoStore
{
    public string UploadsDir { get; }

    public PhotoStore()
    {
        // Mount en Railway Volume på /data og sæt env DATA_DIR=/data
        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        UploadsDir = Path.Combine(dataDir, "uploads");
        Directory.CreateDirectory(UploadsDir);
    }

    public object[] List(int max = 120)
    {
        var dir = new DirectoryInfo(UploadsDir);
        if (!dir.Exists) return Array.Empty<object>();

        return dir.GetFiles("*.jpg")
                  .OrderBy(f => f.CreationTimeUtc)
                  .TakeLast(max)
                  .Select(f => (object)new
                  {
                      url = $"/uploads/{f.Name}",
                      ts = new DateTimeOffset(f.CreationTimeUtc).ToUnixTimeMilliseconds()
                  })
                  .ToArray();
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var name = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Random.Shared.Next(1000, 9999)}.jpg";
        var dest = Path.Combine(UploadsDir, name);
        await using var fs = File.Create(dest);
        await file.CopyToAsync(fs);
        return $"/uploads/{name}";
    }
}
