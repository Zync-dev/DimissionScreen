using DimissionScreen.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class AdminModel : PageModel
{
    private readonly SettingsStore _settings;
    private readonly PhotoStore _photos;
    private readonly IConfiguration _cfg;

    public AdminModel(SettingsStore settings, PhotoStore photos, IConfiguration cfg)
    {
        _settings = settings;
        _photos = photos;
        _cfg = cfg;
    }

    public bool Authorized { get; private set; }
    public bool Saved { get; private set; }
    public bool Deleted { get; private set; }
    public PartySettings Current { get; private set; } = new();
    public PhotoInfo[] Photos { get; private set; } = Array.Empty<PhotoInfo>();

    [BindProperty] public string? Key { get; set; }
    [BindProperty] public string? Title { get; set; }
    [BindProperty] public string? Subtitle { get; set; }
    [BindProperty] public string? JamUrl { get; set; }
    [BindProperty] public string? Theme { get; set; }
    [BindProperty] public bool Lyrics { get; set; }
    [BindProperty] public string? DeleteFile { get; set; }

    private bool CheckKey(string? key)
    {
        var configured = _cfg["Admin:Key"];
        return string.IsNullOrEmpty(configured) || configured == key;
    }

    private void Load()
    {
        Current = _settings.Get();
        Photos = _photos.List(60);
    }

    public void OnGet(string? key)
    {
        Key = key;
        Authorized = CheckKey(key);
        Load();
    }

    public IActionResult OnPostSave()
    {
        Authorized = CheckKey(Key);
        if (Authorized)
        {
            var s = _settings.Get();
            s.Title = string.IsNullOrWhiteSpace(Title) ? s.Title : Title.Trim();
            s.Subtitle = Subtitle?.Trim() ?? s.Subtitle;
            s.JamUrl = string.IsNullOrWhiteSpace(JamUrl) ? s.JamUrl : JamUrl.Trim();
            s.Theme = Theme == "dark" ? "dark" : "light";
            s.Lyrics = Lyrics;
            _settings.Save(s);
            Saved = true;
        }
        Load();
        return Page();
    }

    public IActionResult OnPostDelete()
    {
        Authorized = CheckKey(Key);
        if (Authorized && !string.IsNullOrWhiteSpace(DeleteFile))
        {
            _photos.Delete(DeleteFile);
            Deleted = true;
        }
        Load();
        return Page();
    }

    // GET /admin?handler=Download&key=...  – pakker alle billeder som zip
    public IActionResult OnGetDownload(string? key)
    {
        if (!CheckKey(key)) return Forbid();

        var list = _photos.List(100000);
        if (list.Length == 0) return Content("Ingen billeder endnu.");

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var i = 1;
            foreach (var p in list)
            {
                var path = Path.Combine(_photos.UploadsDir, p.file);
                if (!System.IO.File.Exists(path)) continue;

                var entry = zip.CreateEntry(EntryName(i, p.name, p.file), CompressionLevel.NoCompression);
                using var es = entry.Open();
                using var fs = System.IO.File.OpenRead(path);
                fs.CopyTo(es);
                i++;
            }
        }

        return File(ms.ToArray(), "application/zip", "dimission-billeder.zip");
    }

    private static string EntryName(int index, string? name, string file)
    {
        var prefix = index.ToString("D3");
        if (!string.IsNullOrWhiteSpace(name))
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            if (clean.Length > 0) return $"{prefix}_{clean}.jpg";
        }
        return $"{prefix}_{file}";
    }
}
