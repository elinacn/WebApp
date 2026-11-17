using WebApp.Api.DAL;
using WebApp.Api.Models;

namespace WebApp.Api.Services;

// Tracks the days a user completes quests. Days are counted in Oslo time,
// so a quest at 00:30 Norwegian time counts for the new day.
public class StreakService
{
    private static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private readonly IProgressRepository _progress;

    public StreakService(IProgressRepository progress)
    {
        _progress = progress;
    }

    public static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Oslo));

    public async Task RecordActivityAsync(int userId)
    {
        var today = Today();
        if (await _progress.HasActivityAsync(userId, today)) return;

        await _progress.AddActivityAsync(new DailyActivity { UserId = userId, Date = today });
    }

    // Days in a row with activity, ending today (or yesterday, so the streak isn't lost before today's quest)
    public async Task<int> GetStreakAsync(int userId)
    {
        var days = (await _progress.GetActivityDatesAsync(userId)).ToHashSet();

        var day = days.Contains(Today()) ? Today() : Today().AddDays(-1);
        var streak = 0;
        while (days.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    // Monday to Sunday of the current week: true for each day with activity
    public async Task<List<bool>> GetCurrentWeekAsync(int userId)
    {
        var today = Today();
        var monday = today.AddDays(-((7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7));
        var sunday = monday.AddDays(6);

        var active = (await _progress.GetActivityDatesAsync(userId, monday, sunday)).ToHashSet();

        return Enumerable.Range(0, 7).Select(i => active.Contains(monday.AddDays(i))).ToList();
    }
}
