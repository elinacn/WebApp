namespace WebApp.Models;

public class SavedQuest
{
    public int UserId { get; set; }
    public int QuestId { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
