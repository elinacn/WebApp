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
}
