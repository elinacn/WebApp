namespace WebApp.Models;

public class DailyStreaks
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly ActivityDate { get; set; }
}