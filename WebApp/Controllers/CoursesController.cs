using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.DAL;

namespace WebApp.Controllers;

// Only admins can manage courses; browsing remains public.
public class CoursesController : Controller
{
    private readonly ICourseRepository _courses;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(ICourseRepository courses, ILogger<CoursesController> logger)
    {
        _courses = courses;
        _logger = logger;
    }

    // READ: list, with quests loaded for the counts
    public async Task<IActionResult> Index()
    {
        var courses = await _courses.GetAllAsync();
        return View(courses);
    }

    // READ: single, with its quests
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return BadRequest();

        var course = await _courses.GetWithQuestsAsync(id.Value);
        if (course is null)
        {
            _logger.LogWarning("Course {CourseId} not found", id);
            return NotFound();
        }
        return View(course);
    }

    // CREATE
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new Course());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([Bind("Code,Title,Description")] Course course)
    {
        Normalize(course);
        await CheckDuplicatesAsync(course);

        if (!ModelState.IsValid) return View(course);

        try
        {
            await _courses.AddAsync(course);
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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return BadRequest();
        var course = await _courses.GetByIdAsync(id.Value);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Title,Description")] Course course)
    {
        if (id != course.Id) return BadRequest();

        Normalize(course);
        await CheckDuplicatesAsync(course);

        if (!ModelState.IsValid) return View(course);

        // Copy only the form fields, so the stored course keeps its quests
        var existing = await _courses.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Code = course.Code;
        existing.Title = course.Title;
        existing.Description = course.Description;

        try
        {
            await _courses.UpdateAsync(existing);
            _logger.LogInformation("Updated course {CourseId}", id);
            TempData["Success"] = "Course updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (await _courses.GetWithQuestsAsync(id) is null) return NotFound();
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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();
        var course = await _courses.GetWithQuestsAsync(id.Value);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course is null) return RedirectToAction(nameof(Index));

        try
        {
            // The database sets CourseId to null on the course's quests
            await _courses.DeleteAsync(course);
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
        if (await _courses.CodeExistsAsync(course.Code, course.Id))
            ModelState.AddModelError(nameof(Course.Code), "A course with this code already exists.");

        if (await _courses.TitleExistsAsync(course.Title, course.Id))
            ModelState.AddModelError(nameof(Course.Title), "A course with this title already exists.");
    }
}
