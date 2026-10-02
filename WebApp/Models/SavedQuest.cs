namespace WebApp.Models;

// The database code should fill these in. Each property is shown on the page.
public class SavedQuest
{
    public string UserId { get; set; } = string.Empty;
    public int QuestId { get; set; }
    public DateTime SavedAt { get; set; }
}
