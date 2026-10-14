using Shelfora.Models;

namespace Shelfora.Services;

public interface ILibraryService
{
    Task<DashboardViewModel> DashboardAsync(CancellationToken cancellationToken = default);
    Task<BorrowTransaction> BorrowAsync(BorrowInput input, CancellationToken cancellationToken = default);
    Task<ReturnRecord> ReturnAsync(int transactionId, ReturnInput input, CancellationToken cancellationToken = default);
    Task<List<OverdueItem>> OverdueAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
