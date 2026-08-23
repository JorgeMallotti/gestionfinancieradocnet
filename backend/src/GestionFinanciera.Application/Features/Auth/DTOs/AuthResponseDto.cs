namespace GestionFinanciera.Application.Features.Auth.DTOs;

/// <summary>
/// Successful authentication payload. The access token is returned in the body;
/// the refresh token is delivered ONLY as an httpOnly cookie (never in JSON).
/// </summary>
public sealed record AuthResponseDto(
    string AccessToken,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    Guid CompanyId,
    string CompanyName);
