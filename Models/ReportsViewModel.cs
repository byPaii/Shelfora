using Shelfora.Models;

namespace Shelfora.Models;

public sealed class ReportsViewModel
{
    public string Type { get; init; } = "inventory";
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public List<Book> Books { get; init; } = [];
    public List<BorrowTransaction> Transactions { get; init; } = [];
    public List<OverdueItem> Overdue { get; init; } = [];
    public List<ReturnRecord> ReturnEvents { get; init; } = [];
    public List<PopularBookItem> PopularBooks { get; init; } = [];
    public List<MemberActivityItem> MemberActivity { get; init; } = [];
}

public sealed class MemberActivityItem
{
    public int MemberId { get; init; }
    public string MemberName { get; init; } = "";
    public int Transactions { get; init; }
    public int CopiesBorrowed { get; init; }
    public int Outstanding { get; init; }
}
