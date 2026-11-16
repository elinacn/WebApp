using System.Net;
using WebApp.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.DAL;

namespace WebApp.Controllers;

// Sign-in with username and password. The login cookie carries the user's ID,
// which the rest of the app reads with User.GetUserId().
public class AccountController : Controller
{
    private readonly IUserRepository _users;
    private readonly IWebHostEnvironment _environment;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUserRepository users, IPasswordHasher<User> passwordHasher, ILogger<AccountController> logger, IWebHostEnvironment environment)
    {
        _users = users;
        _environment = environment;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["CanSetupAdmin"] = await CanSetupAdminAsync();
        return View();
    }

    // One-time bootstrap is available only from this computer in Development,
    // and disappears as soon as an admin exists. No default admin password is stored.
    private Task<bool> CanSetupAdminAsync()
    {
        if (!_environment.IsDevelopment() || Request.HttpContext.Connection.RemoteIpAddress is not { } ip
            || !IPAddress.IsLoopback(ip)) return Task.FromResult(false);
        return NoAdminExistsAsync();
    }

    private async Task<bool> NoAdminExistsAsync() => await _users.CountAdminsAsync() == 0;

    [HttpGet]
    public async Task<IActionResult> SetupAdmin()
    {
        if (!await CanSetupAdminAsync()) return NotFound();
        return View(new AdminSetupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAdmin(AdminSetupViewModel model)
    {
        if (!await CanSetupAdminAsync()) return NotFound();
        if (PasswordRules.Validate(model.Password) is string error)
            ModelState.AddModelError(nameof(model.Password), error);
        if (await _users.GetByUsernameAsync("admin") is not null)
            ModelState.AddModelError(string.Empty, "An account named admin already exists. Choose another bootstrap account with your team.");
        if (!ModelState.IsValid) return View(model);

        var user = new User { Username = "admin", Name = "Administrator", StudyProgram = "Administration", IsAdmin = true };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        try
        {
            await _users.AddAsync(user);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create the initial admin account");
            ModelState.AddModelError(string.Empty, "The admin account could not be created. Refresh and try again.");
            return View(model);
        }
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, AppRoles.Admin)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        _logger.LogInformation("Created the initial admin account {UserId}", user.Id);
        TempData["Success"] = "Admin account created. You are now signed in as admin.";
        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? username, string? password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Username"] = username;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Enter your username and password.");
            return View();
        }

        var name = username.Trim().ToLower();
        var user = await _users.GetByUsernameAsync(name);

        // Same message whether the username or the password is wrong, so the form
        // doesn't reveal which usernames exist
        var result = user?.PasswordHash is null
            ? PasswordVerificationResult.Failed
            : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed sign-in for username {Username}", name);
            ModelState.AddModelError(string.Empty, "Wrong username or password.");
            return View();
        }

        // Hashes made with older settings are upgraded on a successful sign-in
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _users.UpdateAsync(user);
        }

        // The cookie carries these claims; the ID is how every later request knows who this is
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.IsAdmin ? AppRoles.Admin : AppRoles.Student)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        _logger.LogInformation("User {UserId} ({Username}) signed in", user.Id, user.Username);
        TempData["Success"] = $"Signed in as {user.Name}.";

        // Only redirect within this site, so a crafted link can't send users elsewhere
        return LocalRedirect(returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Username} signed out", User.Identity?.Name);
        TempData["Success"] = "Signed out.";
        return RedirectToAction("Index", "Home");
    }
}
