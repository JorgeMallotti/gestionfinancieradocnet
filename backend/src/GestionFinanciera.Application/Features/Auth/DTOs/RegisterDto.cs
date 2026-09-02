using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Auth.DTOs;

/// <summary>
/// Signup payload — opens a CLIENT account (person or company) under the single
/// seeded bank. The account starts with status <see cref="AccountStatus.Pending"/>
/// until the bank Admin approves it. No company/bank is ever created here.
/// </summary>
public sealed record RegisterDto(
    string DisplayName,
    ClientKind Kind,
    string Email,
    string Password);
