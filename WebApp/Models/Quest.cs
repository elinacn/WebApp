namespace WebApp.Models;

// The database code should fill these in. just barebones right now
public class Quest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public int PointsReward { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public bool IsFeatured { get; set; }
}
