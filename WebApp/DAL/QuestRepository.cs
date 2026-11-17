using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.DAL;

public class QuestRepository : IQuestRepository
{
    private readonly SheetsDbContext _db;

    public QuestRepository(SheetsDbContext db)
    {
        _db = db;
    }

    public async Task<Quest?> GetByIdAsync(int id) => await _db.Quests.FindAsync(id);

    public Task<Quest?> GetWithQuestionsAsync(int id, bool tracking = false)
    {
        var query = tracking ? _db.Quests.AsQueryable() : _db.Quests.AsNoTracking();
        return query.Include(q => q.Questions).ThenInclude(q => q.Options).FirstOrDefaultAsync(q => q.Id == id);
    }

    public Task<bool> ExistsAsync(int id) => _db.Quests.AnyAsync(q => q.Id == id);

    public Task<bool> TitleExistsAsync(string title, int excludeId = 0) =>
        _db.Quests.AnyAsync(q => q.Title == title && q.Id != excludeId);

    public async Task<List<Quest>> SearchAsync(string? search, string? difficulty, int? creatorId = null, int? savedByUserId = null)
    {
        var query = _db.Quests.AsNoTracking().Include(q => q.Questions).AsQueryable();
        if (creatorId.HasValue) query = query.Where(q => q.CreatorUserId == creatorId.Value);
        if (savedByUserId.HasValue)
            query = query.Where(q => _db.SavedQuests.Any(s => s.UserId == savedByUserId && s.QuestId == q.Id));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(q => EF.Functions.Like(q.Title, $"%{search}%"));
        if (difficulty is not null) query = query.Where(q => EF.Functions.Like(q.Difficulty, difficulty));
        return await query.OrderBy(q => q.Title).ToListAsync();
    }

    public Task<List<int>> GetSavedIdsAsync(int userId) =>
        _db.SavedQuests.AsNoTracking().Where(s => s.UserId == userId).Select(s => s.QuestId).ToListAsync();

    public Task<Dictionary<int, int>> GetBestPercentAsync(int userId) =>
        _db.QuestCompletions.AsNoTracking().Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.QuestId, c => c.MaxScore == 0 ? 100 : c.BestScore * 100 / c.MaxScore);

    public async Task<bool> ToggleSaveAsync(int userId, int questId)
    {
        var bookmark = await _db.SavedQuests.FindAsync(userId, questId);
        if (bookmark is null) _db.SavedQuests.Add(new SavedQuest { UserId = userId, QuestId = questId });
        else _db.SavedQuests.Remove(bookmark);
        await _db.SaveChangesAsync();
        return bookmark is null;
    }

    public async Task AddAsync(Quest quest)
    {
        _db.Quests.Add(quest);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Quest quest)
    {
        // Clearing the tracked Questions collection removes its old required children.
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Quest quest)
    {
        _db.Quests.Remove(quest);
        await _db.SaveChangesAsync();
    }
}
