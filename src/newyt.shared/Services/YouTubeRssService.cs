using System.Xml.Linq;
using newyt.shared.Models;
using Microsoft.Extensions.Logging;

namespace newyt.shared.Services;

public class YouTubeRssService
{
    private readonly HttpClient _httpClient;
    private readonly ThumbnailDownloaderService? _thumbnailDownloader;
    private readonly ILogger<YouTubeRssService>? _logger;

    public YouTubeRssService(HttpClient httpClient, ThumbnailDownloaderService? thumbnailDownloader = null, ILogger<YouTubeRssService>? logger = null)
    {
        _httpClient = httpClient;
        _thumbnailDownloader = thumbnailDownloader;
        _logger = logger;
    }

    public async Task<List<Video>> GetVideosFromChannelAsync(string channelId)
    {
        var videos = new List<Video>();
        
        try
        {
            var feedUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
            var response = await _httpClient.GetStringAsync(feedUrl);
            
            var doc = XDocument.Parse(response);
            var ns = XNamespace.Get("http://www.w3.org/2005/Atom");
            var ytNs = XNamespace.Get("http://www.youtube.com/xml/schemas/2015");
            
            var entries = doc.Descendants(ns + "entry");
            
            foreach (var entry in entries)
            {
                var videoId = entry.Element(ytNs + "videoId")?.Value;
                var title = entry.Element(ns + "title")?.Value;
                var link = entry.Element(ns + "link")?.Attribute("href")?.Value;
                var publishedStr = entry.Element(ns + "published")?.Value;
                
                if (!string.IsNullOrEmpty(videoId) && !string.IsNullOrEmpty(title) && 
                    !string.IsNullOrEmpty(link) && DateTime.TryParse(publishedStr, out var published) &&
                    !link.Contains("/shorts/"))
                {
                    var video = new Video
                    {
                        VideoId = videoId,
                        Title = title,
                        Url = link,
                        PublishedAt = published
                    };

                    // Download thumbnail if service is available
                    if (_thumbnailDownloader != null)
                    {
                        try
                        {
                            // Check if thumbnail already exists
                            var existingThumbnailPath = _thumbnailDownloader.GetThumbnailPath(videoId);
                            if (existingThumbnailPath != null)
                            {
                                video.ThumbnailPath = existingThumbnailPath;
                                _logger?.LogDebug("Using existing thumbnail for video {VideoId}", videoId);
                            }
                            else
                            {
                                // Download new thumbnail
                                var thumbnailPath = await _thumbnailDownloader.DownloadThumbnailAsync(videoId);
                                if (thumbnailPath != null)
                                {
                                    video.ThumbnailPath = thumbnailPath;
                                    _logger?.LogInformation("Downloaded thumbnail for video {VideoId}: {Title}", videoId, title);
                                }
                                else
                                {
                                    _logger?.LogWarning("Failed to download thumbnail for video {VideoId}: {Title}", videoId, title);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error processing thumbnail for video {VideoId}: {Title}", videoId, title);
                        }
                    }

                    videos.Add(video);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch videos for channel {channelId}", ex);
        }
        
        return videos;
    }
    
    public async Task<string?> GetChannelNameFromFeedAsync(string channelId)
    {
        try
        {
            var feedUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
            var response = await _httpClient.GetStringAsync(feedUrl);
            
            var doc = XDocument.Parse(response);
            var ns = XNamespace.Get("http://www.w3.org/2005/Atom");
            
            return doc.Descendants(ns + "title").FirstOrDefault()?.Value;
        }
        catch
        {
            return null;
        }
    }
}