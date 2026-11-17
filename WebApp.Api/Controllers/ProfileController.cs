using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Api.DAL;
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
    private readonly IUserRepository _users;
    private readonly IProgressRepository _progress;
    private readonly StreakService _streaks;

    public ProfileController(IUserRepository users, IProgressRepository progress, StreakService streaks)
    {
        _users = users;
        _progress = progress;
        _streaks = streaks;
    }

    // GET /api/profile
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var userId = User.GetUserId()!.Value;
        var user = await _users.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        var completions = await _progress.GetRecentAsync(userId, 10);
        var recent = completions.Select(c => new ActivityDto(c.QuestId, c.Quest.Title,
            c.BestScore, c.MaxScore, c.Attempts, c.LastCompletedAt)).ToList();

        return new ProfileDto(
            user.ToDto(),
            await _streaks.GetStreakAsync(userId),
            await _streaks.GetCurrentWeekAsync(userId),
            recent);
    }
}
