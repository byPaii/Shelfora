using System.ComponentModel.DataAnnotations;

namespace Shelfora.Models;

public sealed class Book
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string BookCode { get; set; } = "";
    [StringLength(20)] public string? Isbn { get; set; }
    [Required, StringLength(180)] public string Title { get; set; } = "";
    [Required, StringLength(180)] public string Author { get; set; } = "";
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    [StringLength(120)] public string? Publisher { get; set; }
    [Range(1000, 2200)] public int? PublicationYear { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Url, StringLength(500)] public string? CoverImageUrl { get; set; }
    [StringLength(100)] public string? ShelfLocation { get; set; }
    [Range(0, int.MaxValue)] public int TotalCopies { get; set; }
    [Range(0, int.MaxValue)] public int AvailableCopies { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public ICollection<BorrowTransaction> Transactions { get; set; } = new List<BorrowTransaction>();
}

public sealed class Category
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public sealed class Member
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string MemberCode { get; set; } = "";
    [Required, StringLength(160)] public string FullName { get; set; } = "";
    [EmailAddress, StringLength(254)] public string? Email { get; set; }
    [Phone, StringLength(40)] public string? ContactNumber { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    public DateTime MembershipDate { get; set; } = DateTime.UtcNow.Date;
    public bool IsActive { get; set; } = true;
    [StringLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public ICollection<BorrowTransaction> Transactions { get; set; } = new List<BorrowTransaction>();
}

public enum BorrowStatus { Active, PartiallyReturned, Completed }

public sealed class BorrowTransaction
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string TransactionCode { get; set; } = "";
    public int MemberId { get; set; }
    public Member? Member { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    [Range(1, 1000)] public int Quantity { get; set; }
    public int QuantityReturned { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public BorrowStatus Status { get; set; } = BorrowStatus.Active;
    [StringLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public ICollection<ReturnRecord> Returns { get; set; } = new List<ReturnRecord>();
    public int Outstanding => Quantity - QuantityReturned;
    public bool IsOverdue(DateTime today) => Outstanding > 0 && DueDate.Date < today.Date;
}

public sealed class ReturnRecord
{
    public int Id { get; set; }
    public int BorrowTransactionId { get; set; }
    public BorrowTransaction? BorrowTransaction { get; set; }
    [Required, StringLength(36)] public string SubmissionToken { get; set; } = Guid.NewGuid().ToString("N");
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    public DateTime ReturnedAtUtc { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class LibrarySetting
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string LibraryName { get; set; } = "Shelfora";
    [StringLength(300)] public string? Address { get; set; }
    [EmailAddress, StringLength(254)] public string? ContactEmail { get; set; }
    [Phone, StringLength(40)] public string? ContactNumber { get; set; }
    [Range(1, 365)] public int DefaultLoanDays { get; set; } = 14;
    [Range(typeof(decimal), "0", "100000")] public decimal DailyFineRate { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
