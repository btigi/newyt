using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Models;

namespace newyt.web.Pages;

public class WatchedVideosModel : PageModel
{
    private readonly AppDbContext _context;

    public WatchedVideosModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Video> Videos { get; set; } = [];
    public WatchStats WatchStats { get; set; } = new();

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

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Videos = await _context.Videos
            .Include(v => v.Channel)
            .Where(v => v.IsWatched)
            .OrderByDescending(v => v.WatchedAt)
            .ToListAsync();

        // Calculate watch statistics
        var allVideos = await _context.Videos.ToListAsync();
        WatchStats = new WatchStats
        {
            TotalVideos = allVideos.Count,
            WatchedCount = allVideos.Count(v => v.IsWatched),
            UnwatchedCount = allVideos.Count(v => !v.IsWatched)
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