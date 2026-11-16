namespace WebApp.Api.Models;

// A day (in Oslo time) on which a user completed at least one quest. Used for streaks.
public class DailyActivity
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly Date { get; set; }
}
