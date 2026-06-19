using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class TvModel : PageModel
{
    private readonly PhotoStore _photos;
    private readonly SpotifyService _spotify;
    private readonly IConfiguration _cfg;

    public TvModel(PhotoStore photos, SpotifyService spotify, IConfiguration cfg)
    {
        _photos = photos;
        _spotify = spotify;
        _cfg = cfg;
    }

    public string Title => _cfg["Party:Title"] ?? "Dimission";
    public string Subtitle => _cfg["Party:Subtitle"] ?? "";
    public string JamUrl => _cfg["Party:JamUrl"] ?? "";
    public string UploadUrl => $"{Request.Scheme}://{Request.Host}/Upload";

    public void OnGet() { }

    // GET /Tv?handler=Photos
    public IActionResult OnGetPhotos() => new JsonResult(_photos.List());

    // GET /Tv?handler=NowPlaying
    public async Task<IActionResult> OnGetNowPlaying()
        => new JsonResult(await _spotify.GetCurrentlyPlayingAsync());
}
