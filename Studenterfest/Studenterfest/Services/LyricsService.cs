using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DimissionScreen.Services;

public class LyricsService
{
    private readonly IHttpClientFactory _http;
    private readonly ConcurrentDictionary<string, object> _cache = new();

    private static readonly Regex TsRegex =
        new(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]", RegexOptions.Compiled);

    public LyricsService(IHttpClientFactory http) => _http = http;

    public async Task<object> GetAsync(string trackId, string track, string artist, string album, int durationMs)
    {
        var key = !string.IsNullOrEmpty(trackId) ? trackId : $"{artist}|{track}|{durationMs}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var durationSec = (int)Math.Round(durationMs / 1000.0);

        // Prøv fuld kunstner-streng først, derefter kun den primære kunstner
        var artists = new List<string> { artist };
        var first = artist.Split(',')[0].Trim();
        if (!string.Equals(first, artist, StringComparison.OrdinalIgnoreCase))
            artists.Add(first);

        object result = new { found = false, lines = Array.Empty<object>() };
        foreach (var a in artists)
        {
            var lines = await FetchOnceAsync(track, a, album, durationSec);
            if (lines is { Length: > 0 })
            {
                result = new { found = true, lines };
                break;
            }
        }

        _cache[key] = result;
        return result;
    }

    private async Task<object[]?> FetchOnceAsync(string track, string artist, string album, int durationSec)
    {
        try
        {
            var url = "https://lrclib.net/api/get"
                + "?artist_name=" + Uri.EscapeDataString(artist)
                + "&track_name=" + Uri.EscapeDataString(track)
                + "&album_name=" + Uri.EscapeDataString(album ?? "")
                + "&duration=" + durationSec;

            var client = _http.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("DimissionScreen/1.0 (graduation party display)");

            var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("syncedLyrics", out var s) || s.ValueKind != JsonValueKind.String)
                return null;
            var synced = s.GetString();
            if (string.IsNullOrWhiteSpace(synced)) return null;

            return Parse(synced);
        }
        catch
        {
            return null;
        }
    }

    private static object[] Parse(string lrc)
    {
        var list = new List<(int t, string text)>();
        foreach (var raw in lrc.Replace("\r", "").Split('\n'))
        {
            var matches = TsRegex.Matches(raw);
            if (matches.Count == 0) continue;

            var text = TsRegex.Replace(raw, "").Trim();
            foreach (Match m in matches)
            {
                var min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var sec = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                var ms = min * 60000 + sec * 1000;
                if (m.Groups[3].Success)
                {
                    var frac = m.Groups[3].Value;
                    ms += frac.Length switch
                    {
                        1 => int.Parse(frac) * 100,
                        2 => int.Parse(frac) * 10,
                        _ => int.Parse(frac)
                    };
                }
                list.Add((ms, text));
            }
        }
        return list.OrderBy(x => x.t)
                   .Select(x => (object)new { t = x.t, text = x.text })
                   .ToArray();
    }
}
