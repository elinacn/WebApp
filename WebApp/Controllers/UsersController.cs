using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.DAL;
using Microsoft.AspNetCore.Authorization;
using WebApp.Extensions;

namespace WebApp.Controllers;

public class UsersController : Controller
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<Models.User> _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository users, IPasswordHasher<Models.User> passwordHasher, ILogger<UsersController> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // READ: list, highest points first
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Index()
    {
        var users = await _users.GetAllAsync();
        return View(users);
    }

    // READ: single
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return BadRequest();

        var user = await _users.GetByIdAsync(id.Value);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }
        return View(user);
    }

    // CREATE
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Points and QuizzesCompleted are left out on purpose: they come from quiz results, not this form.
    // The password is a separate field so it is never bound onto the entity or shown again.
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([Bind("Username,Name,StudyProgram,CreatedAt,IsAdmin")] User user, string? password)
    {
        // Custom server-side rules on top of the data annotations
        if (await _users.UsernameExistsAsync(user.Username))
            ModelState.AddModelError(nameof(Models.User.Username), "This username is already taken.");

        if (PasswordRules.Validate(password) is string passwordError)
            ModelState.AddModelError(nameof(password), passwordError);

        if (!ModelState.IsValid) return View(user);

        try
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password!);
            await _users.AddAsync(user);
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
    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return BadRequest();
        }

        if (!User.IsInRole(AppRoles.Admin) && id.Value != User.GetUserId())
        {
            return Forbid();
        }

        var user = await _users.GetByIdAsync(id.Value);

        return user is null ? NotFound() : View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    // newPassword is optional: leave it empty to keep the current password
    public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Name,StudyProgram,CreatedAt,IsAdmin")] User user, string? newPassword, string? currentPassword)
    {
        if (!User.IsInRole(AppRoles.Admin) && id != User.GetUserId())
        {
            return Forbid();
        }
        if (id != user.Id) return BadRequest();

        if (await _users.UsernameExistsAsync(user.Username, id))
            ModelState.AddModelError(nameof(Models.User.Username), "This username is already taken.");

        if (!string.IsNullOrEmpty(newPassword) && PasswordRules.Validate(newPassword) is string passwordError)
            ModelState.AddModelError(nameof(newPassword), passwordError);

        var existing = await _users.GetByIdAsync(id);
        if (existing is null) return NotFound();

        if (User.IsInRole(AppRoles.Admin) && existing.IsAdmin && !user.IsAdmin
            && await _users.CountAdminsAsync() <= 1)
        {
            ModelState.AddModelError(nameof(Models.User.IsAdmin), "Keep at least one admin account.");
        }

        if (!string.IsNullOrEmpty(newPassword) && id == User.GetUserId())
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                ModelState.AddModelError(nameof(currentPassword), "Enter your current password to change it.");
            }
            else if (existing.PasswordHash is null ||
                _passwordHasher.VerifyHashedPassword(existing, existing.PasswordHash, currentPassword)
                    == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(currentPassword), "Your current password is incorrect.");
            }
        }

        if (!ModelState.IsValid) return View(user);

        // Copy only the editable fields onto the stored user, so Points and
        // QuizzesCompleted keep their values instead of being reset to 0
        // Ignore any role field submitted by a regular user.
        if (User.IsInRole(AppRoles.Admin)) existing.IsAdmin = user.IsAdmin;
        existing.Username = user.Username;
        existing.Name = user.Name;
        existing.StudyProgram = user.StudyProgram;
        existing.CreatedAt = user.CreatedAt;
        if (!string.IsNullOrEmpty(newPassword))
            existing.PasswordHash = _passwordHasher.HashPassword(existing, newPassword);

        try
        {
            await _users.UpdateAsync(existing);
            _logger.LogInformation("Updated user {UserId}{PasswordChanged}", id, string.IsNullOrEmpty(newPassword) ? "" : " (password changed)");
            TempData["Success"] = "User updated.";
            return User.IsInRole(AppRoles.Admin)
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Profile", "Profile");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await _users.ExistsAsync(id)) return NotFound();
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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();

        var user = await _users.GetByIdAsync(id.Value);
        return user is null ? NotFound() : View(user);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return RedirectToAction(nameof(Index));
        if (user.IsAdmin && await _users.CountAdminsAsync() <= 1)
        {
            TempData["Error"] = "You cannot delete the last admin account.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _users.DeleteAsync(user);
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
