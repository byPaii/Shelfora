using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shelfora.Data;
using Shelfora.Models;

namespace Shelfora.Migrations;

[DbContext(typeof(LibraryDbContext))]
public sealed class LibraryDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity<Book>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>(x => x.BookCode).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string?>(x => x.Isbn).HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<string>(x => x.Title).IsRequired().HasMaxLength(180).HasColumnType("nvarchar(180)");
            b.Property<string>(x => x.Author).IsRequired().HasMaxLength(180).HasColumnType("nvarchar(180)");
            b.Property<int>(x => x.CategoryId).HasColumnType("int");
            b.Property<string?>(x => x.Publisher).HasMaxLength(120).HasColumnType("nvarchar(120)");
            b.Property<int?>(x => x.PublicationYear).HasColumnType("int");
            b.Property<string?>(x => x.Description).HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<string?>(x => x.CoverImageUrl).HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string?>(x => x.ShelfLocation).HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<int>(x => x.TotalCopies).HasColumnType("int");
            b.Property<int>(x => x.AvailableCopies).HasColumnType("int");
            b.Property<DateTime>(x => x.CreatedAtUtc).HasColumnType("datetime2");
            b.Property<DateTime>(x => x.UpdatedAtUtc).HasColumnType("datetime2");
            b.Property<byte[]>(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.BookCode).IsUnique();
            b.HasIndex(x => x.Isbn).IsUnique().HasFilter("[Isbn] IS NOT NULL");
            b.HasIndex(x => new { x.Title, x.Author });
            b.ToTable("Books", t => t.HasCheckConstraint("CK_Books_CopyCounts", "[TotalCopies] >= 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]"));
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>(x => x.Name).IsRequired().HasMaxLength(80).UseCollation("Latin1_General_100_CI_AS").HasColumnType("nvarchar(80)");
            b.Property<string?>(x => x.Description).HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTime>(x => x.CreatedAtUtc).HasColumnType("datetime2");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Name).IsUnique();
            b.ToTable("Categories");
        });

        modelBuilder.Entity<Member>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>(x => x.MemberCode).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<string>(x => x.FullName).IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)");
            b.Property<string?>(x => x.Email).HasMaxLength(254).HasColumnType("nvarchar(254)");
            b.Property<string?>(x => x.ContactNumber).HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<string?>(x => x.Address).HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTime>(x => x.MembershipDate).HasColumnType("datetime2");
            b.Property<bool>(x => x.IsActive).HasColumnType("bit");
            b.Property<string?>(x => x.Notes).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime>(x => x.CreatedAtUtc).HasColumnType("datetime2");
            b.Property<DateTime>(x => x.UpdatedAtUtc).HasColumnType("datetime2");
            b.Property<byte[]>(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.MemberCode).IsUnique();
            b.HasIndex(x => x.FullName);
            b.ToTable("Members");
        });

        modelBuilder.Entity<BorrowTransaction>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>(x => x.TransactionCode).IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<int>(x => x.MemberId).HasColumnType("int");
            b.Property<int>(x => x.BookId).HasColumnType("int");
            b.Property<int>(x => x.Quantity).HasColumnType("int");
            b.Property<int>(x => x.QuantityReturned).HasColumnType("int");
            b.Property<DateTime>(x => x.BorrowDate).HasColumnType("datetime2");
            b.Property<DateTime>(x => x.DueDate).HasColumnType("datetime2");
            b.Property<DateTime?>(x => x.ReturnedAtUtc).HasColumnType("datetime2");
            b.Property<BorrowStatus>(x => x.Status).HasColumnType("int");
            b.Property<string?>(x => x.Notes).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime>(x => x.CreatedAtUtc).HasColumnType("datetime2");
            b.Property<DateTime>(x => x.UpdatedAtUtc).HasColumnType("datetime2");
            b.Property<byte[]>(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("rowversion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TransactionCode).IsUnique();
            b.HasIndex(x => new { x.DueDate, x.Status });
            b.HasIndex(x => x.BookId);
            b.HasIndex(x => x.MemberId);
            b.ToTable("BorrowTransactions", t =>
            {
                t.HasCheckConstraint("CK_BorrowTransactions_Quantities", "[Quantity] > 0 AND [QuantityReturned] >= 0 AND [QuantityReturned] <= [Quantity]");
                t.HasCheckConstraint("CK_BorrowTransactions_Dates", "[DueDate] >= [BorrowDate]");
            });
        });

        modelBuilder.Entity<ReturnRecord>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<int>(x => x.BorrowTransactionId).HasColumnType("int");
            b.Property<string>(x => x.SubmissionToken).IsRequired().HasMaxLength(36).HasColumnType("nvarchar(36)");
            b.Property<int>(x => x.Quantity).HasColumnType("int");
            b.Property<DateTime>(x => x.ReturnedAtUtc).HasColumnType("datetime2");
            b.Property<string?>(x => x.Notes).HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.BorrowTransactionId);
            b.HasIndex(x => x.SubmissionToken).IsUnique();
            b.ToTable("ReturnRecords", t => t.HasCheckConstraint("CK_ReturnRecords_Quantity", "[Quantity] > 0"));
        });

        modelBuilder.Entity<LibrarySetting>(b =>
        {
            b.Property<int>(x => x.Id).ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<string>(x => x.LibraryName).IsRequired().HasMaxLength(160).HasColumnType("nvarchar(160)");
            b.Property<string?>(x => x.Address).HasMaxLength(300).HasColumnType("nvarchar(300)");
            b.Property<string?>(x => x.ContactEmail).HasMaxLength(254).HasColumnType("nvarchar(254)");
            b.Property<string?>(x => x.ContactNumber).HasMaxLength(40).HasColumnType("nvarchar(40)");
            b.Property<int>(x => x.DefaultLoanDays).HasColumnType("int");
            b.Property<decimal>(x => x.DailyFineRate).HasPrecision(10, 2).HasColumnType("decimal(10,2)");
            b.Property<string?>(x => x.Description).HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            b.Property<DateTime>(x => x.UpdatedAtUtc).HasColumnType("datetime2");
            b.HasKey(x => x.Id);
            b.ToTable("LibrarySettings");
        });

        modelBuilder.Entity<Book>().HasOne(x => x.Category).WithMany(x => x.Books).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BorrowTransaction>().HasOne(x => x.Book).WithMany(x => x.Transactions).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BorrowTransaction>().HasOne(x => x.Member).WithMany(x => x.Transactions).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReturnRecord>().HasOne(x => x.BorrowTransaction).WithMany(x => x.Returns).HasForeignKey(x => x.BorrowTransactionId).OnDelete(DeleteBehavior.Cascade);
    }
}
