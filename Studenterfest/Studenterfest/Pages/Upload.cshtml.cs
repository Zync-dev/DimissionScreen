using DimissionScreen.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DimissionScreen.Pages;

public class UploadModel : PageModel
{
    private readonly PhotoStore _photos;

    public UploadModel(PhotoStore photos) => _photos = photos;

    public void OnGet() { }

    // POST /Upload  (multipart/form-data, felt "photo"). Antiforgery valideres automatisk.
    public async Task<IActionResult> OnPostAsync(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
            return BadRequest(new { error = "ingen fil" });
        if (photo.Length > 15_000_000)
            return BadRequest(new { error = "filen er for stor (max 15 MB)" });

        var url = await _photos.SaveAsync(photo);
        return new JsonResult(new { ok = true, url });
    }
}
