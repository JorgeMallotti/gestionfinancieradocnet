using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Accounts.DTOs;

/// <summary>Account response. The bank treasury is also an account (IsTreasury=true).</summary>
public sealed record AccountDto(
    Guid Id,
    Guid OwnerUserId,
    string DisplayName,
    ClientKind Kind,
    AccountStatus Status,
    decimal Balance,
    string Currency,
    bool IsTreasury,
    DateTimeOffset CreatedAt)
{
    public static AccountDto FromEntity(ClientAccount a) => new(
        a.Id,
        a.OwnerUserId,
        a.DisplayName,
        a.Kind,
        a.Status,
        a.Balance,
        a.Currency,
        a.IsTreasury,
        a.CreatedAt);
}
