using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.DAL;

public class ProgressRepository : IProgressRepository
{
    private readonly SheetsDbContext _context;

    public ProgressRepository(SheetsDbContext context)
    {
        _context = context;
    }

    public Task<List<QuestCompletion>> GetRecentAsync(int userId, int limit) =>
        _context.QuestCompletions.AsNoTracking().Include(c => c.Quest)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastCompletedAt).ThenByDescending(c => c.Id)
            .Take(limit).ToListAsync();

    public Task<QuestCompletion?> GetCompletionAsync(int userId, int questId) =>
        _context.QuestCompletions.FirstOrDefaultAsync(c => c.UserId == userId && c.QuestId == questId);

    public async Task SaveCompletionAsync(QuestCompletion completion)
    {
        if (completion.Id == 0) _context.QuestCompletions.Add(completion);
        // Repositories share one context per request, so the completion and the
        // tracked user's XP are saved together in one transaction.
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasActivityAsync(string userId, DateOnly date) =>
        _context.DailyStreaks.AnyAsync(a => a.UserId == userId && a.ActivityDate == date);

    public async Task<List<DateOnly>> GetActivityDatesAsync(string userId, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _context.DailyStreaks.AsNoTracking().Where(a => a.UserId == userId);
        if (from.HasValue) query = query.Where(a => a.ActivityDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ActivityDate <= to.Value);
        return await query.Select(a => a.ActivityDate).ToListAsync();
    }

    public async Task AddActivityAsync(DailyStreaks activity)
    {
        _context.DailyStreaks.Add(activity);
        await _context.SaveChangesAsync();
    }
}
