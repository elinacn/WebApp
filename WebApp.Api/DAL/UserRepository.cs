using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Models;

namespace WebApp.Api.DAL;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> SearchAsync(string? search, string? program, string? role)
    {
        var query = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(u => EF.Functions.Like(u.Name, $"%{text}%") || EF.Functions.Like(u.Username, $"%{text}%"));
        }
        if (!string.IsNullOrWhiteSpace(program)) query = query.Where(u => u.StudyProgram == program);
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
        return await query.OrderBy(u => u.Name).ToListAsync();
    }

    public Task<List<string>> GetProgramsAsync() =>
        _context.Users.Select(u => u.StudyProgram).Distinct().OrderBy(p => p).ToListAsync();

    public Task<List<User>> GetLeaderboardAsync() =>
        _context.Users.AsNoTracking().Where(u => u.Role == Roles.Student)
            .OrderByDescending(u => u.Points).ThenBy(u => u.Name).ToListAsync();

    public async Task<User?> GetByIdAsync(int id) => await _context.Users.FindAsync(id);

    public Task<User?> GetByUsernameAsync(string username)
    {
        var name = username.Trim().ToLower();
        return _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name);
    }

    public Task<bool> ExistsAsync(int id) => _context.Users.AnyAsync(u => u.Id == id);

    public Task<bool> UsernameExistsAsync(string username, int excludeId = 0)
    {
        var name = username.ToLower();
        return _context.Users.AnyAsync(u => u.Username.ToLower() == name && u.Id != excludeId);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        // Keep unchanged fields, such as XP, out of the update.
        _context.Entry(user).DetectChanges();
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}
