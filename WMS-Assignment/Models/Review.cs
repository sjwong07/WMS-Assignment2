using System.ComponentModel.DataAnnotations;

public class Review
{
    public int Id { get; set; }
    public string MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } // 1 to 5 stars

    [MaxLength(500)]
    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}