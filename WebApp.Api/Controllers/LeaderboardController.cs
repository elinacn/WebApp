using Microsoft.AspNetCore.Mvc;
using WebApp.Api.DAL;
using WebApp.Api.Dtos;

namespace WebApp.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly IUserRepository _users;

    public LeaderboardController(IUserRepository users)
    {
        _users = users;
    }

    // GET /api/leaderboard?search=an&program=Web programming&top=3
    // Students ranked by XP. Ranks are computed before filtering, so a user keeps their real rank in search results.
    [HttpGet]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> Get(string? search, string? program, int? top)
    {
        var students = await _users.GetLeaderboardAsync();

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
