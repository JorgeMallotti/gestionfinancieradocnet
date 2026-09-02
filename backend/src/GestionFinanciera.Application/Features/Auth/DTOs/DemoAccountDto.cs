namespace GestionFinanciera.Application.Features.Auth.DTOs;

/// <summary>
/// Public metadata of a demo account (quick access for the MVP demo).
/// Deliberately contains NO credentials — passwords never leave the backend.
/// </summary>
public sealed record DemoAccountDto(
    string Key,
    string Role,
    string CompanyName,
    string Description);
