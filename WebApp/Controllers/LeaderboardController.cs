using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels;
using WebApp.Models;

namespace WebApp.Controllers;

public class LeaderboardController : Controller
{
    private readonly LeaderboardDbContext _context;

    public LeaderboardController(LeaderboardDbContext context)
    {
        _context = context;
    }

    public IActionResult Index() => RedirectToAction(nameof(Leaderboard));

    public async Task<IActionResult> Leaderboard(string? search, string? course)
    {
        // start querying the QuestAttempts table
        var query = _context.QuestAttempts.AsQueryable();

        // filter by course
        if (!string.IsNullOrEmpty(course))
        {
            query = query.Where(q => q.Course == course);
        }

        // group attempts by student and course, summing up their points and counting completed quizzes
        var rawEntries = await query
            .GroupBy(q => new { q.StudentName, q.Course })
            .Select(g => new LeaderboardEntryViewModel
            {
                Name = g.Key.StudentName,
                Course = g.Key.Course,
                Points = g.Sum(x => x.PointsEarned),
                QuizzesCompleted = g.Count()
            })
            .ToListAsync();

            var availableCourses = await _context.QuestAttempts
                .Select(q => q.Course)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

        var model = new LeaderboardViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Course = course?.Trim() ?? string.Empty,
            Courses = availableCourses
        };

        var ranked = rawEntries
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
