using WebApp.DAL;
using WebApp.Models;

namespace WebApp.Services;

public class DailyStreakService
{
    private readonly IProgressRepository _progress;

    public DailyStreakService(IProgressRepository progress)
    {
        _progress = progress;
    }
    
    public async Task<List<bool>> GetCurrentWeekActivityAsync(string userId)
    {
        var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        var osloTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, osloTimeZone);
        var today = DateOnly.FromDateTime(osloTime);

        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = today.AddDays(-diff);
        var sunday = monday.AddDays(6);

        var activeDates = await _progress.GetActivityDatesAsync(userId, monday, sunday);

        var activeSet = activeDates.ToHashSet();
        var weekStatus = new List<bool>();

        for (int i = 0; i < 7; i++)
        {
            weekStatus.Add(activeSet.Contains(monday.AddDays(i)));
        }

        return weekStatus;
    }

    public async Task RecordActivityAsync(string userId)
    {
        var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        var osloTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, osloTimeZone);

        var today = DateOnly.FromDateTime(osloTime);

        var alreadyRecorded = await _progress.HasActivityAsync(userId, today);

        if (alreadyRecorded)
        {
            return;
        }

        var activity = new DailyStreaks
        {
            UserId = userId,
            ActivityDate = today
        };

        await _progress.AddActivityAsync(activity);
    }
    public async Task<int> GetStreakAsync(string userId)
    {
    var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
    var osloTime = TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow, osloTimeZone);

    var today = DateOnly.FromDateTime(osloTime);

    var dates = await _progress.GetActivityDatesAsync(userId);

    var activityDays = dates.ToHashSet();

    var day = activityDays.Contains(today)
        ? today
        : today.AddDays(-1);

    var streak = 0;

    while (activityDays.Contains(day))
    {
        streak++;
        day = day.AddDays(-1);
    }

    return streak;
    }
    
    
}

