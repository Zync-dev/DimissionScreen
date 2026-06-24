using DimissionScreen.Services;
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
}
