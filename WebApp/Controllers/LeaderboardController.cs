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

    public async Task<IActionResult> Leaderboard(string? search, string? program)
    {
        var model = new LeaderboardViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Program = program?.Trim() ?? string.Empty,
            Programs = await _context.Users.AsNoTracking()
                .Select(u => u.StudyProgram)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync()
        };

        // Rank the selected program before searching so a name search keeps its rank.
        var ranked = await _context.Users.AsNoTracking()
            .Where(u => model.Program.Length == 0 || u.StudyProgram == model.Program)
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Name)
            .Select(u => new LeaderboardEntryViewModel
            {
                Name = u.Name,
                Program = u.StudyProgram,
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
