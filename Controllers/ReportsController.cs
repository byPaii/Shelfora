using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;
using Shelfora.Services;

namespace Shelfora.Controllers;

public sealed class ReportsController(LibraryDbContext db, ILibraryService library) : Controller
{
    public async Task<IActionResult> Index(string type = "inventory", DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var model = await BuildReport(type, from, to, ct);
        return View(model);
    }

    public async Task<IActionResult> Export(string type = "inventory", DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var report = await BuildReport(type, from, to, ct);
        var rows = new List<string[]>();
        switch (type)
        {
            case "popular":
                rows.Add(["Book", "Borrowed copies"]);
                rows.AddRange(report.PopularBooks.Select(x => new[] { x.Title, x.BorrowCount.ToString() }));
                break;
            case "members":
                rows.Add(["Member", "Transactions", "Copies borrowed", "Outstanding"]);
                rows.AddRange(report.MemberActivity.Select(x => new[] { x.MemberName, x.Transactions.ToString(), x.CopiesBorrowed.ToString(), x.Outstanding.ToString() }));
                break;
            case "returns":
                rows.Add(["Transaction", "Member", "Book", "Return date", "Quantity returned", "Notes"]);
                rows.AddRange(report.ReturnEvents.Select(x => new[] { x.BorrowTransaction?.TransactionCode ?? "", x.BorrowTransaction?.Member?.FullName ?? "", x.BorrowTransaction?.Book?.Title ?? "", x.ReturnedAtUtc.ToString("yyyy-MM-dd"), x.Quantity.ToString(), x.Notes ?? "" }));
                break;
            case "borrowing":
                rows.Add(["Transaction", "Member", "Book", "Borrow date", "Due date", "Quantity", "Returned", "Status"]);
                rows.AddRange(report.Transactions.Select(x => new[] { x.TransactionCode, x.Member?.FullName ?? "", x.Book?.Title ?? "", x.BorrowDate.ToString("yyyy-MM-dd"), x.DueDate.ToString("yyyy-MM-dd"), x.Quantity.ToString(), x.QuantityReturned.ToString(), x.Status.ToString() }));
                break;
            case "overdue":
                rows.Add(["Transaction", "Member", "Book", "Due date", "Outstanding", "Days overdue", "Estimated fine"]);
                rows.AddRange(report.Overdue.Select(x => new[] { x.Transaction.TransactionCode, x.Transaction.Member?.FullName ?? "", x.Transaction.Book?.Title ?? "", x.Transaction.DueDate.ToString("yyyy-MM-dd"), x.Transaction.Outstanding.ToString(), x.DaysOverdue.ToString(), x.EstimatedFine.ToString("0.00") }));
                break;
            default:
                rows.Add(["Book ID", "Title", "Author", "Category", "Total copies", "Available copies"]);
                rows.AddRange(report.Books.Select(x => new[] { x.BookCode, x.Title, x.Author, x.Category?.Name ?? "", x.TotalCopies.ToString(), x.AvailableCopies.ToString() }));
                break;
        }
        var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(CsvExportFormatter.EscapeField))));
        return File(CsvExportFormatter.Encode(csv), "text/csv; charset=utf-8", $"shelfora-{type}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task<ReportsViewModel> BuildReport(string type, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var model = new ReportsViewModel { Type = type, From = from, To = to };
        if (type == "overdue") model = new ReportsViewModel { Type = type, From = from, To = to, Overdue = await library.OverdueAsync(from, to, ct) };
        else if (type == "popular")
        {
            var query = db.BorrowTransactions.AsNoTracking().AsQueryable();
            if (from.HasValue) query = query.Where(x => x.BorrowDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.BorrowDate < to.Value.Date.AddDays(1));
            var popular = await query.GroupBy(x => new { x.BookId, Title = x.Book!.Title })
                .Select(g => new PopularBookItem { BookId = g.Key.BookId, Title = g.Key.Title, BorrowCount = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.BorrowCount).Take(100).ToListAsync(ct);
            model = new ReportsViewModel { Type = type, From = from, To = to, PopularBooks = popular };
        }
        else if (type == "members")
        {
            var query = db.BorrowTransactions.AsNoTracking().AsQueryable();
            if (from.HasValue) query = query.Where(x => x.BorrowDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.BorrowDate < to.Value.Date.AddDays(1));
            var activity = await query.GroupBy(x => new { x.MemberId, Name = x.Member!.FullName })
                .Select(g => new MemberActivityItem
                {
                    MemberId = g.Key.MemberId, MemberName = g.Key.Name, Transactions = g.Count(),
                    CopiesBorrowed = g.Sum(x => x.Quantity), Outstanding = g.Sum(x => x.Quantity - x.QuantityReturned)
                }).OrderByDescending(x => x.CopiesBorrowed).ToListAsync(ct);
            model = new ReportsViewModel { Type = type, From = from, To = to, MemberActivity = activity };
        }
        else if (type == "returns")
        {
            var query = db.ReturnRecords.AsNoTracking().Include(x => x.BorrowTransaction).ThenInclude(x => x!.Book)
                .Include(x => x.BorrowTransaction).ThenInclude(x => x!.Member).AsQueryable();
            if (from.HasValue) query = query.Where(x => x.ReturnedAtUtc >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.ReturnedAtUtc < to.Value.Date.AddDays(1));
            model = new ReportsViewModel { Type = type, From = from, To = to, ReturnEvents = await query.OrderByDescending(x => x.ReturnedAtUtc).ToListAsync(ct) };
        }
        else if (type == "borrowing")
        {
            var query = db.BorrowTransactions.AsNoTracking().Include(x => x.Book).Include(x => x.Member).AsQueryable();
            if (from.HasValue) query = query.Where(x => x.BorrowDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.BorrowDate < to.Value.Date.AddDays(1));
            model = new ReportsViewModel { Type = type, From = from, To = to, Transactions = await query.OrderByDescending(x => x.BorrowDate).ToListAsync(ct) };
        }
        else
        {
            var query = db.Books.AsNoTracking().Include(x => x.Category).AsQueryable();
            if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.Date.AddDays(1));
            model = new ReportsViewModel { Type = "inventory", From = from, To = to, Books = await query.OrderBy(x => x.Title).ToListAsync(ct) };
        }
        return model;
    }

}
