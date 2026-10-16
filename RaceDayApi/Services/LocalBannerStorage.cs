using Microsoft.AspNetCore.Http;

namespace RaceDayApi.Services;

/// <summary>Keeps banner images in wwwroot/uploads/banners so the API runs without any cloud account.</summary>
public class LocalBannerStorage(IWebHostEnvironment environment) : IBannerStorage
{
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "banners");

        // The folder is created on first use, so a fresh clone runs without a wwwroot folder.
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using var target = File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(target, cancellationToken);

        return $"/uploads/banners/{fileName}";
    }
}
