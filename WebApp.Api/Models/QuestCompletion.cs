namespace WebApp.Api.Models;

// Join entity for the many-to-many relationship between User and Quest:
// one row per user per completed quest, with the best score and attempt count.
// Dates are stored in UTC.
public class QuestCompletion
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int QuestId { get; set; }
    public Quest Quest { get; set; } = null!;

    // Points for the correctly answered questions in the best attempt
    public int BestScore { get; set; }

    // The quest's total points at the last attempt (quests can be edited later)
    public int MaxScore { get; set; }

    public int Attempts { get; set; }
    public DateTime FirstCompletedAt { get; set; }
    public DateTime LastCompletedAt { get; set; }
}
