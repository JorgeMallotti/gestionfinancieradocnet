namespace GestionFinanciera.Application.Features.Auth.DTOs;

/// <summary>Login payload.</summary>
public sealed record LoginDto(
    string Email,
    string Password);
