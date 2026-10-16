using Microsoft.AspNetCore.Http;

namespace RaceDayApi.Services;

/// <summary>Stores uploaded event banners. Part 3 will move this to Azure Blob Storage behind the same interface.</summary>
public interface IBannerStorage
{
    /// <summary>Saves the file and returns the URL path the browser can load it from.</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken);
}
