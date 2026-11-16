using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Dtos;
using WebApp.Api.Models;

namespace WebApp.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaderboardController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/leaderboard?search=an&program=Web programming&top=3
    // Students ranked by XP. Ranks are computed before filtering, so a user keeps their real rank in search results.
    [HttpGet]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> Get(string? search, string? program, int? top)
    {
        var students = await _db.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Student)
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Name)
            .ToListAsync();

        IEnumerable<LeaderboardEntryDto> ranked = students.Select((u, index) =>
            new LeaderboardEntryDto(index + 1, u.Id, u.Name, u.StudyProgram, u.Points, u.QuizzesCompleted));

        if (!string.IsNullOrWhiteSpace(search))
            ranked = ranked.Where(e => e.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(program))
            ranked = ranked.Where(e => e.StudyProgram == program);
        if (top is > 0)
            ranked = ranked.Take(top.Value);

        return ranked.ToList();
    }
}
