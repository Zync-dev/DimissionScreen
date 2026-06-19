using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class SpotifyLoginModel : PageModel
{
    private readonly SpotifyService _spotify;
    public SpotifyLoginModel(SpotifyService spotify) => _spotify = spotify;

    public IActionResult OnGet() => Redirect(_spotify.BuildAuthorizeUrl());
}
