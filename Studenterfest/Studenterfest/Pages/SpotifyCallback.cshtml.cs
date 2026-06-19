using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class SpotifyCallbackModel : PageModel
{
    private readonly SpotifyService _spotify;
    public SpotifyCallbackModel(SpotifyService spotify) => _spotify = spotify;

    public string? RefreshToken { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? code, string? error)
    {
        if (!string.IsNullOrEmpty(error)) { Error = error; return; }
        if (string.IsNullOrEmpty(code)) { Error = "Mangler ?code i callback."; return; }
        RefreshToken = await _spotify.ExchangeCodeAsync(code);
    }
}
