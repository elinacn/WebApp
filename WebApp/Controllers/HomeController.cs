using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Extensions;
using WebApp.Models;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly SheetsDbContext _context;

    public HomeController(SheetsDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // The signed-in user's code points from completed quests (0 for visitors)
        var userId = User.GetUserId();
        var user = userId is null ? null : await _context.Users.FindAsync(userId);
        ViewData["CodePoints"] = user?.Points ?? 0;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Target of UseStatusCodePagesWithReExecute in Program.cs.
    // Not named StatusCode because that would hide ControllerBase.StatusCode(int).
    [Route("Home/StatusCode/{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        return View("StatusCode", code);
    }
}
