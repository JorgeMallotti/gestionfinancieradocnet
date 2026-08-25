namespace GestionFinanciera.Api.Extensions;

/// <summary>
/// Best-effort client IP for audit logging. Uses the direct connection address;
/// behind a reverse proxy you would add X-Forwarded-For handling (ForwardedHeaders).
/// </summary>
public static class HttpContextExtensions
{
    public static string? GetClientIpAddress(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();
}
