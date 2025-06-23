using System.Xml.Linq;
using newyt.shared.Models;

namespace newyt.shared.Services;

public class YouTubeRssService
{
    private readonly HttpClient _httpClient;

    public YouTubeRssService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
                    !string.IsNullOrEmpty(link) && DateTime.TryParse(publishedStr, out var published))
                {
                    videos.Add(new Video
                    {
                        VideoId = videoId,
                        Title = title,
                        Url = link,
                        PublishedAt = published
                    });
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