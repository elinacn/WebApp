using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Dtos;
using WebApp.Api.Extensions;
using WebApp.Api.Services;

namespace WebApp.Api.Controllers;

// The signed-in user's own stats: XP, streak, this week's activity and latest quests
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StreakService _streaks;

    public ProfileController(AppDbContext db, StreakService streaks)
    {
        _db = db;
        _streaks = streaks;
    }

    // GET /api/profile
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var userId = User.GetUserId()!.Value;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        var recent = await _db.QuestCompletions.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastCompletedAt)
            .Take(10)
            .Select(c => new ActivityDto(c.QuestId, c.Quest.Title, c.BestScore, c.MaxScore, c.Attempts, c.LastCompletedAt))
            .ToListAsync();

        return new ProfileDto(
            user.ToDto(),
            await _streaks.GetStreakAsync(userId),
            await _streaks.GetCurrentWeekAsync(userId),
            recent);
    }
}
