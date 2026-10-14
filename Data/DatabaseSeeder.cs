using Microsoft.EntityFrameworkCore;
using Shelfora.Models;

namespace Shelfora.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(LibraryDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Books.AnyAsync(cancellationToken)) return;

        var categories = new[] { "Fiction", "Science", "History", "Technology", "Biography", "Children" }
            .Select(name => new Category { Name = name }).ToArray();
        db.Categories.AddRange(categories);
        var members = Enumerable.Range(1, 12).Select(i => new Member
        {
            MemberCode = $"MEM-{i:000}", FullName = $"Sample Member {i:00}",
            Email = $"member{i:00}@example.test", ContactNumber = $"+1-555-010-{i:00}",
            Address = $"{i} Sample Street", MembershipDate = DateTime.UtcNow.Date.AddDays(-i * 15), IsActive = true
        }).ToArray();
        db.Members.AddRange(members);

        var titles = new[]
        {
            ("The Quiet Observatory", "Mira Bell", "Fiction"), ("Practical Garden Ecology", "L. Rowan", "Science"),
            ("A Map of Old Harbors", "Noah Vale", "History"), ("Building Reliable Web Apps", "A. Mercer", "Technology"),
            ("The Inventor's Notebook", "Jamie North", "Biography"), ("Clouds for Curious Kids", "R. Finch", "Children"),
            ("The Glass Orchard", "Eden Hart", "Fiction"), ("Everyday Astronomy", "C. Solis", "Science"),
            ("Letters from the Archive", "T. Wren", "History"), ("Thoughtful Interface Design", "P. Rivers", "Technology"),
            ("A Life in Small Steps", "Morgan Lee", "Biography"), ("The Lantern Festival", "K. Moss", "Children"),
            ("The Last Paper Boat", "N. Reed", "Fiction"), ("Wildlife Near Home", "S. Quinn", "Science"),
            ("Crafting with Recycled Paper", "A. Finch", "Children"), ("Networks Made Clear", "D. Avery", "Technology"),
            ("Journeys of Discovery", "R. Lane", "History"), ("The Baker's Almanac", "J. Park", "Biography")
        };
        var books = titles.Select((item, i) =>
        {
            var copies = i % 3 == 0 ? 3 : 4;
            return new Book
            {
                BookCode = $"BK-{i + 1:0000}", Title = item.Item1, Author = item.Item2,
                Category = categories.Single(x => x.Name == item.Item3), Publisher = "Shelfora Sample Press",
                PublicationYear = 2010 + i % 15, ShelfLocation = $"S-{i / 4 + 1:00}",
                Description = "A fictional sample title included for demonstration purposes.",
                TotalCopies = copies, AvailableCopies = copies, CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            };
        }).ToArray();
        db.Books.AddRange(books);
        db.LibrarySettings.Add(new LibrarySetting { LibraryName = "Shelfora", DefaultLoanDays = 14, DailyFineRate = 0m });
        await db.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var active = new BorrowTransaction
        {
            TransactionCode = "BR-SEED-ACTIVE-001", MemberId = members[0].Id, BookId = books[0].Id, Quantity = 1,
            BorrowDate = DateTime.Today.AddDays(-3), DueDate = DateTime.Today.AddDays(11), Status = BorrowStatus.Active
        };
        var overdue = new BorrowTransaction
        {
            TransactionCode = "BR-SEED-OVERDUE-01", MemberId = members[1].Id, BookId = books[1].Id, Quantity = 2,
            BorrowDate = DateTime.Today.AddDays(-25), DueDate = DateTime.Today.AddDays(-11), Status = BorrowStatus.Active
        };
        var complete = new BorrowTransaction
        {
            TransactionCode = "BR-SEED-COMPLETE-1", MemberId = members[2].Id, BookId = books[2].Id, Quantity = 1,
            QuantityReturned = 1, BorrowDate = DateTime.Today.AddDays(-40), DueDate = DateTime.Today.AddDays(-26),
            ReturnedAtUtc = now.AddDays(-30), Status = BorrowStatus.Completed
        };
        books[0].AvailableCopies--; books[1].AvailableCopies -= 2;
        db.BorrowTransactions.AddRange(active, overdue, complete);
        await db.SaveChangesAsync(cancellationToken);
        db.ReturnRecords.Add(new ReturnRecord
        {
            BorrowTransactionId = complete.Id, Quantity = 1, ReturnedAtUtc = complete.ReturnedAtUtc!.Value,
            Notes = "Sample completed return."
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
