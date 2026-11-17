using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Extensions;
using WebApp.DAL;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserRepository _users;
    private readonly IProgressRepository _progress;
    private readonly DailyStreakService _streakService;

    public ProfileController(IUserRepository users, IProgressRepository progress, DailyStreakService streakService)
    {
        _users = users;
        _progress = progress;
        _streakService = streakService;
    }

    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();
        if (userId is null) return Challenge();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user is null) return NotFound();

        var recent = await _progress.GetRecentAsync(user.Id, 5);
        var model = new ProfileViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username,
            TotalXp = user.Points,
            QuizzesCompleted = user.QuizzesCompleted,
            Streak = await _streakService.GetStreakAsync(user.Id.ToString()),
            RecentActivity = recent
                .Select(c => new ProfileActivityViewModel
                {
                    QuestTitle = c.Quest.Title,
                    BestScore = c.BestScore,
                    MaxScore = c.MaxScore,
                    Attempts = c.Attempts,
                    CompletedAtUtc = c.LastCompletedAt
                })
                .ToList()
        };

        return View(model);
    }
}
