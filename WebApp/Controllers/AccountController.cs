using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers;

// MVP sign-in: users have no passwords yet, so you pick who to sign in as.
// The login cookie and the rest of the app work the same once passwords are added.
public class AccountController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SheetsDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync();
        ViewData["Users"] = users
            .Select(u => new SelectListItem($"{u.Name} ({u.Username})", u.Id.ToString()))
            .ToList();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(int userId, string? returnUrl = null)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            _logger.LogWarning("Sign-in attempted for unknown user {UserId}", userId);
            TempData["Error"] = "Choose a user from the list.";
            return RedirectToAction(nameof(Login), new { returnUrl });
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
