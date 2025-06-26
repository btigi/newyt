using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using newyt.shared.Data;
using newyt.shared.Services;

namespace newyt.console.Services;

public class VideoFetcherService : BackgroundService
{
    private readonly ILogger<VideoFetcherService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _fetchInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public VideoFetcherService(ILogger<VideoFetcherService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Video Fetcher Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchNewVideosAsync();
                
                _logger.LogInformation("Next video fetch scheduled in {Interval} minutes", _fetchInterval.TotalMinutes);
                await Task.Delay(_fetchInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Video Fetcher Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching videos");
                
                // Wait a shorter time on error before retrying
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }

    private async Task FetchNewVideosAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var thumbnailDownloader = scope.ServiceProvider.GetRequiredService<ThumbnailDownloaderService>();
        var youTubeServiceLogger = scope.ServiceProvider.GetRequiredService<ILogger<YouTubeRssService>>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        
        // Create YouTube service with thumbnail downloader
        var youTubeService = new YouTubeRssService(httpClient, thumbnailDownloader, youTubeServiceLogger);

        _logger.LogInformation("Starting video fetch operation");

        var channels = await context.Channels.ToListAsync();
        
        if (channels.Count == 0)
        {
            _logger.LogInformation("No channels found to fetch videos from");
            return;
        }

        int totalNewVideos = 0;

        foreach (var channel in channels)
        {
            try
            {
                _logger.LogInformation("Fetching videos for channel: {ChannelName} ({ChannelId})", 
                    channel.Name, channel.ChannelId);

                var videos = await youTubeService.GetVideosFromChannelAsync(channel.ChannelId);
                int newVideosForChannel = 0;

                foreach (var video in videos)
                {
                    var existingVideo = await context.Videos.FirstOrDefaultAsync(v => v.VideoId == video.VideoId);

                    if (existingVideo == null)
                    {
                        video.ChannelId = channel.Id;
                        context.Videos.Add(video);
                        newVideosForChannel++;
                        totalNewVideos++;
                        
                        _logger.LogInformation("Found new video: {VideoTitle} (Thumbnail: {ThumbnailPath})", video.Title, video.ThumbnailPath ?? "Not downloaded");
                    }
                    else if (existingVideo.ThumbnailPath == null && video.ThumbnailPath != null)
                    {
                        // Update existing video with thumbnail if it didn't have one before
                        existingVideo.ThumbnailPath = video.ThumbnailPath;
                        _logger.LogInformation("Updated thumbnail for existing video: {VideoTitle}", video.Title);
                    }
                }

                if (newVideosForChannel > 0)
                {
                    _logger.LogInformation("Found {Count} new videos for channel {ChannelName}", 
                        newVideosForChannel, channel.Name);
                }
                else
                {
                    _logger.LogInformation("No new videos found for channel {ChannelName}", channel.Name);
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching videos for channel {ChannelName} ({ChannelId})", 
                    channel.Name, channel.ChannelId);
            }
        }

        if (totalNewVideos > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved {Count} new videos to database", totalNewVideos);
        }
        else
        {
            _logger.LogInformation("No new videos found across all channels");
        }
    }
}