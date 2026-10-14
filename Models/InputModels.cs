using System.ComponentModel.DataAnnotations;

namespace Shelfora.Models;

public sealed class BorrowInput
{
    [Required] public int MemberId { get; set; }
    [Required] public int BookId { get; set; }
    [Range(1, 1000)] public int Quantity { get; set; }
    [DataType(DataType.Date)] public DateTime BorrowDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
    [StringLength(1000)] public string? Notes { get; set; }
}

public sealed class ReturnInput
{
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Required, StringLength(36)] public string SubmissionToken { get; set; } = Guid.NewGuid().ToString("N");
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class DashboardViewModel
{
    public string LibraryName { get; init; } = "Shelfora";
    public int Titles { get; init; }
    public int Copies { get; init; }
    public int Available { get; init; }
    public int Borrowed { get; init; }
    public int Members { get; init; }
    public int ActiveMembers { get; init; }
    public int Overdue { get; init; }
    public List<BorrowTransaction> RecentTransactions { get; init; } = [];
    public List<ReturnRecord> RecentlyReturned { get; init; } = [];
    public List<BorrowTransaction> OverdueTransactions { get; init; } = [];
    public List<Book> LowStock { get; init; } = [];
    public List<PopularBookItem> PopularBooks { get; init; } = [];
    public int[] MonthlyBorrowings { get; init; } = new int[12];
    public int[] MonthlyReturns { get; init; } = new int[12];
}

public sealed class PopularBookItem
{
    public int BookId { get; init; }
    public string Title { get; init; } = "";
    public int BorrowCount { get; init; }
}

public sealed class OverdueItem
{
    public BorrowTransaction Transaction { get; init; } = null!;
    public int DaysOverdue { get; init; }
    public decimal EstimatedFine { get; init; }
}
