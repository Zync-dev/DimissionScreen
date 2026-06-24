using DimissionScreen.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// --- Railway: bind til 0.0.0.0:$PORT (Kestrel binder ellers kun til localhost) ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SpotifyService>();
builder.Services.AddSingleton<PhotoStore>();
builder.Services.AddSingleton<LyricsService>();
builder.Services.AddSingleton<SettingsStore>();
// Lad AJAX sende antiforgery-token via header
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

var app = builder.Build();

var photos = app.Services.GetRequiredService<PhotoStore>();

// Statiske filer: wwwroot (css/js) ...
app.UseStaticFiles();
// ... og de uploadede billeder fra volumen, serveret under /uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(photos.UploadsDir),
    RequestPath = "/uploads"
});

app.UseRouting();
app.MapRazorPages();

app.Run();
