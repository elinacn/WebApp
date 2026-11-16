using WebApp.Api.Models;

namespace WebApp.Api.DAL;

public interface IQuestRepository
{
    Task<List<Quest>> SearchAsync(string? search, string? difficulty, int? courseId, string? status, int? userId);
    Task<Dictionary<int, int>> GetBestPercentAsync(int userId);
    Task<Quest?> GetByIdAsync(int id);
    Task<Quest?> GetWithQuestionsAsync(int id, bool tracking = false);
    Task<bool> TitleExistsAsync(string title, int excludeId);
    Task<bool> CourseExistsAsync(int courseId);
    Task AddAsync(Quest quest);
    Task UpdateAsync(Quest quest);
    Task DeleteAsync(Quest quest);
}
