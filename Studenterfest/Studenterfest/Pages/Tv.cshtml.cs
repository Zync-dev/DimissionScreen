using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class TvModel : PageModel
{
    private readonly PhotoStore _photos;
    private readonly SpotifyService _spotify;
    private readonly LyricsService _lyrics;
    private readonly JamStore _jam;
    private readonly IConfiguration _cfg;

    public TvModel(PhotoStore photos, SpotifyService spotify, LyricsService lyrics, JamStore jam, IConfiguration cfg)
    {
        _photos = photos;
        _spotify = spotify;
        _lyrics = lyrics;
        _jam = jam;
        _cfg = cfg;
    }

    public string Title => _cfg["Party:Title"] ?? "Dimission";
    public string Subtitle => _cfg["Party:Subtitle"] ?? "";
    public string JamUrl => _jam.Get();
    public string UploadUrl => $"{Request.Scheme}://{Request.Host}/Upload";

    public void OnGet() { }

    // GET /Tv?handler=Photos
    public IActionResult OnGetPhotos() => new JsonResult(_photos.List());

    // GET /Tv?handler=NowPlaying
    public async Task<IActionResult> OnGetNowPlaying()
        => new JsonResult(await _spotify.GetCurrentlyPlayingAsync());

    // GET /Tv?handler=Jam  (det aktuelle jam-link, så TV'et kan opdatere QR live)
    public IActionResult OnGetJam() => new JsonResult(new { url = _jam.Get() });

    // GET /Tv?handler=Lyrics&track=...&artist=...&album=...&durationMs=...&trackId=...
    public async Task<IActionResult> OnGetLyrics(string? trackId, string? track, string? artist, string? album, int durationMs)
    {
        if (string.IsNullOrWhiteSpace(track) || string.IsNullOrWhiteSpace(artist))
            return new JsonResult(new { found = false, lines = Array.Empty<object>() });

        return new JsonResult(await _lyrics.GetAsync(trackId ?? "", track, artist, album ?? "", durationMs));
    }
}
