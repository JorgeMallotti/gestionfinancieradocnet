using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Dashboard;

/// <summary>
/// Dashboard aggregations for the caller's own account: current balance plus
/// incoming/outgoing totals and a monthly series. Business logic lives here;
/// raw data comes from the account and movement repositories.
/// </summary>
public sealed class DashboardService(
    IAccountRepository accounts,
    IMovementRepository movements) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetMySummaryAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<DashboardSummaryDto>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var account = await GetAccountAsync(companyId, userId, ct);
        if (account is null)
            return Result<DashboardSummaryDto>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        var rows = await movements.GetByAccountInRangeAsync(companyId, account.Id, from, to, ct);

        decimal incoming = rows.Where(m => m.ToAccountId == account.Id).Sum(m => m.Amount);
        decimal outgoing = rows.Where(m => m.FromAccountId == account.Id).Sum(m => m.Amount);

        var summary = new DashboardSummaryDto(account.Balance, incoming, outgoing, rows.Count, from, to);
        return Result<DashboardSummaryDto>.Success(summary);
    }

    public async Task<Result<IReadOnlyList<MonthlyPointDto>>> GetMyMonthlyAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<IReadOnlyList<MonthlyPointDto>>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var account = await GetAccountAsync(companyId, userId, ct);
        if (account is null)
            return Result<IReadOnlyList<MonthlyPointDto>>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        var rows = await movements.GetByAccountInRangeAsync(companyId, account.Id, from, to, ct);

        var points = rows
            .GroupBy(m => new { m.OccurredAt.Year, m.OccurredAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                decimal incoming = g.Where(m => m.ToAccountId == account.Id).Sum(m => m.Amount);
                decimal outgoing = g.Where(m => m.FromAccountId == account.Id).Sum(m => m.Amount);
                return new MonthlyPointDto(g.Key.Year, g.Key.Month, incoming, outgoing, incoming - outgoing);
            })
            .ToList();

        return Result<IReadOnlyList<MonthlyPointDto>>.Success(points);
    }

    private async Task<ClientAccount?> GetAccountAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        await accounts.GetByOwnerUserIdAsync(companyId, userId, ct);

    private static bool IsInvalidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from.HasValue && to.HasValue && from.Value > to.Value;
}
