using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers;

// Sign-in with username and password. The login cookie carries the user's ID,
// which the rest of the app reads with User.GetUserId().
public class AccountController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SheetsDbContext context, IPasswordHasher<User> passwordHasher, ILogger<AccountController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name);

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
            await _context.SaveChangesAsync();
        }

        // The cookie carries these claims; the ID is how every later request knows who this is
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name)
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
