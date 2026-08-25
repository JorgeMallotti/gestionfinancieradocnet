namespace GestionFinanciera.Application.Features.Dashboard.DTOs;

/// <summary>Aggregated financial overview for a period.</summary>
public sealed record DashboardSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    int TransactionCount,
    DateTimeOffset? From,
    DateTimeOffset? To);
