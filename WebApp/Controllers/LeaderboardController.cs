using Microsoft.AspNetCore.Mvc;
using WebApp.DAL;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class LeaderboardController : Controller
{
    private readonly IUserRepository _users;

    public LeaderboardController(IUserRepository users)
    {
        _users = users;
    }

    public IActionResult Index() => RedirectToAction(nameof(Leaderboard));

    public async Task<IActionResult> Leaderboard(string? search)
    {
        var searchTerm = search?.Trim() ?? string.Empty;
        var users = await _users.GetLeaderboardAsync(searchTerm);

        var model = new LeaderboardViewModel
        {
            Search = searchTerm,
            Entries = users.Select((u, index) => new LeaderboardEntryViewModel
            {
                Rank = index + 1,
                Name = u.Name,
                Points = u.Points,
                QuizzesCompleted = u.QuizzesCompleted
            }).ToList()
        };

        return View(model);
    }
} 
