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

    public async Task<IActionResult> Leaderboard(string? search, string? course)
    {
        var model = new LeaderboardViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Course = course?.Trim() ?? string.Empty,
            Courses = await _context.Users.AsNoTracking()
                .Select(u => u.Course)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync()
        };

        // Rank the selected course before searching so a name search keeps its rank.
        var ranked = await _context.Users.AsNoTracking()
            .Where(u => model.Course.Length == 0 || u.Course == model.Course)
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Name)
            .Select(u => new LeaderboardEntryViewModel
            {
                Name = u.Name,
                Course = u.Course,
                Points = u.Points,
                QuizzesCompleted = u.QuizzesCompleted
            })
            .ToListAsync();

        for (var i = 0; i < ranked.Count; i++)
        {
            ranked[i].Rank = i + 1;
        }

        model.Entries = ranked
            .Where(s => s.Name.Contains(model.Search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return View(model);
    }
}
