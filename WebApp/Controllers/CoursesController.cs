using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers;

public class CoursesController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(SheetsDbContext context, ILogger<CoursesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // READ: list, with quests loaded for the counts
    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses.AsNoTracking()
            .Include(c => c.Quests)
            .OrderBy(c => c.Title)
            .ToListAsync();
        return View(courses);
    }

    // READ: single, with its quests
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return BadRequest();

        var course = await FindWithQuestsAsync(id.Value);
        if (course is null)
        {
            _logger.LogWarning("Course {CourseId} not found", id);
            return NotFound();
        }
        return View(course);
    }

    // CREATE
    public IActionResult Create() => View(new Course());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Title,Description")] Course course)
    {
        Normalize(course);
        await CheckDuplicatesAsync(course);

        if (!ModelState.IsValid) return View(course);

        try
        {
            _context.Add(course);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
            TempData["Success"] = "Course created.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create course {Code}", course.Code);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
            return View(course);
        }
    }

    // UPDATE
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return BadRequest();
        var course = await _context.Courses.FindAsync(id);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Title,Description")] Course course)
    {
        if (id != course.Id) return BadRequest();

        Normalize(course);
        await CheckDuplicatesAsync(course);

        if (!ModelState.IsValid) return View(course);

        // Copy only the form fields, so the stored course keeps its quests
        var existing = await _context.Courses.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Code = course.Code;
        existing.Title = course.Title;
        existing.Description = course.Description;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated course {CourseId}", id);
            TempData["Success"] = "Course updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await _context.Courses.AnyAsync(c => c.Id == id)) return NotFound();
            _logger.LogError(ex, "Concurrency conflict updating course {CourseId}", id);
            ModelState.AddModelError(string.Empty, "This course was changed elsewhere. Reload and try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update course {CourseId}", id);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
        }
        return View(course);
    }

    // DELETE: the confirmation page shows which quests will be left without a course
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();
        var course = await FindWithQuestsAsync(id.Value);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course is null) return RedirectToAction(nameof(Index));

        try
        {
            // The database sets CourseId to null on the course's quests
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted course {CourseId} ({Code})", id, course.Code);
            TempData["Success"] = "Course deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete course {CourseId}", id);
            TempData["Error"] = "Could not delete the course.";
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<Course?> FindWithQuestsAsync(int id) =>
        _context.Courses.AsNoTracking()
            .Include(c => c.Quests.OrderBy(q => q.Title))
            .FirstOrDefaultAsync(c => c.Id == id);

    // Codes are stored trimmed and upper-case, so "web-101" and "WEB-101 " are the same code
    private static void Normalize(Course course)
    {
        course.Code = course.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        course.Title = course.Title?.Trim() ?? string.Empty;
        course.Description = string.IsNullOrWhiteSpace(course.Description) ? null : course.Description.Trim();
    }

    // Unique code is also enforced by the database; unique title avoids two identical-looking courses
    private async Task CheckDuplicatesAsync(Course course)
    {
        if (await _context.Courses.AnyAsync(c => c.Code == course.Code && c.Id != course.Id))
            ModelState.AddModelError(nameof(Course.Code), "A course with this code already exists.");

        if (await _context.Courses.AnyAsync(c => c.Title == course.Title && c.Id != course.Id))
            ModelState.AddModelError(nameof(Course.Title), "A course with this title already exists.");
    }
}
