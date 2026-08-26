using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions.DTOs;

/// <summary>
/// List/filter query for transactions. All filters are optional; the tenant
/// (company) always comes from the JWT, never from the query string.
/// </summary>
public sealed record TransactionQueryDto(
    int Page = 1,
    int PageSize = 20,
    TransactionType? Type = null,
    Guid? CategoryId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
