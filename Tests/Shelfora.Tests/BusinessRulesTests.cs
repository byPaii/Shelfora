using Shelfora.Services;
using Xunit;

namespace Shelfora.Tests;

public sealed class BusinessRulesTests
{
    [Fact]
    public void DueDateUsesTheConfiguredLoanDuration()
    {
        var due = LoanRules.CalculateDueDate(new DateTime(2026, 10, 1, 16, 30, 0), 14);
        Assert.Equal(new DateTime(2026, 10, 15), due);
    }

    [Fact]
    public void DueDateRejectsInvalidLoanDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LoanRules.CalculateDueDate(DateTime.Today, 0));
    }

    [Fact]
    public void FineUsesOverdueDaysOutstandingQuantityAndConfiguredRate()
    {
        var estimate = LoanRules.EstimateFine(new DateTime(2026, 10, 10), new DateTime(2026, 10, 14), 3, 2.50m);
        Assert.Equal(4, estimate.DaysOverdue);
        Assert.Equal(30.00m, estimate.EstimatedFine);
    }

    [Fact]
    public void FineIsZeroBeforeDueDate()
    {
        var estimate = LoanRules.EstimateFine(new DateTime(2026, 10, 14), new DateTime(2026, 10, 13), 1, 5m);
        Assert.Equal((0, 0m), estimate);
    }

    [Theory]
    [InlineData("A book, title", "\"A book, title\"")]
    [InlineData("Said \"hello\"", "\"Said \"\"hello\"\"\"")]
    [InlineData("line one\r\nline two", "\"line one\r\nline two\"")]
    [InlineData("=2+2", "\"'=2+2\"")]
    [InlineData("  @SUM(A1:A2)", "\"'  @SUM(A1:A2)\"")]
    public void CsvFieldsAreQuotedEscapedAndSafeForSpreadsheets(string input, string expected)
    {
        Assert.Equal(expected, CsvExportFormatter.EscapeField(input));
    }
}
