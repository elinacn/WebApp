using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class LeaderboardController : Controller
{
    private readonly SheetsDbContext _context;

    public LeaderboardController(SheetsDbContext context)
    {
        _context = context;
    }

    public IActionResult Index() => RedirectToAction(nameof(Leaderboard));

    public async Task<IActionResult> Leaderboard(string? search)
    {
        var query = _context.Users.AsNoTracking();

        var searchTerm = search?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => EF.Functions.Like(u.Name, $"%{searchTerm}%"));
        }

        var users = await query
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Name)
            .ToListAsync();

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
