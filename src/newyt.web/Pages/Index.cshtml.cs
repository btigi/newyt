using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using newyt.shared.Data;
using newyt.shared.Models;
using newyt.shared.Services;
using newyt.web.Models;

namespace newyt.web.Pages;

public enum SortOption
{
    DateNewest,
    DateOldest,
    ChannelAZ,
    ChannelZA,
    TitleAZ,
    TitleZA
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ThumbnailDownloaderService _thumbnailDownloader;
    private readonly ILogger<YouTubeRssService> _youTubeServiceLogger;
    private readonly HttpClient _httpClient;
    private readonly UIConfiguration _uiConfig;

    public IndexModel(AppDbContext context, ThumbnailDownloaderService thumbnailDownloader, 
        ILogger<YouTubeRssService> youTubeServiceLogger, HttpClient httpClient, IOptions<UIConfiguration> uiConfig)
    {
        _context = context;
        _thumbnailDownloader = thumbnailDownloader;
        _youTubeServiceLogger = youTubeServiceLogger;
        _httpClient = httpClient;
        _uiConfig = uiConfig.Value;
    }

    public List<Video> Videos { get; set; } = [];
    
    public bool ShowThumbnails => _uiConfig.ShowThumbnails;

    [BindProperty(SupportsGet = true)]
    public SortOption SortBy { get; set; } = SortOption.DateNewest;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Debug: Log the configuration value
        Console.WriteLine($"DEBUG: ShowThumbnails configuration value: {_uiConfig.ShowThumbnails}");
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostMarkWatchedAsync(int videoId)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video != null)
        {
            video.IsWatched = true;
            video.WatchedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            SuccessMessage = "Video marked as watched!";
        }

        return RedirectToPage(new { SortBy });
    }

    public async Task<IActionResult> OnPostMarkWatchedAjaxAsync(int videoId)
    {
        try
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                return new JsonResult(new { success = false, message = "Video not found." });
            }

            video.IsWatched = true;
            video.WatchedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Video marked as watched!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "An error occurred while marking the video as watched." });
        }
    }

    public async Task<IActionResult> OnPostRefreshVideosAsync()
    {
        try
        {
            // Create YouTube service with thumbnail downloader
            var youTubeService = new YouTubeRssService(_httpClient, _thumbnailDownloader, _youTubeServiceLogger);
            
            var channels = await _context.Channels.ToListAsync();
            int newVideosCount = 0;

            foreach (var channel in channels)
            {
                var videos = await youTubeService.GetVideosFromChannelAsync(channel.ChannelId);
                
                foreach (var video in videos)
                {
                    // Check if video already exists
                    var existingVideo = await _context.Videos
                        .FirstOrDefaultAsync(v => v.VideoId == video.VideoId);
                    
                    if (existingVideo == null)
                    {
                        video.ChannelId = channel.Id;
                        _context.Videos.Add(video);
                        newVideosCount++;
                    }
                    else if (existingVideo.ThumbnailPath == null && video.ThumbnailPath != null)
                    {
                        // Update existing video with thumbnail if it didn't have one before
                        existingVideo.ThumbnailPath = video.ThumbnailPath;
                    }
                }
            }

            if (newVideosCount > 0)
            {
                await _context.SaveChangesAsync();
                SuccessMessage = $"Found {newVideosCount} new videos!";
            }
            else
            {
                SuccessMessage = "No new videos found.";
            }
        }
        catch (Exception ex)
        {
            SuccessMessage = $"Error refreshing videos: {ex.Message}";
        }

        return RedirectToPage(new { SortBy });
    }

    private async Task LoadDataAsync()
    {
        var query = _context.Videos
            .Include(v => v.Channel)
            .Where(v => !v.IsWatched);

        // Apply sorting
        query = SortBy switch
        {
            SortOption.DateNewest => query.OrderByDescending(v => v.PublishedAt),
            SortOption.DateOldest => query.OrderBy(v => v.PublishedAt),
            SortOption.ChannelAZ => query.OrderBy(v => v.Channel.Name).ThenByDescending(v => v.PublishedAt),
            SortOption.ChannelZA => query.OrderByDescending(v => v.Channel.Name).ThenByDescending(v => v.PublishedAt),
            SortOption.TitleAZ => query.OrderBy(v => v.Title),
            SortOption.TitleZA => query.OrderByDescending(v => v.Title),
            _ => query.OrderByDescending(v => v.PublishedAt)
        };

        Videos = await query.ToListAsync();
    }

    public string GetSortDisplayName(SortOption sort)
    {
        return sort switch
        {
            SortOption.DateNewest => "Newest First",
            SortOption.DateOldest => "Oldest First",
            SortOption.ChannelAZ => "Channel A-Z",
            SortOption.ChannelZA => "Channel Z-A",
            SortOption.TitleAZ => "Title A-Z",
            SortOption.TitleZA => "Title Z-A",
            _ => "Newest First"
        };
    }
}