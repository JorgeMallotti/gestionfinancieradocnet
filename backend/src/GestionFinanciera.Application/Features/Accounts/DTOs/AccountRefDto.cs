namespace GestionFinanciera.Application.Features.Accounts.DTOs;

/// <summary>Lightweight account reference used when picking a transfer counterparty.</summary>
public sealed record AccountRefDto(
    Guid Id,
    string DisplayName,
    bool IsTreasury);
