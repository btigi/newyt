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

    public AddChannelModel(AppDbContext context, YouTubeRssService youTubeService)
    {
        _context = context;
        _youTubeService = youTubeService;
    }

    [BindProperty]
    [Required(ErrorMessage = "Channel ID required")]
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
            var channelId = ChannelInput.Trim();

            if (!channelId.StartsWith("UC"))
            {
                ErrorMessage = "Please provide a valid YouTube Channel ID starting with 'UC'";
                return RedirectToPage();
            }

            // Check if channel already exists
            var existingChannel = await _context.Channels
                .FirstOrDefaultAsync(c => c.ChannelId == channelId);

            if (existingChannel != null)
            {
                ErrorMessage = "This channel has already been added";
                return RedirectToPage();
            }

            // Try to get channel name from RSS feed to validate the channel exists
            var channelName = await _youTubeService.GetChannelNameFromFeedAsync(channelId);
            
            if (string.IsNullOrEmpty(channelName))
            {
                ErrorMessage = "Could not find a YouTube channel with this ID. Please check the ID and try again.";
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

            SuccessMessage = $"Successfully added channel: {channelName}";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred while adding the channel: {ex.Message}";
            return RedirectToPage();
        }
    }
}