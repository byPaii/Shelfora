using Microsoft.EntityFrameworkCore;
using Shelfora.Models;

namespace Shelfora.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BorrowTransaction> BorrowTransactions => Set<BorrowTransaction>();
    public DbSet<ReturnRecord> ReturnRecords => Set<ReturnRecord>();
    public DbSet<LibrarySetting> LibrarySettings => Set<LibrarySetting>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Book>().ToTable(t => t.HasCheckConstraint("CK_Books_CopyCounts", "[TotalCopies] >= 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]"));
        model.Entity<Book>().HasIndex(x => x.BookCode).IsUnique();
        model.Entity<Book>().HasIndex(x => x.Isbn).IsUnique().HasFilter("[Isbn] IS NOT NULL");
        model.Entity<Book>().HasIndex(x => new { x.Title, x.Author });
        model.Entity<Book>().HasOne(x => x.Category).WithMany(x => x.Books).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Category>().Property(x => x.Name).UseCollation("Latin1_General_100_CI_AS");
        model.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        model.Entity<Member>().HasIndex(x => x.MemberCode).IsUnique();
        model.Entity<Member>().HasIndex(x => x.FullName);
        model.Entity<BorrowTransaction>().HasIndex(x => x.TransactionCode).IsUnique();
        model.Entity<BorrowTransaction>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_BorrowTransactions_Quantities", "[Quantity] > 0 AND [QuantityReturned] >= 0 AND [QuantityReturned] <= [Quantity]");
            t.HasCheckConstraint("CK_BorrowTransactions_Dates", "[DueDate] >= [BorrowDate]");
        });
        model.Entity<BorrowTransaction>().HasIndex(x => new { x.DueDate, x.Status });
        model.Entity<BorrowTransaction>().HasOne(x => x.Book).WithMany(x => x.Transactions).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<BorrowTransaction>().HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ReturnRecord>().HasOne(x => x.BorrowTransaction).WithMany(x => x.Returns).HasForeignKey(x => x.BorrowTransactionId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<ReturnRecord>().HasIndex(x => x.SubmissionToken).IsUnique();
        model.Entity<ReturnRecord>().ToTable(t => t.HasCheckConstraint("CK_ReturnRecords_Quantity", "[Quantity] > 0"));
        model.Entity<LibrarySetting>().Property(x => x.DailyFineRate).HasPrecision(10, 2);
    }
}
