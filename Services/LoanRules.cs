namespace Shelfora.Services;

public static class LoanRules
{
    public static DateTime CalculateDueDate(DateTime borrowDate, int loanDays)
    {
        if (loanDays < 1) throw new ArgumentOutOfRangeException(nameof(loanDays));
        return borrowDate.Date.AddDays(loanDays);
    }

    public static (int DaysOverdue, decimal EstimatedFine) EstimateFine(DateTime dueDate, DateTime today, int outstandingQuantity, decimal dailyRate)
    {
        if (outstandingQuantity < 0) throw new ArgumentOutOfRangeException(nameof(outstandingQuantity));
        if (dailyRate < 0) throw new ArgumentOutOfRangeException(nameof(dailyRate));
        var days = Math.Max(0, (today.Date - dueDate.Date).Days);
        return (days, days * outstandingQuantity * dailyRate);
    }
}
