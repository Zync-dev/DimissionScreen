namespace DimissionScreen.Services;

public record PhotoInfo(string url, long ts, string? name, string file);

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

    public PhotoInfo[] List(int max = 120)
    {
        var dir = new DirectoryInfo(UploadsDir);
        if (!dir.Exists) return Array.Empty<PhotoInfo>();

        return dir.GetFiles("*.jpg")
                  .OrderBy(f => f.CreationTimeUtc)
                  .TakeLast(max)
                  .Select(f =>
                  {
                      var baseName = Path.GetFileNameWithoutExtension(f.Name);
                      var sidecar = Path.Combine(UploadsDir, baseName + ".txt");
                      string? name = null;
                      if (File.Exists(sidecar))
                      {
                          try { name = File.ReadAllText(sidecar).Trim(); } catch { /* ignorér */ }
                          if (string.IsNullOrWhiteSpace(name)) name = null;
                      }
                      return new PhotoInfo(
                          $"/uploads/{f.Name}",
                          new DateTimeOffset(f.CreationTimeUtc).ToUnixTimeMilliseconds(),
                          name,
                          f.Name);
                  })
                  .ToArray();
    }

    public async Task<string> SaveAsync(IFormFile file, string? name)
    {
        var stamp = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Random.Shared.Next(1000, 9999)}";
        var fileName = stamp + ".jpg";
        var dest = Path.Combine(UploadsDir, fileName);
        await using (var fs = File.Create(dest))
        {
            await file.CopyToAsync(fs);
        }

        name = CleanName(name);
        if (!string.IsNullOrEmpty(name))
        {
            try { await File.WriteAllTextAsync(Path.Combine(UploadsDir, stamp + ".txt"), name); }
            catch { /* navn er valgfrit */ }
        }

        return $"/uploads/{fileName}";
    }

    public void Delete(string? fileName)
    {
        // kun rent filnavn – ingen sti-traversal
        var safe = Path.GetFileName(fileName ?? "");
        if (string.IsNullOrEmpty(safe) || !safe.EndsWith(".jpg")) return;

        var jpg = Path.Combine(UploadsDir, safe);
        if (File.Exists(jpg)) { try { File.Delete(jpg); } catch { } }

        var txt = Path.Combine(UploadsDir, Path.GetFileNameWithoutExtension(safe) + ".txt");
        if (File.Exists(txt)) { try { File.Delete(txt); } catch { } }
    }

    private static string CleanName(string? n)
    {
        if (string.IsNullOrWhiteSpace(n)) return "";
        n = n.Replace("\r", " ").Replace("\n", " ").Trim();
        return n.Length > 40 ? n.Substring(0, 40) : n;
    }
}
