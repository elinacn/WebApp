using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Data;
using WebApp.Api.Dtos;
using WebApp.Api.Models;

namespace WebApp.Api.Controllers;

// Anyone can browse courses; only admins can create, edit or delete them
[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(AppDbContext db, ILogger<CoursesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/courses?search=web – matches code or title
    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetAll(string? search)
    {
        var query = _db.Courses.AsNoTracking().Include(c => c.Quests).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(c => EF.Functions.Like(c.Code, $"%{text}%") || EF.Functions.Like(c.Title, $"%{text}%"));
        }

        var courses = await query.OrderBy(c => c.Title).ToListAsync();
        return courses.Select(c => c.ToDto()).ToList();
    }

    // GET /api/courses/2 – with its quests
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDetailDto>> Get(int id)
    {
        var course = await _db.Courses.AsNoTracking()
            .Include(c => c.Quests.OrderBy(q => q.Title))
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
        {
            _logger.LogWarning("Course {CourseId} not found", id);
            return NotFound();
        }

        return new CourseDetailDto(course.Id, course.Code, course.Title, course.Description,
            course.Quests.Select(q => new CourseQuestDto(q.Id, q.Title, q.Difficulty)).ToList());
    }

    // POST /api/courses
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CourseFormDto form)
    {
        Normalize(form);
        await CheckDuplicatesAsync(form, courseId: 0);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var course = new Course { Code = form.Code, Title = form.Title, Description = form.Description };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
        return CreatedAtAction(nameof(Get), new { id = course.Id }, course.ToDto());
    }

    // PUT /api/courses/2
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CourseFormDto form)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        Normalize(form);
        await CheckDuplicatesAsync(form, id);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        course.Code = form.Code;
        course.Title = form.Title;
        course.Description = form.Description;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated course {CourseId} ({Code})", id, course.Code);
        return NoContent();
    }

    // DELETE /api/courses/2 – its quests are kept, just without a course
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted course {CourseId} ({Code})", id, course.Code);
        return NoContent();
    }

    // Codes are stored trimmed and upper-case, so "web-101" and "WEB-101 " are the same code
    private static void Normalize(CourseFormDto form)
    {
        form.Code = (form.Code ?? string.Empty).Trim().ToUpperInvariant();
        form.Title = (form.Title ?? string.Empty).Trim();
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
    }

    private async Task CheckDuplicatesAsync(CourseFormDto form, int courseId)
    {
        if (await _db.Courses.AnyAsync(c => c.Code == form.Code && c.Id != courseId))
            ModelState.AddModelError(nameof(CourseFormDto.Code), "A course with this code already exists.");

        if (await _db.Courses.AnyAsync(c => c.Title == form.Title && c.Id != courseId))
            ModelState.AddModelError(nameof(CourseFormDto.Title), "A course with this title already exists.");
    }
}
