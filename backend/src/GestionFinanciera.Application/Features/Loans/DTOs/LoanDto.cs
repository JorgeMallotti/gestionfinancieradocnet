using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Loans.DTOs;

/// <summary>Loan response. Outstanding = Amount − RepaidAmount.</summary>
public sealed record LoanDto(
    Guid Id,
    Guid ClientAccountId,
    string ClientDisplayName,
    decimal Amount,
    decimal RepaidAmount,
    decimal OutstandingAmount,
    string Currency,
    string Reason,
    LoanStatus Status,
    Guid? DecidedByUserId,
    DateTimeOffset? DecidedAt,
    string? DecisionNote,
    DateTimeOffset CreatedAt)
{
    public static LoanDto FromEntity(Loan l) => new(
        l.Id,
        l.ClientAccountId,
        l.ClientAccount?.DisplayName ?? string.Empty,
        l.Amount,
        l.RepaidAmount,
        l.Amount - l.RepaidAmount,
        l.Currency,
        l.Reason,
        l.Status,
        l.DecidedByUserId,
        l.DecidedAt,
        l.DecisionNote,
        l.CreatedAt);
}
