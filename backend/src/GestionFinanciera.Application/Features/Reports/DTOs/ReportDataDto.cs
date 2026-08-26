using GestionFinanciera.Application.Features.Transactions.DTOs;

namespace GestionFinanciera.Application.Features.Reports.DTOs;

/// <summary>
/// The data model consumed by the PDF/Excel generators and the email service.
/// Built by the report service from the transaction repository (data access)
/// — generators never touch EF Core directly.
/// </summary>
public sealed record ReportDataDto(
    string CompanyName,
    IReadOnlyList<TransactionDto> Transactions,
    decimal TotalIncome,
    decimal TotalExpenses,
    DateTimeOffset? From,
    DateTimeOffset? To)
{
    public decimal Balance => TotalIncome - TotalExpenses;
}
