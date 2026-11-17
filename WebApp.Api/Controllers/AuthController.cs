using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Api.DAL;
using WebApp.Api.Dtos;
using WebApp.Api.Extensions;
using WebApp.Api.Models;

namespace WebApp.Api.Controllers;

// Sign in, sign out and "who am I". The login cookie carries the user's ID and role.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository users, IPasswordHasher<User> passwordHasher, ILogger<AuthController> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto login)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var name = login.Username.Trim().ToLower();
        var user = await _users.GetByUsernameAsync(name);

        // Same message whether the username or the password is wrong, so the
        // response doesn't reveal which usernames exist
        var result = user is null
            ? PasswordVerificationResult.Failed
            : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, login.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed sign-in for username {Username}", name);
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Wrong username or password." });
        }

        // Hashes made with older settings are upgraded on a successful sign-in
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, login.Password);
            await _users.UpdateAsync(user);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        _logger.LogInformation("User {UserId} ({Username}) signed in", user.Id, user.Username);
        return user.ToDto();
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {UserId} signed out", User.GetUserId());
        return NoContent();
    }

    // GET /api/auth/me – the React app calls this on page load to restore the session
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _users.GetByIdAsync(User.GetUserId()!.Value);
        return user is null ? Unauthorized() : user.ToDto();
    }
}
