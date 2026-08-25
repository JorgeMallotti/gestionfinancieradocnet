using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Dashboard.DTOs;

/// <summary>
/// Amount per category for one transaction type. Percentage is relative to the
/// total of that type in the period (computed in the service layer).
/// </summary>
public sealed record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    TransactionType Type,
    decimal Amount,
    decimal Percentage);
