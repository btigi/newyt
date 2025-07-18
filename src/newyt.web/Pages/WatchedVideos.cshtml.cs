using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using newyt.shared.Data;
using newyt.shared.Models;
using newyt.web.Models;

namespace newyt.web.Pages;

public class WatchedVideosModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UIConfiguration _uiConfig;

    public WatchedVideosModel(AppDbContext context, IOptions<UIConfiguration> uiConfig)
    {
        _context = context;
        _uiConfig = uiConfig.Value;
    }

    public List<Video> Videos { get; set; } = [];
    public WatchStats WatchStats { get; set; } = new();
    
    public bool ShowThumbnails => _uiConfig.ShowThumbnails;

    [BindProperty(SupportsGet = true)]
    public SortOption SortBy { get; set; } = SortOption.DateNewest;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
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

        return RedirectToPage(new { SortBy });
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
        var query = _context.Videos
            .Include(v => v.Channel)
            .Where(v => v.IsWatched);

        // Apply sorting (for watched videos, use WatchedAt for date sorting when available)
        query = SortBy switch
        {
            SortOption.DateNewest => query.OrderByDescending(v => v.WatchedAt ?? v.PublishedAt),
            SortOption.DateOldest => query.OrderBy(v => v.WatchedAt ?? v.PublishedAt),
            SortOption.ChannelAZ => query.OrderBy(v => v.Channel.Name).ThenByDescending(v => v.WatchedAt ?? v.PublishedAt),
            SortOption.ChannelZA => query.OrderByDescending(v => v.Channel.Name).ThenByDescending(v => v.WatchedAt ?? v.PublishedAt),
            SortOption.TitleAZ => query.OrderBy(v => v.Title),
            SortOption.TitleZA => query.OrderByDescending(v => v.Title),
            _ => query.OrderByDescending(v => v.WatchedAt ?? v.PublishedAt)
        };

        Videos = await query.ToListAsync();

        // Calculate watch statistics
        var allVideos = await _context.Videos.ToListAsync();
        WatchStats = new WatchStats
        {
            TotalVideos = allVideos.Count,
            WatchedCount = allVideos.Count(v => v.IsWatched),
            UnwatchedCount = allVideos.Count(v => !v.IsWatched)
        };
    }

    public string GetSortDisplayName(SortOption sort)
    {
        return sort switch
        {
            SortOption.DateNewest => "Recently Watched",
            SortOption.DateOldest => "Oldest Watched",
            SortOption.ChannelAZ => "Channel A-Z",
            SortOption.ChannelZA => "Channel Z-A",
            SortOption.TitleAZ => "Title A-Z",
            SortOption.TitleZA => "Title Z-A",
            _ => "Recently Watched"
        };
    }
}

public class WatchStats
{
    public int TotalVideos { get; set; }
    public int WatchedCount { get; set; }
    public int UnwatchedCount { get; set; }
    public double WatchedPercentage => TotalVideos > 0 ? (double)WatchedCount / TotalVideos * 100 : 0;
} 