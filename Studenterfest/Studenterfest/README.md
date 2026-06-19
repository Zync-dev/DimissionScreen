# Dimission-storskærm 🎓

Storskærm til dimissionsfesten: slideshow af gæsternes billeder + "spiller nu" fra
Spotify-jammet + to QR-koder (billed-upload og join-jam).
ASP.NET Core **Razor Pages** (.NET 8), hostet på Railway. Den bærbare ved siden af
TV'et åbner bare `/Tv` i fuldskærm.

## Struktur
```
Pages/
  Index            -> redirecter til /Tv
  Tv               -> storskærmen + named handlers (?handler=Photos, ?handler=NowPlaying)
  Upload           -> telefon-siden (OnPostAsync håndterer upload, med antiforgery)
  SpotifyLogin     -> /spotify/login
  SpotifyCallback  -> /spotify/callback
Services/
  PhotoStore       -> gemmer/lister billeder på volumen
  SpotifyService   -> OAuth, token-refresh, "spiller nu"
wwwroot/css, wwwroot/js  -> styles og klient-logik
```
Billederne gemmes på en **Railway Volume**, så de overlever genstart.
Jam-linket og titlen kommer fra konfiguration (server-side injiceret i Tv.cshtml),
ikke hardcodet i JS.

---

## 1) Spotify-app (gratis)
1. https://developer.spotify.com/dashboard -> Create app.
2. Notér Client ID og Client secret.
3. Redirect URIs (udfyldes når du har Railway-URL'en fra trin 2):
   https://DIT-PROJEKT.up.railway.app/spotify/callback

> Du skal være VÆRT for jammet - musikken streamer fra din konto, så
> /me/player/currently-playing viser præcis det jammet afspiller.

## 2) Deploy til Railway
1. Læg koden i et GitHub-repo.
2. Railway -> New Project -> Deploy from GitHub repo. Railway finder Dockerfile
   automatisk (krævet til .NET - Railpack understøtter det ikke endnu).
3. Settings -> Networking -> Generate Domain for at få din offentlige URL.
4. Indsæt redirect-URI'en i Spotify-dashboardet (trin 1.3).

## 3) Volume (så billeder ikke forsvinder)
1. På servicen: + Volume, mount path /data.
2. Env var: DATA_DIR=/data.

## 4) Env vars (Variables)
```
DATA_DIR=/data
Party__JamUrl=https://open.spotify.com/DIT-JAM-LINK
Spotify__ClientId=din_client_id
Spotify__ClientSecret=din_client_secret
Spotify__RedirectUri=https://DIT-PROJEKT.up.railway.app/spotify/callback
```
(Dobbelt-underscore __ er .NET's måde at læse sektioner fra env vars på.)

## 5) Forbind Spotify (én gang)
1. Åbn https://DIT-PROJEKT.up.railway.app/spotify/login, godkend.
2. Du lander på en side der viser et refresh token.
3. Tilføj det som env var: Spotify__RefreshToken=det_lange_token. Railway redeployer.

## 6) Til festen
1. Bærbar -> TV via HDMI.
2. Start Spotify-jammet på din konto.
3. Åbn https://DIT-PROJEKT.up.railway.app/Tv -> F11 for fuldskærm -> træk over på TV'et.
4. Gæster scanner QR-koderne med telefonens kamera.

---

## Kør lokalt (test)
```bash
dotnet run
```
Åbn http://127.0.0.1:8080/Tv. Udfyld evt. Party:JamUrl i appsettings.json.
Til lokal Spotify-test: sæt Spotify:RedirectUri = http://127.0.0.1:8080/spotify/callback
i appsettings, tilføj samme URI i Spotify-dashboardet, kør /spotify/login.

## Fejlsøgning
- Billeder forsvandt efter deploy: volume mangler, eller DATA_DIR != /data.
- "Spotify ikke forbundet": Spotify__RefreshToken mangler eller client id/secret er forkert.
- "Intet spiller": musikken kører ikke på din konto lige nu (jam skal streame fra dig).
- QR viser tekst i stedet for kode: den bærbare har ikke internet (QR-lib hentes fra CDN).
- Upload fejler: skal være HTTPS - brug Railway-domænet (ikke en lokal IP). Antiforgery-
  tokenet rendres i Upload.cshtml og sendes som header RequestVerificationToken.

## Næste skridt
- SignalR i stedet for polling (billeder popper op uden delay).
- Beskeder/lykønskninger sammen med billederne.
- En skjult /admin-side til at fjerne et billede.
