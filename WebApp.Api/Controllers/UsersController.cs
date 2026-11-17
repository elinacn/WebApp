using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Api.DAL;
using WebApp.Api.Dtos;
using WebApp.Api.Extensions;
using WebApp.Api.Models;

namespace WebApp.Api.Controllers;

// User management, for admins only
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository users, IPasswordHasher<User> passwordHasher, ILogger<UsersController> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // GET /api/users?search=an&program=Web programming&role=Student
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(string? search, string? program, string? role)
    {
        var users = await _users.SearchAsync(search, program, role);
        return users.Select(u => u.ToDto()).ToList();
    }

    // GET /api/users/programs – the distinct study programs, for the filter dropdown
    [HttpGet("programs")]
    public async Task<ActionResult<List<string>>> GetPrograms() =>
        await _users.GetProgramsAsync();

    // GET /api/users/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }
        return user.ToDto();
    }

    // POST /api/users
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateDto form)
    {
        Normalize(form);
        await ValidateAsync(form, userId: 0);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Points and quizzes completed start at 0; they only come from quest results
        var user = new User { Username = form.Username, Name = form.Name, StudyProgram = form.StudyProgram, Role = form.Role };
        user.PasswordHash = _passwordHasher.HashPassword(user, form.Password);
        await _users.AddAsync(user);

        _logger.LogInformation("User {AdminId} created user {UserId} ({Username}, {Role})", User.GetUserId(), user.Id, user.Username, user.Role);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user.ToDto());
    }

    // PUT /api/users/3 – NewPassword is optional; leave it empty to keep the current one
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UserUpdateDto form)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();

        Normalize(form);
        await ValidateAsync(form, id);

        // An admin can't remove their own admin rights, so there is always at least one admin
        if (id == User.GetUserId() && form.Role != Roles.Admin)
            ModelState.AddModelError(nameof(UserFormDto.Role), "You can't remove your own admin role.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        user.Username = form.Username;
        user.Name = form.Name;
        user.StudyProgram = form.StudyProgram;
        user.Role = form.Role;
        if (!string.IsNullOrEmpty(form.NewPassword))
            user.PasswordHash = _passwordHasher.HashPassword(user, form.NewPassword);
        await _users.UpdateAsync(user);

        _logger.LogInformation("User {AdminId} updated user {UserId}{PasswordChanged}",
            User.GetUserId(), id, string.IsNullOrEmpty(form.NewPassword) ? "" : " (password changed)");
        return NoContent();
    }

    // DELETE /api/users/3 – their completions and streak days are deleted too (cascade)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == User.GetUserId())
            return Conflict(new ProblemDetails { Status = 409, Title = "You can't delete your own account while signed in." });

        var user = await _users.GetByIdAsync(id);
        if (user is null) return NotFound();

        await _users.DeleteAsync(user);

        _logger.LogInformation("User {AdminId} deleted user {UserId} ({Username})", User.GetUserId(), id, user.Username);
        return NoContent();
    }

    private static void Normalize(UserFormDto form)
    {
        form.Username = (form.Username ?? string.Empty).Trim();
        form.Name = (form.Name ?? string.Empty).Trim();
        form.StudyProgram = (form.StudyProgram ?? string.Empty).Trim();
    }

    private async Task ValidateAsync(UserFormDto form, int userId)
    {
        if (!Roles.All.Contains(form.Role))
            ModelState.AddModelError(nameof(UserFormDto.Role), "Choose a valid role.");

        // Usernames are unique ignoring case, since sign-in ignores case too
        var name = form.Username.ToLower();
        if (await _users.UsernameExistsAsync(name, userId))
            ModelState.AddModelError(nameof(UserFormDto.Username), "This username is already taken.");
    }
}
