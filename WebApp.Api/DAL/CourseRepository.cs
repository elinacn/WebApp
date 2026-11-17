using Microsoft.EntityFrameworkCore;
using WebApp.Api.Models;
using WebApp.Api.Data;

namespace WebApp.Api.DAL;

// All course queries and saves stay in the data access layer.
public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _context;

    public CourseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetAllAsync(string? search)
    {
        var query = _context.Courses.AsNoTracking().Include(c => c.Quests).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(c => EF.Functions.Like(c.Code, $"%{text}%") || EF.Functions.Like(c.Title, $"%{text}%"));
        }
        return await query.OrderBy(c => c.Title).ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(int id) =>
        await _context.Courses.FindAsync(id);

    public Task<Course?> GetWithQuestsAsync(int id) =>
        _context.Courses.AsNoTracking()
            .Include(c => c.Quests.OrderBy(q => q.Title))
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> CodeExistsAsync(string code, int excludeId) =>
        _context.Courses.AnyAsync(c => c.Code == code && c.Id != excludeId);

    public Task<bool> TitleExistsAsync(string title, int excludeId) =>
        _context.Courses.AnyAsync(c => c.Title == title && c.Id != excludeId);

    public async Task AddAsync(Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Course course)
    {
        _context.Courses.Update(course);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Course course)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }
}
