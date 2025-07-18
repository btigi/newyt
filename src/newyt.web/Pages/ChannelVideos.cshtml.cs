using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using newyt.shared.Data;
using newyt.shared.Models;
using newyt.web.Models;

namespace newyt.web.Pages;

public enum VideoFilter
{
    All,
    Unwatched,
    Watched
}

public class ChannelVideosModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UIConfiguration _uiConfig;

    public ChannelVideosModel(AppDbContext context, IOptions<UIConfiguration> uiConfig)
    {
        _context = context;
        _uiConfig = uiConfig.Value;
    }

    public Channel? Channel { get; set; }
    public List<Video> Videos { get; set; } = [];
    public ChannelStats Stats { get; set; } = new();
    
    public bool ShowThumbnails => _uiConfig.ShowThumbnails;

    [BindProperty(SupportsGet = true)]
    public int ChannelId { get; set; }

    [BindProperty(SupportsGet = true)]
    public SortOption SortBy { get; set; } = SortOption.DateNewest;

    [BindProperty(SupportsGet = true)]
    public VideoFilter Filter { get; set; } = VideoFilter.Unwatched;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (ChannelId <= 0)
        {
            return NotFound();
        }

        await LoadDataAsync();

        if (Channel == null)
        {
            return NotFound();
        }

        return Page();
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

        return RedirectToPage(new { ChannelId, SortBy, Filter });
    }

    public async Task<IActionResult> OnPostMarkUnwatchedAsync(int videoId)
    {
        var video = await _context.Videos.FindAsync(videoId);
        if (video != null)
        {
            video.IsWatched = false;
            video.WatchedAt = null;
            await _context.SaveChangesAsync();
            
            SuccessMessage = "Video marked as unwatched!";
        }

        return RedirectToPage(new { ChannelId, SortBy, Filter });
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

    public async Task<IActionResult> OnPostMarkUnwatchedAjaxAsync(int videoId)
    {
        try
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                return new JsonResult(new { success = false, message = "Video not found." });
            }

            video.IsWatched = false;
            video.WatchedAt = null;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Video marked as unwatched!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "An error occurred while marking the video as unwatched." });
        }
    }

    private async Task LoadDataAsync()
    {
        // Load channel information
        Channel = await _context.Channels
            .Include(c => c.Videos)
            .FirstOrDefaultAsync(c => c.Id == ChannelId);

        if (Channel == null)
        {
            return;
        }

        // Build query based on filter
        var query = _context.Videos
            .Include(v => v.Channel)
            .Where(v => v.ChannelId == ChannelId);

        // Apply filter
        query = Filter switch
        {
            VideoFilter.Unwatched => query.Where(v => !v.IsWatched),
            VideoFilter.Watched => query.Where(v => v.IsWatched),
            _ => query // All videos
        };

        // Apply sorting
        query = SortBy switch
        {
            SortOption.DateNewest => query.OrderByDescending(v => v.PublishedAt),
            SortOption.DateOldest => query.OrderBy(v => v.PublishedAt),
            SortOption.TitleAZ => query.OrderBy(v => v.Title),
            SortOption.TitleZA => query.OrderByDescending(v => v.Title),
            SortOption.ChannelAZ => query.OrderBy(v => v.PublishedAt), // Not relevant for single channel
            SortOption.ChannelZA => query.OrderByDescending(v => v.PublishedAt), // Not relevant for single channel
            _ => query.OrderByDescending(v => v.PublishedAt)
        };

        Videos = await query.ToListAsync();

        // Calculate stats
        var allChannelVideos = Channel.Videos.ToList();
        Stats = new ChannelStats
        {
            TotalVideos = allChannelVideos.Count,
            WatchedCount = allChannelVideos.Count(v => v.IsWatched),
            UnwatchedCount = allChannelVideos.Count(v => !v.IsWatched),
            FilteredCount = Videos.Count
        };
    }

    public string GetSortDisplayName(SortOption sort)
    {
        return sort switch
        {
            SortOption.DateNewest => "Newest First",
            SortOption.DateOldest => "Oldest First",
            SortOption.TitleAZ => "Title A-Z",
            SortOption.TitleZA => "Title Z-A",
            SortOption.ChannelAZ => "Newest First", // Fallback for single channel
            SortOption.ChannelZA => "Newest First", // Fallback for single channel
            _ => "Newest First"
        };
    }

    public string GetFilterDisplayName(VideoFilter filter)
    {
        return filter switch
        {
            VideoFilter.All => "All Videos",
            VideoFilter.Unwatched => "Unwatched Only",
            VideoFilter.Watched => "Watched Only",
            _ => "All Videos"
        };
    }
}

public class ChannelStats
{
    public int TotalVideos { get; set; }
    public int WatchedCount { get; set; }
    public int UnwatchedCount { get; set; }
    public int FilteredCount { get; set; }
    public double WatchedPercentage => TotalVideos > 0 ? (double)WatchedCount / TotalVideos * 100 : 0;
} 