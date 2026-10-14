using Microsoft.EntityFrameworkCore;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Services;

public sealed class LibraryService(LibraryDbContext db) : ILibraryService
{
    public async Task<DashboardViewModel> DashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var books = db.Books.AsNoTracking();
        var transactions = db.BorrowTransactions.AsNoTracking();
        var monthStart = new DateTime(today.Year, 1, 1);
        var monthEnd = monthStart.AddYears(1);
        var monthly = await transactions.Where(x => x.BorrowDate >= monthStart && x.BorrowDate < monthEnd)
            .GroupBy(x => x.BorrowDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);
        var months = new int[12];
        foreach (var item in monthly) months[item.Month - 1] = item.Count;
        var monthlyReturnData = await db.ReturnRecords.AsNoTracking().Where(x => x.ReturnedAtUtc >= monthStart && x.ReturnedAtUtc < monthEnd)
            .GroupBy(x => x.ReturnedAtUtc.Month)
            .Select(g => new { Month = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);
        var returnMonths = new int[12];
        foreach (var item in monthlyReturnData) returnMonths[item.Month - 1] = item.Count;
        var popular = await transactions.GroupBy(x => new { x.BookId, Title = x.Book!.Title })
            .Select(g => new PopularBookItem { BookId = g.Key.BookId, Title = g.Key.Title, BorrowCount = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.BorrowCount).Take(5).ToListAsync(cancellationToken);

        return new DashboardViewModel
        {
            LibraryName = await db.LibrarySettings.AsNoTracking().Select(x => x.LibraryName).FirstOrDefaultAsync(cancellationToken) ?? "Shelfora",
            Titles = await books.CountAsync(cancellationToken),
            Copies = await books.SumAsync(x => (int?)x.TotalCopies, cancellationToken) ?? 0,
            Available = await books.SumAsync(x => (int?)x.AvailableCopies, cancellationToken) ?? 0,
            Borrowed = await transactions.SumAsync(x => (int?)(x.Quantity - x.QuantityReturned), cancellationToken) ?? 0,
            Members = await db.Members.CountAsync(cancellationToken),
            ActiveMembers = await db.Members.CountAsync(x => x.IsActive, cancellationToken),
            Overdue = await transactions.CountAsync(x => x.DueDate < today && x.QuantityReturned < x.Quantity, cancellationToken),
            RecentTransactions = await transactions.Include(x => x.Book).Include(x => x.Member)
                .OrderByDescending(x => x.CreatedAtUtc).Take(8).ToListAsync(cancellationToken),
            RecentlyReturned = await db.ReturnRecords.AsNoTracking().Include(x => x.BorrowTransaction).ThenInclude(x => x!.Book)
                .Include(x => x.BorrowTransaction).ThenInclude(x => x!.Member).OrderByDescending(x => x.ReturnedAtUtc).Take(6).ToListAsync(cancellationToken),
            OverdueTransactions = await transactions.Where(x => x.DueDate < today && x.QuantityReturned < x.Quantity)
                .Include(x => x.Book).Include(x => x.Member).OrderBy(x => x.DueDate).Take(8).ToListAsync(cancellationToken),
            LowStock = await books.Include(x => x.Category).Where(x => x.AvailableCopies <= 1).OrderBy(x => x.AvailableCopies).Take(8).ToListAsync(cancellationToken),
            PopularBooks = popular,
            MonthlyBorrowings = months,
            MonthlyReturns = returnMonths
        };
    }

    public async Task<BorrowTransaction> BorrowAsync(BorrowInput input, CancellationToken cancellationToken = default)
    {
        if (input.Quantity < 1) throw new InvalidOperationException("Borrow quantity must be greater than zero.");
        if (input.DueDate.Date < input.BorrowDate.Date) throw new InvalidOperationException("Due date cannot be before borrow date.");
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var member = await db.Members.SingleOrDefaultAsync(x => x.Id == input.MemberId, cancellationToken)
            ?? throw new InvalidOperationException("Member was not found.");
        if (!member.IsActive) throw new InvalidOperationException("Inactive members cannot borrow books.");
        var book = await db.Books.SingleOrDefaultAsync(x => x.Id == input.BookId, cancellationToken)
            ?? throw new InvalidOperationException("Book was not found.");
        if (input.Quantity > book.AvailableCopies) throw new InvalidOperationException("Requested quantity exceeds available inventory.");

        book.AvailableCopies -= input.Quantity;
        book.UpdatedAtUtc = DateTime.UtcNow;
        var borrow = new BorrowTransaction
        {
            TransactionCode = $"BR-{Guid.NewGuid():N}"[..30],
            MemberId = member.Id,
            BookId = book.Id,
            Quantity = input.Quantity,
            BorrowDate = input.BorrowDate.Date,
            DueDate = input.DueDate.Date,
            Notes = input.Notes
        };
        db.BorrowTransactions.Add(borrow);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return borrow;
    }

    public async Task<ReturnRecord> ReturnAsync(int transactionId, ReturnInput input, CancellationToken cancellationToken = default)
    {
        if (input.Quantity < 1) throw new InvalidOperationException("Return quantity must be greater than zero.");
        if (!Guid.TryParseExact(input.SubmissionToken, "N", out _)) throw new InvalidOperationException("Invalid return submission token.");
        var previous = await db.ReturnRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SubmissionToken == input.SubmissionToken, cancellationToken);
        if (previous is not null)
        {
            if (previous.BorrowTransactionId != transactionId) throw new InvalidOperationException("Return submission token has already been used.");
            return previous;
        }
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var borrow = await db.BorrowTransactions.Include(x => x.Book)
            .SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Borrow transaction was not found.");
        var outstanding = borrow.Quantity - borrow.QuantityReturned;
        if (input.Quantity > outstanding) throw new InvalidOperationException("Return quantity cannot exceed the outstanding quantity.");

        var returnedAt = DateTime.UtcNow;
        var record = new ReturnRecord { BorrowTransactionId = borrow.Id, SubmissionToken = input.SubmissionToken, Quantity = input.Quantity, ReturnedAtUtc = returnedAt, Notes = input.Notes };
        borrow.QuantityReturned += input.Quantity;
        borrow.Status = borrow.QuantityReturned == borrow.Quantity ? BorrowStatus.Completed : BorrowStatus.PartiallyReturned;
        borrow.ReturnedAtUtc = borrow.Status == BorrowStatus.Completed ? returnedAt : null;
        borrow.UpdatedAtUtc = returnedAt;
        borrow.Book!.AvailableCopies += input.Quantity;
        borrow.Book.UpdatedAtUtc = returnedAt;
        db.ReturnRecords.Add(record);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return record;
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var duplicate = await db.ReturnRecords.AsNoTracking()
                .SingleOrDefaultAsync(x => x.SubmissionToken == input.SubmissionToken, cancellationToken);
            if (duplicate?.BorrowTransactionId == transactionId) return duplicate;
            throw;
        }
    }

    public async Task<List<OverdueItem>> OverdueAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var settings = await db.LibrarySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var rate = settings?.DailyFineRate ?? 0m;
        var transactions = await db.BorrowTransactions.AsNoTracking()
            .Where(x => x.DueDate < today && x.QuantityReturned < x.Quantity)
            .Where(x => !from.HasValue || x.DueDate >= from.Value.Date)
            .Where(x => !to.HasValue || x.DueDate < to.Value.Date.AddDays(1))
            .Include(x => x.Book).Include(x => x.Member).OrderBy(x => x.DueDate).ToListAsync(cancellationToken);
        return transactions.Select(x =>
        {
            var estimate = LoanRules.EstimateFine(x.DueDate, today, x.Outstanding, rate);
            return new OverdueItem { Transaction = x, DaysOverdue = estimate.DaysOverdue, EstimatedFine = estimate.EstimatedFine };
        }).ToList();
    }
}
