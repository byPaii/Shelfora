using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;
using Shelfora.Services;

namespace Shelfora.Controllers;

public sealed class CirculationController(LibraryDbContext db, ILibraryService library, ILogger<CirculationController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, string? sort, int page = 1, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        const int size = 15;
        var query = db.BorrowTransactions.AsNoTracking().Include(x => x.Book).Include(x => x.Member).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.TransactionCode.Contains(q) || x.Member!.FullName.Contains(q) || x.Book!.Title.Contains(q));
        if (status == "active") query = query.Where(x => x.QuantityReturned < x.Quantity && x.DueDate >= DateTime.Today);
        if (status == "overdue") query = query.Where(x => x.QuantityReturned < x.Quantity && x.DueDate < DateTime.Today);
        if (status == "completed") query = query.Where(x => x.QuantityReturned == x.Quantity);
        query = sort == "due"
            ? query.OrderBy(x => x.DueDate)
            : query.OrderByDescending(x => x.BorrowDate);
        ViewBag.Page = page; ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync(ct) / (double)size);
        ViewBag.Status = status; ViewBag.Query = q; ViewBag.Sort = sort;
        return View(await query.Skip((Math.Max(page, 1) - 1) * size).Take(size).ToListAsync(ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await db.BorrowTransactions.AsNoTracking().Include(x => x.Book).Include(x => x.Member)
            .Include(x => x.Returns).SingleOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Borrow(CancellationToken ct)
    {
        await LoadOptions(ct);
        var setting = await db.LibrarySettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var input = new BorrowInput { BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(setting?.DefaultLoanDays ?? 14) };
        return View(input);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadOptions(ct); return View(input); }
        try
        {
            var transaction = await library.BorrowAsync(input, ct);
            TempData["Success"] = $"Borrow recorded: {transaction.TransactionCode}.";
            return RedirectToAction(nameof(Details), new { id = transaction.Id });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Borrow operation failed"); ModelState.AddModelError("", "The borrow could not be saved. Please try again."); }
        await LoadOptions(ct);
        return View(input);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id, ReturnInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Enter a return quantity greater than zero."; return RedirectToAction(nameof(Details), new { id }); }
        try
        {
            await library.ReturnAsync(id, input, ct);
            TempData["Success"] = "Return recorded.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException ex) { logger.LogError(ex, "Return operation failed for transaction {TransactionId}", id); TempData["Error"] = "The return could not be saved. Please try again."; }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Overdue(CancellationToken ct) => View(await library.OverdueAsync(cancellationToken: ct));

    private async Task LoadOptions(CancellationToken ct)
    {
        ViewBag.Members = await db.Members.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(ct);
        ViewBag.Books = await db.Books.AsNoTracking().Where(x => x.AvailableCopies > 0).OrderBy(x => x.Title).ToListAsync(ct);
    }
}
