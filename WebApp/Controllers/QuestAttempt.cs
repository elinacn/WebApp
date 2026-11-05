namespace WebApp.Models;

public class QuestAttempt
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public int QuestId { get; set; } 
    public int PointsEarned { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
}