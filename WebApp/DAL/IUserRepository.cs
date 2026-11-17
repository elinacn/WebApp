using WebApp.Models;

namespace WebApp.DAL;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetLeaderboardAsync(string? search = null, int? limit = null);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsAsync(int id);
    Task<bool> UsernameExistsAsync(string username, int excludeId = 0);
    Task<int> CountAdminsAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
