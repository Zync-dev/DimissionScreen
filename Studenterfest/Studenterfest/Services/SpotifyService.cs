using System.Net.Http.Headers;
using System.Text.Json;

namespace DimissionScreen.Services;

public class SpotifyService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;
    private readonly string _tokenFile;

    private string? _refreshToken;
    private string? _accessToken;
    private DateTimeOffset _expiry = DateTimeOffset.MinValue;

    public SpotifyService(IHttpClientFactory http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;

        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? "data";
        Directory.CreateDirectory(dataDir);
        _tokenFile = Path.Combine(dataDir, "spotify_token.txt");

        // Prioritet: env var -> fil i volumen
        _refreshToken = _cfg["Spotify:RefreshToken"];
        if (string.IsNullOrWhiteSpace(_refreshToken) && File.Exists(_tokenFile))
            _refreshToken = File.ReadAllText(_tokenFile).Trim();
    }

    private string ClientId => _cfg["Spotify:ClientId"] ?? "";
    private string ClientSecret => _cfg["Spotify:ClientSecret"] ?? "";
    private string RedirectUri => _cfg["Spotify:RedirectUri"]
                                  ?? "http://127.0.0.1:8080/spotify/callback";

    public string BuildAuthorizeUrl()
    {
        const string scope = "user-read-currently-playing user-read-playback-state";
        return "https://accounts.spotify.com/authorize?response_type=code"
             + "&client_id=" + Uri.EscapeDataString(ClientId)
             + "&scope=" + Uri.EscapeDataString(scope)
             + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri);
    }

    public async Task<string> ExchangeCodeAsync(string code)
    {
        var client = _http.CreateClient();
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret
        };
        var resp = await client.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(body));
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("refresh_token", out var rt))
        {
            _refreshToken = rt.GetString();
            TrySaveToken();
        }
        if (root.TryGetProperty("access_token", out var at))
        {
            _accessToken = at.GetString();
            _expiry = DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32() - 30);
        }
        return _refreshToken ?? "(intet refresh token modtaget \u2013 tjek client id/secret og redirect uri)";
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _expiry)
            return _accessToken;
        if (string.IsNullOrWhiteSpace(_refreshToken))
            return null;

        var client = _http.CreateClient();
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken!,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret
        };
        var resp = await client.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(body));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        _accessToken = root.GetProperty("access_token").GetString();
        _expiry = DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32() - 30);

        // Spotify kan rotere refresh token
        if (root.TryGetProperty("refresh_token", out var rt))
        {
            _refreshToken = rt.GetString();
            TrySaveToken();
        }
        return _accessToken;
    }

    public async Task<object> GetCurrentlyPlayingAsync()
    {
        var token = await GetAccessTokenAsync();
        if (token == null)
            return new { isPlaying = false, connected = false };

        var client = _http.CreateClient();
        var msg = new HttpRequestMessage(HttpMethod.Get,
            "https://api.spotify.com/v1/me/player/currently-playing");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.SendAsync(msg);
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
            return new { isPlaying = false, connected = true };
        if (!resp.IsSuccessStatusCode)
            return new { isPlaying = false, connected = true, error = (int)resp.StatusCode };

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("item", out var item) || item.ValueKind != JsonValueKind.Object)
            return new { isPlaying = false, connected = true };

        var title = item.GetProperty("name").GetString();
        var trackId = item.TryGetProperty("id", out var idv) ? idv.GetString() : null;
        var artist = string.Join(", ", item.GetProperty("artists")
            .EnumerateArray().Select(a => a.GetProperty("name").GetString()));
        var album = item.GetProperty("album");
        var albumName = album.GetProperty("name").GetString();

        string? art = null;
        var images = album.GetProperty("images");
        if (images.GetArrayLength() > 0)
            art = images[0].GetProperty("url").GetString();

        var progress = root.TryGetProperty("progress_ms", out var p) ? p.GetInt32() : 0;
        var duration = item.GetProperty("duration_ms").GetInt32();
        var playing = root.TryGetProperty("is_playing", out var ip) && ip.GetBoolean();

        var next = playing ? await GetNextAsync(token) : null;

        return new
        {
            isPlaying = playing,
            connected = true,
            id = trackId,
            title,
            artist,
            album = albumName,
            art,
            progressMs = progress,
            durationMs = duration,
            next
        };
    }

    private async Task<object?> GetNextAsync(string token)
    {
        try
        {
            var client = _http.CreateClient();
            var msg = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/queue");
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await client.SendAsync(msg);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("queue", out var q) || q.ValueKind != JsonValueKind.Array || q.GetArrayLength() == 0)
                return null;

            var item = q[0];
            var title = item.GetProperty("name").GetString();
            var artist = string.Join(", ", item.GetProperty("artists").EnumerateArray().Select(a => a.GetProperty("name").GetString()));
            return new { title, artist };
        }
        catch
        {
            return null;
        }
    }

    private void TrySaveToken()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_refreshToken))
                File.WriteAllText(_tokenFile, _refreshToken);
        }
        catch { /* fil-skrivning fejler hvis ingen volume; env var dækker så */ }
    }
}
