using System.ComponentModel.DataAnnotations;

namespace newyt.shared.Models;

public class Video
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string VideoId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    public int ChannelId { get; set; }
    
    public DateTime PublishedAt { get; set; }
    
    public bool IsWatched { get; set; } = false;
    
    public DateTime? WatchedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Channel Channel { get; set; } = null!;
}