using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Controllers;

public sealed class SettingsController(LibraryDbContext db, ILogger<SettingsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await db.LibrarySettings.FirstOrDefaultAsync(ct) ?? new LibrarySetting());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LibrarySetting input, CancellationToken ct)
    {
        var setting = await db.LibrarySettings.FirstOrDefaultAsync(ct);
        if (!ModelState.IsValid) return View(input);
        if (setting is null) { setting = new LibrarySetting(); db.LibrarySettings.Add(setting); }
        setting.LibraryName = input.LibraryName.Trim(); setting.Address = input.Address;
        setting.ContactEmail = input.ContactEmail; setting.ContactNumber = input.ContactNumber;
        setting.DefaultLoanDays = input.DefaultLoanDays; setting.DailyFineRate = input.DailyFineRate;
        setting.Description = input.Description; setting.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Library settings saved."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not save library settings"); ModelState.AddModelError("", "Could not save settings. Please try again."); return View(input); }
    }
}
