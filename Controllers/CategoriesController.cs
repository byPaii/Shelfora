using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Controllers;

public sealed class CategoriesController(LibraryDbContext db, ILogger<CategoriesController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await db.Categories.AsNoTracking().Include(x => x.Books).OrderBy(x => x.Name).ToListAsync(ct));
    public IActionResult Create() => View(new Category());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category, CancellationToken ct)
    {
        await ValidateName(category, null, ct);
        if (!ModelState.IsValid) return View(category);
        category.Id = 0;
        category.Books.Clear();
        db.Categories.Add(category);
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Category created."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not create category"); ModelState.AddModelError(nameof(category.Name), "A category with this name already exists."); return View(category); }
    }
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category input, CancellationToken ct)
    {
        if (id != input.Id) return BadRequest();
        var item = await db.Categories.FindAsync([id], ct);
        if (item is null) return NotFound();
        await ValidateName(input, id, ct);
        if (!ModelState.IsValid) return View(input);
        item.Name = input.Name.Trim(); item.Description = input.Description;
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Category updated."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not update category {CategoryId}", id); ModelState.AddModelError(nameof(input.Name), "A category with this name already exists."); return View(input); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Categories.Include(x => x.Books).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Books.Count != 0) TempData["Error"] = "Category cannot be deleted while books are assigned to it.";
        else { db.Categories.Remove(item); await db.SaveChangesAsync(ct); TempData["Success"] = "Category deleted."; }
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidateName(Category category, int? id, CancellationToken ct)
    {
        category.Name = category.Name?.Trim() ?? "";
        if (await db.Categories.AnyAsync(x => x.Name.ToLower() == category.Name.ToLower() && x.Id != id, ct))
            ModelState.AddModelError(nameof(category.Name), "Category names must be unique (case-insensitive).");
    }
}
