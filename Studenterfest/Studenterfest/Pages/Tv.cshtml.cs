using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class TvModel : PageModel
{
    private readonly PhotoStore _photos;
    private readonly SpotifyService _spotify;
    private readonly LyricsService _lyrics;
    private readonly SettingsStore _settings;

    public TvModel(PhotoStore photos, SpotifyService spotify, LyricsService lyrics, SettingsStore settings)
    {
        _photos = photos;
        _spotify = spotify;
        _lyrics = lyrics;
        _settings = settings;
    }

    private PartySettings? _s;
    private PartySettings S => _s ??= _settings.Get();

    public string Title => S.Title ?? "Dimission";
    public string Subtitle => S.Subtitle ?? "";
    public string JamUrl => S.JamUrl ?? "";
    public string Theme => S.Theme;
    public bool LyricsEnabled => S.Lyrics;
    public string UploadUrl => $"{Request.Scheme}://{Request.Host}/Upload";

    public void OnGet() { }

    // GET /Tv?handler=Photos
    public IActionResult OnGetPhotos() => new JsonResult(_photos.List());

    // GET /Tv?handler=NowPlaying
    public async Task<IActionResult> OnGetNowPlaying()
        => new JsonResult(await _spotify.GetCurrentlyPlayingAsync());

    // GET /Tv?handler=Settings  (live indstillinger til skærmen)
    public IActionResult OnGetSettings()
    {
        var s = _settings.Get();
        return new JsonResult(new { title = s.Title, subtitle = s.Subtitle, theme = s.Theme, lyrics = s.Lyrics, jamUrl = s.JamUrl });
    }

    // GET /Tv?handler=Lyrics&track=...&artist=...&album=...&durationMs=...&trackId=...
    public async Task<IActionResult> OnGetLyrics(string? trackId, string? track, string? artist, string? album, int durationMs)
    {
        if (string.IsNullOrWhiteSpace(track) || string.IsNullOrWhiteSpace(artist))
            return new JsonResult(new { found = false, lines = Array.Empty<object>() });

        return new JsonResult(await _lyrics.GetAsync(trackId ?? "", track, artist, album ?? "", durationMs));
    }
}
