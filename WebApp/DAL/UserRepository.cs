using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.DAL;

public class UserRepository : IUserRepository
{
    private readonly SheetsDbContext _context;

    public UserRepository(SheetsDbContext context)
    {
        _context = context;
    }

    public Task<List<User>> GetAllAsync() =>
        _context.Users.AsNoTracking()
            .OrderByDescending(u => u.Points).ThenBy(u => u.Username).ToListAsync();

    public async Task<List<User>> GetLeaderboardAsync(string? search = null, int? limit = null)
    {
        var query = _context.Users.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => EF.Functions.Like(u.Name, $"%{search}%"));
        query = query.OrderByDescending(u => u.Points).ThenBy(u => u.Name);
        if (limit.HasValue) query = query.Take(limit.Value);
        return await query.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id) => await _context.Users.FindAsync(id);

    public Task<User?> GetByUsernameAsync(string username)
    {
        var name = username.Trim().ToLower();
        return _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name);
    }

    public Task<bool> ExistsAsync(int id) => _context.Users.AnyAsync(u => u.Id == id);

    public Task<bool> UsernameExistsAsync(string username, int excludeId = 0) =>
        _context.Users.AnyAsync(u => u.Username == username && u.Id != excludeId);

    public Task<int> CountAdminsAsync() => _context.Users.CountAsync(u => u.IsAdmin);

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        // Users are loaded with tracking; update only the fields that changed.
        _context.Entry(user).DetectChanges();
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}
