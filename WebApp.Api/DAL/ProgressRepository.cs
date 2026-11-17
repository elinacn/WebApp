using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Models;

namespace WebApp.Api.DAL;

public class ProgressRepository : IProgressRepository
{
    private readonly AppDbContext _context;

    public ProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<QuestCompletion>> GetRecentAsync(int userId, int limit) =>
        _context.QuestCompletions.AsNoTracking().Include(c => c.Quest)
            .Where(c => c.UserId == userId).OrderByDescending(c => c.LastCompletedAt)
            .Take(limit).ToListAsync();

    public Task<QuestCompletion?> GetCompletionAsync(int userId, int questId) =>
        _context.QuestCompletions.FirstOrDefaultAsync(c => c.UserId == userId && c.QuestId == questId);

    public async Task SaveCompletionAsync(QuestCompletion completion)
    {
        if (completion.Id == 0) _context.QuestCompletions.Add(completion);
        // All repositories share one context per request, keeping the completion
        // and the tracked user's XP in the same save transaction.
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasActivityAsync(int userId, DateOnly date) =>
        _context.DailyActivities.AnyAsync(a => a.UserId == userId && a.Date == date);

    public async Task<List<DateOnly>> GetActivityDatesAsync(int userId, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _context.DailyActivities.AsNoTracking().Where(a => a.UserId == userId);
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);
        return await query.Select(a => a.Date).ToListAsync();
    }

    public async Task AddActivityAsync(DailyActivity activity)
    {
        _context.DailyActivities.Add(activity);
        await _context.SaveChangesAsync();
    }
}
