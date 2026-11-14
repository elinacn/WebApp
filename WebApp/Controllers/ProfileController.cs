using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Extensions;
using WebApp.Models;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly DailyStreakService _streakService;

    public ProfileController(SheetsDbContext context, DailyStreakService streakService)
    {
        _context = context;
        _streakService = streakService;
    }

    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();
        if (userId is null) return Challenge();

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user is null) return NotFound();

        var model = new ProfileViewModel
        {
            Name = user.Name,
            Username = user.Username,
            TotalXp = user.Points,
            QuizzesCompleted = user.QuizzesCompleted,
            Streak = await _streakService.GetStreakAsync(user.Id.ToString()),
            RecentActivity = await _context.QuestCompletions.AsNoTracking()
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.LastCompletedAt)
                .ThenByDescending(c => c.Id)
                .Take(5)
                .Select(c => new ProfileActivityViewModel
                {
                    QuestTitle = c.Quest.Title,
                    BestScore = c.BestScore,
                    MaxScore = c.MaxScore,
                    Attempts = c.Attempts,
                    CompletedAtUtc = c.LastCompletedAt
                })
                .ToListAsync()
        };

        return View(model);
    }
}
