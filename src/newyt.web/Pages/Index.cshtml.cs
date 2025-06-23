using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Models;
using newyt.shared.Services;

namespace newyt.web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly YouTubeRssService _youTubeService;

    public IndexModel(AppDbContext context, YouTubeRssService youTubeService)
    {
        _context = context;
        _youTubeService = youTubeService;
    }

    public List<Video> Videos { get; set; } = [];
    public List<Channel> Channels { get; set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
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

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefreshVideosAsync()
    {
        try
        {
            var channels = await _context.Channels.ToListAsync();
            int newVideosCount = 0;

            foreach (var channel in channels)
            {
                var videos = await _youTubeService.GetVideosFromChannelAsync(channel.ChannelId);
                
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

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Videos = await _context.Videos
            .Include(v => v.Channel)
            .Where(v => !v.IsWatched)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync();

        Channels = await _context.Channels
            .Include(c => c.Videos)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}