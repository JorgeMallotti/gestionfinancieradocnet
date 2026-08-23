namespace GestionFinanciera.Application.Features.Auth.DTOs;

/// <summary>Signup payload — creates a new company and its first Admin user.</summary>
public sealed record RegisterDto(
    string CompanyName,
    string FullName,
    string Email,
    string Password);
