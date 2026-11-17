using WebApp.Models;

namespace WebApp.DAL;

public interface IProgressRepository
{
    Task<List<QuestCompletion>> GetRecentAsync(int userId, int limit);
    Task<QuestCompletion?> GetCompletionAsync(int userId, int questId);
    Task SaveCompletionAsync(QuestCompletion completion);
    Task<bool> HasActivityAsync(string userId, DateOnly date);
    Task<List<DateOnly>> GetActivityDatesAsync(string userId, DateOnly? from = null, DateOnly? to = null);
    Task AddActivityAsync(DailyStreaks activity);
}
