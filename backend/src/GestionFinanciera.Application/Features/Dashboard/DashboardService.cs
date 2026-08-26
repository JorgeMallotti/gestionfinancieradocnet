using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Dashboard;

/// <summary>
/// Dashboard aggregations. Percentages and balances are computed here (business
/// logic); raw sums come from the transaction repository (data access).
/// </summary>
public sealed class DashboardService(ITransactionRepository repository) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<DashboardSummaryDto>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        decimal income = await repository.SumByTypeAsync(companyId, TransactionType.Income, from, to, ct);
        decimal expenses = await repository.SumByTypeAsync(companyId, TransactionType.Expense, from, to, ct);
        int count = await repository.CountAsync(companyId, from, to, ct);

        var summary = new DashboardSummaryDto(income, expenses, income - expenses, count, from, to);
        return Result<DashboardSummaryDto>.Success(summary);
    }

    public async Task<Result<IReadOnlyList<CategoryBreakdownDto>>> GetBreakdownAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<IReadOnlyList<CategoryBreakdownDto>>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var rows = await repository.GetCategoryBreakdownAsync(companyId, type, from, to, ct);
        decimal total = rows.Sum(r => r.Amount);

        var result = rows
            .Select(r => r with
            {
                Percentage = total > 0 ? Math.Round(r.Amount / total * 100, 2) : 0,
            })
            .ToList();

        return Result<IReadOnlyList<CategoryBreakdownDto>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<MonthlyPointDto>>> GetMonthlyAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<IReadOnlyList<MonthlyPointDto>>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var points = await repository.GetMonthlySeriesAsync(companyId, from, to, ct);

        var result = points
            .Select(p => p with { Balance = p.Income - p.Expenses })
            .ToList();

        return Result<IReadOnlyList<MonthlyPointDto>>.Success(result);
    }

    private static bool IsInvalidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from.HasValue && to.HasValue && from.Value > to.Value;
}
