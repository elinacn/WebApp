using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SheetsApp.Data;
using SheetsApp.Models;

namespace SheetsApp.Controllers;

public class SheetsController : Controller
{
    private readonly SheetsDbContext _context;
    private readonly ILogger<SheetsController> _logger;

    public SheetsController(SheetsDbContext context, ILogger<SheetsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // READ: list
    public async Task<IActionResult> Index()
    {
        var sheets = await _context.Sheets.AsNoTracking().OrderBy(s => s.Title).ToListAsync();
        return View(sheets);
    }

    // READ: single
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return BadRequest();

        var sheet = await _context.Sheets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (sheet is null)
        {
            _logger.LogWarning("Sheet {SheetId} not found", id);
            return NotFound();
        }
        return View(sheet);
    }

    // CREATE
    public IActionResult Create() => View(new Sheet());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,RowCount,CreatedAt")] Sheet sheet)
    {
        // Custom server-side rule on top of the data annotations
        if (await _context.Sheets.AnyAsync(s => s.Title == sheet.Title))
            ModelState.AddModelError(nameof(Sheet.Title), "A sheet with this title already exists.");

        if (!ModelState.IsValid) return View(sheet);

        try
        {
            _context.Add(sheet);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created sheet {SheetId} ({Title})", sheet.Id, sheet.Title);
            TempData["Success"] = "Sheet created.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create sheet {Title}", sheet.Title);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
            return View(sheet);
        }
    }

    // UPDATE
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return BadRequest();
        var sheet = await _context.Sheets.FindAsync(id);
        return sheet is null ? NotFound() : View(sheet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,RowCount,CreatedAt")] Sheet sheet)
    {
        if (id != sheet.Id) return BadRequest();

        if (await _context.Sheets.AnyAsync(s => s.Title == sheet.Title && s.Id != id))
            ModelState.AddModelError(nameof(Sheet.Title), "A sheet with this title already exists.");

        if (!ModelState.IsValid) return View(sheet);

        try
        {
            _context.Update(sheet);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated sheet {SheetId}", id);
            TempData["Success"] = "Sheet updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await _context.Sheets.AnyAsync(s => s.Id == id)) return NotFound();
            _logger.LogError(ex, "Concurrency conflict updating sheet {SheetId}", id);
            ModelState.AddModelError(string.Empty, "This sheet was changed elsewhere. Reload and try again.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update sheet {SheetId}", id);
            ModelState.AddModelError(string.Empty, "Could not save changes. Please try again.");
        }
        return View(sheet);
    }

    // DELETE
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return BadRequest();
        var sheet = await _context.Sheets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return sheet is null ? NotFound() : View(sheet);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sheet = await _context.Sheets.FindAsync(id);
        if (sheet is null) return RedirectToAction(nameof(Index));

        try
        {
            _context.Sheets.Remove(sheet);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted sheet {SheetId}", id);
            TempData["Success"] = "Sheet deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete sheet {SheetId}", id);
            TempData["Error"] = "Could not delete the sheet.";
        }
        return RedirectToAction(nameof(Index));
    }
}