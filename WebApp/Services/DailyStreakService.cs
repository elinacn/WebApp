using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Services;

public class DailyStreakService
{
    private readonly SheetsDbContext _context;

    public DailyStreakService(SheetsDbContext context)
    {
        _context = context;
    }

    public async Task RecordActivityAsync(string userId)
    {
        var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        var osloTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, osloTimeZone);

        var today = DateOnly.FromDateTime(osloTime);

        var alreadyRecorded = await _context.DailyStreaks
            .AnyAsync(s => s.UserId == userId &&
                           s.ActivityDate == today);

        if (alreadyRecorded)
        {
            return;
        }

        var activity = new DailyStreaks
        {
            UserId = userId,
            ActivityDate = today
        };

        _context.DailyStreaks.Add(activity);
        await _context.SaveChangesAsync();
    }
    public async Task<int> GetStreakAsync(string userId)
    {
    var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
    var osloTime = TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow, osloTimeZone);

    var today = DateOnly.FromDateTime(osloTime);

    var dates = await _context.DailyStreaks
        .Where(s => s.UserId == userId)
        .Select(s => s.ActivityDate)
        .ToListAsync();

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