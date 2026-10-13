using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class LeaderboardController : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Leaderboard));

    public IActionResult Leaderboard(string? search, string? course)
    {
        // Sample scores until student accounts and quiz results are stored.
        var students = new List<LeaderboardEntryViewModel>
        {
            new() { Name = "Anna", Course = "Web programming", Points = 3000, QuizzesCompleted = 24 },
            new() { Name = "Beatrice", Course = "Web programming", Points = 2400, QuizzesCompleted = 20 },
            new() { Name = "Calvin", Course = "Programming basics", Points = 2350, QuizzesCompleted = 19 },
            new() { Name = "Elina", Course = "Web programming", Points = 2100, QuizzesCompleted = 17 },
            new() { Name = "Froed", Course = "Programming basics", Points = 1800, QuizzesCompleted = 15 },
            new() { Name = "Johan", Course = "Web programming", Points = 1250, QuizzesCompleted = 12 },
            new() { Name = "Øyvind", Course = "Programming basics", Points = 1100, QuizzesCompleted = 10 },
            new() { Name = "Maya", Course = "Web programming", Points = 850, QuizzesCompleted = 8 }
        };

        var model = new LeaderboardViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Course = course?.Trim() ?? string.Empty,
            Courses = students.Select(s => s.Course).Distinct().OrderBy(c => c).ToList()
        };

        // Rank the selected course before searching so a name search keeps its rank.
        var ranked = students
            .Where(s => model.Course.Length == 0 || s.Course == model.Course)
            .OrderByDescending(s => s.Points)
            .ThenBy(s => s.Name)
            .ToList();

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
