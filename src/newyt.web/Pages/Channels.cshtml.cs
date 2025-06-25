using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Models;

namespace newyt.web.Pages;

public class ChannelsModel : PageModel
{
    private readonly AppDbContext _context;

    public ChannelsModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Channel> Channels { get; set; } = [];
    public ChannelsSummary Summary { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public SortOption SortBy { get; set; } = SortOption.ChannelAZ;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // Apply sorting
        var channels = SortBy switch
        {
            SortOption.ChannelAZ => await _context.Channels.Include(c => c.Videos).OrderBy(c => c.Name).ToListAsync(),
            SortOption.ChannelZA => await _context.Channels.Include(c => c.Videos).OrderByDescending(c => c.Name).ToListAsync(),
            SortOption.DateNewest => await _context.Channels.Include(c => c.Videos).OrderByDescending(c => c.CreatedAt).ToListAsync(),
            SortOption.DateOldest => await _context.Channels.Include(c => c.Videos).OrderBy(c => c.CreatedAt).ToListAsync(),
            SortOption.TitleAZ => await _context.Channels.Include(c => c.Videos).OrderBy(c => c.Name).ToListAsync(), // Same as ChannelAZ for channels
            SortOption.TitleZA => await _context.Channels.Include(c => c.Videos).OrderByDescending(c => c.Name).ToListAsync(), // Same as ChannelZA for channels
            _ => await _context.Channels.Include(c => c.Videos).OrderBy(c => c.Name).ToListAsync()
        };

        Channels = channels;

        // Calculate summary statistics
        var totalVideos = Channels.SelectMany(c => c.Videos).Count();
        var totalWatchedVideos = Channels.SelectMany(c => c.Videos).Count(v => v.IsWatched);
        var totalUnwatchedVideos = totalVideos - totalWatchedVideos;

        Summary = new ChannelsSummary
        {
            TotalChannels = Channels.Count,
            TotalVideos = totalVideos,
            TotalWatchedVideos = totalWatchedVideos,
            TotalUnwatchedVideos = totalUnwatchedVideos,
            ActiveChannels = Channels.Count(c => c.Videos.Any(v => !v.IsWatched))
        };
    }

    public string GetSortDisplayName(SortOption sort)
    {
        return sort switch
        {
            SortOption.ChannelAZ => "Channel A-Z",
            SortOption.ChannelZA => "Channel Z-A",
            SortOption.DateNewest => "Recently Added",
            SortOption.DateOldest => "Oldest Added",
            SortOption.TitleAZ => "Channel A-Z", // Same as ChannelAZ
            SortOption.TitleZA => "Channel Z-A", // Same as ChannelZA
            _ => "Channel A-Z"
        };
    }
}

public class ChannelsSummary
{
    public int TotalChannels { get; set; }
    public int TotalVideos { get; set; }
    public int TotalWatchedVideos { get; set; }
    public int TotalUnwatchedVideos { get; set; }
    public int ActiveChannels { get; set; }
    public double WatchedPercentage => TotalVideos > 0 ? (double)TotalWatchedVideos / TotalVideos * 100 : 0;
} 