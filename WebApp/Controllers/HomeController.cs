using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Extensions;
using WebApp.DAL;
using WebApp.Models;
using WebApp.Services;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IUserRepository _users;
    private readonly DailyStreakService _streakService;

    public HomeController(IUserRepository users, DailyStreakService streakService)
    {
        _users = users;
        _streakService = streakService;
    }

    public async Task<IActionResult> Index()
    {
        // The signed-in user's code points from completed quests (0 for visitors)
        var userId = User.GetUserId();
        var user = userId is null ? null : await _users.GetByIdAsync(userId.Value);
        ViewData["CodePoints"] = user?.Points ?? 0;

        int streak = 0;
        List<bool> weekActivity = [false, false, false, false, false, false, false];

        if (userId is not null)
        {
            var idString = userId.Value.ToString();
            streak = await _streakService.GetStreakAsync(idString);
            weekActivity = await _streakService.GetCurrentWeekActivityAsync(idString);
        }
        ViewData["Streak"] = streak;
        ViewData["WeekActivity"] = weekActivity;

        // fetches top 3 user for mini leaderboard
        var dbUsers = await _users.GetLeaderboardAsync(limit: 3);

        // 2. Map them and assign ranks in memory
        var topUsers = dbUsers.Select((u, index) => new LeaderboardEntryViewModel
        {
            Rank = index + 1,
            Name = u.Name,
            Points = u.Points,
            QuizzesCompleted = u.QuizzesCompleted
        }).ToList();

        ViewData["TopUsers"] = topUsers;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Target of UseStatusCodePagesWithReExecute in Program.cs.
    // Not named StatusCode because that would hide ControllerBase.StatusCode(int).
    [Route("Home/StatusCode/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        return View("StatusCode", code);
    }
}
