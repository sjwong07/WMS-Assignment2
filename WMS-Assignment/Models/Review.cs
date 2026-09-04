using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;

public class Review
{
    public int Id { get; set; }

    // Make MenuItemId nullable for general cafe reviews
    [MaxLength(100)]
    public string? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } // 1 to 5 stars

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}