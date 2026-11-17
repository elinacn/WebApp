using WebApp.Models;

namespace WebApp.DAL;

// Database operations used by the course controller.
public interface ICourseRepository
{
    Task<List<Course>> GetAllAsync();
    Task<Course?> GetByIdAsync(int id);
    Task<Course?> GetWithQuestsAsync(int id);
    Task<bool> CodeExistsAsync(string code, int excludeId);
    Task<bool> TitleExistsAsync(string title, int excludeId);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
}
