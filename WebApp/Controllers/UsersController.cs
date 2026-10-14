using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers;

public class UsersController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(SheetsDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // READ: list, highest points first
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.AsNoTracking()
            .OrderByDescending(u => u.Points)
            .ThenBy(u => u.Username)
            .ToListAsync();
        return View(users);
    }

    // READ: single
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return BadRequest();

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }
        return View(user);
    }

    // CREATE
    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Points and QuizzesCompleted are left out on purpose: they come from quiz results, not this form
    public async Task<IActionResult> Create([Bind("Username,Name,Course,CreatedAt")] User user)
    {
        // Custom server-side rule on top of the data annotations
        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            ModelState.AddModelError(nameof(Models.User.Username), "This username is already taken.");

        if (!ModelState.IsValid) return View(user);

        try
        {
            _context.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created user {UserId} ({Username})", user.Id, user.Username);
            TempData["Success"] = "User created.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create user {Username}", user.Username);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
            return View(user);
        }
    }

    // UPDATE
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return BadRequest();
        var user = await _context.Users.FindAsync(id);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Name,Course,CreatedAt")] User user)
    {
        if (id != user.Id) return BadRequest();

        if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id))
            ModelState.AddModelError(nameof(Models.User.Username), "This username is already taken.");

        if (!ModelState.IsValid) return View(user);

        // Copy only the editable fields onto the stored user, so Points and
        // QuizzesCompleted keep their values instead of being reset to 0
        var existing = await _context.Users.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Username = user.Username;
        existing.Name = user.Name;
        existing.Course = user.Course;
        existing.CreatedAt = user.CreatedAt;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated user {UserId}", id);
            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == id)) return NotFound();
            _logger.LogError(ex, "Concurrency conflict updating user {UserId}", id);
            ModelState.AddModelError(string.Empty, "This user was changed elsewhere. Reload and try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", id);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
        }
        return View(user);
    }

    // DELETE
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return RedirectToAction(nameof(Index));

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted user {UserId}", id);
            TempData["Success"] = "User deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", id);
            TempData["Error"] = "Could not delete the user.";
        }
        return RedirectToAction(nameof(Index));
    }
}
