using WebApp.Api.Models;

namespace WebApp.Api.DAL;

public interface IUserRepository
{
    Task<List<User>> SearchAsync(string? search, string? program, string? role);
    Task<List<string>> GetProgramsAsync();
    Task<List<User>> GetLeaderboardAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsAsync(int id);
    Task<bool> UsernameExistsAsync(string username, int excludeId = 0);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
