using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using newyt.shared.Data;
using newyt.shared.Models;
using newyt.shared.Services;
using System.ComponentModel.DataAnnotations;

namespace newyt.web.Pages;

public class AddChannelModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly YouTubeRssService _youTubeService;
    private readonly YouTubeChannelResolver _channelResolver;

    public AddChannelModel(AppDbContext context, YouTubeRssService youTubeService, YouTubeChannelResolver channelResolver)
    {
        _context = context;
        _youTubeService = youTubeService;
        _channelResolver = channelResolver;
    }

    [BindProperty]
    [Required(ErrorMessage = "Channel name, URL, or ID required")]
    public string ChannelInput { get; set; } = string.Empty;

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Try to resolve the input to a channel ID
            var channelId = await _channelResolver.ResolveChannelIdAsync(ChannelInput.Trim());

            if (string.IsNullOrEmpty(channelId))
            {
                ErrorMessage = "Could not find a YouTube channel with that name, URL, or ID. Please check your input and try again.";
                return RedirectToPage();
            }

            // Check if channel already exists
            var existingChannel = await _context.Channels
                .FirstOrDefaultAsync(c => c.ChannelId == channelId);

            if (existingChannel != null)
            {
                ErrorMessage = $"Channel '{existingChannel.Name}' has already been added";
                return RedirectToPage();
            }

            // Try to get channel name from RSS feed to validate the channel exists
            var channelName = await _youTubeService.GetChannelNameFromFeedAsync(channelId);
            
            if (string.IsNullOrEmpty(channelName))
            {
                ErrorMessage = "Found the channel but could not validate it. The channel might not have any videos or RSS feed may be unavailable.";
                return RedirectToPage();
            }

            // Create and save the channel
            var channel = new Channel
            {
                ChannelId = channelId,
                Name = channelName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Channels.Add(channel);
            await _context.SaveChangesAsync();

            SuccessMessage = $"Successfully added channel: {channelName} (ID: {channelId})";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred while adding the channel: {ex.Message}";
            return RedirectToPage();
        }
    }
}