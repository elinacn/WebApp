using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

public class ProfileController : Controller
{
    private readonly DailyStreakService _streakService;

    public ProfileController(DailyStreakService streakService)
    {
        _streakService = streakService;
    }

    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        ViewData["Streak"] = await _streakService.GetStreakAsync(userId);

        return View();
    }
}