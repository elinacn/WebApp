using WebApp.Api.Models;

namespace WebApp.Api.DAL;

public interface IProgressRepository
{
    Task<List<QuestCompletion>> GetRecentAsync(int userId, int limit);
    Task<QuestCompletion?> GetCompletionAsync(int userId, int questId);
    Task SaveCompletionAsync(QuestCompletion completion);
    Task<bool> HasActivityAsync(int userId, DateOnly date);
    Task<List<DateOnly>> GetActivityDatesAsync(int userId, DateOnly? from = null, DateOnly? to = null);
    Task AddActivityAsync(DailyActivity activity);
}
