using System.ComponentModel.DataAnnotations;

namespace newyt.shared.Models;

public class Channel
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ChannelId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<Video> Videos { get; set; } = [];
}