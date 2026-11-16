using WebApp.Models;

namespace WebApp.DAL;

public interface IProgressRepository
{
    Task<List<QuestCompletion>> GetRecentAsync(int userId, int limit);
}
