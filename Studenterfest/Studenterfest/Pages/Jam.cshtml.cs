using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class JamModel : PageModel
{
    private readonly JamStore _jam;
    private readonly IConfiguration _cfg;

    public JamModel(JamStore jam, IConfiguration cfg)
    {
        _jam = jam;
        _cfg = cfg;
    }

    public string Current { get; private set; } = "";
    public bool Saved { get; private set; }
    public bool Authorized { get; private set; }

    [BindProperty] public string? NewUrl { get; set; }
    [BindProperty] public string? Key { get; set; }

    private bool CheckKey(string? key)
    {
        var configured = _cfg["Admin:Key"];
        return string.IsNullOrEmpty(configured) || configured == key;
    }

    public void OnGet(string? key)
    {
        Key = key;
        Authorized = CheckKey(key);
        Current = _jam.Get();
    }

    public IActionResult OnPost()
    {
        Authorized = CheckKey(Key);
        if (Authorized && !string.IsNullOrWhiteSpace(NewUrl))
        {
            _jam.Set(NewUrl.Trim());
            Saved = true;
        }
        Current = _jam.Get();
        return Page();
    }
}
