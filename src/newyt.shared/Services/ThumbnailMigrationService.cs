using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using newyt.shared.Data;

namespace newyt.shared.Services;

public class ThumbnailMigrationService
{
    private readonly AppDbContext _context;
    private readonly ThumbnailDownloaderService _thumbnailDownloader;
    private readonly ILogger<ThumbnailMigrationService> _logger;

    public ThumbnailMigrationService(
        AppDbContext context, 
        ThumbnailDownloaderService thumbnailDownloader, 
        ILogger<ThumbnailMigrationService> logger)
    {
        _context = context;
        _thumbnailDownloader = thumbnailDownloader;
        _logger = logger;
    }

    public async Task DownloadMissingThumbnailsAsync(int maxBatchSize = 50)
    {
        _logger.LogInformation("Starting thumbnail migration for existing videos");

        // Get videos without thumbnails
        var videosWithoutThumbnails = await _context.Videos
            .Where(v => v.ThumbnailPath == null || v.ThumbnailPath == string.Empty)
            .Take(maxBatchSize)
            .ToListAsync();

        if (videosWithoutThumbnails.Count == 0)
        {
            _logger.LogInformation("No videos found that need thumbnail migration");
            return;
        }

        _logger.LogInformation("Found {Count} videos that need thumbnail downloads", videosWithoutThumbnails.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var video in videosWithoutThumbnails)
        {
            try
            {
                var thumbnailPath = await _thumbnailDownloader.DownloadThumbnailAsync(video.VideoId);
                
                if (thumbnailPath != null)
                {
                    video.ThumbnailPath = thumbnailPath;
                    successCount++;
                    _logger.LogDebug("Downloaded thumbnail for video {VideoId}: {Title}", video.VideoId, video.Title);
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning("Failed to download thumbnail for video {VideoId}: {Title}", video.VideoId, video.Title);
                }

                // Small delay to be respectful to YouTube's servers
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Error downloading thumbnail for video {VideoId}: {Title}", video.VideoId, video.Title);
            }
        }

        if (successCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Thumbnail migration completed. Success: {SuccessCount}, Failures: {FailureCount}", 
                successCount, failureCount);
        }
        else
        {
            _logger.LogWarning("No thumbnails were successfully downloaded");
        }
    }
} 