using Microsoft.Extensions.Logging;

namespace newyt.shared.Services;

public class ThumbnailDownloaderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThumbnailDownloaderService> _logger;
    private readonly string _thumbnailsPath;

    public ThumbnailDownloaderService(HttpClient httpClient, ILogger<ThumbnailDownloaderService> logger, string thumbnailsPath = "wwwroot/thumbnails")
    {
        _httpClient = httpClient;
        _logger = logger;
        _thumbnailsPath = thumbnailsPath;
        
        // Ensure thumbnails directory exists
        Directory.CreateDirectory(_thumbnailsPath);
    }

    public async Task<string?> DownloadThumbnailAsync(string videoId)
    {
        try
        {
            // YouTube thumbnail URLs in order of preference (highest quality first)
            var thumbnailUrls = new[]
            {
                $"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg",
                $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg", 
                $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg",
                $"https://i.ytimg.com/vi/{videoId}/default.jpg"
            };

            foreach (var thumbnailUrl in thumbnailUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(thumbnailUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var fileName = $"{videoId}.jpg";
                        var filePath = Path.Combine(_thumbnailsPath, fileName);
                        
                        var content = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(filePath, content);
                        
                        _logger.LogInformation("Downloaded thumbnail for video {VideoId} from {ThumbnailUrl}", videoId, thumbnailUrl);
                        
                        // Return the relative path for web serving
                        return $"/thumbnails/{fileName}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download thumbnail from {ThumbnailUrl} for video {VideoId}", thumbnailUrl, videoId);
                }
            }
            
            _logger.LogWarning("Could not download any thumbnail for video {VideoId}", videoId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading thumbnail for video {VideoId}", videoId);
            return null;
        }
    }

    public bool ThumbnailExists(string videoId)
    {
        var fileName = $"{videoId}.jpg";
        var filePath = Path.Combine(_thumbnailsPath, fileName);
        return File.Exists(filePath);
    }

    public string? GetThumbnailPath(string videoId)
    {
        if (ThumbnailExists(videoId))
        {
            return $"/thumbnails/{videoId}.jpg";
        }
        return null;
    }
} 