using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Models;

namespace WebApp.Api.DAL;

// Database operations for quest browsing and management. Grading and XP rules stay in services.
public class QuestRepository : IQuestRepository
{
    private readonly AppDbContext _db;

    public QuestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Quest>> SearchAsync(string? search, string? difficulty, int? courseId, string? status, int? userId)
    {
        var query = _db.Quests.AsNoTracking().Include(q => q.Questions).Include(q => q.Course).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(q => EF.Functions.Like(q.Title, $"%{text}%")
                || (q.Description != null && EF.Functions.Like(q.Description, $"%{text}%")));
        }

        if (DifficultyRules.Normalize(difficulty) is string level)
            query = query.Where(q => q.Difficulty == level);

        if (courseId is not null)
            query = query.Where(q => q.CourseId == courseId);

        if (userId is not null && status is "completed" or "notCompleted")
        {
            var completed = _db.QuestCompletions.Where(c => c.UserId == userId).Select(c => c.QuestId);
            query = status == "completed"
                ? query.Where(q => completed.Contains(q.Id))
                : query.Where(q => !completed.Contains(q.Id));
        }

        return await query.ToListAsync();
    }

    public Task<Dictionary<int, int>> GetBestPercentAsync(int userId) =>
        _db.QuestCompletions.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.QuestId, c => c.MaxScore == 0 ? 100 : c.BestScore * 100 / c.MaxScore);

    public async Task<Quest?> GetByIdAsync(int id) => await _db.Quests.FindAsync(id);

    public Task<Quest?> GetWithQuestionsAsync(int id, bool tracking = false)
    {
        var query = tracking ? _db.Quests.AsQueryable() : _db.Quests.AsNoTracking();
        return query.Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public Task<bool> TitleExistsAsync(string title, int excludeId) =>
        _db.Quests.AnyAsync(q => q.Title == title && q.Id != excludeId);

    public Task<bool> CourseExistsAsync(int courseId) =>
        _db.Courses.AnyAsync(c => c.Id == courseId);

    public async Task AddAsync(Quest quest)
    {
        _db.Quests.Add(quest);
        await _db.SaveChangesAsync();
        await _db.Entry(quest).Reference(q => q.Course).LoadAsync();
    }

    public async Task UpdateAsync(Quest quest)
    {
        // The quest is loaded with tracking. Replacing its Questions collection deletes
        // the old required child rows and their options through EF's cascade rules.
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Quest quest)
    {
        _db.Quests.Remove(quest);
        await _db.SaveChangesAsync();
    }
}
