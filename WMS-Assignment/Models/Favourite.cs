public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public string MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; }
}