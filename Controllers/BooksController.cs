using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Controllers;

public sealed class BooksController(LibraryDbContext db, ILogger<BooksController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, int? categoryId, string? availability, int? year, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        const int pageSize = 12;
        var query = db.Books.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q) || x.Author.Contains(q) || x.BookCode.Contains(q) || (x.Isbn != null && x.Isbn.Contains(q)));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (availability == "available") query = query.Where(x => x.AvailableCopies > 0);
        if (availability == "unavailable") query = query.Where(x => x.AvailableCopies == 0);
        if (year.HasValue) query = query.Where(x => x.PublicationYear == year);
        query = sort switch
        {
            "date" => query.OrderByDescending(x => x.CreatedAtUtc),
            "available" => query.OrderByDescending(x => x.AvailableCopies).ThenBy(x => x.Title),
            _ => query.OrderBy(x => x.Title)
        };
        ViewBag.Categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Years = await db.Books.AsNoTracking().Where(x => x.PublicationYear.HasValue).Select(x => x.PublicationYear!.Value).Distinct().OrderByDescending(x => x).ToListAsync(cancellationToken);
        ViewBag.CategoryId = categoryId;
        ViewBag.Availability = availability;
        ViewBag.Year = year;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync(cancellationToken) / (double)pageSize);
        ViewBag.Query = q;
        return View(await query.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().Include(x => x.Category)
            .Include(x => x.Transactions).ThenInclude(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return book is null ? NotFound() : View(book);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadCategories(cancellationToken);
        return View(new Book { TotalCopies = 1, AvailableCopies = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book, CancellationToken cancellationToken)
    {
        await ValidateBook(book, null, cancellationToken);
        if (!ModelState.IsValid) { await LoadCategories(cancellationToken); return View(book); }
        book.Id = 0;
        book.Category = null;
        book.Transactions.Clear();
        book.AvailableCopies = book.TotalCopies;
        book.CreatedAtUtc = book.UpdatedAtUtc = DateTime.UtcNow;
        db.Books.Add(book);
        try { await db.SaveChangesAsync(cancellationToken); TempData["Success"] = "Book added."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not create book"); ModelState.AddModelError("", "Could not save the book. Check for a duplicate book code or ISBN."); }
        await LoadCategories(cancellationToken);
        return View(book);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var book = await db.Books.FindAsync([id], cancellationToken);
        if (book is null) return NotFound();
        await LoadCategories(cancellationToken);
        return View(book);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Book input, CancellationToken cancellationToken)
    {
        if (id != input.Id) return BadRequest();
        var book = await db.Books.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (book is null) return NotFound();
        await ValidateBook(input, id, cancellationToken);
        var borrowed = book.TotalCopies - book.AvailableCopies;
        if (input.TotalCopies < borrowed) ModelState.AddModelError(nameof(input.TotalCopies), $"Total copies cannot be less than {borrowed} currently borrowed copies.");
        if (!ModelState.IsValid) { await LoadCategories(cancellationToken); return View(input); }
        book.BookCode = input.BookCode; book.Isbn = input.Isbn; book.Title = input.Title; book.Author = input.Author;
        book.CategoryId = input.CategoryId; book.Publisher = input.Publisher; book.PublicationYear = input.PublicationYear;
        book.Description = input.Description; book.CoverImageUrl = input.CoverImageUrl; book.ShelfLocation = input.ShelfLocation;
        book.TotalCopies = input.TotalCopies; book.AvailableCopies = input.TotalCopies - borrowed; book.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); TempData["Success"] = "Book updated."; return RedirectToAction(nameof(Details), new { id }); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Could not update book {BookId}", id); ModelState.AddModelError("", "Could not save changes. Check for a duplicate identifier or ISBN."); }
        await LoadCategories(cancellationToken);
        return View(input);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var book = await db.Books.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (book is null) return NotFound();
        if (book.TotalCopies != book.AvailableCopies || await db.BorrowTransactions.AnyAsync(x => x.BookId == id, cancellationToken))
            TempData["Error"] = "This book has borrowing history or copies currently borrowed and cannot be deleted.";
        else { db.Books.Remove(book); await db.SaveChangesAsync(cancellationToken); TempData["Success"] = "Book deleted."; }
        return RedirectToAction(nameof(Index));
    }

    private Task LoadCategories(CancellationToken ct) => SetCategoriesAsync(ct);
    private async Task SetCategoriesAsync(CancellationToken ct) => ViewBag.Categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    private async Task ValidateBook(Book book, int? id, CancellationToken ct)
    {
        var duplicateCode = await db.Books.AnyAsync(x => x.BookCode == book.BookCode && x.Id != id, ct);
        if (duplicateCode) ModelState.AddModelError(nameof(book.BookCode), "Book identifier is already in use.");
        if (!string.IsNullOrWhiteSpace(book.Isbn) && await db.Books.AnyAsync(x => x.Isbn == book.Isbn && x.Id != id, ct))
            ModelState.AddModelError(nameof(book.Isbn), "ISBN is already in use.");
        if (!await db.Categories.AnyAsync(x => x.Id == book.CategoryId, ct)) ModelState.AddModelError(nameof(book.CategoryId), "Choose a valid category.");
        if (book.AvailableCopies > book.TotalCopies) ModelState.AddModelError(nameof(book.AvailableCopies), "Available copies cannot exceed total copies.");
    }
}
