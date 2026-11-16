using WebApp.Models;

namespace WebApp.DAL;

public interface IQuestRepository
{
    Task<Quest?> GetByIdAsync(int id);
    Task<Quest?> GetWithQuestionsAsync(int id, bool tracking = false);
    Task<bool> ExistsAsync(int id);
    Task<bool> TitleExistsAsync(string title, int excludeId = 0);
    Task<List<Quest>> SearchAsync(string? search, string? difficulty, int? creatorId = null, int? savedByUserId = null);
    Task<List<int>> GetSavedIdsAsync(int userId);
    Task<Dictionary<int, int>> GetBestPercentAsync(int userId);
    Task<bool> ToggleSaveAsync(int userId, int questId);
    Task AddAsync(Quest quest);
    Task UpdateAsync(Quest quest);
    Task DeleteAsync(Quest quest);
}
