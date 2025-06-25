using System.Text.RegularExpressions;

namespace newyt.shared.Services;

public class YouTubeChannelResolver
{
    private readonly HttpClient _httpClient;

    public YouTubeChannelResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> ResolveChannelIdAsync(string input)
    {
        input = input.Trim();

        // If it already looks like a channel ID, return it
        if (input.StartsWith("UC") && input.Length == 24)
        {
            return input;
        }

        // Try different YouTube URL patterns
        var channelId = await TryExtractFromUrl(input);
        if (!string.IsNullOrEmpty(channelId))
        {
            return channelId;
        }

        // Try treating it as a channel name
        channelId = await TryResolveChannelName(input);
        if (!string.IsNullOrEmpty(channelId))
        {
            return channelId;
        }

        return null;
    }

    private async Task<string?> TryExtractFromUrl(string input)
    {
        // Direct channel ID URL: youtube.com/channel/UCxxxxx
        var channelIdMatch = Regex.Match(input, @"youtube\.com/channel/(UC[a-zA-Z0-9_-]{22})");
        if (channelIdMatch.Success)
        {
            return channelIdMatch.Groups[1].Value;
        }

        // Handle custom URLs and @handles by scraping
        var urls = new List<string>();

        // @handle format: youtube.com/@channelname
        var handleMatch = Regex.Match(input, @"youtube\.com/@([a-zA-Z0-9_.-]+)");
        if (handleMatch.Success)
        {
            urls.Add($"https://www.youtube.com/@{handleMatch.Groups[1].Value}");
        }

        // Custom URL: youtube.com/c/channelname
        var customMatch = Regex.Match(input, @"youtube\.com/c/([a-zA-Z0-9_.-]+)");
        if (customMatch.Success)
        {
            urls.Add($"https://www.youtube.com/c/{customMatch.Groups[1].Value}");
        }

        // User URL: youtube.com/user/username
        var userMatch = Regex.Match(input, @"youtube\.com/user/([a-zA-Z0-9_.-]+)");
        if (userMatch.Success)
        {
            urls.Add($"https://www.youtube.com/user/{userMatch.Groups[1].Value}");
        }

        // If input looks like a full URL, try it directly
        if (input.Contains("youtube.com/"))
        {
            urls.Add(input);
        }

        foreach (var url in urls)
        {
            var channelId = await ScrapeChannelIdFromPage(url);
            if (!string.IsNullOrEmpty(channelId))
            {
                return channelId;
            }
        }

        return null;
    }

    private async Task<string?> TryResolveChannelName(string channelName)
    {
        // Clean up the channel name
        channelName = channelName.Trim().Replace(" ", "");

        // Try different URL patterns for channel names
        var urlsToTry = new[]
        {
            $"https://www.youtube.com/@{channelName}",
            $"https://www.youtube.com/c/{channelName}",
            $"https://www.youtube.com/user/{channelName}",
            $"https://www.youtube.com/@{channelName.ToLower()}",
            $"https://www.youtube.com/c/{channelName.ToLower()}"
        };

        foreach (var url in urlsToTry)
        {
            var channelId = await ScrapeChannelIdFromPage(url);
            if (!string.IsNullOrEmpty(channelId))
            {
                return channelId;
            }
        }

        return null;
    }

    private async Task<string?> ScrapeChannelIdFromPage(string url)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            
            // Look for the externalId in the page source as mentioned by the user
            var externalIdMatch = Regex.Match(response, @"""externalId""\s*:\s*""(UC[a-zA-Z0-9_-]{22})""");
            if (externalIdMatch.Success)
            {
                return externalIdMatch.Groups[1].Value;
            }

            // Fallback: look for other patterns that might contain the channel ID
            var channelIdPatterns = new[]
            {
                @"""channelId""\s*:\s*""(UC[a-zA-Z0-9_-]{22})""",
                @"""browseId""\s*:\s*""(UC[a-zA-Z0-9_-]{22})""",
                @"""webChannelId""\s*:\s*""(UC[a-zA-Z0-9_-]{22})""",
                @"channel/(UC[a-zA-Z0-9_-]{22})",
                @"""UC[a-zA-Z0-9_-]{22}"""
            };

            foreach (var pattern in channelIdPatterns)
            {
                var match = Regex.Match(response, pattern);
                if (match.Success)
                {
                    var channelId = match.Groups[1].Value;
                    if (channelId.StartsWith("UC") && channelId.Length == 24)
                    {
                        return channelId;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors and try next URL
        }

        return null;
    }
} 